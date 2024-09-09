using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using UnityEditor;
using UnityEngine;
using File = Google.Apis.Drive.v3.Data.File;

public class GoogleDriverServices
{
    static List<Task> listTask = new List<Task>();
    static string     ApkFile  = "application/vnd.android.package-archive";
    static string     ZipFile  = "application/zip";

    [MenuItem("Build/UploadFile")]
    static void TestUpload() { RunNow(); }

    static async Task RunNow()
    {
        // CreateFolder("testFolder", "1nteHm_RihLOJZ0IsgBfshHkuqiGIaxxN", service);
    }

    static async Task DeleteAllFromServicesAccount()
    {
        var servicesAccountModel = CommonServices.GetDataModel<ServicesAccountModel>(CommonServices.GetPathBuildInformation("servicesAccount.json"));
        var service              = await CommonServices.GetDriveServices();
        var listRequest          = service.Files.List();
        listRequest.Fields = "nextPageToken, files(id, name, owners)";
        var files = await listRequest.ExecuteAsync();

        var serviceEmail = servicesAccountModel.client_email;

        foreach (var file in files.Files)
        {
            var isOwner = file.Owners.Any(owner => owner.EmailAddress == serviceEmail);

            if (isOwner)
            {
                Debug.Log($"Deleting file: {file.Name} ({file.Id})");

                var deleteRequest = service.Files.Delete(file.Id);
                await deleteRequest.ExecuteAsync();

                Debug.Log("Deleted.");
            }
            else
            {
                Debug.Log($"File {file.Name} ({file.Id}) is not owned by the service account and will not be deleted.");
            }
        }
    }

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
        var isBatchMode = CommonServices.IsBatchMode();
        var path        = Application.dataPath;
        var webglModel  = CommonServices.GetDataModel<BuildWebGlInformation>(CommonServices.GetPathBuildInformation("WebGlInformation.json"));
        var zipFilePath = $"{GetBuildFilePath()}Client/webgl/{webglModel.webGlInformation.outputFileName}.zip";
        var service     = await CommonServices.GetDriveServices(webglModel.webGlInformation.IsUseServicesAccount());
        var uploadInfo  = path.Replace("Assets", "");
        //read from file
        var folderId          = System.IO.File.ReadAllText($"{uploadInfo}/uploadInfo.txt");
        var environmentFolder = await CreateFolder(webglModel.webGlInformation.buildEnvironment, folderId, service);
        var platFormFolder    = await CreateFolder("webgl", environmentFolder, service);
        var zipFile           = "";
        await UploadFileInternal(zipFilePath, platFormFolder, service, ZipFile, (x) => { zipFile = x; });
        var googleLinkPath = path.Replace("Assets", "");
        googleLinkPath = $"{googleLinkPath}googleInfo.txt";
        System.IO.File.WriteAllText(googleLinkPath, zipFile);

