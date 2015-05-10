using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Kudu.Client.Editor;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using Newtonsoft.Json;
using SimpleWAWS.Kudu;
using Newtonsoft.Json.Converters;
using SimpleWAWS.Code;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Trace;
using System.Configuration;

namespace SimpleWAWS.Models
{
    public class Site : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}";

        public override string CsmId
        {
            get
            {
                return string.Format(_csmIdTemplate, SubscriptionId, ResourceGroupName, SiteName);
            }
        }

        public string SiteName { get; private set; }

        public Dictionary<string, string> AppSettings { get; set; }

        public Dictionary<string, string> Metadata { get; set; }

        public string HostName { get; set; }

        public string ScmHostName { get; set; }

        public Site(string subscriptionId, string resourceGroupName, string name)
            : base (subscriptionId, resourceGroupName)
        {
            this.SiteName = name;
        }

        public string Url
        {
            get 
            {
                return String.Format("https://{0}/", HostName);
            }
        }

        public string MobileUrl
        {
            get
            {
                return Url + "jsclient";
            }
        }

        public string ScmUrl
        {
            get
            {
                return String.Format("https://{0}/", ScmHostName);
            }
        }

        public string ScmUrlWithCreds
        {
            get
            {
                return String.Format("https://{0}:{1}@{2}/", PublishingUserName, PublishingPassword, ScmHostName);
            }
        }

        public string KuduConsoleWithCreds
        {
            get
            {
                return ScmUrlWithCreds + "DebugConsole";
            }
        }

        public string GitUrlWithCreds
        {
            get
            {
                return ScmUrlWithCreds + SiteName + ".git";
            }
        }

        public string MonacoUrl
        {
            get
            {
                return ScmUrl + "dev";
            }
        }

        public string ContentDownloadUrl
        {
            get
            {
                return ScmUrl + "zip/site/wwwroot";
            }
        }

        public string IbizaUrl
        {
            get
            {
                return string.Concat("https://portal.azure.com/", ConfigurationManager.AppSettings["tryWebsitesTenantName"], "#resource", CsmId);
            }
        }

        public string PublishingUserName { get; set; }

        public string PublishingPassword { get; set; }

        public void FireAndForget()
        {
            try
            {
                var httpHeaders = "GET / HTTP/1.0\r\n" +
                "Host: " + this.HostName + "\r\n" +
                "\r\n";
                using (var tcpClient = new TcpClient(this.HostName, 80))
                {
                    tcpClient.Client.Send(Encoding.ASCII.GetBytes(httpHeaders));
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                //log and ignore any tcp exceptions
                SimpleTrace.Diagnostics.Error(ex, "TCP Error");
            }
        }
    }
}
