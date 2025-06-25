using System;

[Serializable]
public class BuildIosInformation : IBuildInformation
{
    public IosInformation data = new IosInformation();

    public bool   IsDevelopment() { return this.data.IsDevelopment(); }
    public string BlueprintPath   => this.data.blueprintPath;
    public string DefineSymbol    => this.data.scriptDefinition;
    public string VersionCode     => this.data.buildNumber;
    public string TelegramWorker  => this.data.telegramWorker;
    public string CCdInfo         => this.data.ccdInfo;
    public bool   clearCached     => this.data.clearCached;
}

[Serializable]
public class IosInformation : BaseBuildData
{
    public CustomVersion customVersion          = new CustomVersion();
    public string        signingTeamId          = "";
    public string        bundleIdentifier       = "com.abc.test";
    public string        productName            = "";
    public string        shouldUploadToAppStore = "false";
    public string        accountAppleId         = "";
    public string        accountPassword        = "";
    public string        fastLanePath           = "";
    public string        fastLaneSession        = "";
    public bool          isSandBox              = false;
    public bool          IsUploadAppstoreConnect() { return this.shouldUploadToAppStore.Equals("true"); }

    public bool OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
}