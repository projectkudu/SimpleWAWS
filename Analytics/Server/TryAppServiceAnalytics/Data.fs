module TryAppServiceAnalytics.Data

open Microsoft.FSharp.Data.TypeProviders
open Microsoft.FSharp.Linq.NullableOperators
open Types
open System
open System.Linq
open System.Data.Objects.SqlClient
open Microsoft.FSharp.Linq.RuntimeHelpers

type private EntityConnection = SqlEntityConnection<ConnectionStringName="TryAppService", ConfigFile="compilation.config", Pluralize = true>

let intervals (timeRange: TimeRange) aggregate = 
    let startTime, endTime = timeRange
    let difference = endTime - startTime
    let col = match aggregate with
              | Day -> [ for i in 0..(difference.TotalDays |> int) + 1 -> i] |> List.map (fun n -> startTime.AddDays (n |> float))
              | Week -> [ for i in 0..(difference.TotalDays |> int) / 7 + 1  -> i] |> List.map (fun n -> startTime.AddDays (n * 7 |> float))
              | Month  -> [ for i in 0..(difference.TotalDays |> int) / 30 + 1 -> i] |> List.map (fun n -> startTime.AddMonths (n))
    Seq.zip col (List.tail col)

type TimeWrappedResult<'a> = { startTime: DateTime; endTime: DateTime; value: 'a }
let wrap (startTime, endTime) a = { startTime = startTime; endTime = endTime; value = a }
let private getContext () = EntityConnection.GetDataContext (System.Environment.GetEnvironmentVariable ("TryAppService"))

let private executeQueryOverTimeRangeAsync fq (timeRange: TimeRange) aggregate =
    intervals timeRange aggregate
    |> Seq.map (fun timeRange -> async {
                    use context = getContext ()
                    return fq timeRange context
                        |> wrap timeRange })
    |> Async.Parallel
    |> Async.RunSynchronously

type Kpi = { Visits: int; Logins: int; FreeTrialClicks: int }
let appServiceKpis (timeRange: TimeRange) aggregate =
    let execute f = executeQueryOverTimeRangeAsync f timeRange aggregate
    let visits =
         execute (fun (startTime, endTime) context -> query {
                      for u in context.UserAssignedExperiments do
                      where (u.DateTime >= startTime && u.DateTime < endTime)
                      groupBy u.UserName
                      count
                  })

    let logins =
        execute (fun (startTime, endTime) context -> query {
                     for u in context.UserActivities do
                     where (u.DateTime >= startTime && u.DateTime < endTime)
                     groupBy u.UserName
                     count
                 })

    let freeTrialClicks =
        execute (fun (startTime, endTime) context -> query {
                    for u in context.UIEvents do
                    where (u.DateTime >= startTime && u.DateTime < endTime && u.EventName = "FREE_TRIAL_CLICK")
                    groupBy u.UserName
                    count
                })

    //TODO: maybe change this?
    Seq.zip3 visits logins freeTrialClicks
    |> Seq.map (fun (v, l, f) -> wrap (v.startTime, v.endTime) { Visits = v.value; Logins = l.value; FreeTrialClicks = f.value })
    |> Array.ofSeq

// users creating webApps, MobileApps, or both
type AppType = 
    | Web
    | Mobile
    | Mix
    | Unknown
type AppsCreates = { WebApps: int; MobileApps: int; Mix: int; Unknown: int }

let private getUsersToAppTypeQuery startTime endTime (context: EntityConnection.ServiceTypes.SimpleDataContextTypes.EntityContainer) =
    query {
        for userName, usedMobile, usedWeb in (query {
                for u in context.UserActivities do
                where (u.DateTime >= startTime && u.DateTime < endTime)
                groupBy u.UserName into g
                select ( g.Key
                        , g.Max (fun r -> if r.AppService = "Mobile" then 1 else 0)
                        , g.Max (fun r -> if r.AppService = "Web" then 1 else 0)) }) do
        select (if usedMobile + usedWeb = 2 then Mix
                elif usedMobile = 1 then Mobile
                elif usedWeb = 1 then Web
                else Unknown
              , userName) }

