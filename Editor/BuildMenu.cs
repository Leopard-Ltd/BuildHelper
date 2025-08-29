using BuildHelper.Workflows;
using UnityEditor;

public static class BuildMenu
{
    static void SwitchPlatform() { BuildCmd.SwitchPlatform(); }

    [MenuItem("BuildScripts/SetBlueprintPath")]
    static void SetBlueprintDataPath() { BuildCmd.SetBlueprintDataPath(); }

    [MenuItem("BuildScripts/Build Android from Editor")]
    static void BuildAndroidOnEditor() { BuildCmd.BuildAndroidOnEditor(); }

    [MenuItem("BuildScripts/TryRynSyncData")]
    static void TryRunSyncData() { BuildCmd.TryRunSyncData(); }

    [MenuItem("BuildScripts/Build Android")]
    static void BuildAndroid() { BuildCmd.BuildAndroid(); }

    [MenuItem("BuildScripts/Build Ios")]
    static void BuildIos() { BuildCmd.BuildIos(); }

    [MenuItem("BuildScripts/Build WebGl")]
    static void BuildWebGL() { BuildCmd.BuildWebGL(); }

    [MenuItem("BuildScripts/UploadTestFlight")]
    static void UploadTestFlight() { BuildCmd.UploadTestFlight(); }

    [MenuItem("BuildScripts/UploadAAbToGooglePlay")]
    static void UploadAAbToGooglePlay() { BuildCmd.UploadAAbToGooglePlay(); }

    [MenuItem("BuildScripts/ProcessBlueprintAndroid")]
    static void BlueprintWorkFlowAndroid() { BuildCmd.BlueprintWorkFlowAndroid(); }

    [MenuItem("BuildScripts/ProcessBlueprintIos")]
    static void BlueprintWorkFlowIos() { BuildCmd.BlueprintWorkFlowIos(); }

    [MenuItem("BuildScripts/ProcessBlueprintWegbl")]
    static void BlueprintWorkFlowWebGL() { BuildCmd.BlueprintWorkFlowWebgl(); }

    [MenuItem("BuildScripts/UploadGoogleAndroid")]
    static void UploadGoogleDriveAndroidPlatform() { UploadBuild.UploadGoogleDriveAndroidPlatform().GetAwaiter().GetResult(); }

    [MenuItem("BuildScripts/UploadGoogleIos")]
    static void UploadGoogleDriveIosPlatform() { UploadBuild.UploadGoogleDriveIosPlatform().GetAwaiter().GetResult(); }

    [MenuItem("BuildScripts/UploadGoogleWebGL")]
    static void UploadGoogleDriveWebGlPlatForm() { UploadBuild.UploadGoogleDriveWebGlPlatForm().GetAwaiter().GetResult(); }

    [MenuItem("BuildScripts/UploadTelegramWebgl")]
    static void UploadWebGl() { UploadBuildTelegram.UploadWebGl(); }

    [MenuItem("BuildScripts/Upload CCD")]
    static void ProcessCcd() { UnityCCD.ProcessCcd().GetAwaiter().GetResult(); }
}