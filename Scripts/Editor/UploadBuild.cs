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

public class UploadBuild
{
    static List<Task> listTask = new List<Task>();
    static string     ApkFile  = "application/vnd.android.package-archive";
    static string     ZipFile  = "application/zip";
    static string     IpaFile  = "application/x-itunes-ipa";

    [MenuItem("BuildHelper/UploadFile")]
    static void TestUpload() { UploadGoogleDriveWebGlPlatForm(); }

    static void RunNow()
    {
        // CreateFolder("testFolder", "1nteHm_RihLOJZ0IsgBfshHkuqiGIaxxN", service);
    }

    static async void DeleteAllFromServicesAccount()
    {
        var servicesAccountModel = CommonServices.GetDataModel<ServicesAccountModel>(CommonServices.GetPathInformation("servicesAccount.json"));
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

    static async void UploadGoogleDriveWebGlPlatForm()
    {
        try
        {
            var isBatchMode = CommonServices.IsBatchMode();
            var webglModel  = CommonServices.GetDataModel<BuildWebGlInformation>(CommonServices.GetPathInformation("WebGlInformation.json"));
            CommonServices.CheckToClearToken(webglModel.data);
            var zipFilePath = $"{CommonServices.GetBuildPath()}Client/webgl/{webglModel.data.outputFileName}.zip";
            var service     = await CommonServices.GetDriveServices(webglModel.data.IsUseServicesAccount());
            //read from file
            var folderId = System.IO.File.ReadAllText($"{CommonServices.GetPathInformation("uploadInfo.txt")}");

            var environmentFolder = await CreateFolder(webglModel.data.buildEnvironment, folderId, service);
            var platFormFolder    = await CreateFolder("webgl", environmentFolder, service);

            var list = new List<string> { $"https://drive.google.com/drive/folders/{folderId}" };

            var zipFile = "";
            await UploadFileInternal(zipFilePath, platFormFolder, service, ZipFile, (x) => { zipFile = x; });
            list.Add(zipFile);

            System.IO.File.WriteAllText(CommonServices.GetPathInformation("googleInfo.txt"), string.Join(",", list));

            if (isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (Exception e)
        {
            //ignore
            throw new Exception(e.Message);
        }
    }

    [MenuItem("BuildHelper/UploadIos")]
    static async void UploadGoogleDriveIosPlatform()
    {
        var isBatchMode = CommonServices.IsBatchMode();

        try
        {
            var buildIosInformation = CommonServices.GetDataModel<BuildIosInformation>(CommonServices.GetPathInformation("IosInformation.json"));
            var service             = await CommonServices.GetDriveServices(buildIosInformation.data.IsUseServicesAccount());
            CommonServices.CheckToClearToken(buildIosInformation.data);
            var outputFileName      = buildIosInformation.data.outputFileName;
            //read from file
            var ipaPath        = $"{CommonServices.GetBuildPath()}/Client/ios/{buildIosInformation.data.outputFileName}/{buildIosInformation.data.outputFileName}.ipa/";
            var ipaFilePath    = CommonServices.FindFileInFolder(ipaPath);
            var folderId       = await System.IO.File.ReadAllTextAsync($"{CommonServices.GetPathInformation("uploadInfo.txt")}");
            var platFormFolder = await CreateFolder("ios", folderId, service);
            var versionFolder  = await CreateFolder($"{outputFileName}-{buildIosInformation.data.buildNumber}", platFormFolder, service);
            //find Ipa file in ipaPath
            var ipaLink = "";
            CommonServices.LogMessage($"Start upload");
            await UploadFileInternal(ipaFilePath, versionFolder, service, IpaFile, (x) => { ipaLink = x; });

            var list = new List<string> { $"https://drive.google.com/drive/folders/{folderId}" };

            if (!string.IsNullOrEmpty(ipaLink))
            {
                list.Add(ipaLink);
            }

            await System.IO.File.WriteAllTextAsync($"{CommonServices.GetPathInformation("googleInfo.txt")}", string.Join(",", list));
            CommonServices.LogMessage($"Upload Finish");
        }
        catch (Exception e)
        {
            CommonServices.LogMessage($"Upload Error: {e.Message}");

            throw new Exception(e.Message);
        }
        finally
        {
            if (isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }

    static async void UploadGoogleDriveAndroidPlatform()
    {
        var isBatchMode             = CommonServices.IsBatchMode();
        var buildAndroidInformation = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathInformation("AndroidInformation.json"));
        CommonServices.CheckToClearToken(buildAndroidInformation.data);
        var finalBuildVersion = CommonServices.GetFinalAndroidBuildVersion();
        var tmp               = buildAndroidInformation.data.outputFileName.Split("-");
        var outputFileName    = $"{tmp[0]}-{finalBuildVersion}-{tmp[2]}";
        var internalFilePath  = $"{CommonServices.GetBuildPath()}Client/Android/{outputFileName}";
        var apkFilePath       = $"{internalFilePath}.apk";
        var aabFilePath       = $"{internalFilePath}.aab";
        var zipFilePath       = $"{internalFilePath}-{PlayerSettings.bundleVersion}-v{buildAndroidInformation.data.buildNumber}-IL2CPP.symbols.zip";

        if (!System.IO.File.Exists(apkFilePath))
        {
            throw new Exception("Apk File not found");
        }

        if (!System.IO.File.Exists(aabFilePath) && buildAndroidInformation.data.BuildAppBundle())
        {
            throw new Exception("Aab File not found");
        }

        var service = await CommonServices.GetDriveServices(buildAndroidInformation.data.IsUseServicesAccount());

        //read from file
        var folderId = System.IO.File.ReadAllText($"{CommonServices.GetPathInformation("uploadInfo.txt")}");
        //BuildEnvironment
        var environmentFolder = await CreateFolder(buildAndroidInformation.data.buildEnvironment, folderId, service);

        //create platform folder
        var platFormFolder = await CreateFolder("Android", environmentFolder, service);

        //Create VersionFolder
        var versionFolder = await CreateFolder($"{outputFileName}", platFormFolder, service);

        listTask = new List<Task>();
        var urlApk  = "";
        var urlAab  = "";
        var zipFile = "";
        listTask.Add(UploadFileInternal(apkFilePath, versionFolder, service, ApkFile, (x) => { urlApk = x; }));

        if (buildAndroidInformation.data.BuildAppBundle())
        {
            listTask.Add(UploadFileInternal(aabFilePath, versionFolder, service, ApkFile, (x) => { urlAab  = x; }));
            listTask.Add(UploadFileInternal(zipFilePath, versionFolder, service, ZipFile, (x) => { zipFile = x; }));
        }

        await Task.WhenAll(listTask);
        listTask.Clear();
        var list = new List<string> { $"https://drive.google.com/drive/folders/{folderId}" };

        if (!string.IsNullOrEmpty(urlApk))
        {
            list.Add(urlApk);
        }

        if (!string.IsNullOrEmpty(urlAab))
        {
            list.Add(urlAab);
        }

        if (!string.IsNullOrEmpty(zipFile)) list.Add(zipFile);

        System.IO.File.WriteAllText($"{CommonServices.GetPathInformation("googleInfo.txt")}", string.Join(",", list));

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

    private static async void ShareWriter(DriveService service, string folderId, string userEmail)
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