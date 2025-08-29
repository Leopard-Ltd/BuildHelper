using System;
using System.IO;
using BuildHelper.Workflows;
using UnityEditor;
using UnityEngine;

public static class BuildMenu
{
    [MenuItem("BuildScripts/Export Data To UpLoad")]
    static void ExportDataPath()
    {
        var path             = "";
        path += $"{Application.dataPath}\n";
        path += $"{Application.persistentDataPath}\n";
        var uploadHelper = $"{Application.dataPath.Replace("Assets", "Packages/BuildHelper/UploadHelper")}";
        File.WriteAllTextAsync($"{uploadHelper}/DataPath.txt", path);
    }

    static void SwitchPlatform()
    {
        ExportDataPath();
        BuildCmd.SwitchPlatform();
    }

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
    static void UploadGoogleDriveAndroidPlatform() { UploadBuild.UploadGoogleDriveAndroidPlatform(); }

    [MenuItem("BuildScripts/UploadGoogleIos")]
    static void UploadGoogleDriveIosPlatform() { UploadBuild.UploadGoogleDriveIosPlatform(); }

    [MenuItem("BuildScripts/UploadGoogleWebGL")]
    static void UploadGoogleDriveWebGlPlatForm() { UploadBuild.UploadGoogleDriveWebGlPlatForm(); }

    [MenuItem("BuildScripts/UploadTelegramWebgl")]
    static void UploadWebGl() { UploadBuildTelegram.UploadWebGl(); }

    [MenuItem("BuildScripts/Upload CCD")]
    static void ProcessCcd() { UnityCCD.ProcessCcd(); }
}