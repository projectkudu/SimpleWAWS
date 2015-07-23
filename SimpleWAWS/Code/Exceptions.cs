using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class MoreThanOneResourceGroupException : Exception
    {
        public MoreThanOneResourceGroupException()
            : base(Resources.Error_MoreThanOneFreeResource)
        { }
    }

    public class NoFreeResourceGroupsException : Exception
    {
        public NoFreeResourceGroupsException()
            : base(Resources.Error_NoFreeResourcesAvailable)
        { }
    }

    public class InvalidUserIdentityException : Exception
    {
        public InvalidUserIdentityException()
            : base(Resources.Error_InvalidUserIdentity)
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
            : base(Resources.Error_InvalidGithubRepo)
        { }
    }
}