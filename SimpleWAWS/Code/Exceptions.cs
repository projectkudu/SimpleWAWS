using System;
using System.Net;

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

    public class ResourceCanOnlyBeExtendedOnce : Exception
    {
        public ResourceCanOnlyBeExtendedOnce()
            : base(Resources.Server.Error_ResourceExpirationTimeAlreadyExtended)
        { }
    }

    public class FailedRequestException : Exception
    {
        public Uri Uri { get; private set; }

        public string Content { get; private set; }

        public HttpStatusCode StatusCode { get; private set; }

        public FailedRequestException(Uri uri, string content, HttpStatusCode statusCode, string message)
            : base($"{message}, {uri}, {content}, {statusCode}")
        {
            this.Uri = uri;
            this.Content = content;
            this.StatusCode = statusCode;
        }
    }

    public class StorageNotReadyException : Exception
    {
        public StorageNotReadyException()
            : base()
        { }
    }
}