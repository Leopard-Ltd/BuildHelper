namespace Editor
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
            return;
            var data = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathBuildInformation("AndroidInformation.json"));

            var packageName        = PlayerSettings.applicationIdentifier;
            var aabFilePath        = "app-release.aab";
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
                    ApplicationName       = "Google Play Upload"
                });

                var editRequest = service.Edits.Insert(new Google.Apis.AndroidPublisher.v3.Data.AppEdit(), packageName);
                var edit        = editRequest.Execute();
                var editId      = edit.Id;
                CommonServices.LogMessage("Edit ID: " + editId);

                using (var fileStream = new FileStream(aabFilePath, FileMode.Open))
                {
                    var uploadRequest = service.Edits.Bundles.Upload(packageName, editId, fileStream, "application/octet-stream");
                    var progress      = uploadRequest.Upload();

                    if (progress.Status == UploadStatus.Completed)
                    {
                        CommonServices.LogMessage("Upload AAB thành công! Bạn có thể kiểm tra trong Bundle Explorer.");
                    }
                    else
                    {
                        CommonServices.LogMessage("Lỗi khi upload: " + progress.Exception);
                    }
                }

                CommonServices.LogMessage("File AAB đã được upload vào 'Bundle Explorer' nhưng KHÔNG release.");
            }
            catch (Exception ex)
            {
                CommonServices.LogMessage("Lỗi: " + ex.Message);
            }
        }

        private static string GetServicesAccountUpload() { return ""; }
    }
}