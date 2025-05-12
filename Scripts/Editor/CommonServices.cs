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

public class CommonServices
{
    public static async Task<DriveService> GetDriveServices(bool isServicesAccount = true) { return isServicesAccount ? await GetService() : await GetDriveServicesWithCredential(); }

    public static async Task<DriveService> GetDriveServicesWithCredential()
    {
        var      tokenPath   = $"{Application.persistentDataPath}/token";
        string[] scopes      = { DriveService.Scope.Drive };
        var      credentials = Application.dataPath;
        credentials = credentials.Replace("Assets", "");
        credentials = $"{credentials}/servicesAccount.json";
        var stream = new FileStream(credentials, FileMode.Open, FileAccess.Read);

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

    public static Task<DriveService> GetService(string servicesAccountFileName = "servicesAccount.json")
    {
        var servicesAccountPath = Application.dataPath;
        servicesAccountPath = servicesAccountPath.Replace("Assets", "");
        servicesAccountPath = $"{servicesAccountPath}/{servicesAccountFileName}";
        GoogleCredential credential;

        using (var stream = new FileStream(servicesAccountPath, FileMode.Open, FileAccess.Read))
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

    public static string GetPathBuildInformation(string fileName)
    {
        var filePath = Application.dataPath;
        filePath = filePath.Replace("/Assets", "");
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

    public static void LogMessage(string message)
    {
        Debug.Log(message);
        Console.WriteLine(message);
    }

    public static string GetBuildPath(string outputFileName) { return Path.GetFullPath($"../Build/Client/Android/{outputFileName}"); }

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

    public static string GetRootPath()
    {
        var path           = Application.dataPath;
        var rootPath       = path.Replace("Assets", "");
        var tmp            = rootPath.Split("/");
        var buildPath      = "";
     

        for (var i = 0; i < tmp.Length - 2; i++)
        {
            buildPath += tmp[i] + "/";
        }

        return buildPath;
    }

    public static string GetProjectPath()
    {
       return Path.GetFullPath(Application.dataPath + "/..");
    }
}