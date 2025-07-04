using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildCmd
{
    private const string PlatformOsx     = "osx-x64";
    private const string PlatformWin64   = "win-x64";
    private const string PlatformWin32   = "win-x86";
    private const string PlatformAndroid = "android";
    private const string PlatformIOS     = "ios";
    private const string PlatformWebGL   = "webgl";

    private class BuildTargetInfo
    {
        public string           Platform; // eg "win-x64"
        public BuildTarget      BuildTarget;
        public BuildTargetGroup BuildTargetGroup;
    }

    [MenuItem("BuildHelper/SetBlueprintPath")]
    static void SetBlueprintDataPath()
    {
#if UNITY_ANDROID

        var buildAndroidPlatForm = new BuildAndroidPlatForm();
        var data                 = new BuildAndroidInformation();
        buildAndroidPlatForm.SetupBlueprintPath(data);
#endif
    }

    [MenuItem("BuildHelper/Build Android from Editor")]
    static void BuildAndroidOnEditor()
    {
#if UNITY_ANDROID
        var buildAndroidPlatForm = new BuildAndroidPlatForm();
        var data                 = new BuildAndroidInformation();
        var scriptDefineSymbol   = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        data.data.scriptDefinition = scriptDefineSymbol;
        data.data.outputFileName   = "output-1.0.0-1";

        buildAndroidPlatForm.SetUpAndBuild(data);
#endif
    }

    [MenuItem("BuildHelper/Build Android")]
    static async void BuildAndroid()
    {
#if UNITY_ANDROID
        var data        = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathInformation("AndroidInformation.json"));
        var isBatchMode = CommonServices.IsBatchMode();

        if (data == null)
        {
            Console.WriteLine("No data model found");

            throw new Exception("No data model found");
        }

        try
        {
            var buildAndroidPlatForm = new BuildAndroidPlatForm();
            await buildAndroidPlatForm.SetUpAndBuild(data);

            OnAfterExecute(isBatchMode, () =>
            {
                var folderPath = Path.GetFullPath($"../Build/Client/Android/");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows
                    Process.Start("explorer.exe", folderPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "open",
                        Arguments       = folderPath,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Linux
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "xdg-open",
                        Arguments       = folderPath,
                        UseShellExecute = true
                    });
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            throw;
        }
#endif
    }

    [MenuItem("BuildHelper/Build Ios")]
    static void BuildIos()
    {
        var data        = CommonServices.GetDataModel<BuildIosInformation>(CommonServices.GetPathInformation("IosInformation.json"));
        var isBatchMode = CommonServices.IsBatchMode();

        if (data == null)
        {
            Console.WriteLine("No data model found");

            throw new Exception("No data model found");
        }

        try
        {
            var buildIosPlatForm = new BuildIosPlatForm();
            buildIosPlatForm.SetUpAndBuild(data);

            OnAfterExecute(isBatchMode);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            throw;
        }
    }

    [MenuItem("BuildHelper/Build WebGl")]
    static void BuildWebGL()
    {
        var data        = CommonServices.GetDataModel<BuildWebGlInformation>(CommonServices.GetPathInformation("WebGlInformation.json"));
        var isBatchMode = CommonServices.IsBatchMode();

        if (data == null)
        {
            Console.WriteLine("No data model found");

            throw new Exception("No data model found");
        }

        try
        {
#if UNITY_WEBGL
            var buildWebGlPlatForm = new BuildWebGlPlatForm();

            buildWebGlPlatForm.SetUpAndBuild(data);

            OnAfterExecute(isBatchMode);
#endif
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            throw;
        }
    }

    private static readonly List<BuildTargetInfo> Targets = new()
    {
        new BuildTargetInfo
        {
            Platform         = PlatformWin32, BuildTarget = BuildTarget.StandaloneWindows,
            BuildTargetGroup = BuildTargetGroup.Standalone
        },
        new BuildTargetInfo
        {
            Platform         = PlatformWin64, BuildTarget = BuildTarget.StandaloneWindows64,
            BuildTargetGroup = BuildTargetGroup.Standalone
        },
        new BuildTargetInfo
        {
            Platform         = PlatformOsx, BuildTarget = BuildTarget.StandaloneOSX,
            BuildTargetGroup = BuildTargetGroup.Standalone
        },
        new BuildTargetInfo
        {
            Platform = PlatformAndroid, BuildTarget = BuildTarget.Android, BuildTargetGroup = BuildTargetGroup.Android
        },
        new BuildTargetInfo
            { Platform = PlatformIOS, BuildTarget = BuildTarget.iOS, BuildTargetGroup = BuildTargetGroup.iOS },
        new BuildTargetInfo
            { Platform = PlatformWebGL, BuildTarget = BuildTarget.WebGL, BuildTargetGroup = BuildTargetGroup.WebGL }
    };

    public static void WriteReport(BuildReport report)
    {
        Directory.CreateDirectory("../Build/Logs");
        var platform = Targets.SingleOrDefault(t => t.BuildTarget == report.summary.platform)?.Platform ?? "unknown";
        var filePath = $"../Build/Logs/Build-Client-Report.{platform}.log";
        var summary  = report.summary;

        using (var file = new StreamWriter(filePath))
        {
            file.WriteLine($"Build {summary.guid} for {summary.platform}.");

            file.WriteLine(
                $"Build began at {summary.buildStartedAt} and ended at {summary.buildEndedAt}. Total {summary.totalTime}.");

            file.WriteLine($"Build options: {summary.options}");
            file.WriteLine($"Build output to: {summary.outputPath}");

            file.WriteLine(
                $"Build result: {summary.result} ({summary.totalWarnings} warnings, {summary.totalErrors} errors).");

            file.WriteLine($"Build size: {summary.totalSize}");

            file.WriteLine();

            foreach (var step in report.steps)
            {
                WriteStep(file, step);
            }

            file.WriteLine();

#if UNITY_2022_1_OR_NEWER
            foreach (var buildFile in report.GetFiles())
#else
            foreach (var buildFile in report.files)
#endif
            {
                file.WriteLine($"Role: {buildFile.role}, Size: {buildFile.size} bytes, Path: {buildFile.path}");
            }

            file.WriteLine();
        }
    }

    [MenuItem("BuildHelper/UploadTestFlight")]
    static void UploadTestFlight()
    {
        var isBatchMode = CommonServices.IsBatchMode();

        UploadIOSBuild.UploadTestFlight();

        OnAfterExecute(isBatchMode);
    }

    [MenuItem("BuildHelper/UploadAAbToGooglePlay")]
    static void UploadAAbToGooglePlay()
    {
        var isBatchMode = CommonServices.IsBatchMode();
#if UNITY_ANDROID

        UploadAABToGooglePlay.UploadAAb();
#endif

        OnAfterExecute(isBatchMode);
    }

    [MenuItem("BuildHelper/ProcessBlueprintAndroid")]
    static async void BlueprintWorkFlowAndroid()
    {
        var isBatchMode = CommonServices.IsBatchMode();
#if ADDRESSABLE && BLUEPRINT_WORKFLOW
        await new BlueprintWorkFlowAndroid().ProcessBlueprint();
#endif
        OnAfterExecute(isBatchMode);
    }

    [MenuItem("BuildHelper/ProcessBlueprintIos")]
    static async void BlueprintWorkFlowIos()
    {
        var isBatchMode = CommonServices.IsBatchMode();
#if ADDRESSABLE && BLUEPRINT_WORKFLOW
        await new BlueprintWorkFlowIos().ProcessBlueprint();
#endif
        OnAfterExecute(isBatchMode);
    }

    [MenuItem("BuildHelper/ProcessBlueprintWegbl")]
    static async void BlueprintWorkFlowWebgl()
    {
        var isBatchMode = CommonServices.IsBatchMode();
#if ADDRESSABLE && BLUEPRINT_WORKFLOW && UNITY_WEBGL
        await new BlueprintWorkFlowWebGl().ProcessBlueprint();
#endif
        OnAfterExecute(isBatchMode);
    }

    private static void WriteStep(StreamWriter file, BuildStep step)
    {
        file.WriteLine($"Step {step.name}  Depth: {step.depth} Time: {step.duration}");

        foreach (var message in step.messages)
        {
            file.WriteLine($"{Prefix(message.type)}: {message.content}");
        }

        file.WriteLine();
    }

    static void OnAfterExecute(bool isBatchMode, Action action = null)
    {
        if (isBatchMode)
        {
            EditorApplication.Exit(0);
        }
        else
        {
            action?.Invoke();
        }
    }

    private static string Prefix(LogType type) =>
        type switch
        {
            LogType.Assert => "A",
            LogType.Error => "E",
            LogType.Exception => "X",
            LogType.Log => "L",
            LogType.Warning => "W",
            _ => "????"
        };
}