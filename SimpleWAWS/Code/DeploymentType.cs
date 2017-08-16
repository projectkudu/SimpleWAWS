namespace SimpleWAWS.Code
{
    public enum DeploymentType
    {
        ZipDeploy = 0,
        GitWithCsmDeploy,
        GitNoCsmDeploy,
        CsmDeploy,
        FunctionDeploy,
        RbacOnly
    }
}