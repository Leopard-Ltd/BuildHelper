using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

#if ADDRESSABLE
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif
using UnityEditor.Build;
using UnityEngine;

public abstract class BaseBuildPlatForm
{
    public virtual void SetUpAndBuild(IBuildInformation data)
    {
        this.ResetBuildSettings();

        try
        {
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }
        catch (Exception e)
        {
            CommonServices.LogMessage(e);
        }

        this.BuildAddressable(data);

        EditorUserBuildSettings.development = data.IsDevelopment();
    }

    private void FindAndSetGameVersion(IBuildInformation data)
    {
        var path                   = Application.dataPath;
        var featureGameVersionPath = $"{path}/FeatureTemplate/Scripts/Services/FeatureGameVersion.cs";

        if (!System.IO.File.Exists(featureGameVersionPath)) return;

        var fileContent = System.IO.File.ReadAllText(featureGameVersionPath);

        var newBuildInfo   = $"BuildInfo=\"Unity Version: {Application.unityVersion} | Build: {PlayerSettings.bundleVersion} - {data.VersionCode} - {System.DateTime.Now}\";";
        var updatedContent = System.Text.RegularExpressions.Regex.Replace(fileContent, @"BuildInfo\s*=\s*\"".*\"";", newBuildInfo);
        System.IO.File.WriteAllText(featureGameVersionPath, updatedContent);
    }

    public void SetupBlueprintPath(IBuildInformation data)
    {
        var blueprintConfig = $"{Application.dataPath}/Resources/GameConfigs/BlueprintConfig.asset";

        if (!System.IO.File.Exists(blueprintConfig))
        {
            CommonServices.LogMessage("Blueprint config not found");

            return;
        }

        if (!data.DefineSymbol.Contains("SET_BLUEPRINT_PATH"))
        {
            CommonServices.LogMessage("No need to set blueprint path");

            return;
        }

        var content     = System.IO.File.ReadAllText(blueprintConfig);
        var pattern     = @"(resourceBlueprintPath:\s*).*";
        var replacement = $"$1{data.BlueprintPath}/";

        var result = Regex.Replace(content, pattern, replacement);
        System.IO.File.WriteAllText(blueprintConfig, result);
        CommonServices.LogMessage($"Reset blueprint path to {data.BlueprintPath}");
    }

    private void ResetBuildSettings()
    {
        EditorUserBuildSettings.allowDebugging                = false;
        EditorUserBuildSettings.connectProfiler               = false;
        EditorUserBuildSettings.buildScriptsOnly              = false;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
        EditorUserBuildSettings.development                   = false;
        EditorUserBuildSettings.waitForManagedDebugger        = false;
        EditorUserBuildSettings.waitForPlayerConnection       = false;
    }

    protected virtual void PreprocessBuild(IBuildInformation data)
    {
        this.FindAndSetGameVersion(data);
        this.SetupBlueprintPath(data);
    }

    protected virtual void SetAllGroupsToLZMA()
    {
#if ADDRESSABLE
        // Access the addressable asset settings
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        // Iterate over all groups
        foreach (var group in settings.groups)
        {
            // Set the compression type to LZMA for each group
            var schema = group.GetSchema<BundledAssetGroupSchema>();

            if (schema != null)
            {
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZMA;
                schema.UseUnityWebRequestForLocalBundles = false;
            }
        }
#endif
    }

    private void BuildAddressable(IBuildInformation data)
    {
#if ADDRESSABLE
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null) return;
#if !NO_LZMA
        SetAllGroupsToLZMA();
#endif
        CommonServices.LogMessage($"--------------------");
        CommonServices.LogMessage($"Clean addressable");
        CommonServices.LogMessage($"--------------------");
        AddressableAssetSettings.CleanPlayerContent();
        CommonServices.LogMessage($"--------------------");
        CommonServices.LogMessage($"Build addressable");
        CommonServices.LogMessage($"--------------------");
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        var success = string.IsNullOrEmpty(result.Error);

        if (!success)
        {
            var errorMessage = "Addressable build error encountered: " + result.Error;
            CommonServices.LogMessage(errorMessage);

            throw new Exception(errorMessage);
        }

        CommonServices.LogMessage($"--------------------");
        CommonServices.LogMessage($"Finish building addressable");
        CommonServices.LogMessage($"--------------------");
        UploadAllCcd(data);
#endif
    }

    protected void SetScriptDefineSymbols(NamedBuildTarget targetGroup, string[] scripts) { PlayerSettings.SetScriptingDefineSymbols(targetGroup, scripts); }

