module AuthTests

open NUnit.Framework
open Foq
open FsUnit
open SimpleWAWS.Authentication
open System
open System.IO
open System.Web
open System.Collections.Specialized

let getMockedHttpContext () =
    let request = Mock<HttpRequestBase>()
                    .Setup(fun r -> <@ r.Url @>).Returns(new Uri("http://example.com"))
                    .Setup(fun r -> <@ r.UserAgent @>).Returns("Mozilla/")
                    .Setup(fun r -> <@ r.Headers @>).Returns(new NameValueCollection())
                    .Setup(fun r -> <@ r.Cookies @>).Returns(new HttpCookieCollection())
                    .Setup(fun r -> <@ r.UrlReferrer @>).Returns(new Uri("http://msdn.com"))
                    .Setup(fun r -> <@ r.QueryString @>).Returns(new NameValueCollection())
                    .Create()

    let response = Mock<HttpResponseBase>()
                    .Setup(fun r -> <@ r.Cookies @>).Returns(new HttpCookieCollection())
                    .Create()

    let user = Mock<TryWebsitesPrincipal>()
                    .Create()

    let identity = Mock<TryWebsitesIdentity>()
                    .Create()

    let context = Mock<HttpContextBase>()
                    .Setup(fun c -> <@ c.Request @>).Returns(request)
                    .Setup(fun c -> <@ c.Response @>).Returns(response)
                    .Setup(fun c -> <@ c.User @>).Returns(user)
                    .Create()
    context


[<TestCase("provider", "google", "google")>]
[<TestCase("Provider", "Facebook", "Facebook")>]
[<TestCase("something", "somethingelse", AuthConstants.DefaultAuthProvider)>]
[<TestCase("", "", AuthConstants.DefaultAuthProvider)>]
let ``Select correct provider from queryString`` (queryStringName, queryStringValue, provider) =
    let context = getMockedHttpContext()
    context.Request.QueryString.Add(queryStringName, queryStringValue)
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


[<Test>]
let ``Ensure multiple calls to Authenticate request result in only 1 anonymous user created`` ()=
    let getUser (context: HttpContextBase) =
        SimpleWAWS.Extensions.Decrypt(context.Response.Cookies.[AuthConstants.AnonymousUser].Value |> Uri.UnescapeDataString, AuthConstants.EncryptionReason)
    let initialContext = getMockedHttpContext()
    initialContext |> SecurityManager.HandleAnonymousUser |> ignore
    let initialUser = getUser initialContext
    for i in 1 .. 10 do
        let testContext = getMockedHttpContext()
        for h in initialContext.Request.Headers.AllKeys do 
            testContext.Request.Headers.Add (h, initialContext.Request.Headers.[h])
        testContext |> SecurityManager.HandleAnonymousUser |> ignore
        (getUser testContext) |> should equal initialUser