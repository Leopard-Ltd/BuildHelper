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
public class IosInformation
{
    public string        scriptDefinition   = "TMP";
    public string        outputFileName     = "output";
    public string        buildNumber        = "1";
    public CustomVersion customVersion      = new CustomVersion();
    public string        optimizeSizeBuild  = "false";
    public string        isBuildDevelopment = "false";
    public string        signingTeamId      = "";
    public string        bundleIdentifier   = "com.abc.test";

    public string blueprintPath = "BlueprintData";

    public bool OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
}