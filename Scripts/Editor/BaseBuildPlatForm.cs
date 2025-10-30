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
using UnityEditor.Build;
using UnityEngine;
#if ADDRESSABLE
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

namespace BuildHelper.Workflows
{
    public abstract class BaseBuildPlatForm
    {
        public virtual async Task SetUpAndBuild(IBuildInformation data)
        {
            this.ResetBuildSettings();
            CommonServicesHelper.CheckToClearCached(data);
            this.TryDisableLogo();

            this.BuildAddressable(data);

            EditorUserBuildSettings.development = data.IsDevelopment();
        }

        protected void SetOrientation(bool lanscape)
        {
#if UNITY_ANDROID || UNITY_IOS

            if (lanscape)
            {
                PlayerSettings.defaultInterfaceOrientation           = UIOrientation.LandscapeLeft;
                PlayerSettings.allowedAutorotateToLandscapeLeft      = true;
                PlayerSettings.allowedAutorotateToLandscapeRight     = true;
                PlayerSettings.allowedAutorotateToPortrait           = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            }
            else
            {
                PlayerSettings.defaultInterfaceOrientation           = UIOrientation.Portrait;
                PlayerSettings.allowedAutorotateToLandscapeLeft      = false;
                PlayerSettings.allowedAutorotateToLandscapeRight     = false;
                PlayerSettings.allowedAutorotateToPortrait           = true;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            }
#endif
        }

        protected virtual void TryDisableLogo()
        {
            try
            {
                PlayerSettings.SplashScreen.showUnityLogo = false;
            }
            catch (Exception e)
            {
                CommonServicesHelper.LogMessage(e);
            }
        }

        private void FindAndSetGameVersion(IBuildInformation data)
        {
            var path                   = Application.dataPath;
            var featureGameVersionPath = $"{path}/FeatureTemplate/Scripts/Services/FeatureGameVersion.cs";

            if (!File.Exists(featureGameVersionPath)) return;

            var fileContent = File.ReadAllText(featureGameVersionPath);

            var newBuildInfo   = $"BuildInfo=\"Unity Version: {Application.unityVersion} | Build: {PlayerSettings.bundleVersion} - {data.VersionCode} - {DateTime.Now}\";";
            var updatedContent = Regex.Replace(fileContent, @"BuildInfo\s*=\s*\"".*\"";", newBuildInfo);
            File.WriteAllText(featureGameVersionPath, updatedContent);
        }

        public void SetupBlueprintPath(IBuildInformation data)
        {
            var blueprintConfig = $"{Application.dataPath}/Resources/GameConfigs/BlueprintConfig.asset";

            if (!File.Exists(blueprintConfig))
            {
                CommonServicesHelper.LogMessage("Blueprint config not found");

                return;
            }

            if (!data.DefineSymbol.Contains("SET_BLUEPRINT_PATH"))
            {
                CommonServicesHelper.LogMessage("No need to set blueprint path");

                return;
            }

            var content     = File.ReadAllText(blueprintConfig);
            var pattern     = @"(resourceBlueprintPath:\s*).*";
            var replacement = $"$1{data.BlueprintPath}/";

            var result = Regex.Replace(content, pattern, replacement);
            File.WriteAllText(blueprintConfig, result);
            CommonServicesHelper.LogMessage($"Reset blueprint path to {data.BlueprintPath}");
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
                    schema.Compression                       = BundledAssetGroupSchema.BundleCompressionMode.LZMA;
                    schema.UseUnityWebRequestForLocalBundles = false;
                }
            }
#endif
        }

        protected virtual void BuildAddressable(IBuildInformation data)
        {
#if ADDRESSABLE
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null) return;
#if !NO_LZMA
            SetAllGroupsToLZMA();
#endif
            CommonServicesHelper.LogMessage($"--------------------");
            CommonServicesHelper.LogMessage($"Clean addressable");
            CommonServicesHelper.LogMessage($"--------------------");
            AddressableAssetSettings.CleanPlayerContent();
            CommonServicesHelper.LogMessage($"--------------------");
            CommonServicesHelper.LogMessage($"Build addressable");
            CommonServicesHelper.LogMessage($"--------------------");
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
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

#endif
        }

        protected Task AfterBuild(IBuildInformation data) { return UnityCCD.ProcessCcd(); }

        protected void SetScriptDefineSymbols(NamedBuildTarget targetGroup, string[] scripts) { PlayerSettings.SetScriptingDefineSymbols(targetGroup, scripts); }

        protected string[] LoadSceneOnPath()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            return scenes;
        }
    }
}