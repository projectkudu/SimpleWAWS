using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class MoreThanOneResourceGroupException : Exception
    {
        public MoreThanOneResourceGroupException()
            //This should use the server version of the error, but due to a string bug they are not the same.
            : base(Resources.Client.Information_YouCantHaveMoreThanOne)
        { }
    }

    public class NoFreeResourceGroupsException : Exception
    {
        public NoFreeResourceGroupsException()
            : base(Resources.Server.Error_NoFreeResourcesAvailable)
        { }
    }

    public class InvalidUserIdentityException : Exception
    {
        public InvalidUserIdentityException()
            : base(Resources.Server.Error_InvalidUserIdentity)
        { }
    }

    public class CsmDeploymentFailedException : Exception
    {
        public CsmDeploymentFailedException(string message)
            : base(message)
        { }
    }

    public class InvalidGithubRepoException : Exception
    {
        public InvalidGithubRepoException()
            : base(Resources.Server.Error_InvalidGithubRepo)
        { }
    }
}