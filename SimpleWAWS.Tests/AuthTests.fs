module AuthTests

open NUnit.Framework
open FsUnit
open SimpleWAWS.Authentication
open System
open System.IO
open System.Web

[<TestCase("provider=google", "google")>]
[<TestCase("Provider=Facebook", "Facebook")>]
[<TestCase("something=somethingelse", AuthConstants.DefaultAuthProvider)>]
[<TestCase("", AuthConstants.DefaultAuthProvider)>]
let ``Select correct provider from queryString`` (queryString, provider) =
    let context = new HttpContext(new HttpRequest("", "http://example.com", queryString), new HttpResponse(new StringWriter()));
    SecurityManager.SelectedProvider(context) |> should equal provider

[<TestCase("live.com:1111102", "MSA")>]
[<TestCase("11111234", "OrgId")>]
[<TestCase("", "OrgId")>]
let ``GetIssuerName for AAD Accounts`` (altSecId, issuer) =
    let authProvider = new AADProvider()
    authProvider.GetIssuerName altSecId |> should equal issuer


[<TestCase("user@example", "live.com:12244556464", "MSA")>]
let ``Check session cookie format`` (email, puid, issuer) =
    let principal = new TryWebsitesPrincipal(new TryWebsitesIdentity(email, puid, issuer))
    let provider = new FacebookAuthProvider()
    let cookie = provider.CreateSessionCookie principal
    cookie |> should not' (be Null)

    let values = SimpleWAWS.Extensions.Decrypt((cookie.Value |> Uri.UnescapeDataString), AuthConstants.EncryptionReason).Split(';')
    values |> Seq.length |> should equal 4
    values
        |> Seq.zip [|email; puid; issuer|]
        |> Seq.iter (fun (v, e) -> v |> should equal e)

    let couldParse, date = values |> Seq.last |> DateTime.TryParse
    couldParse |> should be True

[<TestCase("user@example.com", "puid", "Google")>]
let ``Check identity name`` (email, puid, issuer) =
    let identity = new TryWebsitesIdentity(email, puid, issuer)
    identity.Name |> should equal (String.Join("#", issuer, email))

