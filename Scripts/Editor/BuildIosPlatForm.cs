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

        if (!string.IsNullOrEmpty(data.data.productName))
        {
            PlayerSettings.productName = data.data.productName;
        }

        EditorUserBuildSettings.connectProfiler = data.data.IsDevelopment();

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, data.data.bundleIdentifier);
        PlayerSettings.iOS.appleDeveloperTeamID = data.data.signingTeamId;
        PlayerSettings.iOS.buildNumber          = data.data.buildNumber;
        this.SetScriptDefineSymbols(NamedBuildTarget.iOS, data.data.scriptDefinition.Split(";"));
        var il2CppCodeGeneration = data.data.OptimizeSizeBuild() ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, il2CppCodeGeneration);
        var outputFileName = data.data.outputFileName;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS), ManagedStrippingLevel.High);

        if (data.data.customVersion.IsCustomVersion())
        {
            PlayerSettings.bundleVersion = data.data.customVersion.version;
        }

        if (data.data.customVersion.IsAutoVersion())
        {
            PlayerSettings.bundleVersion = $"{PlayerSettings.bundleVersion}.{data.data.buildNumber}";
        }

        var dPath = Application.dataPath;
        dPath = CommonServices.GetPathInformation("buildversion.txt");
        File.WriteAllText(dPath, PlayerSettings.bundleVersion);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes           = this.LoadSceneOnPath(),
            target           = BuildTarget.iOS,
            options          = BuildOptions.None,
            locationPathName = $"{CommonServices.GetBuildPath(outputFileName,"ios")}",
            targetGroup      = BuildTargetGroup.iOS
        };

        this.PreprocessBuild(data);
        var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildCmd.WriteReport(buildResult);
        this.AfterBuild(data);
        CommonServices.LogMessage(buildResult.summary.result != BuildResult.Succeeded ? "Build failed" : "Build succeeded");
    }
}