using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class TryWebsitesIdentity : IIdentity
    {
        public TryWebsitesIdentity(string email, string puid, string issure)
        {
            this.Name = string.Format(CultureInfo.InvariantCulture, "{0}#{1}", issure, email);
            this.Email = email;
            this.Puid = puid;
            this.Issuer = issure;
            this.AuthenticationType = issure;
            this.IsAuthenticated = true;
        }
        public string Name { get; private set; }
        public string AuthenticationType { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public string UniqueName
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0};{1}", Issuer, Email);
            }
        }
        public string Puid { get; private set; }
        public string Email { get; private set; }
        public string Issuer { get; private set; }
        public bool IsAnonymous { get { return Issuer != null && Issuer.Equals("Anonymous", StringComparison.OrdinalIgnoreCase); } }

        public override string ToString()
        {
            return Name;
        }
    }
}