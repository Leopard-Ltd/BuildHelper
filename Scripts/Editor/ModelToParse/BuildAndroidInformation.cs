using System;

[Serializable]
public class BuildAndroidInformation : IBuildInformation
{
    public AndroidInformation data = new AndroidInformation();

    public bool   IsDevelopment() { return this.data.IsDevelopment(); }
    public string BlueprintPath   => this.data.blueprintPath;
    public string DefineSymbol    => this.data.scriptDefinition;
    public string VersionCode     => this.data.buildNumber;
    public string TelegramWorker  => this.data.telegramWorker;
    public string CCdInfo         => this.data.ccdInfo;
    public bool   clearCached     => this.data.clearCached;
}

[Serializable]
public class AndroidInformation : BaseBuildData
{
    public string        keyName          = "user.keystore";
    public string        keyPass          = "123456";
    public string        aliasName        = "hai";
    public string        aliasPass        = "123456";
    public string        buildAppBundle   = "false";
    public CustomVersion customVersion    = new CustomVersion();
    public string        bundleIdentifier = "";
    public string        productName      = "";
    public string        minify           = "true";
    public string        splitBinary      = "true";
	public string scriptingBackend="il2cpp";
    public bool BuildAppBundle() { return this.buildAppBundle.Equals("true"); }

    public bool OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
    public bool IsMinify()      { return this.minify.Equals("true"); }
    public bool IsSplitBinary() { return this.splitBinary.Equals("true"); }
}

[Serializable]
public class CustomVersion
{
    public string isCustomVersion = "false";
    public string version         = "1.0.0";
    public string autoVersion     = "false";

    public bool IsAutoVersion() { return this.autoVersion.Equals("true"); }

    public bool IsCustomVersion() { return this.isCustomVersion.Equals("true"); }
}