    protected string[] LoadSceneOnPath()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        return scenes;
    }

    #region Upload to CCD

    [MenuItem("Build/Upload All CCD")]
    private static async void UploadAllCcd(IBuildInformation data)
    {
        if (data.CCdInfo.StringIsNullOrEmpty())
        {
            return;
        }

        var ccdInfo = JsonUtility.FromJson<UnityCCDInfo>(data.CCdInfo);

        if (!ccdInfo.allowUpdate)
        {
            return;
        }

        if (ccdInfo.bucketId.StringIsNullOrEmpty() ||
            ccdInfo.clientId.StringIsNullOrEmpty() ||
            ccdInfo.clientSecret.StringIsNullOrEmpty() ||
            ccdInfo.projectId.StringIsNullOrEmpty() ||
            ccdInfo.environmentId.StringIsNullOrEmpty())
        {
            throw new Exception("❌ Thông tin CCD không đầy đủ. Vui lòng kiểm tra lại.");
        }

        var ccdBuildPath  = $"{CommonServices.GetProjectPath()}/CCDBuildData";
        var searchPattern = "*.bundle";
        var bundleFiles   = Directory.GetFiles(ccdBuildPath, searchPattern, SearchOption.AllDirectories);
        var listTask      = new List<Task>();
        var listOut       = new List<string>();

        foreach (var file in bundleFiles)
        {
            CommonServices.LogMessage($"📤 Đang Process {file} lên CCD...");
            listTask.Add(UploadToCcd(file, ccdInfo.projectId, ccdInfo.environmentId, ccdInfo.bucketId, ccdInfo.clientId, ccdInfo.clientSecret, listOut));
        }

        await Task.WhenAll(listTask);

        CommonServices.LogMessage(listOut.Count == bundleFiles.Length ? "✅ Tất cả file đã được upload lên CCD." : "❌ Không thể upload một số file lên CCD.");
    }

    private static async Task UploadToCcd(string filePath, string projectId, string environmentId, string bucketId, string keyId, string secretKey, List<string> output)
    {
        var client = new HttpClient();

        var fileNameInCcd = Path.GetFileName(filePath);
        var contentType   = "application/octet-stream";

        var entryUrl = $"https://services.api.unity.com/ccd/management/v1/projects/{projectId}/environments/{environmentId}/buckets/{bucketId}/entries/";
        CommonServices.LogMessage(entryUrl);
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{secretKey}"));
        var contentHash = GetMD5Hash(filePath);
        var contentSize = new FileInfo(filePath).Length;
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        CommonServices.LogMessage("📥 Đang lấy danh sách entries...");
        var getResp = await client.GetAsync($"{entryUrl}");

        if (!getResp.IsSuccessStatusCode)
        {
            CommonServices.LogMessage("❌ Không thể lấy danh sách entry.");

            return;
        }

        var listJson = await getResp.Content.ReadAsStringAsync();
        var entries  = JArray.Parse(listJson);

        CommonServices.LogMessage($"🧹 Đang xoá {entries.Count} entries...");

        foreach (var entry in entries)
        {
            var entryId = entry["entryid"]?.ToString();

            if (string.IsNullOrEmpty(entryId)) continue;
            var delResp = await client.DeleteAsync($"{entryUrl}/{entryId}");

            CommonServices.LogMessage(delResp.IsSuccessStatusCode
                ? $"✅ Xoá {entry["path"]}"
                : $"❌ Lỗi xoá {entry["path"]}");
        }

        CommonServices.LogMessage("⬆️ Upload lại file...");

        var payload = new
        {
            path         = fileNameInCcd,
            content_hash = contentHash,
            content_size = contentSize,
            content_type = contentType,
            signed_url   = true
        };

        var payloadJson = JsonConvert.SerializeObject(payload);

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var content  = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(entryUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            CommonServices.LogMessage("❌ Failed to create entry:");
            CommonServices.LogMessage(await response.Content.ReadAsStringAsync());

            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        CommonServices.LogMessage("✅ Entry created. Response:");
        CommonServices.LogMessage(responseBody);

        // Parse JSON và lấy signed_url
        var json      = JObject.Parse(responseBody);
        var signedUrl = json["signed_url"]?.ToString();

        if (string.IsNullOrEmpty(signedUrl))
        {
            CommonServices.LogMessage("❌ signed_url not found in response.");

            return;
        }

        // PUT file lên signed_url
        CommonServices.LogMessage("📤 Uploading file to signed URL...");

        await using var fileStream = File.OpenRead(filePath);

        using var streamContent = new StreamContent(fileStream);

        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var putResponse = await client.PutAsync(signedUrl, streamContent);

        if (putResponse.IsSuccessStatusCode)
        {
            output.Add(filePath);
            CommonServices.LogMessage("✅ Upload successful!");
        }
        else
        {
            CommonServices.LogMessage("❌ Upload failed:");
            CommonServices.LogMessage(await putResponse.Content.ReadAsStringAsync());
        }
    }

    static string GetMD5Hash(string filePath)
    {
        using var md5 = MD5.Create();

        using var stream = File.OpenRead(filePath);

        var hash = md5.ComputeHash(stream);

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    #endregion
}