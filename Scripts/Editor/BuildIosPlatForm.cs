using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildIosPlatForm : BaseBuildPlatForm
{
    public override void SetUpAndBuild(IBuildInformation baseData)
    {
        var data = (BuildIosInformation)baseData;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        base.SetUpAndBuild(data);
        if (!string.IsNullOrEmpty(data.iosInformation.productName))
        {
            PlayerSettings.productName = data.iosInformation.productName;
        }
        EditorUserBuildSettings.connectProfiler = data.iosInformation.IsDevelopment();

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, data.iosInformation.bundleIdentifier);
        PlayerSettings.iOS.appleDeveloperTeamID = data.iosInformation.signingTeamId;
        PlayerSettings.iOS.buildNumber          = data.iosInformation.buildNumber;
        this.SetScriptDefineSymbols(NamedBuildTarget.iOS, data.iosInformation.scriptDefinition.Split(";"));
        var il2CppCodeGeneration = data.iosInformation.OptimizeSizeBuild() ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, il2CppCodeGeneration);
        var outputFileName = data.iosInformation.outputFileName;
        PlayerSettings.stripEngineCode             = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS), ManagedStrippingLevel.High);
        if (data.iosInformation.customVersion.IsCustomVersion())
        {
            PlayerSettings.bundleVersion = data.iosInformation.customVersion.version;
        }
        
        if (data.iosInformation.customVersion.IsAutoVersion())
        {
            PlayerSettings.bundleVersion = $"{PlayerSettings.bundleVersion}.{data.iosInformation.buildNumber}";
        }
        var dPath = Application.dataPath;
        dPath = dPath.Replace("Assets", "buildversion.txt");
        File.WriteAllText(dPath, PlayerSettings.bundleVersion);
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes           = this.LoadSceneOnPath(),
            target           = BuildTarget.iOS,
            options          = BuildOptions.None,
            locationPathName = this.GetBuildPath(outputFileName),
            targetGroup      = BuildTargetGroup.iOS
        };
        this.PreprocessBuild(data);
        var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildCmd.WriteReport(buildResult);
        Console.WriteLine(buildResult.summary.result != BuildResult.Succeeded ? "Build failed" : "Build succeeded");
    }

    private string GetBuildPath(string outputFileName)
    {
        return Path.GetFullPath($"../Build/Client/Ios/{outputFileName}");
    }
}