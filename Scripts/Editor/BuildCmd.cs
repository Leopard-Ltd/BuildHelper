using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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

    [MenuItem("Build/Build Android")]
    static void BuildAndroid()
    {
        var data        = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathBuildInformation("AndroidInformation.json"));
        var isBatchMode = CommonServices.IsBatchMode();

        if (data == null)
        {
            Console.WriteLine("No data model found");

            throw new Exception("No data model found");
        }

        try
        {
            var buildAndroidPlatForm = new BuildAndroidPlatForm();
            buildAndroidPlatForm.SetUpAndBuild(data);

            if (isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            throw;
        }
    }

    static void BuildIOS()
    {
        // var data = GetDataModel();
    }

    [MenuItem("Build/Build WebGl")]
    static void BuildWebGL()
    {
        var data        = CommonServices.GetDataModel<BuildWebGlInformation>(CommonServices.GetPathBuildInformation("WebGlInformation.json"));
        var isBatchMode = CommonServices.IsBatchMode();

        if (data == null)
        {
            Console.WriteLine("No data model found");

            throw new Exception("No data model found");
        }

        try
        {
            var buildWebGlPlatForm = new BuildWebGlPlatForm();

            buildWebGlPlatForm.SetUpAndBuild(data);

            if (isBatchMode)
            {
                EditorApplication.Exit(0);
            }
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

    private static void WriteStep(StreamWriter file, BuildStep step)
    {
        file.WriteLine($"Step {step.name}  Depth: {step.depth} Time: {step.duration}");

        foreach (var message in step.messages)
        {
            file.WriteLine($"{Prefix(message.type)}: {message.content}");
        }

        file.WriteLine();
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