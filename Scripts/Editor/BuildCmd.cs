namespace BuildHelper.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
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

        public static void SetBlueprintDataPath()
        {
#if UNITY_ANDROID
            var buildAndroidPlatForm = new BuildAndroidPlatForm();
            var data                 = new BuildAndroidInformation();
            buildAndroidPlatForm.SetupBlueprintPath(data);
#endif
        }

        public static async void BuildAndroidOnEditor()
        {
#if UNITY_ANDROID
            var jsonfile = $"{CommonServicesHelper.GetProjectPath()}/Packages/BuildHelper/SampleConfig/AndroidInformation.json";
            var data     = CommonServicesHelper.GetDataModel<BuildAndroidInformation>(jsonfile);
            data.data.scriptDefinition = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            var buildAndroidPlatForm = new BuildAndroidPlatForm();
            await buildAndroidPlatForm.SetUpAndBuild(data);

            OnAfterExecute(Application.isBatchMode, () =>
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
#endif
        }

        public static void ExportDataPath()
        {
            var path = "";
            path += $"{Application.dataPath}\n";
            path += $"{Application.persistentDataPath}\n";

            var buildPath  = CommonServicesHelper.GetBuildPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parent     = Path.GetDirectoryName(buildPath);
            var configPath = Path.Combine(parent, "Configs");

            File.WriteAllTextAsync($"{configPath}/DataPath.txt", path);
        }

        public static void SwitchPlatform()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();
            var pathAndroid = CommonServicesHelper.GetPathInformation("AndroidInformation.json");
            var pathIos     = CommonServicesHelper.GetPathInformation("IosInformation.json");
            var pathWebGl   = CommonServicesHelper.GetPathInformation("WebGlInformation.json");

            if (File.Exists(pathAndroid))
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }
            }
            else if (File.Exists(pathIos))
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                }
            }
            else if (File.Exists(pathWebGl))
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                }
            }

            if (isBatchMode)
            {
                EditorApplication.Exit(0);
            }
            else
            {
                CommonServicesHelper.LogMessage("Switched platform successfully.");
            }
        }

        public static async void ExportAndroidProject()
        {
#if UNITY_ANDROID
            var data = CommonServicesHelper.GetDataModel<BuildAndroidInformation>(CommonServicesHelper.GetPathInformation("AndroidInformation.json"));
            // var jsonfile    = $"{CommonServicesHelper.GetProjectPath()}/Packages/BuildHelper/SampleConfig/AndroidInformation.json";
            // var data        = CommonServicesHelper.GetDataModel<BuildAndroidInformation>(jsonfile);
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            if (data == null)
            {
                CommonServicesHelper.LogMessage("No data model found");

                throw new Exception("No data model found");
            }

            try
            {
                await TryRunSyncDataBatchModeAsync();
                var buildAndroidPlatForm = new ExportAndroidPlatForm();
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
                CommonServicesHelper.LogMessage(e);

                throw;
            }
#endif
        }

        public static void TryRunSyncData() { _ = TryRunSyncDataBatchModeAsync(); }

        static async Task TryRunSyncDataBatchModeAsync()
        {
            ExportDataPath();
            CommonServicesHelper.LogMessage($"Auto Sync Data enable, disable add : DISABLE_AUTO_SYNC_DATA");
#if DISABLE_AUTO_SYNC_DATA
        return;
#endif
            var type = Type.GetType("SyncDataBatchMode, SyncGoogle.Editor");

            if (type == null)
            {
                CommonServicesHelper.LogMessage("⚠ SyncDataBatchMode class not found.");

                return;
            }

            var method = type.GetMethod("SyncGoogleDriver", BindingFlags.Public | BindingFlags.Static);

            if (method == null)
            {
                CommonServicesHelper.LogMessage("⚠ SyncGoogleDriver method not found.");

                return;
            }

            if (method.Invoke(null, null) is Task task)
            {
                await task;
            }

            CommonServicesHelper.LogMessage("✅ Sync data completed successfully.");
            await TryToRemoveSyncDataBatchModeClassAsync();
        }

        public static Task TryToRemoveSyncDataBatchModeClassAsync()
        {
#if PRODUCTION||!BLUEPRINT_ONLINE
            var guids = AssetDatabase.FindAssets("SyncGoogleDriver");

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (File.Exists(assetPath))
                {
                    CommonServicesHelper.LogMessage($"Deleting: {assetPath}");

                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            AssetDatabase.Refresh();

            CommonServicesHelper.LogMessage("Delete Done.");
#endif
            return Task.CompletedTask;
        }

        public static async void BuildAndroid()
        {
#if UNITY_ANDROID
            var data        = CommonServicesHelper.GetDataModel<BuildAndroidInformation>(CommonServicesHelper.GetPathInformation("AndroidInformation.json"));
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            if (data == null)
            {
                CommonServicesHelper.LogMessage("No data model found");

                throw new Exception("No data model found");
            }

            try
            {
                await TryRunSyncDataBatchModeAsync();
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
                CommonServicesHelper.LogMessage(e);

                throw;
            }
#endif
        }

        public static async void BuildIos()
        {
            var data        = CommonServicesHelper.GetDataModel<BuildIosInformation>(CommonServicesHelper.GetPathInformation("IosInformation.json"));
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            if (data == null)
            {
                CommonServicesHelper.LogMessage("No data model found");

                throw new Exception("No data model found");
            }
#if UNITY_IOS
        try
        {
            await TryRunSyncDataBatchModeAsync();
            var buildIosPlatForm = new BuildIosPlatForm();
            buildIosPlatForm.SetUpAndBuild(data);

            OnAfterExecute(isBatchMode);
        }
        catch (Exception e)
        {
            CommonServicesHelper.LogMessage(e);

            throw;
        }
#endif
        }

        public static async void BuildWebGL()
        {
            var data        = CommonServicesHelper.GetDataModel<BuildWebGlInformation>(CommonServicesHelper.GetPathInformation("WebGlInformation.json"));
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            if (data == null)
            {
                CommonServicesHelper.LogMessage("No data model found");

                throw new Exception("No data model found");
            }

            try
            {
#if UNITY_WEBGL
                await TryRunSyncDataBatchModeAsync();
                var buildWebGlPlatForm = new BuildWebGlPlatForm();

                buildWebGlPlatForm.SetUpAndBuild(data);
                CheckToActiveFireBaseData();
                OnAfterExecute(isBatchMode);
#endif
            }
            catch (Exception e)
            {
                CommonServicesHelper.LogMessage(e);

                throw;
            }
        }

        public static void CheckToActiveFireBaseData()
        {
#if !FIREBASE_WEBGL
            return;
#endif
            var data = CommonServicesHelper.GetDataModel<BuildWebGlInformation>(CommonServicesHelper.GetPathInformation("WebGlInformation.json"));

            var firebaseConfigPath = $"{Application.dataPath}/FirebaseWebglConfig.txt";

            if (!File.Exists(firebaseConfigPath))
            {
                return;
            }

            var firebaseContent = File.ReadAllText(firebaseConfigPath);

            var buildPath = CommonServicesHelper.GetBuildPath(
                data.data.outputFileName,
                "webgl");

            var indexHtmlPath = $"{buildPath}/index.html";

            if (!File.Exists(indexHtmlPath))
            {
                return;
            }

            var indexHtmlContent = File.ReadAllText(indexHtmlPath);

            var pattern = @"const\s+firebaseConfig\s*=\s*\{[\s\S]*?\};";

            var match = Regex.Match(indexHtmlContent, pattern);

            if (!match.Success)
            {
                CommonServicesHelper.LogMessage("Firebase config not found.");

                return;
            }

            indexHtmlContent = Regex.Replace(indexHtmlContent, pattern, _ => firebaseContent, RegexOptions.Multiline);

            File.WriteAllText(indexHtmlPath, indexHtmlContent);
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

        public static void UploadTestFlight()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            UploadIOSBuild.UploadTestFlight();

            OnAfterExecute(isBatchMode);
        }

        public static void UploadAAbToGooglePlay()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();
#if UNITY_ANDROID
            UploadAABToGooglePlay.UploadAAb();
#endif

            OnAfterExecute(isBatchMode);
        }

        public static async void BlueprintWorkFlowAndroid()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();
#if ADDRESSABLE && BLUEPRINT_WORKFLOW
            await new BlueprintWorkFlowAndroid().ProcessBlueprint();
#endif
            OnAfterExecute(isBatchMode);
        }

        public static async void BlueprintWorkFlowIos()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();
#if ADDRESSABLE && BLUEPRINT_WORKFLOW
            await new BlueprintWorkFlowIos().ProcessBlueprint();
#endif
            OnAfterExecute(isBatchMode);
        }

        public static async void BlueprintWorkFlowWebgl()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();
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
}