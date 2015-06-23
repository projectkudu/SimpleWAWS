using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{

    public class CsmSiteDeployment
    {
        public string id { get; set; }
        public DeployStatus? status { get; set; }
        public string status_text { get; set; }
        public string author_email { get; set; }
        public string author { get; set; }
        public string deployer { get; set; }
        public string message { get; set; }
        public string progress { get; set; }
        public DateTime? received_time { get; set; }
        public DateTime? start_time { get; set; }
        public DateTime? end_time { get; set; }
        public DateTime? last_success_end_time { get; set; }
        public bool? complete { get; set; }
        public bool? active { get; set; }
        public bool? is_temp { get; set; }
        public bool? is_readonly { get; set; }
        public string url { get; set; }
        public string log_url { get; set; }
        public string site_name { get; set; }
    }

    public enum DeployStatus
    {
        Pending,
        Building,
        Deploying,
        Failed,
        Success
    }
}