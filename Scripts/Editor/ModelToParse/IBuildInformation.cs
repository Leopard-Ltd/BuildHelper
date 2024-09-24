public interface IBuildInformation
{
    bool   IsDevelopment();
    string BlueprintPath { get; }
}