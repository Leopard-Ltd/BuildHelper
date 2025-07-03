[System.Serializable]
public class BlueprintWorkFlowData
{
    public string addressableProfile = "GithubCCD";
    public string environmentName    = "production";
    public string platFormName       = "android";
    public string versionName        = "v1";
    
    public string remoteBuildPath      = "";
    public string remoteLoadPath       = "";
    public string blueprintName        = "Blueprints";
    public string blueprintVersion     = "v0.0.1";
    public string labelName            = "";
    public string groupAssignBlueprint = "";

    public string          ccdPlatform     = "";
    public GoogleDriveData googleDriveData = new();
    public GithubCdnData   githubCdnData   = new();
}

[System.Serializable]
public class GoogleDriveData
{
    public string spreadsheetId       = "";
    public string servicesAccountJson = "";
    public string sheetBuilderConfig  = "BuilderConfig";
}

[System.Serializable]
public class GithubCdnData
{
    public string gitHubUrl    = "";
    public string branchName   = "";
    public string rootFolder   = "";
    public string gitExistPath = "";
}