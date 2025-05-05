using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;

public class BuildWebGlPlatForm : BaseBuildPlatForm
{
    public override void SetUpAndBuild(IBuildInformation baseData)
    {
        base.SetUpAndBuild(baseData);
        var data = (BuildWebGlInformation)baseData;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.WebGL), ManagedStrippingLevel.High);
        this.SetupOptional();
        //auto profile
        EditorUserBuildSettings.connectProfiler = data.IsDevelopment();
        this.SetScriptDefineSymbols(NamedBuildTarget.WebGL, data.webGlInformation.scriptDefinition.Split(";"));
        var il2CppCodeGeneration = data.webGlInformation.OptimizeSizeBuild() ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, il2CppCodeGeneration);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes           = this.LoadSceneOnPath(),
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None,
            locationPathName = Path.GetFullPath($"../Build/Client/webgl/{data.webGlInformation.outputFileName}"),
            targetGroup      = BuildTargetGroup.WebGL
        };

        this.PreprocessBuild(data);
        var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildCmd.WriteReport(buildResult);
        CommonServices.LogMessage(buildResult.summary.result != BuildResult.Succeeded ? "Build failed" : "Build succeeded");
        CommonServices.LogMessage("Build Webgl Done");
    }

    private void SetupOptional()
    {
#if UNITY_WEBGL
        PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.runInBackground             = true;
        PlayerSettings.WebGL.powerPreference       = WebGLPowerPreference.HighPerformance;
        PlayerSettings.WebGL.dataCaching           = true;
        PlayerSettings.WebGL.exceptionSupport      = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
#if UNITY_6000_0_OR_NEWER

        // PlayerSettings.WebGL.webAssemblyTable  = true;
        // PlayerSettings.WebGL.webAssemblyBigInt = true;
#endif

#if FACEBOOK_INSTANT_GAME
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Disable compression for FBInstant game
        PlayerSettings.WebGL.decompressionFallback = false; // Disable compression for FBInstant game
        PlayerSettings.runInBackground = false;
#endif
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.WebGL.initialMemorySize = 64;
        UserBuildSettings.codeOptimization     = WasmCodeOptimization.DiskSize;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
#endif
#if WEBGL_PRODCTION
    PlayerSettings.WebGL.showDiagnostics = false;
    PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
#endif
#endif
    }
}