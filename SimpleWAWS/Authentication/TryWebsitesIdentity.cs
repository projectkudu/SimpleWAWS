using System;
using System.Globalization;
using System.Linq;
using System.Security.Principal;

namespace SimpleWAWS.Authentication
{
    public class TryWebsitesIdentity : IIdentity
    {
        public TryWebsitesIdentity(string email, string puid, string issuer)
        {
            this.Name = string.Format(CultureInfo.InvariantCulture, "{0}#{1}", issuer, email);
            this.Email = email;
            this.Puid = puid;
            this.Issuer = issuer;
            this.AuthenticationType = issuer;
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
        public string FilteredName
        {
            get
            {
                var anonymizedEmail = "unknown.com";
                anonymizedEmail = String.Join(String.Empty, Email.Split('@').Skip(1));
                return string.Format(CultureInfo.InvariantCulture, "{0};[FILTERED]@{1}", Issuer, anonymizedEmail);
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