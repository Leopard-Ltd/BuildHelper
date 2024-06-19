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
    static void TestUpload() { UploadFile("E:/ABS.txt", "1q0oGNCVdiegiGhrg27fPNNfwNaxesEfW", "text/plain"); }

    static string UploadFile(string filePath, string folderId, string contentType)
    {
        var service = GetService();

        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name    = Path.GetFileName(filePath),
            Parents = new List<string> { folderId }
        };

        FilesResource.CreateMediaUpload request;

        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            request        = service.Files.Create(fileMetadata, stream, contentType);
            request.Fields = "id";
            request.Upload();
        }

        var file = request.ResponseBody;
        Debug.Log("File ID: " + file.Id);

        return file.Id;
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