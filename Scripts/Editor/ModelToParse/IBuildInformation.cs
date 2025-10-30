namespace BuildHelper.Workflows
{
    public interface IBuildInformation
    {
        bool          IsDevelopment();
        string        BlueprintPath  { get; }
        string        DefineSymbol   { get; }
        string        VersionCode    { get; }
        public string TelegramWorker { get; }
        string        CCdInfo        { get; }
        bool          clearCached    { get; }
        public bool   IsLandScape    { get; }
    }
}