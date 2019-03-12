<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="SimpleWAWS.CaptchaAuth._Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>ReCaptcha Auth for Try App Service</title>
    <style>
        #footer {
            position: absolute;
            bottom: 0;
            height: 40px;
            margin-top: 40px;
        }

        body {
            font-family: "Segoe UI","Helvetica Neue","Lucida Grande","Roboto";
        }

        .centerBox {
            width: 500px;
            height: 400px;
            top: 50%;
            left: 50%;
            position: absolute;
            margin-top: -200px;
            margin-left: -250px;
            border-style: solid;
            border-width: 1px;
            border-color: silver;
            text-align: center;
        }

        .loginHeader {
            color: #404040;
            font-size: 1.3rem;
            font-weight: 600;
            font-family: "Segoe UI","Helvetica Neue","Lucida Grande","Roboto";
            padding-bottom: 20px;
            padding-top: 50px;
        }

        .successMessage {
            padding-top: 20px;
        }

        .recaptchacenter {
            text-align: -webkit-center;
        }
    </style>
</head>
<body>

        <div class="centerBox">
            <div class="loginHeader">Solve a puzzle to prove you are human</div>
            <div id="ReCaptchaContainer" class="recaptchacenter">
            </div>
            <div id="lblMessage" class="successMessage" />
        </div>
    <script src="https://www.google.com/recaptcha/api.js?onload=renderRecaptcha&render=explicit" async defer></script>

    <script type="text/javascript">
        var your_site_key = '<%= Environment.GetEnvironmentVariable("ReCaptchaSiteKey")%>';
        var renderRecaptcha = function () {
            grecaptcha.render('ReCaptchaContainer', {
                'sitekey': your_site_key,
                'callback': reCaptchaCallback,
                theme: 'light', //light or dark
                type: 'image',// image or audio
                size: 'normal'//normal or compact
            });
        };

        var reCaptchaCallback = function (response) {
            if (response !== '') {
                document.getElementById('lblMessage').innerHTML = "Success. Redirecting...";
                setTimeout(function () {
                    window.location.href = "http://tryappservice.azure.com/?code=" + response + "&state=" + urlParams["state"];
                }, 1000);

                //theForm.submit();
            }
            else { //should never come here
                document.getElementById('lblMessage').innerHTML = "Please try again.";
            }

        };

        var urlParams;
        (window.onpopstate = function () {
            var match,
                pl = /\+/g,  // Regex for replacing addition symbol with a space
                search = /([^&=]+)=?([^&]*)/g,
                decode = function (s) { return decodeURIComponent(s.replace(pl, " ")); },
                query = window.location.search.substring(1);

            urlParams = {};
            while (match = search.exec(query))
                urlParams[decode(match[1])] = decode(match[2]);
        })();

    </script>

    </div>

        <div style="color: #fff;
    padding-top: 10px;
    font-size: 12px;
    text-decoration-style: none!important;
    background-color: #343a40; position:fixed; bottom:0; width:100vw; text-align:center">
            <div>
                <div style="color: #fff; padding-top: 10px; padding-left: 20px; font-size: 12px; text-decoration-style: none!important; background-color: #343a40">Microsoft © 2019 | <a href="https://azure.microsoft.com/overview/sales-number/" target="_blank" style="color: inherit; text-decoration: inherit">Contact Us</a> | <a href="https://feedback.azure.com/forums/34192--general-feedback" target="_blank" style="color: inherit; text-decoration: inherit">Feedback</a> | <a href="https://www.microsoft.com/en-us/legal/intellectualproperty/Trademarks/" target="_blank" style="color: inherit; text-decoration: inherit">Trademarks</a> | <a href="https://go.microsoft.com/fwlink/?LinkId=248681&amp;clcid=0x409" target="_blank" style="color: inherit; text-decoration: inherit">Privacy &amp; Cookies</a></div>
            </div>
        </div>

</body>

</html>
