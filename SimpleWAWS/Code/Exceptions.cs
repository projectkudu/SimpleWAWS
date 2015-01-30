using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public class MoreThanOneSiteException : Exception
    {
        public MoreThanOneSiteException(string message)
            : base(message)
        { }
    }

    public class NoFreeSitesException : Exception
    {
        public NoFreeSitesException(string message)
            : base(message)
        { }
    }

    public class InvalidUserIdentityException : Exception
    {
        public InvalidUserIdentityException(string message)
            : base(message)
        { }
    }
}