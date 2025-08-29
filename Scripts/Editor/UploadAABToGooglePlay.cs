#if UNITY_ANDROID
namespace BuildHelper.Workflows
{
    using System;
    using System.IO;
    using Google.Apis.AndroidPublisher.v3;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Services;
    using Google.Apis.Upload;
    using UnityEditor;

    public static class UploadAABToGooglePlay
    {
        public static void UploadAAb()
        {
            var data = CommonServicesHelper.GetDataModel<BuildAndroidInformation>(CommonServicesHelper.GetPathInformation("AndroidInformation.json"));

            if (!data.data.BuildAppBundle())
                return;

            var packageName = PlayerSettings.applicationIdentifier;

            var outputFileName = CommonServicesHelper.FindFileInFolder($"{CommonServicesHelper.GetBuildPath()}/Client/Android/", ".aab");
            var aabFilePath    = CommonServicesHelper.GetBuildPath(outputFileName);

            var serviceAccountJson = GetServicesAccountUpload();

            try
            {
                GoogleCredential credential;

                using (var stream = new FileStream(serviceAccountJson, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);
                }

                var service = new AndroidPublisherService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName       = "Google Play Upload",
                    HttpClientFactory = new CustomClientFactory()
                });

                var editRequest = service.Edits.Insert(new Google.Apis.AndroidPublisher.v3.Data.AppEdit(), packageName);
                var edit        = editRequest.Execute();
                var editId      = edit.Id;
                CommonServicesHelper.LogMessage("Edit ID: " + editId);

                using (var fileStream = new FileStream(aabFilePath, FileMode.Open))
                {
                    var uploadRequest = service.Edits.Bundles.Upload(packageName, editId, fileStream, "application/octet-stream");
                    var progress      = uploadRequest.Upload();

                    if (progress.Status == UploadStatus.Completed)
                    {
                        CommonServicesHelper.LogMessage("Upload AAB thành công! Bạn có thể kiểm tra trong Bundle Explorer.");
                    }
                    else
                    {
                        CommonServicesHelper.LogMessage("Lỗi khi upload: " + progress.Exception);
                    }
                }

                CommonServicesHelper.LogMessage("File AAB đã được upload vào 'Bundle Explorer' nhưng KHÔNG release.");
            }
            catch (Exception ex)
            {
                CommonServicesHelper.LogMessage("Lỗi: " + ex.Message);
            }
        }

        private static string GetServicesAccountUpload()
        {
            var filePath = CommonServicesHelper.GetPathInformation("googleUploadServicesAccount.json");

            return filePath;
        }
    }
}
#endif