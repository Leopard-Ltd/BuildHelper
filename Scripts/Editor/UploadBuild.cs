namespace BuildHelper.Workflows
{
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

        static async void DeleteAllFromServicesAccount()
        {
            var servicesAccountModel = CommonServicesHelper.GetDataModel<ServicesAccountModel>(CommonServicesHelper.GetPathInformation("servicesAccount.json"));
            var service              = await CommonServicesHelper.GetDriveServices();
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

        public static async Task UploadGoogleDriveWebGlPlatForm()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            try
            {
                var webglModel = CommonServicesHelper.GetDataModel<BuildWebGlInformation>(CommonServicesHelper.GetPathInformation("WebGlInformation.json"));
                CommonServicesHelper.CheckToClearToken(webglModel.data);
                var zipFilePath = $"{CommonServicesHelper.GetBuildPath()}Client/webgl/{webglModel.data.outputFileName}.zip";
                var service     = await CommonServicesHelper.GetDriveServices(webglModel.data.IsUseServicesAccount());
                service.HttpClient.Timeout = TimeSpan.FromHours(1);
                //read from file
                var folderId = System.IO.File.ReadAllText($"{CommonServicesHelper.GetPathInformation("uploadInfo.txt")}");

                var environmentFolder = await CreateFolder(webglModel.data.buildEnvironment, folderId, service);
                var platFormFolder    = await CreateFolder("webgl", environmentFolder, service);

                var list = new List<string> { $"https://drive.google.com/drive/folders/{folderId}" };

                var zipFile = "";
                await UploadFileInternal(zipFilePath, platFormFolder, service, ZipFile, (x) => { zipFile = x; });
                list.Add(zipFile);

                System.IO.File.WriteAllText(CommonServicesHelper.GetPathInformation("googleInfo.txt"), string.Join(",", list));

                if (isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception e)
            {
                //ignore
                CommonServicesHelper.LogMessage($"Upload Error: {e.Message}");
            }
            finally
            {
                if (isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        public static async Task UploadGoogleDriveIosPlatform()
        {
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            try
            {
                var buildIosInformation = CommonServicesHelper.GetDataModel<BuildIosInformation>(CommonServicesHelper.GetPathInformation("IosInformation.json"));
                var service             = await CommonServicesHelper.GetDriveServices(buildIosInformation.data.IsUseServicesAccount());
                service.HttpClient.Timeout = TimeSpan.FromHours(1);
                CommonServicesHelper.CheckToClearToken(buildIosInformation.data);
                var outputFileName = buildIosInformation.data.outputFileName;
                //read from file
                var ipaPath        = $"{CommonServicesHelper.GetBuildPath()}/Client/ios/{buildIosInformation.data.outputFileName}/{buildIosInformation.data.outputFileName}.ipa/";
                var ipaFilePath    = CommonServicesHelper.FindFileInFolder(ipaPath);
                var folderId       = await System.IO.File.ReadAllTextAsync($"{CommonServicesHelper.GetPathInformation("uploadInfo.txt")}");
                var platFormFolder = await CreateFolder("ios", folderId, service);
                var versionFolder  = await CreateFolder($"{outputFileName}-{buildIosInformation.data.buildNumber}", platFormFolder, service);
                //find Ipa file in ipaPath
                var ipaLink = "";
                CommonServicesHelper.LogMessage($"Start upload");
                await UploadFileInternal(ipaFilePath, versionFolder, service, IpaFile, (x) => { ipaLink = x; });

                var list = new List<string> { $"https://drive.google.com/drive/folders/{folderId}" };

                if (!string.IsNullOrEmpty(ipaLink))
                {
                    list.Add(ipaLink);
                }

                await System.IO.File.WriteAllTextAsync($"{CommonServicesHelper.GetPathInformation("googleInfo.txt")}", string.Join(",", list));
                CommonServicesHelper.LogMessage($"Upload Finish");
            }
            catch (Exception e)
            {
                CommonServicesHelper.LogMessage($"Upload Error: {e.Message}");

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

        public static async Task UploadGoogleDriveAndroidPlatform()
        {
            CommonServicesHelper.LogMessage("Start Upload Android Google Drive");
            var isBatchMode = CommonServicesHelper.IsBatchMode();

            try
            {
                var buildAndroidInformation = CommonServicesHelper.GetDataModel<BuildAndroidInformation>(CommonServicesHelper.GetPathInformation("AndroidInformation.json"));
                CommonServicesHelper.CheckToClearToken(buildAndroidInformation.data);
                var finalBuildVersion = CommonServicesHelper.GetFinalAndroidBuildVersion();
                var tmp               = buildAndroidInformation.data.outputFileName.Split("-");
                var outputFileName    = $"{tmp[0]}-{finalBuildVersion}-{tmp[2]}";
                var internalFilePath  = $"{CommonServicesHelper.GetBuildPath()}Client/Android/{outputFileName}";
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

                var service = await CommonServicesHelper.GetDriveServices(buildAndroidInformation.data.IsUseServicesAccount());
                CommonServicesHelper.LogMessage("Get Service Done");
                service.HttpClient.Timeout = TimeSpan.FromHours(1);
                //read from file
                var folderId = System.IO.File.ReadAllText($"{CommonServicesHelper.GetPathInformation("uploadInfo.txt")}");
                //BuildEnvironment
                var environmentFolder = await CreateFolder(buildAndroidInformation.data.buildEnvironment, folderId, service);

                //create platform folder
                var platFormFolder = await CreateFolder("Android", environmentFolder, service);

                //Create VersionFolder
                var versionFolder = await CreateFolder($"{outputFileName}", platFormFolder, service);
                CommonServicesHelper.LogMessage("Start upload File");
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

                System.IO.File.WriteAllText($"{CommonServicesHelper.GetPathInformation("googleInfo.txt")}", string.Join(",", list));
            }
            catch (Exception e)
            {
                CommonServicesHelper.LogMessage($"Upload Failed: {e.Message}");
            }
            finally
            {
                if (isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        static async Task<string> CreateFolder(string folderName, string parentFolder, DriveService service)
        {
            CommonServicesHelper.LogMessage($"Start Create folder {folderName} in {parentFolder}");

            try
            {
                var folderToDelete = FindFolder(service, parentFolder, folderName);

                if (folderToDelete != null)
                {
                    CommonServicesHelper.LogMessage($"Folder already exists: {folderToDelete.Id}");
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

                CommonServicesHelper.LogMessage("Folder created ID: " + file.Id);
                return file.Id;
            }
            catch (Google.GoogleApiException gex)
            {
                CommonServicesHelper.LogMessage($"Google API Error: {gex.Error?.Message} ({gex.Error?.Code})");
                throw;
            }
            catch (Exception ex)
            {
                CommonServicesHelper.LogMessage($"Unexpected error: {ex.Message}");
                throw;
            }
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
            CommonServicesHelper.LogMessage($"Ownership transferred to {userEmail}.");
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

        private static async Task UploadFileInternal(string filePath, string folderId, DriveService service, string contentType, Action<string> onComplete)
        {
            var fileInfo = new FileInfo(filePath);

            var fileMetadata = new File()
            {
                Name    = Path.GetFileName(filePath),
                Parents = new List<string> { folderId }
            };

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var request = service.Files.Create(fileMetadata, stream, contentType);
                request.Fields            = "id";
                request.SupportsAllDrives = true;

                // Set chunk size để tránh bị timeout
                request.ChunkSize = GetOptimalChunkSize(fileInfo.Length);
                CommonServicesHelper.LogMessage("Uploading file: " + fileMetadata.Name);
                var progress = await request.UploadAsync();

                if (progress.Status == UploadStatus.Failed)
                {
                    CommonServicesHelper.LogMessage($"Upload failed: {progress.Exception.Message}");
                }

                if (progress.Status == UploadStatus.Completed)
                {
                    var fileId  = request.ResponseBody.Id;
                    var urlFile = $"https://drive.google.com/uc?export=download&id={fileId}";
                    onComplete(urlFile);
                    CommonServicesHelper.LogMessage($"Upload complete: {urlFile}");
                }
            }
        }
        
        private static int GetOptimalChunkSize(long fileSize)
        {
            const int minChunk = ResumableUpload.MinimumChunkSize; 

            if (fileSize <= 10 * 1024 * 1024) // < 10MB
                return minChunk * 16; // 4MB
            else if (fileSize <= 200 * 1024 * 1024) // < 200MB
                return minChunk * 64; // 16MB
            else if (fileSize <= 1024 * 1024 * 1024) // < 1GB
                return minChunk * 128; // 32MB
            else
                return minChunk * 256; // 64MB 
        }
    }
}