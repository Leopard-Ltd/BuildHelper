using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using UnityEngine;

public static class CommonServices
{
    public static async Task<DriveService> GetDriveServices(bool isServicesAccount = true) { return isServicesAccount ? await GetService() : await GetDriveServicesWithCredential(); }

    public static async Task<DriveService> GetDriveServicesWithCredential()
    {
        var      tokenPath = $"{Application.persistentDataPath}/token";
        string[] scopes    = { DriveService.Scope.Drive };
        var      stream    = new FileStream(GetPathInformation("servicesAccount.json"), FileMode.Open, FileAccess.Read);

        // Request authorization
        var cr = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            (await GoogleClientSecrets.FromStreamAsync(stream)).Secrets,
            scopes,
            "user",
            CancellationToken.None,
            new FileDataStore(tokenPath, true)
        );

        var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = cr
        });

        return service;
    }

    public static void CheckToClearToken(BaseBuildData data)
    {
        if (data.IsUseServicesAccount())
        {
            return;
        }

        if (!data.clearCachedCredentials)
            return;

        var tokenPath = $"{Application.persistentDataPath}/token";

        if (Directory.Exists(tokenPath))
        {
            Directory.Delete(tokenPath, true);
        }
    }

    public static Task<DriveService> GetService(string servicesAccountFileName = "servicesAccount.json")
    {
        GoogleCredential credential;

        using (var stream = new FileStream(GetPathInformation(servicesAccountFileName), FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(DriveService.Scope.Drive);
        }

        return Task.FromResult(new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName       = "JenkinsBuild",
        }));
    }

    public static string GetPathInformation(string fileName)
    {
        var buildPath = GetBuildPath();
        var filePath  = buildPath.Replace("Build", "Configs");
        filePath = $"{filePath}/{fileName}";

        return filePath;
    }

    public static T GetDataModel<T>(string filePath) where T : class
    {
        T data = null;

        var fileContents = File.ReadAllText(filePath, Encoding.UTF8);
        fileContents = fileContents.Replace("\n", "").Replace("\r", "");

        data = JsonUtility.FromJson<T>(fileContents);

        return data;
    }

    public static bool IsBatchMode()
    {
        var args        = Environment.GetCommandLineArgs().ToList();
        var isBatchMode = args.Contains("-batchmode");
        //LogMessage($"Command Line Ne {string.Join(",", args)}, {isBatchMode}");

        return isBatchMode;
    }

    public static void LogMessage(object message)
    {
        Debug.Log(message);
        Console.WriteLine(message);
    }

    public static bool StringIsNullOrEmpty(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return true;
        }

        return false;
    }
    public static string GetBuildPath(string outputFileName, string platform = "Android") { return $"{GetBuildPath()}/Client/{platform}/{outputFileName}"; }

    public static string FindFileInFolder(string pathFolder, string searchPattern = "*.ipa")
    {
        if (!Directory.Exists(pathFolder))
        {
            LogMessage($"Folder '{pathFolder}' does not exist.");

            return string.Empty;
        }

        try
        {
            var files = Directory.GetFiles(pathFolder, searchPattern, SearchOption.TopDirectoryOnly);

            if (files.Length > 0)
            {
                var foundFile = files[0];
                LogMessage($"Found file '{foundFile}' with pattern '{searchPattern}' in '{pathFolder}'.");

                return foundFile;
            }

            LogMessage($"Cannot find any file with pattern '{searchPattern}' in '{pathFolder}'.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error while searching files: {ex.Message}");

            throw;
        }

        return string.Empty;
    }

    public static string GetFinalAndroidBuildVersion()
    {
        var version = System.IO.File.ReadAllText(GetPathInformation("buildversion.txt"));

        return version;
    }

    /// <summary>
    /// Directory containt many project
    /// </summary>
    /// <returns></returns>
    public static string GetRootPath()
    {
        var path      = Application.dataPath;
        var rootPath  = path.Replace("Assets", "");
        var tmp       = rootPath.Split("/");
        var buildPath = "";

        for (var i = 0; i < tmp.Length - 2; i++)
        {
            buildPath += tmp[i] + "/";
        }

        return buildPath;
    }

    /// <summary>
    /// Directory contain Build
    /// </summary>
    /// <returns></returns>
    public static string GetBuildPath()
    {
        var path  = Application.dataPath;
        var tmp   = path.Split('/');
        var final = "";
        var count = 2;

        if (ValidBuildPath())
        {
            count = 1;
        }

        for (var i = 0; i < tmp.Length - count; i++)
        {
            final += tmp[i] + "/";
        }

        return $"{final}Build/";
    }

    static bool ValidBuildPath(string keyword = "JenkinsFiles")
    {
        var projectPath = GetProjectPath();

        var matchingFolders = Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories)
            .Where(folder => Path.GetFileName(folder).ToLower().IndexOf(keyword.ToLower(), StringComparison.OrdinalIgnoreCase) >= 0);

        return matchingFolders.Any();
    }

    /// <summary>
    /// Contain Assets Folder
    /// </summary>
    /// <returns></returns>
    public static string GetProjectPath() { return Path.GetFullPath(Application.dataPath + "/.."); }
}