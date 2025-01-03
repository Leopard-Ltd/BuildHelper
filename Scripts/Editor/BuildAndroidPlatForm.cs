using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildAndroidPlatForm : BaseBuildPlatForm
{
    private static string bundleId = Application.identifier;

    public override void SetUpAndBuild(IBuildInformation baseData)
    {
        var data = (BuildAndroidInformation)baseData;
        this.SetPassword(data);
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        base.SetUpAndBuild(data);
        //auto profile
        EditorUserBuildSettings.connectProfiler = data.androidInformation.IsDevelopment();

        if (!string.IsNullOrEmpty(data.androidInformation.bundleIdentifier))
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, data.androidInformation.bundleIdentifier);
        }

        var errors = false;
        EditorUserBuildSettings.buildAppBundle = data.androidInformation.BuildAppBundle();       
        this.SetScriptDefineSymbols(NamedBuildTarget.Android, data.androidInformation.scriptDefinition.Split(";"));
        var il2CppCodeGeneration = data.androidInformation.OptimizeSizeBuild() ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, il2CppCodeGeneration);
        var outputFileName = data.androidInformation.outputFileName;

        if (data.androidInformation.customVersion.IsCustomVersion())
        {
            PlayerSettings.bundleVersion = data.androidInformation.customVersion.version;

            if (data.androidInformation.customVersion.IsAutoVersion())
            {
                var tmp         = outputFileName.Split("-");
                var buildNumber = tmp[2];
                PlayerSettings.bundleVersion = $"{PlayerSettings.bundleVersion}.{buildNumber}";
            }
        }
        else
        {
            var tmp = outputFileName.Split("-");
            tmp[1]         = PlayerSettings.bundleVersion;
            outputFileName = string.Join("-", tmp);
        }

        var dPath = Application.dataPath;
        dPath = dPath.Replace("Assets", "buildversion.txt");
        File.WriteAllText(dPath, PlayerSettings.bundleVersion);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes           = this.LoadSceneOnPath(),
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
            locationPathName = this.GetBuildPath(outputFileName, data.androidInformation.BuildAppBundle()),
            targetGroup      = BuildTargetGroup.Android
        };

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(buildPlayerOptions.targetGroup), ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), bundleId);

#if UNITY_6000_0_OR_NEWER
          UnityEditor.Android.UserBuildSettings.DebugSymbols.level = data.androidInformation.BuildAppBundle() ? Unity.Android.Types.DebugSymbolLevel.Full : Unity.Android.Types.DebugSymbolLevel.None;
#else
        EditorUserBuildSettings.androidCreateSymbols = data.androidInformation.BuildAppBundle() ? AndroidCreateSymbols.Debugging : AndroidCreateSymbols.Disabled;
#endif
        PlayerSettings.Android.bundleVersionCode = int.Parse(data.androidInformation.buildNumber);
        this.SetDefaultSetting(data);
     

        //Build
        this.PreprocessBuild(data);
        var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildCmd.WriteReport(buildResult);
        errors = errors || buildResult.summary.result != BuildResult.Succeeded;
        Console.WriteLine(errors ? "*** Built Android Failed ***" : "Built android successfully!");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
        Debug.Log("Build Android Done");
    }

    private void SetDefaultSetting(BuildAndroidInformation data)
    {
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.stripEngineCode             = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), ManagedStrippingLevel.High);
        PlayerSettings.Android.minifyDebug   = true;
        PlayerSettings.Android.minifyRelease = true;

#if UNITY_6000_0_OR_NEWER
        PlayerSettings.Android.splitApplicationBinary = data.androidInformation.BuildAppBundle();

#else
        PlayerSettings.Android.useAPKExpansionFiles = data.androidInformation.BuildAppBundle();
#endif
    }

    private void SetPassword(BuildAndroidInformation data)
    {
        var filePath = $"{data.androidInformation.keyName}";
        var dPath    = Application.dataPath;
        dPath = dPath.Replace("Assets", "keys/");
        dPath = $"{dPath}{filePath}";

        var finalPath = dPath;
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName      = finalPath;
        PlayerSettings.Android.keystorePass      = data.androidInformation.keyPass;
        PlayerSettings.Android.keyaliasName      = data.androidInformation.aliasName;
        PlayerSettings.Android.keyaliasPass      = data.androidInformation.aliasPass;
    }

    private string GetBuildPath(string outputFileName, bool isAab = false) { return Path.GetFullPath($"../Build/Client/Android/{outputFileName}{(isAab ? ".aab" : ".apk")}"); }
}