        if (isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    static async void UploadAndroidPlatform()
    {
        var isBatchMode             = CommonServices.IsBatchMode();
        var buildAndroidInformation = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathBuildInformation("AndroidInformation.json"));

        var path             = Application.dataPath;
        var internalFilePath = $"{GetBuildFilePath()}Client/Android/{buildAndroidInformation.androidInformation.outputFileName}";

        var apkFilePath = $"{internalFilePath}.apk";
        var aabFilePath = $"{internalFilePath}.aab";
        var zipFilePath = $"{internalFilePath}-{buildAndroidInformation.androidInformation.customVersion.version}-v{buildAndroidInformation.androidInformation.buildNumber}-IL2CPP.symbols.zip";

        if (!System.IO.File.Exists(apkFilePath))
        {
            throw new Exception("Apk File not found");
        }

        if (!System.IO.File.Exists(aabFilePath) && buildAndroidInformation.androidInformation.BuildAppBundle())
        {
            throw new Exception("Aab File not found");
        }

        var service = await CommonServices.GetDriveServices(buildAndroidInformation.androidInformation.IsUseServicesAccount());

        var uploadInfo = path.Replace("Assets", "");
        //read from file
        var folderId = System.IO.File.ReadAllText($"{uploadInfo}/uploadInfo.txt");
        //BuildEnvironment
        var environmentFolder = await CreateFolder(buildAndroidInformation.androidInformation.buildEnvironment, folderId, service);

        //create platform folder
        var platFormFolder = await CreateFolder("Android", environmentFolder, service);

        //Create VersionFolder
        var versionFolder = await CreateFolder($"{buildAndroidInformation.androidInformation.outputFileName}", platFormFolder, service);

        listTask = new List<Task>();
        var urlApk  = "";
        var urlAab  = "";
        var zipFile = "";
        listTask.Add(UploadFileInternal(apkFilePath, versionFolder, service, ApkFile, (x) => { urlApk = x; }));

        if (buildAndroidInformation.androidInformation.BuildAppBundle())
        {
            listTask.Add(UploadFileInternal(aabFilePath, versionFolder, service, ApkFile, (x) => { urlAab  = x; }));
            listTask.Add(UploadFileInternal(zipFilePath, versionFolder, service, ZipFile, (x) => { zipFile = x; }));
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
        System.IO.File.WriteAllText(googleLinkPath, string.Join(",", list));

        if (isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    static async Task<string> CreateFolder(string folderName, string parentFolder, DriveService service)
    {
        var folderToDelete = FindFolder(service, parentFolder, folderName);

        if (folderToDelete != null)
        {
            return folderToDelete.Id;
        }

        var fileMetadata = new File()
        {
            Name     = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents  = new List<string>() { parentFolder }
        };

        var request = service.Files.Create(fileMetadata);
        request.SupportsAllDrives = true;
        request.Fields            = "id";
        var file = await request.ExecuteAsync();
        Console.WriteLine("Folder ID: " + file.Id);
        // await ShareWriter(service, file.Id, OwnerPermission.Keys.First());

        return file.Id;
    }

    private static async Task ShareWriter(DriveService service, string folderId, string userEmail)
    {
        var permission = new Permission
        {
            Role         = "writer",
            Type         = "user",
            EmailAddress = userEmail,
        };

        var request = service.Permissions.Create(permission, folderId);
        request.SupportsAllDrives = true;

        await request.ExecuteAsync();
        Console.WriteLine($"Ownership transferred to {userEmail}.");
    }

    static File FindFolder(DriveService service, string parentFolder, string folderName)
    {
        // Define parameters for the Files.List request
        var listRequest = service.Files.List();
        listRequest.Q                         = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and '{parentFolder}' in parents and trashed = false";
        listRequest.PageSize                  = 100;
        listRequest.Fields                    = "nextPageToken, files(id, name)";
        listRequest.SupportsAllDrives         = true;
        listRequest.IncludeItemsFromAllDrives = true;
        // Execute the request and get the list of files
        IList<File> files = listRequest.Execute().Files;

        // Check if there's exactly one match
        return files.FirstOrDefault();
    }

    static void DeleteFolder(DriveService service, string folderId)
    {
        // Delete the folder
        service.Files.Delete(folderId).Execute();
    }

    private static async Task UploadFileInternal(string apkFilePath, string folderId, DriveService service, string contentType, Action<string> onComplete)
    {
        var fileMetadata = new File()
        {
            Name    = Path.GetFileName(apkFilePath),
            Parents = new List<string> { folderId }
        };

        FilesResource.CreateMediaUpload request;

        var stream = new FileStream(apkFilePath, FileMode.Open);
        request = service.Files.Create(fileMetadata, stream, contentType);

        request.Fields            = "id";
        request.SupportsAllDrives = true;
        var progress = await request.UploadAsync();
        var file     = request.ResponseBody;

        switch (progress.Status)
        {
            case UploadStatus.Failed:
                Console.WriteLine($"Upload failed {progress.Exception}");

                throw progress.Exception;
            case UploadStatus.Completed:
                var fileId = request.ResponseBody.Id;
                Console.WriteLine($"File uploaded successfully. File ID: {fileId}");

                break;
        }

        var urlFile = $"https://drive.google.com/uc?export=download&id={file.Id}";
        onComplete(urlFile);
    }
}