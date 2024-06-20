using System;

[Serializable]
public class BuildAndroidInformation:IBuildInformation
{
    public AndroidInformation androidInformation = new AndroidInformation();

    public bool IsDevelopment()
    {
        return this.androidInformation.IsDevelopment();
    }
}

[Serializable]
public class AndroidInformation
{
    public string        scriptDefinition   = "TMP";
    public string        keyName            = "user";
    public string        keyPass            = "123456";
    public string        aliasName          = "hai";
    public string        aliasPass          = "123456";
    public string        outputFileName     = "output";
    public string        buildNumber        = "1";
    public string        buildAppBundle     = "false";
    public CustomVersion customVersion      = new CustomVersion();
    public string        optimizeSizeBuild  = "false";
    public string        isBuildDevelopment = "false";

    public bool BuildAppBundle() { return this.buildAppBundle.Equals("true"); }

    public bool OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
}

[Serializable]
public class CustomVersion
{
    public string isCustomVersion = "false";
    public string version         = "1.0.0";

    public bool IsCustomVersion() { return this.isCustomVersion.Equals("true"); }
}