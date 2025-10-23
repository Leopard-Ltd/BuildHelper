#if UNITY_ANDROID
namespace BuildHelper.Workflows
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Unity.Android.Types;
    using UnityEditor;
    using UnityEditor.Android;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;
    using AndroidArchitecture = UnityEditor.AndroidArchitecture;

    public class BuildAndroidPlatForm : BaseBuildPlatForm
    {
        private static string bundleId = Application.identifier;

        public override async Task SetUpAndBuild(IBuildInformation baseData)
        {
            var data = (BuildAndroidInformation)baseData;
            this.SetPassword(data);
            await base.SetUpAndBuild(data);
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            if (!string.IsNullOrEmpty(data.data.productName))
            {
                PlayerSettings.productName = data.data.productName;
            }

            //auto profile
            EditorUserBuildSettings.connectProfiler = data.data.IsDevelopment();

            if (!string.IsNullOrEmpty(data.data.bundleIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, data.data.bundleIdentifier);
            }

            var errors = false;
            EditorUserBuildSettings.buildAppBundle = data.data.BuildAppBundle();
            this.SetScriptDefineSymbols(NamedBuildTarget.Android, data.data.scriptDefinition.Split(";"));
            var il2CppCodeGeneration = data.data.OptimizeSizeBuild() ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, il2CppCodeGeneration);
            var outputFileName = data.data.outputFileName;

            if (data.data.customVersion.IsCustomVersion())
            {
                PlayerSettings.bundleVersion = data.data.customVersion.version;
            }

            var metadata      = Application.identifier + ",";
            var outputVersion = PlayerSettings.bundleVersion;

            var tmp = outputFileName.Split("-");
            tmp[1]         = outputVersion;
            outputFileName = string.Join("-", tmp);

            if (data.data.customVersion.IsAutoVersion())
            {
                if (!PlayerSettings.bundleVersion.EndsWith($"{data.data.buildNumber}"))
                {
                    PlayerSettings.bundleVersion = $"{PlayerSettings.bundleVersion}.{data.data.buildNumber}";
                }
            }

            metadata += outputVersion + ",";
            metadata += PlayerSettings.productName;
            File.WriteAllText(CommonServicesHelper.GetPathInformation("AppMetadata.txt"), metadata);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes           = this.LoadSceneOnPath(),
                target           = BuildTarget.Android,
                options          = BuildOptions.None,
                locationPathName = $"{CommonServicesHelper.GetBuildPath(outputFileName)}{(data.data.BuildAppBundle() ? ".aab" : ".apk")}",
                targetGroup      = BuildTargetGroup.Android
            };

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(buildPlayerOptions.targetGroup),
                data.data.scriptingBackend.Equals("il2cpp") ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), bundleId);

#if UNITY_6000_0_OR_NEWER
            UserBuildSettings.DebugSymbols.level = data.data.BuildAppBundle() ? DebugSymbolLevel.Full : DebugSymbolLevel.None;
#else
             EditorUserBuildSettings.androidCreateSymbols = data.androidInformation.BuildAppBundle() ? AndroidCreateSymbols.Debugging : AndroidCreateSymbols.Disabled;
#endif
            PlayerSettings.Android.bundleVersionCode = int.Parse(data.data.buildNumber);
            this.SetDefaultSetting(data);

            //Build
            this.PreprocessBuild(data);
            var buildResult = this.ExecuteBuild(buildPlayerOptions);
            errors = errors || buildResult.summary.result != BuildResult.Succeeded;
            Console.WriteLine(errors ? "*** Built Android Failed ***" : "Built android successfully!");

            if (errors)
            {
                throw new Exception("Build Android Failed");
            }

            Console.WriteLine(new string('=', 80));
            Console.WriteLine();
            await this.AfterBuild(data);
        }

        protected virtual BuildReport ExecuteBuild(BuildPlayerOptions options)
        {
            var buildResult = BuildPipeline.BuildPlayer(options);
            BuildCmd.WriteReport(buildResult);

            return buildResult;
        }

        private void SetDefaultSetting(BuildAndroidInformation data)
        {
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            if (data.data.stripCode)
            {
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), ManagedStrippingLevel.High);
            }
            else
            {
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), ManagedStrippingLevel.Minimal);
            }

            PlayerSettings.Android.minifyDebug   = data.data.IsMinify();
            PlayerSettings.Android.minifyRelease = data.data.IsMinify();

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.Android.splitApplicationBinary = data.data.IsSplitBinary() && data.data.BuildAppBundle();
#else
             PlayerSettings.Android.useAPKExpansionFiles = data.androidInformation.IsSplitBinary();
#endif
        }

        private void SetPassword(BuildAndroidInformation data)
        {
            var fileName = data.data.keyName;

            if (!fileName.EndsWith(".keystore", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".keystore";
            }

            var finalPath = $"{CommonServicesHelper.GetProjectPath()}/keys/{fileName}";

            if (!string.IsNullOrEmpty(data.data.keystorePath))
            {
                var tmp = data.data.keystorePath;
                tmp       = tmp.EndsWith("/") || tmp.EndsWith("\\") ? tmp : tmp + "/";
                finalPath = $"{tmp}{fileName}";
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName      = finalPath;
            PlayerSettings.Android.keystorePass      = data.data.keyPass;
            PlayerSettings.Android.keyaliasName      = data.data.aliasName;
            PlayerSettings.Android.keyaliasPass      = data.data.aliasPass;
        }
    }
}
#endif