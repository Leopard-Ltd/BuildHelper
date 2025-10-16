using System.IO;
using BuildHelper.Workflows;
using UnityEditor;
using UnityEngine;

public static class BuildMenu
{
    [MenuItem("BuildScripts/Export DataPath", priority = 99)]
    public static void ExportDataPath()
    {
        var path = "";
        path += $"{Application.dataPath}\n";
        path += $"{Application.persistentDataPath}\n";

        var buildPath  = CommonServicesHelper.GetBuildPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent     = Path.GetDirectoryName(buildPath);
        var configPath = Path.Combine(parent, "Configs");

        File.WriteAllTextAsync($"{configPath}/DataPath.txt", path);
    }

    public static void SwitchPlatform()
    {
        ExportDataPath();
        BuildCmd.SwitchPlatform();
    }

    [MenuItem("BuildScripts/ExportAndroidProject")]
    public static void ExportAndroidProject() { BuildCmd.ExportAndroidProject(); }

    [MenuItem("BuildScripts/SetBlueprintPath")]
    public static void SetBlueprintDataPath() { BuildCmd.SetBlueprintDataPath(); }

    [MenuItem("BuildScripts/Build Android from Editor")]
    public static void BuildAndroidOnEditor() {  BuildCmd.BuildAndroidOnEditor(); }

    [MenuItem("BuildScripts/TryRynSyncData")]
    public static void TryRunSyncData() { BuildCmd.TryRunSyncData(); }

    [MenuItem("BuildScripts/Build Android")]
    public static void BuildAndroid() {BuildCmd.BuildAndroid(); }

    [MenuItem("BuildScripts/Build Ios")]
    public static void BuildIos() { BuildCmd.BuildIos(); }

    [MenuItem("BuildScripts/Build WebGl")]
    public static void BuildWebGL() {  BuildCmd.BuildWebGL(); }

    [MenuItem("BuildScripts/UploadTestFlight")]
    public static void UploadTestFlight() { BuildCmd.UploadTestFlight(); }

    [MenuItem("BuildScripts/UploadAAbToGooglePlay")]
    public static void UploadAAbToGooglePlay() { BuildCmd.UploadAAbToGooglePlay(); }

    [MenuItem("BuildScripts/ProcessBlueprintAndroid")]
    public static void BlueprintWorkFlowAndroid() {  BuildCmd.BlueprintWorkFlowAndroid(); }

    [MenuItem("BuildScripts/ProcessBlueprintIos")]
    public static void BlueprintWorkFlowIos() {BuildCmd.BlueprintWorkFlowIos(); }

    [MenuItem("BuildScripts/ProcessBlueprintWegbl")]
    public static void BlueprintWorkFlowWebGL() { BuildCmd.BlueprintWorkFlowWebgl(); }

    [MenuItem("BuildScripts/UploadGoogleAndroid")]
    public static void UploadGoogleDriveAndroidPlatform() { UploadBuild.UploadGoogleDriveAndroidPlatform().GetAwaiter().GetResult(); }

    [MenuItem("BuildScripts/UploadGoogleIos")]
    public static void UploadGoogleDriveIosPlatform() { UploadBuild.UploadGoogleDriveIosPlatform().GetAwaiter().GetResult(); }

    [MenuItem("BuildScripts/UploadGoogleWebGL")]
    public static void UploadGoogleDriveWebGlPlatForm() { UploadBuild.UploadGoogleDriveWebGlPlatForm().GetAwaiter().GetResult(); }

    [MenuItem("BuildScripts/UploadTelegramWebgl")]
    public static void UploadWebGl() { UploadBuildTelegram.UploadWebGl(); }

    [MenuItem("BuildScripts/Upload CCD")]
    public static void ProcessCcd() { UnityCCD.ProcessCcd().GetAwaiter().GetResult(); }
}