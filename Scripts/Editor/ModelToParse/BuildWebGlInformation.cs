namespace BuildHelper.Workflows
{
    using System;

    [Serializable]
    public class BuildWebGlInformation : IBuildInformation
    {
        public WebGlInformation data = new WebGlInformation();

        public bool   IsDevelopment() { return this.data.IsDevelopment(); }
        public string BlueprintPath   => this.data.blueprintPath;
        public string DefineSymbol    => this.data.scriptDefinition;
        public string VersionCode     => "1";
        public string TelegramWorker  => this.data.telegramWorker;
        public string CCdInfo         => this.data.ccdInfo;
        public bool   clearCached     => this.data.clearCached;
    }

    [Serializable]
    public class WebGlInformation : BaseBuildData
    {
        public bool OptimizeSizeBuild() { return this.optimizeSizeBuild.Equals("true"); }

        public bool IsDevelopment() { return this.isBuildDevelopment.Equals("true"); }
    }
}