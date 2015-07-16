module TryAppServiceAnalytics.Program

open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.RequestErrors
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Types
open System.Net
open Suave.Web
open System

type Foo = { foo: string; bar: string }

let browse =
    request (fun r ->
        match r.queryParam "genre" with
        | Choice1Of2 genre -> OK (sprintf "Genre: %s" genre)
        | Choice2Of2 msg -> BAD_REQUEST msg)

let getStartAndEndDate startTime endTime =
    let startDate = match startTime with
                    | Some time -> DateTime.Parse time
                    | None -> DateTime (2014, 7, 31, 16, 0, 0, System.DateTimeKind.Utc)
    let endDate = match endTime with
                  | Some time -> DateTime.Parse time
                  | None -> DateTime.Now
    (startDate, endDate)

let getTimeRange (r: HttpRequest) =
    let startTime = match r.queryParam "startTime" with
                    | Choice1Of2 time -> DateTime.Parse time
                    | Choice2Of2 _ -> DateTime.UtcNow.AddDays (-7.0)
    let endTime = match r.queryParam "endTime" with
                  | Choice1Of2 time -> DateTime.Parse time
                  | Choice2Of2 _ -> DateTime.UtcNow
    (startTime, endTime)

let getAggregate (r: HttpRequest) =
    match r.queryParam "aggregate" with
    | Choice1Of2 aggregate -> match aggregate with
                              | "Week" -> Types.Week
                              | "Day" -> Types.Day
                              | "Month" -> Types.Month
                              | _ -> Types.Week
    | Choice2Of2 _ -> Types.Week

let getExperiments (r: HttpRequest) =
    let getExperiment a = match r.queryParam a with
                          | Choice1Of2 e -> e
                          | Choice2Of2 _ -> "Production"
    let a = getExperiment "a"
    let b = getExperiment "b"
    (a, b)

let getReferrer (r: HttpRequest) =
    match r.queryParam "referrer" with
    | Choice1Of2 rf -> rf
    | Choice2Of2 _ -> "-"

let toJson a =
    let settings = new JsonSerializerSettings();
    let dateTimeConverter = new IsoDateTimeConverter ()
    dateTimeConverter.DateTimeFormat <- "yyyy-MM-dd"
    settings.Converters.Add dateTimeConverter
    JsonConvert.SerializeObject (a, settings)

let okJson a = OK (toJson a) >>= Writers.setMimeType "application/json"

let getData f =
    request (fun r ->
        let timeRange = getTimeRange r
        let aggregate = getAggregate r
        okJson (f timeRange aggregate))

let getExperimentResults f =
    request (fun r ->
        let timeRange = getTimeRange r
        let experiments = getExperiments r
        okJson (f timeRange experiments))

let getSourceVariationResult =
    request (fun r ->
        let timerange = getTimeRange r
        let referrer = getReferrer r
        Data.sourceVariationResults timerange referrer |> okJson)

let noCache = Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate" >>= Writers.setHeader "Pragma" "no-cache" >>= Writers.setHeader "Expires" "0"

let analyticsWebPart =
    choose [
        pathRegex "(.*)\.(css|js|html|woff|ttf|svg|jpg)" >>= Files.browseHome
        path Path.Analytics.kpi >>= getData Data.appServiceKpis
        path Path.Analytics.appCreates >>= getData Data.appServiceAppCreates
        path Path.Analytics.appCreatesFreeTrialClicks >>= getData Data.appServiceAppCreatesFreeTrialClicks
        path Path.Analytics.accountTypes >>= getData Data.appServiceAccounts
        path Path.Analytics.accountsTypesFreeTrialClicks >>= getData Data.appServiceAccountsFreeTrialClicks
        path Path.Analytics.referrersCatagories >>= getData Data.referrerCatagories
        path Path.Analytics.templates >>= getData Data.templates
        path Path.Analytics.experiments >>= getData Data.experiments
        path Path.Analytics.experimentResults >>= getExperimentResults Data.experimentResults
        path Path.Analytics.sourceVariations >>= okJson Data.sourceVariations
        path Path.Analytics.sourceVariationResults >>= getSourceVariationResult
        path Path.Info.dbConnection >>= okJson (Environment.GetEnvironmentVariable "TryAppServiceReader")
        pathRegex "(.*)" >>= Files.browseFileHome "index.html" ]
    >>= noCache

let mimeTypes =
    defaultMimeTypesMap
    >=> (function
         | ".woff" -> mkMimeType "application/font-woff" true
         | ".ttf"  -> mkMimeType "application/octet-stream" true
         | ".svg"  -> mkMimeType "image/svg+xml" true
         | _       -> None)

let port =
    match Environment.GetCommandLineArgs () with
    | args when args.Length = 2 -> args.[1]
    | _ -> "8083"
    |> Sockets.Port.Parse 

let webConfig = { defaultConfig with mimeTypesMap = mimeTypes; bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ] }

startWebServer webConfig analyticsWebPart