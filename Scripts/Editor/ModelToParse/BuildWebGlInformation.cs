[System.Serializable]
public class BuildWebGlInformation : IBuildInformation
{
    public WebGlInformation webGlInformation = new WebGlInformation();

    public bool IsDevelopment() { return this.webGlInformation.IsDevelopment(); }
}

[System.Serializable]
public class WebGlInformation
{
    public string scriptDefinition   = "TMP";
    public string outputFileName     = "output";
    public string optimizeSizeBuild  = "false";
    public string isBuildDevelopment = "false";
    public string buildEnvironment   = "Dev";
    public bool   OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

    public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
}