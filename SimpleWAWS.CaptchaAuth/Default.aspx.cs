//Reference: https://www.c-sharpcorner.com/article/integration-of-google-recaptcha-in-websites/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace SimpleWAWS.CaptchaAuth
{
    public partial class _Default : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public bool IsReCaptchaValid()
        {
            var result = false;
            var captchaResponse = Request.Form["g-recaptcha-response"];
            var secretKey = Environment.GetEnvironmentVariable("ReCaptchaSecretKey");
            var apiUrl = "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}";
            var requestUri = string.Format(apiUrl, secretKey, captchaResponse);
            var request = (HttpWebRequest)WebRequest.Create(requestUri);
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    JObject jResponse = JObject.Parse(stream.ReadToEnd());
                    var isSuccess = jResponse.Value<bool>("success");
                    result = (isSuccess) ? true : false;
                }
            }
            return result;
        }

        protected void btnTry_Click(object sender, EventArgs e)
        {
            var txt= (IsReCaptchaValid())
                ? "<span style='color:green'>Captcha verification success</span>"
                : "<span style='color:red'>Captcha verification failed</span>";
        }
    }
}