let appServiceAppCreates =
    executeQueryOverTimeRangeAsync (fun (startTime, endTime) context ->
        let appTypeQuery = getUsersToAppTypeQuery startTime endTime context

        query {
            for appType, _ in appTypeQuery do
            groupBy appType into g
            select (g.Key , g.Count()) }
       |> Seq.fold (fun record (appType, count) ->
                       match appType with
                       | Web -> { record with WebApps = count }
                       | Mobile -> { record with MobileApps = count }
                       | Mix -> { record with Mix = count }
                       | Unknown -> { record with Unknown = count }
                   ) { WebApps = 0; MobileApps = 0; Mix = 0; Unknown = 0 })


let appServiceAppCreatesFreeTrialClicks =
    executeQueryOverTimeRangeAsync (fun (startTime, endTime) context -> 
        let appTypeQuery = getUsersToAppTypeQuery startTime endTime context 
        let usersClickingOnFreeTrial = 
            query {
                for event in context.UIEvents do
                where (event.DateTime >= startTime && event.DateTime < endTime && event.EventName = "FREE_TRIAL_CLICK")
                groupBy event.UserName into g
                select g.Key } |> Seq.toList
        query {
            for appType, userName in appTypeQuery do
            where (usersClickingOnFreeTrial.Contains (userName))
            groupBy appType into g
            select (g.Key, g.Count ()) }
       // Consider a custom JSON serializer for tuples instead
       |> Seq.fold (fun record (appType, count) ->
                       match appType with
                       | Web -> { record with WebApps = count }
                       | Mobile -> { record with MobileApps = count }
                       | Mix -> { record with Mix = count }
                       | Unknown -> { record with Unknown = count }
                   ) { WebApps = 0; MobileApps = 0; Mix = 0; Unknown = 0 })

type AccountTypes = { MSA: int; OrgId: int; AAD: int; Google: int; Facebook: int; Anonymous: int }
let toAccountTypes record (account, count) =
   match account with
   | "MSA" -> { record with MSA = count }
   | "OrgId" -> { record with OrgId = count }
   | "AAD" -> { record with AAD = count }
   | "Google" -> { record with Google = count }
   | "Facebook" -> { record with Facebook = count }
   | "Anonymous" -> { record with Anonymous = count }
   | _ -> record 
let accountType = <@ fun (name : string) ->
        if name.StartsWith "Google" then "Google"
        elif name.StartsWith "Facebook" then "Facebook"
        elif name.StartsWith "MSA" then "MSA"
        elif name.StartsWith "OrgId" then "OrgId"
        elif name.StartsWith "AAD" then "AAD"
        else "Anonymous" @>
let appServiceAccounts =
    executeQueryOverTimeRangeAsync (fun (startTime, endTime) context ->
        query {
            for u in (query {
                        for e in context.UserActivities do
                        where (e.DateTime >= startTime && e.DateTime < endTime)
                        groupBy e.UserName into g
                        select ((%accountType) g.Key) }) do
            groupBy u into g
            select (g.Key, g.Count ()) }
        |> Seq.fold toAccountTypes { MSA = 0; OrgId = 0; AAD = 0; Google = 0; Facebook = 0; Anonymous = 0 })

let appServiceAccountsFreeTrialClicks =
    executeQueryOverTimeRangeAsync (fun (startTime, endTime) context -> 
        query {
            for u in (query {
                        for e in context.UIEvents do
                        where (e.DateTime >= startTime && e.DateTime < endTime && e.EventName = "FREE_TRIAL_CLICK")
                        groupBy e.UserName into g
                        select ((%accountType) g.Key) }) do
            groupBy u into g
            select (g.Key, g.Count ()) }
        // Consider a custom JSON serializer for tuples instead
        |> Seq.fold toAccountTypes { MSA = 0; OrgId = 0; AAD = 0; Google = 0; Facebook = 0; Anonymous = 0 })

let like = <@ fun (key: string) (pattern: string) -> 
        SqlFunctions.PatIndex (pattern, key) ?> 0 @>
let categorizeReferrs = <@ fun key ->
    if (%like) key "htt%://azure.microsoft.com/%/services/app-service/%" then "AppService"
    elif (%like) key "http%://azure.microsoft.com/%/documentation/articles/search-tryappservice%" then "AzureSearch"
    elif (%like) key "htt%://azure.microsoft.com/blog/2015/07/22/azure-search-new-regions-samples-and-datasets%" then "AzureSearch"
    elif (%like) key "htt%://azure.microsoft.com/%/documentation/%" then "AzureDocumentation"
    elif (%like) key "htt%://azure.microsoft.com/%/develop/net/aspnet/%" then "AspNetDevelop"
    elif (%like) key "htt%://%google.com/%" then "Search"
    elif (%like) key "htt%://%bing.com/%" then "Search"
    elif (%like) key "htt%://%yahoo.com/%" then "Search"
    elif (%like) key "htt%://ad.atdmt.com/%" then "Ads"
    elif (%like) key "htt%://%doubleclick.net/%" then "Ads"
    elif (%like) key "htt%://%chango.com/%" then "Ads"
    elif (%like) key "htt%://%media6degrees.com/%" then "Ads"
    elif key = "-" then "Empty"
    else "Uncategorized" @>
