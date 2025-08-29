#if ADDRESSABLE && BLUEPRINT_WORKFLOW
namespace BuildHelper.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Services;
    using Google.Apis.Sheets.v4;
    using Google.Apis.Sheets.v4.Data;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using UnityEngine;
    using CompressionLevel = System.IO.Compression.CompressionLevel;

    /// <summary>
    /// Sync from GoogleDrive => Assign to Group Addressable => build addressable groups=> Upload to CDN
    /// </summary>
    public class BlueprintWorkFlowAndroid
    {
        public virtual async Task ProcessBlueprint()
        {
            this.SetActiveBuild();
            var data = this.GetBlueprintWorkFlowData();
            _ = this.ClearPureCached(data);

            var blueprintVersionPath = await this.GetAllDataFromGoogleDrive(data);
            await this.ProcessAddressable(data);
            this.AddBlueprintFolderPathToAddressable(this.ConvertObsoleteToRelative(blueprintVersionPath), $"{data.blueprintName}_{data.blueprintVersion}", data.groupAssignBlueprint, data.labelName);
            await this.BuildAddressable();
            this.MoveCatalogToCCDData(data);

            switch (data.ccdPlatform.ToLower())
            {
                case "github":
                    await this.UploadToGithub(data);

                    break;
            }
        }

        private async Task ClearPureCached(BlueprintWorkFlowData data)
        {
            if (!data.ccdPlatform.ToLower().Equals("github"))
            {
                return;
            }

            var gitRootFolder = $"{CommonServicesHelper.GetRootPath()}GitCCD";

            if (!string.IsNullOrEmpty(data.githubCdnData.gitExistPath))
                gitRootFolder = data.githubCdnData.gitExistPath;

            if (!string.IsNullOrEmpty(data.githubCdnData.rootFolder))
                gitRootFolder = Path.Combine(gitRootFolder, data.githubCdnData.rootFolder);

            var dataAllBundle = Path.Combine(gitRootFolder, data.remoteBuildPath);

            if (!Directory.Exists(dataAllBundle))
                return;

            var ccdFiles = Directory.GetFiles(dataAllBundle, "*.*", SearchOption.AllDirectories).ToList();

            var basePath = new Uri(data.remoteLoadPath).AbsolutePath.Trim('/');

            try
            {
                foreach (var s in ccdFiles)
                {
                    var relativePath = Path.GetRelativePath(dataAllBundle, s)
                        .Replace("\\", "/")
                        .TrimStart('/');

                    var purgeUrl   = $"https://purge.jsdelivr.net/{basePath}/{relativePath}";
                    var httpClient = new HttpClient();

                    var response = await httpClient.GetAsync(purgeUrl);
                    var content  = await response.Content.ReadAsStringAsync();

                    CommonServicesHelper.LogMessage(response.IsSuccessStatusCode ? $"✅ Purged: {purgeUrl}" : $"⚠️ Failed to purge {purgeUrl}: {response.StatusCode}\n{content}");
                    await Task.Delay(1500);
                }
            }
            catch (Exception ex)
            {
                CommonServicesHelper.LogMessage($"❌ Exception during purge: {ex.Message}");
            }
        }

        protected virtual void SetActiveBuild()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.Android.useCustomKeystore = false;
        }

        protected virtual string GetAALibrary() { return $"{CommonServicesHelper.GetProjectPath()}/Library/com.unity.addressables/aa/Android"; }

        private void MoveCatalogToCCDData(BlueprintWorkFlowData data)
        {
            var ccdFilePath = $"{CommonServicesHelper.GetProjectPath()}/{data.remoteBuildPath}";
            var dataPath    = this.GetAALibrary();

            var catalogBin  = $"{dataPath}/catalog.bin";
            var catalogHash = $"{dataPath}/catalog.hash";
            var settings    = $"{dataPath}/settings.json";

            //copy to ccdFilePath
            File.Copy(catalogBin, $"{ccdFilePath}/catalog.bin", true);
            File.Copy(catalogHash, $"{ccdFilePath}/catalog.hash", true);
            File.Copy(settings, $"{ccdFilePath}/settings.json", true);
        }

        private async Task UploadToGithub(BlueprintWorkFlowData data)
        {
            var gitFolderPath = $"{CommonServicesHelper.GetRootPath()}GitCCD";

            if (string.IsNullOrEmpty(data.githubCdnData.gitExistPath))
            {
                if (!Directory.Exists(gitFolderPath))
                {
                    Directory.CreateDirectory(gitFolderPath);
                    var gitCloneCommand = $"git clone --branch {data.githubCdnData.branchName} {data.githubCdnData.gitHubUrl} --single-branch {gitFolderPath}";
                    await CommonServicesHelper.RunTerminalCommandAsync(gitCloneCommand);
                    CommonServicesHelper.LogMessage($"Clone git repository to {gitFolderPath}");
                }
                else
                {
                    await CommonServicesHelper.RunTerminalCommandAsync("git reset --h", gitFolderPath);
                    await CommonServicesHelper.RunTerminalCommandAsync("git clean -fd", gitFolderPath);
                }
            }
            else
            {
                gitFolderPath = $"{data.githubCdnData.gitExistPath}";

                if (!Directory.Exists(gitFolderPath))
                {
                    throw new DirectoryNotFoundException($"Git folder path does not exist: {gitFolderPath}");
                }

                await CommonServicesHelper.RunTerminalCommandAsync("git reset --hard", gitFolderPath);
                await CommonServicesHelper.RunTerminalCommandAsync("git clean -fd", gitFolderPath);
                // 1. Checkout to random branch
                var randomBranchName = "feature/random-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                await CommonServicesHelper.RunTerminalCommandAsync($"git checkout -B {randomBranchName}", gitFolderPath);

                var allBranchesRaw = await CommonServicesHelper.RunTerminalCommandAsync("git branch", gitFolderPath);

                var branches = allBranchesRaw
                    .Split('\n')
                    .Select(b => b.Replace("*", "").Trim())
                    .Where(b => !string.IsNullOrWhiteSpace(b) && b != randomBranchName)
                    .ToList();

                foreach (var branch in branches)
                {
                    Console.WriteLine($"Delete branch: {branch}");
                    _ = CommonServicesHelper.RunTerminalCommandAsync($"git branch -D {branch}", gitFolderPath);
                }

                await CommonServicesHelper.RunTerminalCommandAsync(
                    $"git checkout -B {data.githubCdnData.branchName} origin/{data.githubCdnData.branchName}",
                    gitFolderPath
                );

                await CommonServicesHelper.RunTerminalCommandAsync($"git branch -D {randomBranchName}", gitFolderPath);

                await CommonServicesHelper.RunTerminalCommandAsync(
                    $"git pull origin {data.githubCdnData.branchName}",
                    gitFolderPath
                );
            }

            // Copy CCD files
            var gitRootFolder = gitFolderPath;

            if (!string.IsNullOrEmpty(data.githubCdnData.rootFolder))
            {
                gitRootFolder = Path.Combine(gitRootFolder, data.githubCdnData.rootFolder);
            }

            var hasChange = this.MoveALlCCdataToGitHubCCd(data, gitRootFolder);

            if (hasChange)
            {
                await CommonServicesHelper.RunTerminalCommandAsync("git add .", gitFolderPath);

                var currentTime   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var commitMessage = $"Update {data.environmentName} {data.versionName} {data.blueprintName} version {data.blueprintVersion}";

                await CommonServicesHelper.RunTerminalCommandAsync($"git commit -m \"{commitMessage}\"", gitFolderPath);
                await CommonServicesHelper.RunTerminalCommandAsync($"git push origin {data.githubCdnData.branchName}", gitFolderPath);

                CommonServicesHelper.LogMessage("Git changes committed and pushed.");
            }
            else
            {
                CommonServicesHelper.LogMessage("No git changes detected. Skip commit and push.");
            }

            AssetDatabase.Refresh();

            //Delete Github Folder after upload
            if (string.IsNullOrEmpty(data.githubCdnData.gitExistPath))
            {
                if (Directory.Exists(gitFolderPath))
                {
                    this.ForceDeleteDirectory(gitFolderPath);
                    CommonServicesHelper.LogMessage($"Deleted git folder: {gitFolderPath}");
                }
                else
                {
                    CommonServicesHelper.LogMessage($"Git folder not found: {gitFolderPath}");
                }
            }
        }

        private void ForceDeleteDirectory(string targetDir)
        {
            var files = Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories);
            var dirs  = Directory.GetDirectories(targetDir, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    var attr = File.GetAttributes(file);

                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);

                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    CommonServicesHelper.LogMessage($"Cannot delete file: {file}\n{ex}");
                }
            }

            foreach (var dir in dirs.Reverse())
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    CommonServicesHelper.LogMessage($"Cannot delete sub-folder: {dir}\n{ex}");
                }
            }

            Directory.Delete(targetDir, true);
        }

        private bool MoveALlCCdataToGitHubCCd(BlueprintWorkFlowData data, string gitRootFolder)
        {
            var hasFileChanged = false;
            var projectPath    = CommonServicesHelper.GetProjectPath();
            var ccdFilePath    = Path.Combine(projectPath, data.remoteBuildPath);

            // Get all bundle and json files
            var ccdFiles = Directory.GetFiles(ccdFilePath, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".bundle") || file.EndsWith(".json") || file.EndsWith(".bin") || file.EndsWith(".hash"))
                .ToList();

            //Delete old files in gitRootFolder
            if (ccdFiles.Count > 0)
            {
                var relativePath = Path.GetRelativePath(projectPath, ccdFiles[0])
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var destinationPath      = Path.Combine(gitRootFolder, relativePath);
                var destinationDirectory = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                foreach (var file in Directory.GetFiles(destinationDirectory))
                {
                    File.Delete(file);
                }
            }

            foreach (var file in ccdFiles)
            {
                // Lấy relative path so với toàn bộ project
                var relativePath = Path.GetRelativePath(projectPath, file)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var destinationPath = Path.Combine(gitRootFolder, relativePath);

                hasFileChanged = true;
                File.Copy(file, destinationPath, true);
                CommonServicesHelper.LogMessage($"Copied {file} to {destinationPath}");
            }

            return hasFileChanged;
        }

        private async Task BuildAddressable()
        {
            AddressableAssetSettings.CleanPlayerContent();
            CommonServicesHelper.LogMessage($"--------------------");
            CommonServicesHelper.LogMessage($"Build addressable");
            CommonServicesHelper.LogMessage($"--------------------");
            AddressableAssetSettings.BuildPlayerContent(out var result);
            var success = string.IsNullOrEmpty(result.Error);

            if (!success)
            {
                var errorMessage = "Addressable build error encountered: " + result.Error;
                CommonServicesHelper.LogMessage(errorMessage);

                throw new Exception(errorMessage);
            }

            CommonServicesHelper.LogMessage($"--------------------");
            CommonServicesHelper.LogMessage($"Finish building addressable");
            CommonServicesHelper.LogMessage($"--------------------");
        }

        private string ConvertObsoleteToRelative(string absolutePath)
        {
            var projectPath = Application.dataPath;

            if (!absolutePath.StartsWith(projectPath))
            {
                throw new ArgumentException("Path is not inside the Unity project Assets folder: " + absolutePath);
            }

            var relativePath = "Assets" + absolutePath.Substring(projectPath.Length).Replace("\\", "/");

            return relativePath;
        }

        private SheetsService GetSheetsService(string json)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets, SheetsService.Scope.Drive);

            // Create Google Sheets API service.
            var getSheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName       = "UnityGoogleSheet",
                HttpClientFactory = new CustomClientFactory()
            });

            return getSheetsService;
        }

        #region GoogleBlueprint

        private async Task<string> GetAllDataFromGoogleDrive(BlueprintWorkFlowData data)
        {
            //Delete old CCd
            var ccdFilePath = $"{CommonServicesHelper.GetProjectPath()}/{data.remoteBuildPath}";

            if (Directory.Exists(ccdFilePath))
            {
                Directory.Delete(ccdFilePath, true);
            }

            var service = this.GetSheetsService(data.googleDriveData.servicesAccountJson);

            var listSheets = await this.GetAllSheetWithSpreadSheet(service, data.googleDriveData.spreadsheetId);

            var allData = await this.GetAllDataForAllSheet(listSheets, data.googleDriveData.spreadsheetId, service);

            var csvBuilder = this.GetDataFromSheetName(data.googleDriveData.sheetBuilderConfig,
                allData);

            var builderConfig = this.ParseCsvToDictionary(csvBuilder);

            var directoryRootPath = Path.Combine(Application.dataPath, "BlueprintRoot");

            if (AssetDatabase.IsValidFolder(directoryRootPath))
            {
                AssetDatabase.DeleteAsset(directoryRootPath);
            }

            var csvDirectory = Path.Combine(directoryRootPath, $"{data.blueprintName}_{data.blueprintVersion}");
            Directory.CreateDirectory(directoryRootPath);
            AssetDatabase.Refresh();
            Directory.CreateDirectory(csvDirectory);
            AssetDatabase.Refresh();

            foreach (var sheet in builderConfig.First().Value)
            {
                var csvOutput = this.GetDataFromSheetName(sheet, allData);
                //Save csv to file
                var filePath = Path.Combine(csvDirectory, $"{sheet}.csv");
                await File.WriteAllTextAsync(filePath, csvOutput);
            }

            // var zipFilePath = $"{directoryRootPath}/{data.blueprintName}_{data.blueprintVersion}.zip";
            //
            // if (File.Exists(zipFilePath))
            // {
            //     File.Delete(zipFilePath);
            // }
            //
            // this.ZipFolderSafe(csvDirectory, zipFilePath);
            return csvDirectory;
        }

        private void ZipFolderSafe(string folderPath, string zipPath)
        {
            try
            {
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                AssetDatabase.DisallowAutoRefresh();

                using var zipToOpen = new FileStream(zipPath, FileMode.Create);

                using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create);

                var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    if (Path.GetExtension(file) == ".meta")
                        continue;

                    var relativePath = file.Substring(folderPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    relativePath = relativePath.Replace("\\", "/"); // Normalize path

                    archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                }
            }
            catch (IOException ex)
            {
                CommonServicesHelper.LogMessage($"ZIP failed: {ex.Message}");
            }
            finally
            {
                AssetDatabase.AllowAutoRefresh();
                AssetDatabase.Refresh();
            }

            CommonServicesHelper.LogMessage($"Zipped folder to: {zipPath}");
        }

        private Dictionary<string, List<string>> ParseCsvToDictionary(string csv)
        {
            var dict = new Dictionary<string, List<string>>();

            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 1; i < lines.Length; i++)
            {
                var line       = lines[i];
                var commaIndex = line.IndexOf(',');

                if (commaIndex < 0) continue;

                var key   = line.Substring(0, commaIndex).Trim();
                var value = line.Substring(commaIndex + 1).Trim().Trim('"');

                var values = new List<string>(value.Split(','));
                dict[key] = values;
            }

            return dict;
        }

        private async Task<List<Sheet>> GetAllSheetWithSpreadSheet(SheetsService services, string spreadSheetId)
        {
            var request  = services.Spreadsheets.Get(spreadSheetId);
            var response = await request.ExecuteAsync();
            var sheets   = response.Sheets;

            return sheets.ToList();
        }

        private async Task<Dictionary<string, CustomValueRange>> GetAllDataForAllSheet(List<Sheet> sheets, string spreadsheetId, SheetsService service)
        {
            const int batchSize        = 50;
            var       valueRangeDict   = new Dictionary<string, ValueRange>();
            var       rangeToSheetName = new Dictionary<string, string>();
            var       ranges           = new List<string>();

            foreach (var s in sheets)
            {
                var sheetName = s.Properties.Title;

                var numRows = (int)(s.Properties.GridProperties.RowCount ?? 1000);
                var numCols = (int)(s.Properties.GridProperties.ColumnCount ?? 26);
                var range   = $"{sheetName}!A1:{GetColumnName(numCols)}{numRows}";

                ranges.Add(range);
                rangeToSheetName[range] = sheetName;
            }

            var totalBatches = (int)Math.Ceiling(ranges.Count / (float)batchSize);

            for (var i = 0; i < ranges.Count; i += batchSize)
            {
                var batchIndex = i / batchSize;
                var batch      = ranges.Skip(i).Take(batchSize).ToList();

                try
                {
                    var request = service.Spreadsheets.Values.BatchGet(spreadsheetId);
                    request.Ranges = batch;
                    var response = await request.ExecuteAsync();

                    if (response?.ValueRanges != null)
                    {
                        foreach (var valueRange in response.ValueRanges)
                        {
                            var key = valueRange.Range;
                            key = key.Replace("'", string.Empty);

                            if (rangeToSheetName.TryGetValue(key, out var sheetName))
                            {
                                valueRangeDict[sheetName] = valueRange;
                            }
                        }
                    }

                    var progress = ((float)(batchIndex + 1) / totalBatches) * 100;
                }
                catch (Exception ex)
                {
                    // ignored
                }

                await Task.Delay(100);
            }

            var input = valueRangeDict.ToDictionary(kvp => kvp.Key, kvp => new CustomValueRange
            {
                Values         = kvp.Value.Values,
                ETag           = kvp.Value.ETag,
                Range          = kvp.Value.Range,
                MajorDimension = kvp.Value.MajorDimension
            });

            await this.GetCustomValueRange(input);

            return input;
        }

        private string GetColumnName(int columnIndex)
        {
            columnIndex--;
            var columnName = "";

            while (columnIndex >= 0)
            {
                int remainder = columnIndex % 26;
                columnName  = (char)(remainder + 'A') + columnName;
                columnIndex = (columnIndex / 26) - 1;
            }

            return columnName;
        }

        private async Task GetCustomValueRange(Dictionary<string, CustomValueRange> input) { }

        private string GetDataFromSheetName(string sheetName, Dictionary<string, CustomValueRange> allSheetDatas)
        {
            var csvOutput = "";

            var values = allSheetDatas[sheetName].Values;

            if (values is { Count: > 0 })
            {
                using var writer = new StringWriter();

                var headerRow = values[0];
                writer.WriteLine(string.Join(",", headerRow.Select(cell => this.EscapeCsvValue(cell?.ToString() ?? string.Empty))));

                for (var i = 1; i < values.Count; i++)
                {
                    var row = values[i];

                    if (row == null || row.All(cell => cell == null)) continue;

                    {
                        var csvRow = row.Select(cell => cell?.ToString() != null ? this.EscapeCsvValue(cell.ToString()) : string.Empty);
                        writer.WriteLine(string.Join(",", csvRow));
                    }
                }

                csvOutput = writer.ToString();
            }

            return csvOutput;
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                value = "\"" + value + "\"";
            }

            return value;
        }

        #endregion

        #region Addressable Flow

        private void AddBlueprintFolderPathToAddressable(string assetPath, string addressableKey, string groupName, string label)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            var group = settings.FindGroup(groupName);

            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (asset == null)
            {
                Debug.LogError($"Asset at {assetPath} not found.");

                return;
            }

            var guid  = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = addressableKey;
            var defaultLabel = addressableKey;

            if (!settings.GetLabels().Contains(defaultLabel))
            {
                settings.AddLabel(defaultLabel);
            }

            if (!entry.labels.Contains(defaultLabel))
            {
                entry.SetLabel(defaultLabel, true);
            }

            if (!string.IsNullOrEmpty(label))
            {
                if (!settings.GetLabels().Contains(label))
                {
                    settings.AddLabel(label);
                }

                if (!entry.labels.Contains(label))
                {
                    entry.SetLabel(label, true);
                }
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log($"Added to Addressable: {assetPath} → Group: {groupName} with key '{addressableKey}'");
        }

        private async Task ProcessAddressable(BlueprintWorkFlowData data)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            var profileName         = data.addressableProfile;
            var copiedFromProfileId = settings.activeProfileId;
            var profileSettings     = settings.profileSettings;

            var profileId = profileSettings.GetProfileId(profileName);

            if (!string.IsNullOrEmpty(profileId))
            {
                CommonServicesHelper.LogMessage($"Profile '{profileName}' already exists, ignore created new.");
            }
            else
            {
                profileId = profileSettings.AddProfile(profileName, copiedFromProfileId);
                CommonServicesHelper.LogMessage($"Created new Addressable profile: {profileName} (ID: {profileId})");
            }

            profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, data.remoteBuildPath);
            profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, data.remoteLoadPath);

            this.EnsureVariableExists(profileSettings, "EnvironmentName", "Development");
            profileSettings.SetValue(profileId, "EnvironmentName", data.environmentName);
            settings.activeProfileId = profileId;

            var group = settings.FindGroup(data.groupAssignBlueprint);

            if (group == null)
            {
                group = settings.CreateGroup(data.groupAssignBlueprint, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var schema = group.GetSchema<BundledAssetGroupSchema>();

            schema.BuildPath.SetVariableByName(settings, "Remote.BuildPath");
            schema.LoadPath.SetVariableByName(settings, "Remote.LoadPath");
            schema.UseAssetBundleCache            = true;
            schema.UseAssetBundleCrc              = true;
            schema.BundleMode                     = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            schema.AssetBundledCacheClearBehavior = BundledAssetGroupSchema.CacheClearBehavior.ClearWhenWhenNewVersionLoaded;
            EditorUtility.SetDirty(schema);
            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
        }

        private void EnsureVariableExists(AddressableAssetProfileSettings settings, string varName, string defaultValue)
        {
            if (!settings.GetAllProfileNames().Contains(varName))
            {
                settings.CreateValue(varName, defaultValue);
            }
        }

        private BlueprintWorkFlowData GetBlueprintWorkFlowData()
        {
            // return this.TestingGetBlueprintWorkFlowData();
            var result = CommonServicesHelper.GetDataModel<BlueprintWorkFlowData>(CommonServicesHelper.GetPathInformation("BlueprintWorkFlowData.json"));
            result.remoteBuildPath = $"{result.remoteBuildPath}/{result.environmentName}/{result.platFormName}/{result.versionName}";
            result.remoteLoadPath  = $"{result.remoteLoadPath}/{result.remoteBuildPath}";

            return result;
        }

        private BlueprintWorkFlowData TestingGetBlueprintWorkFlowData()
        {
            var data = new BlueprintWorkFlowData();
            data.addressableProfile   = "Testing";
            data.environmentName      = "production";
            data.remoteBuildPath      = $"CCDBuildData/{data.environmentName}";
            data.remoteLoadPath       = $"https://github.com/LifeGameStudio/Storage/raw/refs/heads/main/{data.remoteBuildPath}/{data.environmentName}";
            data.groupAssignBlueprint = "VideoGithub";
            data.ccdPlatform          = "github";

            data.googleDriveData = new GoogleDriveData()
            {
                servicesAccountJson =
                    "{   \"type\": \"service_account\",   \"project_id\": \"uploaddriver-427404\",   \"private_key_id\": \"a6d2684e1c7b2d0438cf0bff592f0111b1dbf8b6\",   \"private_key\": \"-----BEGIN PRIVATE KEY-----\\nMIIEuwIBADANBgkqhkiG9w0BAQEFAASCBKUwggShAgEAAoIBAQDdm0st6GQUKnBq\\nR8iUbf9jKOy2WfPw5wS/LHBZuYS3eDxf+gxz7eFJoBTf65MrIhNLPrsqWNnXnlMN\\n7aqGz+eF2X7xc+PAXJwdF6zhnl9Dt8jQJZWeUxSapBOdDBm9NXJmwU5Wy17v0Oh/\\nHxIgi4FeiPTCsnP8HAMLf6qL7APBR3/SFAPXkKnylEB6yKO36Jh13HNg6NwEcyty\\ncqr1BXtbN3S8rhRMAIM3wTklzyReioo4okiQA/78pA62HyqO/lBqs6OWEmCF6LTs\\nPBeOMUhYJtbeEyVIPrhRz3jy7jJM2ZhNLwci7gW9NA2S/OAXOARkyLKgVZYRG723\\nDmABcdzvAgMBAAECgf9UsC7gMcS38CqwcxO0UFUeZVq7qqS5ljpk1MFwM0sE0m5A\\nP3mC1DsZvT48/0oUBxxYenYMjj1cqU9pgz99RaFfsD6oXMws4eIckW+qyVtJWx2N\\nw3nqMistmIaQ/eQtlWn4MDzqmSMu2CdXGAiqvKWJUHg5RPljifuf6VTDzu5LGNHc\\nZ+vqAICaFPLx9LYXzzRjLFqXIcxAp2VqY8Z6kobMEFskszHHMAifuYyrse/pQMJj\\n49hwyQQYJWZMrdUXtDRlPfOAtMH89G4y87xbACyIq+XVfAEeV8uv7o6imNeD7/bD\\nLXAwq82VEXGyFgRz36iHzLc3U2+JApQRtdGjj/ECgYEA8GmPRdAjlZeZA6SxsazF\\nducyjyL55/fx4F4mf5r7hntP1YCrLG2YXDWG9NeK++WTwI4vkyc4dwiOiEtZJy8y\\nc38V4RR3YKARMnem3+RN/wa7WbOFcLp3ew+6mGbmMG7yik3zBExrMfbRVTI8Utj7\\naxTIAXhGGoePlMIVEAymTpECgYEA6/mXMTLNPmMMHO3jgrS7bcx5uT+2/KMXXTbf\\n8wApV8c+4Wr3HnDQcMTuXXAUVtvmpDiqyXpVdRReX08XqGMwszqxWaJWuE2EbK7k\\nwtodNnmCyCn9VoUE9Miun2Pw9A2UrtZOPTAQlS70UFQGpSDdq+FW1b+4TF00o7v1\\nrh2qM38CgYEAqVlicEYW2uhoA/X+qe7PRlvD9KopqeqxemA39EljBq9UZEv3yBsH\\naWTXRR+UKq7kbo56GslU8ByZ8o5JJd4MRultqxh0ox7+HjPE3BABlTTTwnM/+1GO\\nmqRQx8wsOE/fD+eq2QtPs2luufniHmX0bNC9trNXhpaZYKt6lMykVdECgYBtl7Rd\\nOA/USqHkiaMhIBjwLIfXvjyY5pHCS+sEa23IA5Qzkr8EVzanOP7PTG9Vy7k5Scwf\\n2H356yTNNOly3eZPRxH45AlMfUvkQfGigTQSCarwlXfAB/U+TjmzcvIEFo6YCJW7\\nmygIcQ8sg2m2pSXuXrA6g1jvtlXtOS0n2UUjtwKBgCHcrFb2luBAwDlkv0E1H80A\\nuOOy2FKAhveeXF9AHx8xzJoV7SNz48CZTl7VWF6cfuRv8Ez9dm0lnivnrk/ZALrv\\nzN5ZtssiXiWbcGvs1+BbSmy+glR8oY4ubEJDxTyEt2yfkl4BS7/udUGHPMWcEa+N\\nE7rsPqdOiHA4Xh14tXJc\\n-----END PRIVATE KEY-----\\n\",   \"client_email\": \"game-duclh@uploaddriver-427404.iam.gserviceaccount.com\",   \"client_id\": \"101582196153623358428\",   \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\",   \"token_uri\": \"https://oauth2.googleapis.com/token\",   \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\",   \"client_x509_cert_url\": \"https://www.googleapis.com/robot/v1/metadata/x509/game-duclh%40uploaddriver-427404.iam.gserviceaccount.com\",   \"universe_domain\": \"googleapis.com\" } ",
                sheetBuilderConfig = "BuilderConfig",
                spreadsheetId      = "1zX_wbU4Nc1p4_LoW2LK04jhFg8DqAc_J_WmVgoFPbbA"
            };

            data.githubCdnData = new GithubCdnData()
            {
                gitHubUrl  = "git@github.com:LifeGameStudio/Storage.git",
                branchName = "main"
            };

            CommonServicesHelper.LogMessage($"{JsonUtility.ToJson(data)}");

            return data;
        }

        #endregion
    }
}

#endif