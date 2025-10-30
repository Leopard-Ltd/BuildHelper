#if UNITY_WEBGL

using System;
using System.IO;
using System.Threading.Tasks;
using BuildHelper.Workflows;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;

public class BuildWebGlPlatForm : BaseBuildPlatForm
{
    public override async Task SetUpAndBuild(IBuildInformation baseData)
    {
        await base.SetUpAndBuild(baseData);
        var data = (BuildWebGlInformation)baseData;
        this.SetOrientation(data.IsLandScape);

        if (data.data.stripCode)
        {
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.WebGL), ManagedStrippingLevel.High);
        }
        else
        {
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.WebGL), ManagedStrippingLevel.Minimal);
        }

        this.SetupOptional();
        //auto profile
        EditorUserBuildSettings.connectProfiler = data.IsDevelopment();
        this.SetScriptDefineSymbols(NamedBuildTarget.WebGL, data.data.scriptDefinition.Split(";"));
        var il2CppCodeGeneration = data.data.OptimizeSizeBuild() ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, il2CppCodeGeneration);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes           = this.LoadSceneOnPath(),
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None,
            locationPathName = $"{CommonServicesHelper.GetBuildPath(data.data.outputFileName, "webgl")}",
            targetGroup      = BuildTargetGroup.WebGL
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
        CommonServicesHelper.LogMessage("Build Webgl Done");
    }

    protected override void SetAllGroupsToLZMA() { }

    private void SetupOptional()
    {
        PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.runInBackground             = true;
        PlayerSettings.WebGL.powerPreference       = WebGLPowerPreference.HighPerformance;
        PlayerSettings.WebGL.dataCaching           = true;
        PlayerSettings.WebGL.exceptionSupport      = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        UserBuildSettings.codeOptimization         = WasmCodeOptimization.DiskSize;
#if UNITY_6000_0_OR_NEWER
        // PlayerSettings.WebGL.webAssemblyTable  = true;
        // PlayerSettings.WebGL.webAssemblyBigInt = true;
#endif

#if DISABLE_COMPRESS
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Disable compression for FBInstant game
        PlayerSettings.WebGL.decompressionFallback = false; // Disable compression for FBInstant game
        PlayerSettings.runInBackground = false; 
        //UserBuildSettings.codeOptimization = WasmCodeOptimization.DiskSize;
#endif
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.WebGL.initialMemorySize = 64;
#endif
#if WEBGL_PRODCTION
    PlayerSettings.WebGL.showDiagnostics = false;
    PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
#endif
    }
}

#endif