type ReferrerCatagories = { AppService: int; AzureDocumentation: int; AspNetDevelop: int; AzureSearch: int; Search: int; Ads: int; Uncaterorized: int; Empty: int }
let foldToReferrerCatagories =
    Seq.fold (fun record (catagory, count) ->
                        match catagory with
                        | "AppService" -> { record with AppService = count }
                        | "AzureDocumentation" -> { record with AzureDocumentation = count }
                        | "AspNetDevelop" -> { record with AspNetDevelop = count }
                        | "Search" -> { record with Search = count }
                        | "Ads" -> { record with Ads = count }
                        | "Uncategorized" -> { record with Uncaterorized = count }
                        | "Empty" -> { record with Empty = count }
                        | "AzureSearch" -> { record with AzureSearch = count }
                        | _ -> record) { AppService = 0; AzureDocumentation = 0; AspNetDevelop = 0; Search = 0; Ads = 0; Uncaterorized = 0; Empty = 0; AzureSearch = 0 }
type ReferrersAggregation = { Totals: ReferrerCatagories; Created: ReferrerCatagories; FreeTrial: ReferrerCatagories }
let referrerCatagories (startTime, endTime) _ =
    use context = getContext ()
    let totalReferers = query {
        for u in (query {
            for u in context.UserAssignedExperiments do
            where (u.DateTime >= startTime && u.DateTime < endTime)
            groupBy u.Referer into g
            select ((%categorizeReferrs) g.Key, g.Count()) }) do 
        groupBy (fst u) into g
        select (g.Key, g.Sum (fun s -> snd s)) } |> foldToReferrerCatagories

    let anonymousUsersWhoCreates = query {
        for u in context.UserActivities do
        where (u.DateTime >= startTime && u.DateTime < endTime)
        join s in context.UserLoggedIns on (u.UserName = s.LoggedInUserName)
        select s.AnonymousUserName }

    let creates = query {
        for u in (query {
            for u in context.UserAssignedExperiments do
            where (u.DateTime >= startTime && u.DateTime < endTime && anonymousUsersWhoCreates.Contains u.UserName)
            select ((%categorizeReferrs) u.Referer) }) do
        groupBy u into g
        select (g.Key, g.Count ()) } |> foldToReferrerCatagories

    let anonymousUsersWhoClickedFree = query {
        for u in context.UIEvents do
        where (u.DateTime >= startTime && u.DateTime < endTime && u.EventName = "FREE_TRIAL_CLICK")
        join s in context.UserLoggedIns on (u.UserName = s.LoggedInUserName)
        select s.AnonymousUserName }

    let initiations = query {
        for u in (query {
            for u in context.UserAssignedExperiments do
            where (u.DateTime >= startTime && u.DateTime < endTime && anonymousUsersWhoClickedFree.Contains u.UserName)
            select ((%categorizeReferrs) u.Referer) }) do
        groupBy u into g
        select (g.Key, g.Count ()) } |> foldToReferrerCatagories

    { Totals = totalReferers; Created = creates; FreeTrial = initiations }

type templates = { Name: string; Language: string; Count: int }
let templates (startTime, endTime) _ =
    use context = getContext ()
    query {
        for u in context.UserActivities do
        where (u.DateTime >= startTime && u.DateTime < endTime && u.AppService = "Web")
        let key = AnonymousObject<_,_>(u.TemplateName, u.TemplateLanguage)
        groupValBy u key into g
        select { Name = g.Key.Item1; Language = g.Key.Item2; Count = g.Count() } }
    |> Seq.toArray

let experiments (startTime, endTime) _ =
    use context = getContext ()
    query {
        for u in context.UserAssignedExperiments do
        where (u.DateTime >= startTime && u.DateTime < endTime && u.Experiment <> "")
        groupBy u.Experiment into g
        select g.Key }
    |> Seq.toArray

