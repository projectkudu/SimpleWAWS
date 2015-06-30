using System;

namespace SimpleWAWS.Code
{
    public enum DeploymentType
    {
        ZipDeploy,
        GitWithCsmDeploy,
        GitNoCsmDeploy,
        CsmDeploy
    }
}