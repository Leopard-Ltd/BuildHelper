using System;

[Serializable]
public class BuildWebGlInformation : IBuildInformation
{
    public WebGlInformation webGlInformation = new WebGlInformation();

    public bool   IsDevelopment() { return this.webGlInformation.IsDevelopment(); }
    public string BlueprintPath   => this.webGlInformation.blueprintPath;
    public string DefineSymbol    => this.webGlInformation.scriptDefinition;
    public string VersionCode     => "1";
}

[Serializable]
public class WebGlInformation : BaseBuildData
{
    public bool OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
}