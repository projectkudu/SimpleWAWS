using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmSitePublishingCredentials
    {
        public string name { get; set; }
        public string publishingUserName { get; set; }
        public string publishingPassword { get; set; }
        public string scmUri { get; set; }
    }
}