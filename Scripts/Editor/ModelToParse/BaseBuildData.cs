using System.Collections.Generic;

public class BaseBuildData
{
    public string scriptDefinition   = "TMP";
    public string outputFileName     = "output";
    public string buildNumber        = "1";
    public string blueprintPath      = "BlueprintData";
    public string optimizeSizeBuild  = "false";
    public string isBuildDevelopment = "false";
    public string buildEnvironment   = "Dev";


    public List<TelegramInformation> telegramInfos = new List<TelegramInformation>();
}