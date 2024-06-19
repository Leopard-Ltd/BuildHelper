using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using UnityEditor;
using UnityEngine;

public class GoogleDriverServices
{
    static string ApkFile = "application/vnd.android.package-archive";

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

    static void UploadAndroidPlatform()
    {
        var buildAndroidInformation = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathBuildInformation());

        var path = Application.dataPath;

        var apkFilePath =
            $"{GetBuildFilePath()}Client/Android/{buildAndroidInformation.androidInformation.outputFileName}.apk";

        if (!File.Exists(apkFilePath))
        {
            throw new Exception("Apk File not found");
        }

        var contentType = ApkFile;

        var uploadInfo = path.Replace("Assets", "");
        //read from file
        var folderId = File.ReadAllText($"{uploadInfo}/uploadInfo.txt");

        var service = GetService();

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

        var file = request.ResponseBody;
        Console.WriteLine("File ID: " + file.Id);
        var urlFile = $"https://drive.google.com/file/d/{file.Id}/view?usp=drive_link";

        var googleLinkPath = path.Replace("Assets", "");
        googleLinkPath = $"{googleLinkPath}/googleInfo.txt";
        File.WriteAllText(googleLinkPath, urlFile);
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