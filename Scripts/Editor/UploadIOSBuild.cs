using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public static class UploadIOSBuild
{
    public static void UploadTestFlight()
    {
        var data          = CommonServices.GetDataModel<BuildIosInformation>(CommonServices.GetPathBuildInformation("IosInformation.json"));
        var ipaFolderPath = Path.GetFullPath($"../Build/Client/ios/{data.iosInformation.outputFileName}/{data.iosInformation.outputFileName}.ipa");
        var appleId       = data.iosInformation.accountAppleId;
        var appPassword   = data.iosInformation.accountPassword;
        var teamId        = data.iosInformation.signingTeamId;

        var ipaPath = $"{FindIpaFileInFolder(ipaFolderPath)}";
        var command = $"/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter -m upload -f \"{ipaFolderPath}\" -u \"{appleId}\" -p \"{appPassword}\" -itc_provider \"{teamId}\"";

        RunCommand(command);
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