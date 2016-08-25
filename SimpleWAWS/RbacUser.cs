using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class RbacUser
    {
        public string TenantId { get; set; }

        public string UserId { get; set; }

        public string UserPuid { get; set; }
    }
}