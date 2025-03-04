using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class UploadIOSBuild
{
    public static void UploadTestFlight()
    {
        MoveArchive();
        
        // var data          = CommonServices.GetDataModel<BuildIosInformation>(CommonServices.GetPathBuildInformation("IosInformation.json"));
        //var ipaFolderPath = Path.GetFullPath($"../Build/Client/ios/{data.iosInformation.outputFileName}/{data.iosInformation.outputFileName}.ipa");
        // var appleId       = data.iosInformation.accountAppleId;
        //var appPassword   = data.iosInformation.accountPassword;
        // var teamId        = data.iosInformation.signingTeamId;

        //  var ipaPath = $"{FindIpaFileInFolder(ipaFolderPath)}";
        // var command = $"/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter -m upload -f \"{ipaFolderPath}\" -u \"{appleId}\" -p \"{appPassword}\" -itc_provider \"{teamId}\"";

        // RunCommand(command);

       
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
            LogMessage("❌ Lỗi: Thư mục nguồn không tồn tại - " + sourceFolder);

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
                LogMessage("❌ Lỗi tạo thư mục: " + ex.Message);

                return;
            }
        }

        var timestamp       = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var newArchiveName  = $"{data.iosInformation.outputFileName}_{timestamp}.xcarchive";
        var destinationPath = Path.Combine(destinationFolder, newArchiveName);

        try
        {
            CopyDirectory(sourceFolder, destinationPath);
            LogMessage($"✅ Đã sao chép thành công: {destinationPath}");
        }
        catch (Exception ex)
        {
            LogMessage("❌ Lỗi khi sao chép thư mục: " + ex.Message);

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
            LogMessage("❌ Lỗi khi mở thư mục: " + ex.Message);
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

    static string FindIpaFileInFolder(string ipaFolder)
    {
        var result   = "";
        var ipaFiles = Directory.GetFiles(ipaFolder, "*.ipa");

        if (ipaFiles.Length > 0)
        {
            foreach (var file in ipaFiles)
            {
                LogMessage(file);
            }

            result = ipaFiles[0];
        }
        else
        {
            LogMessage("Can not found IpaFile");
        }

        return result;
    }

    static void RunCommand(string command)
    {
        Process process = new Process();
        process.StartInfo.FileName               = "/bin/bash";
        process.StartInfo.Arguments              = $"-c \"{command}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError  = true;
        process.StartInfo.UseShellExecute        = false;
        process.StartInfo.CreateNoWindow         = true;

        process.OutputDataReceived += (sender, args) => LogMessage(args.Data + " ");
        process.ErrorDataReceived  += (sender, args) => LogMessage(args.Data + " ");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        Console.WriteLine("\n[INFO] Upload finished!");
    }

    static void LogMessage(string message)
    {
        Console.WriteLine(message);
        Debug.Log(message);
    }
}