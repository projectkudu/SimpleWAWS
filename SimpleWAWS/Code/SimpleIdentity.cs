using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SimpleWAWS.Code
{
    public class SimpleIdentity : IIdentity
    {
        public SimpleIdentity(string name, string authenticationType)
        {
            this.Name = name;
            this.AuthenticationType = authenticationType;
            this.IsAuthenticated = true;
        }
        public string Name { get; private set; }
        public string AuthenticationType { get; private set; }
        public bool IsAuthenticated { get; private set; }
    }
}