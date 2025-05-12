using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class UploadIOSBuild
{
    public static void UploadTestFlight()
    {
        MoveArchive();

        return;

        var buildIosInformation = CommonServices.GetDataModel<BuildIosInformation>(
            CommonServices.GetPathBuildInformation("IosInformation.json"));

        var buildPath = $"{CommonServices.GetRootPath()}";

        var projectPath = CommonServices.GetProjectPath();
        var ipaFolder   = $"{buildPath}Build/Client/ios/{buildIosInformation.iosInformation.outputFileName}/{buildIosInformation.iosInformation.outputFileName}.ipa/";
        var ipaFilePath = CommonServices.FindFileInFolder(ipaFolder);
        UploadToAppStore(buildIosInformation.iosInformation.fastLanePath, projectPath, ipaFilePath, buildIosInformation);
    }

    static void UploadToAppStore(string fastLanePath, string projectPath, string ipaPath, BuildIosInformation data)
    {
        var fastlaneDir = Path.Combine(projectPath, "fastlane");

        if (Directory.Exists(fastlaneDir))
            Directory.Delete(fastlaneDir, true);

        Directory.CreateDirectory(fastlaneDir);

        // Ghi Appfile
        File.WriteAllText(Path.Combine(fastlaneDir, "Appfile"), $@"
        app_identifier('{data.iosInformation.bundleIdentifier}')
        ");

        // Ghi Fastfile
        File.WriteAllText(Path.Combine(fastlaneDir, "Fastfile"), $@"
        default_platform(:ios)

        platform :ios do
          desc 'Upload to TestFlight using FASTLANE_SESSION'
          lane :upload do
            pilot(
              ipa: '{ipaPath}',
              skip_submission: true,
              skip_waiting_for_build_processing: true
            )
          end
        end
        ");

        RunCommand(fastLanePath, "upload", projectPath, data.iosInformation.fastLaneSession, data.iosInformation.accountAppleId);
    }

    private static void RunCommand(string command, string args, string workingDir, string fastLaneSession, string appleId)
    {
        var process = new Process();
        process.StartInfo.FileName               = command;
        process.StartInfo.Arguments              = args;
        process.StartInfo.WorkingDirectory       = workingDir;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError  = true;
        process.StartInfo.UseShellExecute        = false;
        process.StartInfo.CreateNoWindow         = true;

        process.StartInfo.EnvironmentVariables["FASTLANE_SESSION"] = fastLaneSession;
        process.StartInfo.EnvironmentVariables["FASTLANE_USER"]    = appleId;
        process.StartInfo.EnvironmentVariables["LANG"]             = "en_US.UTF-8";
        process.StartInfo.EnvironmentVariables["LC_ALL"]           = "en_US.UTF-8";

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                CommonServices.LogMessage("[OUT] " + e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                CommonServices.LogMessage("[ERR] " + e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            CommonServices.LogMessage($"Lỗi khi chạy fastlane: {ex.Message}");
        }
    }

    static void MoveArchive()
    {
        var data = CommonServices.GetDataModel<BuildIosInformation>(
            CommonServices.GetPathBuildInformation("IosInformation.json"));

        var userHome          = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var archivePath       = Path.Combine(userHome, "Library", "Developer", "Xcode", "Archives");
        var currentDateTime   = DateTime.Now.ToString("yyyy-MM-dd");
        var destinationFolder = Path.Combine(archivePath, currentDateTime);

        var path         = Application.dataPath.Replace("Assets", string.Empty).TrimEnd('/');
        var parentPath   = Path.GetDirectoryName(path);
        var sourceFolder = Path.Combine(parentPath, "Build/Client/ios", data.iosInformation.outputFileName, $"{data.iosInformation.outputFileName}.xcarchive");

        if (!Directory.Exists(sourceFolder))
        {
            CommonServices.LogMessage("❌ Lỗi: Thư mục nguồn không tồn tại - " + sourceFolder);

            return;
        }

        if (!Directory.Exists(destinationFolder))
        {
            try
            {
                Directory.CreateDirectory(destinationFolder);
            }
            catch (Exception ex)
            {
                CommonServices.LogMessage("❌ Lỗi tạo thư mục: " + ex.Message);

                return;
            }
        }

        var timestamp       = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var newArchiveName  = $"{data.iosInformation.outputFileName}_{timestamp}.xcarchive";
        var destinationPath = Path.Combine(destinationFolder, newArchiveName);

        try
        {
            CopyDirectory(sourceFolder, destinationPath);
            CommonServices.LogMessage($"✅ Đã sao chép thành công: {destinationPath}");
        }
        catch (Exception ex)
        {
            CommonServices.LogMessage("❌ Lỗi khi sao chép thư mục: " + ex.Message);

            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = "open",
                Arguments       = "\"" + archivePath + "\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            CommonServices.LogMessage("❌ Lỗi khi mở thư mục: " + ex.Message);
        }
    }

    static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }
}