#if UNITY_ANDROID
namespace BuildHelper.Workflows
{
    using System.IO;
    using System.IO.Compression;
    using UnityEditor;
    using UnityEditor.Build.Reporting;

    public class ExportAndroidPlatForm : BuildAndroidPlatForm
    {
        protected override BuildReport ExecuteBuild(BuildPlayerOptions options)
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            options.options                                      = BuildOptions.AcceptExternalModificationsToPlayer;

            if (options.locationPathName.EndsWith(".apk") || options.locationPathName.EndsWith(".aab"))
            {
                options.locationPathName = options.locationPathName.Replace(".apk", "")
                    .Replace(".aab", "");
            }

            CommonServicesHelper.LogMessage($"[ExportAndroidPlatForm] Exporting Android Studio project to: {options.locationPathName}");

            var report = BuildPipeline.BuildPlayer(options);
            BuildCmd.WriteReport(report);

            CommonServicesHelper.LogMessage("[ExportAndroidPlatForm] Export complete!");
            string exportFolder = options.locationPathName;
            string zipPath      = exportFolder + ".zip";

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(exportFolder, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            CommonServicesHelper.LogMessage($"[ExportAndroidPlatForm] Zip created: {zipPath}");

            return report;
        }
    }
}
#endif