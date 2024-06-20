using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using UnityEditor;
using UnityEngine;

public class GoogleDriverServices
{
    static List<Task> listTask = new List<Task>();
    static string     ApkFile  = "application/vnd.android.package-archive";
    static string     ZipFile  = "application/zip";

    [MenuItem("Build/UploadFile")]
    static void TestUpload() { UploadAndroidPlatform(); }

    static string GetBuildFilePath()
    {
        var path  = Application.dataPath;
        var tmp   = path.Split('/');
        var final = "";

        for (int i = 0; i < tmp.Length - 2; i++)
        {
            final += tmp[i] + "/";
        }

        return $"{final}Build/";
    }

    static async void UploadWebGlPlatForm()
    {
        var path             = Application.dataPath;
        var webGlInformation = CommonServices.GetDataModel<BuildWebGlInformation>(CommonServices.GetPathBuildInformation("WebGlInformation.json"));
        var zipFilePath      = $"{GetBuildFilePath()}Client/webgl/{webGlInformation.webGlInformation.outputFileName}.zip";
        var service          = GetService();
        var uploadInfo       = path.Replace("Assets", "");
        //read from file
        var folderId       = File.ReadAllText($"{uploadInfo}/uploadInfo.txt");
        var platFormFolder = CreateFolder("webgl", folderId, service);
        var zipFile        = "";
        await UploadFileInternal(zipFilePath, platFormFolder, service, ZipFile, out zipFile);
        var googleLinkPath = path.Replace("Assets", "");
        googleLinkPath = $"{googleLinkPath}googleInfo.txt";
        File.WriteAllText(googleLinkPath, zipFile);
    }

    static async void UploadAndroidPlatform()
    {
        var buildAndroidInformation = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathBuildInformation("AndroidInformation.json"));

        var path             = Application.dataPath;
        var internalFilePath = $"{GetBuildFilePath()}Client/Android/{buildAndroidInformation.androidInformation.outputFileName}";

        var apkFilePath = $"{internalFilePath}.apk";
        var aabFilePath = $"{internalFilePath}.aab";
        var zipFilePath = $"{internalFilePath}-{buildAndroidInformation.androidInformation.customVersion.version}-v{buildAndroidInformation.androidInformation.buildNumber}-IL2CPP.symbols.zip";

        if (!File.Exists(apkFilePath))
        {
            throw new Exception("Apk File not found");
        }

        if (!File.Exists(aabFilePath) && buildAndroidInformation.androidInformation.BuildAppBundle())
        {
            throw new Exception("Aab File not found");
        }

        var service = GetService();

        var uploadInfo = path.Replace("Assets", "");
        //read from file
        var folderId = File.ReadAllText($"{uploadInfo}/uploadInfo.txt");

        //create platform folder
        var platFormFolder = CreateFolder("Android", folderId, service);

        //Create VersionFolder
        var versionFolder = CreateFolder($"{buildAndroidInformation.androidInformation.outputFileName}", platFormFolder, service);

        listTask = new List<Task>();
        var urlApk  = "";
        var urlAab  = "";
        var zipFile = "";
        listTask.Add(UploadFileInternal(apkFilePath, versionFolder, service, ApkFile, out urlApk));

        if (buildAndroidInformation.androidInformation.BuildAppBundle())
        {
            listTask.Add(UploadFileInternal(aabFilePath, versionFolder, service, ApkFile, out urlAab));
            listTask.Add(UploadFileInternal(zipFilePath, versionFolder, service, ZipFile, out zipFile));
        }

        await Task.WhenAll(listTask);
        listTask.Clear();
        var list = new List<string>();

        if (!string.IsNullOrEmpty(urlApk))
        {
            list.Add(urlApk);
        }

        if (!string.IsNullOrEmpty(urlAab))
        {
            list.Add(urlAab);
        }

        if (!string.IsNullOrEmpty(zipFile)) list.Add(zipFile);

        var googleLinkPath = path.Replace("Assets", "");
        googleLinkPath = $"{googleLinkPath}googleInfo.txt";
        File.WriteAllText(googleLinkPath, string.Join(",", list));
    }

    static string CreateFolder(string folderName, string parentFolder, DriveService service)
    {
        var folderToDelete = FindFolder(service, folderName);

        if (folderToDelete != null)
        {
            return folderToDelete.Id;
        }

        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name     = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents  = new List<string>() { parentFolder }
        };

        var request = service.Files.Create(fileMetadata);
        request.Fields = "id";
        var file = request.Execute();
        Console.WriteLine("Folder ID: " + file.Id);

        return file.Id;
    }

    static Google.Apis.Drive.v3.Data.File FindFolder(DriveService service, string folderName)
    {
        // Define parameters for the Files.List request
        var listRequest = service.Files.List();
        listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";

        // Execute the request and get the list of files
        IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

        // Check if there's exactly one match
        return files is { Count: 1 } ? files.First() : null;
    }

    static void DeleteFolder(DriveService service, string folderId)
    {
        // Delete the folder
        service.Files.Delete(folderId).Execute();
    }

    private static Task UploadFileInternal(string apkFilePath, string folderId, DriveService service, string contentType, out string driverLink)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name    = Path.GetFileName(apkFilePath),
            Parents = new List<string> { folderId }
        };

        FilesResource.CreateMediaUpload request;

        using (var stream = new FileStream(apkFilePath, FileMode.Open))
        {
            request        = service.Files.Create(fileMetadata, stream, contentType);
            request.Fields = "id";
            request.Upload();
        }

        var file    = request.ResponseBody;
        var urlFile = $"https://drive.google.com/file/d/{file.Id}/view?usp=drive_link";
        driverLink = urlFile;

        return Task.CompletedTask;
    }

    static DriveService GetService()
    {
        var servicesAccountPath = Application.dataPath;
        servicesAccountPath = servicesAccountPath.Replace("Assets", "");
        servicesAccountPath = $"{servicesAccountPath}/servicesAccount.json";
        GoogleCredential credential;

        using (var stream = new FileStream(servicesAccountPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(DriveService.Scope.Drive);
        }

        return new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName       = "JenkinsBuild",
        });
    }
}