type ExperimentResult = { Name: string; TotalUsers: int; LoggedInUsers: int; FreeTrialUsers: int }
let experimentResults (startTime, endTime) (a, b) =
    use context = getContext ()
    let total = query {
        for u in context.UserAssignedExperiments do
        where (u.DateTime >= startTime && u.DateTime < endTime && (u.Experiment = a || u.Experiment = b))
        groupBy u.Experiment into g
        select (g.Key, g.Count ()) } |> Seq.toArray

    let logins = query {
        for u in (query {
            for u in context.UserAssignedExperiments do
            where (u.DateTime >= startTime && u.DateTime < endTime && (u.Experiment = a || u.Experiment = b))
            join s in context.UserLoggedIns on (u.UserName = s.AnonymousUserName)
            select u.Experiment }) do
        groupBy u into g
        select (g.Key, g.Count ()) } |> Seq.toArray

    let freeTrialClicks = query {
        for u in (query { 
            for u in context.UIEvents do
            where (u.DateTime >= startTime && u.DateTime < endTime && u.EventName = "FREE_TRIAL_CLICK" && (u.Experiment = a || u.Experiment = b))
            select (u.UserName, u.Experiment)
            distinct }) do
        groupBy (snd u) into g
        select (g.Key, g.Count ()) } |> Seq.toArray

    let getValue a c =
        match c |> Array.tryFind (fun (n, _) -> n = a) with
        | Some (_, count) -> count
        | None -> 0
    let getA = getValue a
    let getB = getValue b

    [ { Name = a; TotalUsers = getA total; LoggedInUsers = getA logins; FreeTrialUsers = getA freeTrialClicks }
      { Name = b; TotalUsers = getB total; LoggedInUsers = getB logins; FreeTrialUsers = getB freeTrialClicks } ]

let sourceVariations =
    [| "htt%://azure.microsoft.com/%/services/app-service/%"
       "htt%://azure.microsoft.com/%/develop/net/aspnet/%" |]

//type sourceVariationResult = { TotalUsers: Map<string, int>; LoggedInUsers: Map<string, int>; FreeTrialClicks: Map<string, int> }
let sourceVariationResults (startTime, endTime) referrer =
    use context = getContext ()
    let total = query {
        for u in context.UserAssignedExperiments do
        where (u.DateTime >= startTime && u.DateTime < endTime && (%like) u.Referer referrer)
        groupBy u.SourceVariation into g
        select (g.Key, g.Count ()) } |> Map.ofSeq

    let logins = query {
        for u in (query {
            for u in context.UserAssignedExperiments do
            where (u.DateTime >= startTime && u.DateTime < endTime && (%like) u.Referer referrer)
            join s in context.UserLoggedIns on (u.UserName = s.AnonymousUserName)
            select (u.SourceVariation, u.UserName)
            distinct }) do
        groupBy (fst u) into g
        select (g.Key, g.Count ()) } |> Map.ofSeq

    let freeTrialClicks = query {
        for u in (query {
            for u in context.UserAssignedExperiments do
            where (u.DateTime >= startTime && u.DateTime < endTime && (%like) u.Referer referrer)
            join s in context.UIEvents on (u.UserName = s.AnonymousUserName)
            where (s.EventName = "FREE_TRIAL_CLICK")
            select (u.UserName, u.SourceVariation)
            distinct }) do
        groupBy (snd u) into g
        select (g.Key, g.Count ()) } |> Map.ofSeq
    
    let findM m k =
        match Map.tryFind k m with
        | Some v -> v
        | None -> 0

    total
    |> Map.toSeq
    |> Seq.map (fun (k, _) -> k)
    |> Seq.map (fun k -> { Name = k; TotalUsers = findM total k; LoggedInUsers = findM logins k; FreeTrialUsers = findM freeTrialClicks k  } )

type UserFeedback = { UserName: string; Comment: string; ContactMe: bool; DateTime: string}
let userFeedback (startTime, endTime) =
    use context = getContext ()
    query {
        for u in context.UserFeedbacks do
        where (u.DateTime >= startTime && u.DateTime < endTime)
        select (u.UserName, u.Comment, u.ContactMe, u.DateTime) }
    // This is to work around JSON serializer serilizing DateTime to 'YYYY-MM-DD'
    |> Seq.map (fun (userName, comment, contactMe, dateTime) -> { UserName = userName; Comment = comment; ContactMe = contactMe; DateTime = dateTime.ToString() })
    |> Seq.toArray