using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class MoreThanOneResourceGroupException : Exception
    {
        public MoreThanOneResourceGroupException(string message)
            : base(message)
        { }
    }

    public class NoFreeResourceGroupsException : Exception
    {
        public NoFreeResourceGroupsException(string message)
            : base(message)
        { }
    }

    public class InvalidUserIdentityException : Exception
    {
        public InvalidUserIdentityException(string message)
            : base(message)
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
        public InvalidGithubRepoException(string message)
            : base(message)
        { }
    }
}