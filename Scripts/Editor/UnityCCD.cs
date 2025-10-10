namespace BuildHelper.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class UnityCCD
    {
        public static async Task ProcessCcd()
        {
#if !ADDRESSABLE
            return;
#endif
            var data = CommonServicesHelper.GetDataModel<BuildAndroidInformation>(CommonServicesHelper.GetPathInformation("AndroidInformation.json"));

            if (string.IsNullOrEmpty(data.CCdInfo))
            {
                return;
            }

            var ccdInfo = JsonUtility.FromJson<UnityCCDInfo>(data.CCdInfo);

            if (!ccdInfo.allowUpdate)
            {
                return;
            }

            var totalEntries    = await GetTotalEntries(ccdInfo);
            var ccdBuildPath    = $"{CommonServicesHelper.GetProjectPath()}/CCDBuildData";
            var searchPattern   = "*.bundle";
            var localBundleFile = Directory.GetFiles(ccdBuildPath, searchPattern, SearchOption.AllDirectories).ToList();
            var dic             = ConvertToPathContentSizeDictionary(totalEntries);

            await DeleteAllEntries(totalEntries, ccdInfo, dic, localBundleFile);

            await UploadAllCcd(data, localBundleFile);

            if (localBundleFile.Count > 0)
            {
                await CreateNewRelease(ccdInfo);
            }
            else
            {
                CommonServicesHelper.LogMessage("No new file to upload to CCD.");
            }
        }

        private static Dictionary<string, long> ConvertToPathContentSizeDictionary(List<JObject> entries)
        {
            var result = new Dictionary<string, long>();

            foreach (var entry in entries)
            {
                var path      = entry["path"]?.ToString();
                var sizeToken = entry["content_size"];

                if (!string.IsNullOrEmpty(path) && sizeToken != null && long.TryParse(sizeToken.ToString(), out var contentSize))
                {
                    result[path] = contentSize;
                }
            }

            return result;
        }

        private static async Task DeleteAllEntries(List<JObject> entries, UnityCCDInfo ccdInfo, Dictionary<string, long> nameToSize, List<string> localBundleFile)
        {
            if (nameToSize == null)
            {
                nameToSize = new Dictionary<string, long>();
            }

            var client   = new HttpClient();
            var entryUrl = $"https://services.api.unity.com/ccd/management/v1/projects/{ccdInfo.projectId}/environments/{ccdInfo.environmentId}/buckets/{ccdInfo.bucketId}/entries";

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ccdInfo.clientId}:{ccdInfo.clientSecret}"));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            foreach (var entry in entries)
            {
                var entryId = entry["entryid"]?.ToString();

                if (string.IsNullOrEmpty(entryId)) continue;
                var path      = entry["path"]?.ToString();
                var cloudSize = nameToSize[path];

                var localSize = localBundleFile.FirstOrDefault(file => file.EndsWith(entry["path"]?.ToString())) is { } file
                    ? new FileInfo(file).Length
                    : 0;

                if (cloudSize == localSize && !ccdInfo.forceClearCache)
                {
                    CommonServicesHelper.LogMessage($"✅ Skip {entry["path"]} (size: {cloudSize})");
                    localBundleFile.RemoveAll(file => file.EndsWith(entry["path"]?.ToString()));

                    continue;
                }

                var delResp = await client.DeleteAsync($"{entryUrl}/{entryId}");

                CommonServicesHelper.LogMessage(delResp.IsSuccessStatusCode
                    ? $"✅ Delete {entry["path"]}"
                    : $"❌ Eror delete {entry["path"]}");

                await Task.Delay(100);
            }
        }

        private static async Task UploadAllCcd(IBuildInformation data, List<string> bundleFiles)
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
                throw new Exception("❌ Wrong CCD Info, please check again.");
            }

            var listTask = new List<Task>();
            var listOut  = new List<string>();

            foreach (var file in bundleFiles)
            {
                CommonServicesHelper.LogMessage($"📤 Process {file} to CCD...");
                listTask.Add(UploadToCcd(file, ccdInfo.projectId, ccdInfo.environmentId, ccdInfo.bucketId, ccdInfo.clientId, ccdInfo.clientSecret, listOut));
            }

            await Task.WhenAll(listTask);

            if (listOut.Count < bundleFiles.Count)
            {
                throw new Exception("❌ Can not upload all files to CCD, please check the log for details.");
            }
        }

        private static async Task CreateNewRelease(UnityCCDInfo ccdInfo)
        {
            var url = $"https://services.api.unity.com/ccd/management/v1/projects/{ccdInfo.projectId}/environments/{ccdInfo.environmentId}/buckets/{ccdInfo.bucketId}/releases";
            CommonServicesHelper.LogMessage($"📦 Create new release: {url}");

            var payload = new
            {
                notes = $"Auto Release at {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                metadata = new
                {
                    version = PlayerSettings.bundleVersion
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content     = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var client      = new HttpClient();
            var       credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ccdInfo.clientId}:{ccdInfo.clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                CommonServicesHelper.LogMessage("✅ Release created successfully.");
            }
            else
            {
                CommonServicesHelper.LogMessage("❌ Failed to create release:");
                CommonServicesHelper.LogMessage(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        }

        private static async Task<List<JObject>> GetTotalEntries(UnityCCDInfo ccdInfo)
        {
            var client       = new HttpClient();
            var entryUrlBase = $"https://services.api.unity.com/ccd/management/v1/projects/{ccdInfo.projectId}/environments/{ccdInfo.environmentId}/buckets/{ccdInfo.bucketId}/entries";

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ccdInfo.clientId}:{ccdInfo.clientSecret}"));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            CommonServicesHelper.LogMessage("📥 Get All entries...");

            var       allEntries = new List<JObject>();
            const int pageSize   = 100;
            string?   startingAfter;
            startingAfter = null;

            while (true)
            {
                var url = $"{entryUrlBase}?per_page={pageSize}";

                if (!string.IsNullOrEmpty(startingAfter))
                    url += $"&starting_after={startingAfter}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    CommonServicesHelper.LogMessage($"❌ Can not get entries list. HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");

                    break;
                }

                var content = await response.Content.ReadAsStringAsync();

                // Dữ liệu trả về là mảng JSON thuần
                var entries = JArray.Parse(content);

                if (entries.Count == 0)
                    break;

                CommonServicesHelper.LogMessage($"📄 Get total entries at page {pageSize}: {entries.Count}");
                allEntries.AddRange(entries.Cast<JObject>());

                if (entries.Count < pageSize)
                    break;

                // lấy id của entry cuối cùng để làm `starting_after`
                startingAfter = entries.Last["id"]?.ToString();

                if (string.IsNullOrEmpty(startingAfter))
                {
                    CommonServicesHelper.LogMessage("⚠️ Can not find page navigation.");

                    break;
                }
            }

            CommonServicesHelper.LogMessage($"✅Total entries: {allEntries.Count}");

            return allEntries;
        }

        private static async Task UploadToCcd(string filePath, string projectId, string environmentId, string bucketId, string keyId, string secretKey, List<string> output)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                CommonServicesHelper.LogMessage($"❌ File not found: {filePath}");

                return;
            }

            var client = new HttpClient();

            var fileNameInCcd = Path.GetFileName(filePath);
            var contentType   = "application/octet-stream";

            var entryUrl = $"https://services.api.unity.com/ccd/management/v1/projects/{projectId}/environments/{environmentId}/buckets/{bucketId}/entries/";
            CommonServicesHelper.LogMessage(entryUrl);
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{secretKey}"));

            CommonServicesHelper.LogMessage("⬆️ Upload file...");
            var contentHash = GetMD5Hash(filePath);
            var contentSize = new FileInfo(filePath).Length;

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
                CommonServicesHelper.LogMessage("❌ Failed to create entry:");
                CommonServicesHelper.LogMessage(await response.Content.ReadAsStringAsync());

                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            CommonServicesHelper.LogMessage("✅ Entry created. Response:");
            CommonServicesHelper.LogMessage(responseBody);

            // Parse JSON và lấy signed_url
            var json      = JObject.Parse(responseBody);
            var signedUrl = json["signed_url"]?.ToString();

            if (string.IsNullOrEmpty(signedUrl))
            {
                CommonServicesHelper.LogMessage("❌ signed_url not found in response.");

                return;
            }

            // PUT file lên signed_url
            CommonServicesHelper.LogMessage("📤 Uploading file to signed URL...");

            await using var fileStream = File.OpenRead(filePath);

            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var putResponse = await client.PutAsync(signedUrl, streamContent);

            if (putResponse.IsSuccessStatusCode)
            {
                output.Add(filePath);
                CommonServicesHelper.LogMessage("✅ Upload successful!");
            }
            else
            {
                CommonServicesHelper.LogMessage("❌ Upload failed:");
                CommonServicesHelper.LogMessage(await putResponse.Content.ReadAsStringAsync());
            }
        }

        static string GetMD5Hash(string filePath)
        {
            using var md5 = MD5.Create();

            using var stream = File.OpenRead(filePath);

            var hash = md5.ComputeHash(stream);

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}