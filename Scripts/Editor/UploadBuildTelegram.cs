using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEditor;

public class UploadBuildTelegram
{
    [MenuItem("Build/UploadTelegramWebgl")]
    static async void UploadWebGl()
    {
        var isBatchMode = CommonServices.IsBatchMode();

        try
        {
            var webglModel = CommonServices.GetDataModel<BuildWebGlInformation>(CommonServices.GetPathBuildInformation("WebGlInformation.json"));

            foreach (var item in webglModel.webGlInformation.telegramInfos)
            {
                if (!item.ShouldUploadToTelegram) continue;
                var botToken = item.TelegramBotToken;

                var chatId   = item.TelegramChatId;
                var topicId  = item.TelegramThreadId;
                var filePath = $"{CommonServices.GetBuildPath()}Client/webgl/{webglModel.webGlInformation.outputFileName}.zip";

                await UploadInternal(filePath, chatId, topicId, botToken);
            }
        }
        catch (Exception e)
        {
            CommonServices.LogMessage(e.Message);
        }

        if (isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static async Task UploadInternal(string filePath, string chatId, string topicId, string botToken)
    {
        CommonServices.LogMessage($"Start upload {filePath} to Telegram");

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies        = true
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(60)
        };

        using var       form = new MultipartFormDataContent();
        await using var fs   = File.OpenRead(filePath);

        form.Add(new StringContent(chatId), "chat_id");

        if (!string.IsNullOrEmpty(topicId))
        {
            form.Add(new StringContent(topicId), "message_thread_id");
        }

        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

        var contentType = extension switch
        {
            ".apk" => "application/vnd.android.package-archive",
            ".aab" => "application/octet-stream",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };

        var streamContent = new StreamContent(fs);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        form.Add(streamContent, "document", Path.GetFileName(filePath));

        try
        {
            var response = await client.PostAsync($"https://api.telegram.org/bot{botToken}/sendDocument", form);
            var result   = await response.Content.ReadAsStringAsync();
            CommonServices.LogMessage("Status code: " + response.StatusCode);
            CommonServices.LogMessage("Response: " + result);
            CommonServices.LogMessage(result);
        }
        catch (Exception ex)
        {
            CommonServices.LogMessage($"Lỗi upload: {ex.Message}");

            if (ex.InnerException != null)
            {
                CommonServices.LogMessage($"Chi tiết: {ex.InnerException.Message}");
            }
        }
    }

    [MenuItem("Build/UploadTelegramAndroid")]
    static async void UploadAndroid()
    {
        var isBatchMode = CommonServices.IsBatchMode();

        try
        {
            var buildAndroidInformation = CommonServices.GetDataModel<BuildAndroidInformation>(CommonServices.GetPathBuildInformation("AndroidInformation.json"));
            var listTask                = new List<Task>();

            foreach (var item in buildAndroidInformation.androidInformation.telegramInfos)
            {
                if (!item.ShouldUploadToTelegram) continue;
                var botToken = item.TelegramBotToken;

                var chatId  = item.TelegramChatId;
                var topicId = item.TelegramThreadId;

                var finalBuildVersion = CommonServices.GetFinalAndroidBuildVersion();
                var tmp               = buildAndroidInformation.androidInformation.outputFileName.Split("-");
                var outputFileName    = $"{tmp[0]}-{finalBuildVersion}-{tmp[2]}";
                var internalFilePath  = $"{CommonServices.GetBuildPath()}Client/Android/{outputFileName}";
                var apkFilePath       = $"{internalFilePath}.apk";
                var aabFilePath       = $"{internalFilePath}.aab";
                var zipFilePath       = $"{internalFilePath}-{PlayerSettings.bundleVersion}-v{buildAndroidInformation.androidInformation.buildNumber}-IL2CPP.symbols.zip";

                if (File.Exists(apkFilePath))
                {
                    listTask.Add(UploadInternal(apkFilePath, chatId, topicId, botToken));
                }

                if (File.Exists(aabFilePath))
                {
                    listTask.Add(UploadInternal(aabFilePath, chatId, topicId, botToken));
                }

                if (File.Exists(zipFilePath))
                {
                    listTask.Add(UploadInternal(zipFilePath, chatId, topicId, botToken));
                }
            }

            await Task.WhenAll(listTask);
        }
        catch (Exception e)
        {
            CommonServices.LogMessage(e.Message);
        }

        if (isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    static async void UploadIos()
    {
        var isBatchMode = CommonServices.IsBatchMode();

        if (isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
}