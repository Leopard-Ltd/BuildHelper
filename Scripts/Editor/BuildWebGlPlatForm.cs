using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildWebGlPlatForm : BaseBuildPlatForm
{
    public override void SetUpAndBuild(IBuildInformation baseData)
    {
        base.SetUpAndBuild(baseData);
        var data = (BuildWebGlInformation)baseData;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android), ManagedStrippingLevel.High);
        this.SetupOptional();
        //auto profile
        EditorUserBuildSettings.connectProfiler = data.IsDevelopment();
        this.SetScriptDefineSymbols(NamedBuildTarget.Android, data.webGlInformation.scriptDefinition.Split(";"));
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
        Console.WriteLine(buildResult.summary.result != BuildResult.Succeeded ? "Build failed" : "Build succeeded");
        Debug.Log("Build Android Done");
    }

    private void SetupOptional()
    {
#if UNITY_WEBGL
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Disable compression for FBInstant game
        PlayerSettings.WebGL.decompressionFallback = false; // Disable compression for FBInstant game
        PlayerSettings.runInBackground = false;
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.Default;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.WebGL.initialMemorySize = 64;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.WebGL.showDiagnostics = false;
#if WEBGL_PRODCTION
                PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
#else
        PlayerSettings.WebGL.showDiagnostics = true;
#endif

#endif
#endif
    }
}