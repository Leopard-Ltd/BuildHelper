public interface IBuildInformation
{
    bool   IsDevelopment();
    string BlueprintPath { get; }
    string DefineSymbol { get; }
    string VersionCode { get; }
}