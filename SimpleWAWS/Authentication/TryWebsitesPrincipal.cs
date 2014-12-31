using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class TryWebsitesPrincipal : IPrincipal
    {
        public TryWebsitesPrincipal(IIdentity identity)
        {
            this.Identity = identity;
        }
        public bool IsInRole(string role)
        {
            return true;
        }

        public IIdentity Identity { get; private set; }

    }
}