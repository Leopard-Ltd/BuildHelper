using System;

[Serializable]
public class BuildIosInformation : IBuildInformation
{
    public IosInformation iosInformation = new IosInformation();

    public bool   IsDevelopment() { return this.iosInformation.IsDevelopment(); }
    public string BlueprintPath   => this.iosInformation.blueprintPath;
    public string DefineSymbol    => this.iosInformation.scriptDefinition;
    public string VersionCode     => this.iosInformation.buildNumber;
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

    public bool IsUploadAppstoreConnect() { return this.shouldUploadToAppStore.Equals("true"); }

    public bool OptimizeSizeBuild()    { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
}