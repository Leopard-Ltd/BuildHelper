namespace BuildHelper.Workflows
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    public class BuildIosPlatForm : BaseBuildPlatForm
    {
        public override async Task SetUpAndBuild(IBuildInformation baseData)
        {
            var data = (BuildIosInformation)baseData;
            await base.SetUpAndBuild(data);

            if (!string.IsNullOrEmpty(data.data.productName))
            {
                PlayerSettings.productName = data.data.productName;
            }

            this.SetOrientation(data.IsLandScape);
            EditorUserBuildSettings.connectProfiler = data.data.IsDevelopment();

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, data.data.bundleIdentifier);
            PlayerSettings.iOS.appleDeveloperTeamID = data.data.signingTeamId;
            PlayerSettings.iOS.buildNumber          = data.data.buildNumber;
            this.SetScriptDefineSymbols(NamedBuildTarget.iOS, data.data.scriptDefinition.Split(";"));
            var il2CppCodeGeneration = data.data.OptimizeSizeBuild() ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, il2CppCodeGeneration);
            var outputFileName = data.data.outputFileName;

            if (data.data.stripCode)
            {
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS), ManagedStrippingLevel.High);
            }
            else
            {
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS), ManagedStrippingLevel.Minimal);
            }

            if (data.data.customVersion.IsCustomVersion())
            {
                PlayerSettings.bundleVersion = data.data.customVersion.version;
            }

            if (data.data.customVersion.IsAutoVersion())
            {
                if (!PlayerSettings.bundleVersion.EndsWith($"{data.data.buildNumber}"))
                {
                    PlayerSettings.bundleVersion = $"{PlayerSettings.bundleVersion}.{data.data.buildNumber}";
                }
            }

            var appMetadata = Application.identifier + ",";
            appMetadata += PlayerSettings.bundleVersion + "," + PlayerSettings.productName;
            File.WriteAllText(CommonServicesHelper.GetPathInformation("AppMetadata.txt"), appMetadata);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes           = this.LoadSceneOnPath(),
                target           = BuildTarget.iOS,
                options          = BuildOptions.None,
                locationPathName = $"{CommonServicesHelper.GetBuildPath(outputFileName, "ios")}",
                targetGroup      = BuildTargetGroup.iOS
            };

            this.PreprocessBuild(data);
            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (buildResult.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Build Android Failed");
            }

            await this.AfterBuild(data);
            BuildCmd.WriteReport(buildResult);
            CommonServicesHelper.LogMessage(buildResult.summary.result != BuildResult.Succeeded ? "Build failed" : "Build succeeded");
        }
    }
}