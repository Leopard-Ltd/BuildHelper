using System.Collections.Generic;

public class BaseBuildData
{
    public string scriptDefinition       = "TMP";
    public string outputFileName         = "output";
    public string buildNumber            = "1";
    public string blueprintPath          = "BlueprintData";
    public string optimizeSizeBuild      = "true";
    public string isBuildDevelopment     = "false";
    public string buildEnvironment       = "Dev";
    public bool   clearCachedCredentials = false;
    public string useServicesAccount     = "true";
    public bool   stripCode              = true;

    public bool IsUseServicesAccount() { return this.useServicesAccount.Equals("true"); }
    public bool clearCached = false;

    public List<TelegramInformation> telegramInfos  = new List<TelegramInformation>();
    public string                    telegramWorker = "";
    public string                    ccdInfo        = "";
}

[System.Serializable]
public class UnityCCDInfo
{
    public bool   allowUpdate     = false;
    public string projectId       = "";
    public bool   forceClearCache = false;
    public string environmentId   = "";
    public string bucketId        = "";
    public string clientId        = "";
    public string clientSecret    = "";
}