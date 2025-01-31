﻿[System.Serializable]
public class BuildWebGlInformation : IBuildInformation
{
    public WebGlInformation webGlInformation = new WebGlInformation();

    public bool   IsDevelopment() { return this.webGlInformation.IsDevelopment(); }
    public string BlueprintPath   => this.webGlInformation.blueprintPath;
    public string DefineSymbol    => this.webGlInformation.scriptDefinition;
    public string VersionCode     => "1";
}

[System.Serializable]
public class WebGlInformation
{
    public string scriptDefinition   = "TMP";
    public string outputFileName     = "output";
    public string optimizeSizeBuild  = "false";
    public string isBuildDevelopment = "false";
    public string buildEnvironment   = "Dev";
    public string useServicesAccount = "true";
    public string blueprintPath      = "BlueprintData";
    
    public bool IsUseServicesAccount() { return this.useServicesAccount.Equals("true"); }
    public bool   OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
}