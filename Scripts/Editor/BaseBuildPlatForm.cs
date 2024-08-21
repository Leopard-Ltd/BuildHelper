using System;
using System.Linq;
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
        this.FindAndSetGameVersion();

        this.ResetBuildSettings();
        this.BuildAddressable();

        EditorUserBuildSettings.development = data.IsDevelopment();
    }

    private void FindAndSetGameVersion()
    {
        var path                   = Application.dataPath;
        var featureGameVersionPath = $"{path}/FeatureTemplate/Scripts/Services/FeatureGameVersion.cs";

        if (!System.IO.File.Exists(featureGameVersionPath)) return;

        var fileContent = System.IO.File.ReadAllText(featureGameVersionPath);

        var newBuildInfo   = $"BuildInfo=\"Unity Version: {Application.unityVersion} | Build: {PlayerSettings.bundleVersion} - {PlayerSettings.Android.bundleVersionCode} - {System.DateTime.Now}\";";
        var updatedContent = System.Text.RegularExpressions.Regex.Replace(fileContent, @"BuildInfo\s*=\s*\"".*\"";", newBuildInfo);
        System.IO.File.WriteAllText(featureGameVersionPath, updatedContent);
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

    private void SetAllGroupsToLZMA()
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

    private void BuildAddressable()
    {
#if ADDRESSABLE
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null) return;
#if !NO_LZMA
        SetAllGroupsToLZMA();
#endif
        Console.WriteLine($"--------------------");
        Console.WriteLine($"Clean addressable");
        Console.WriteLine($"--------------------");
        AddressableAssetSettings.CleanPlayerContent();
        Console.WriteLine($"--------------------");
        Console.WriteLine($"Build addressable");
        Console.WriteLine($"--------------------");
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        var success = string.IsNullOrEmpty(result.Error);

        if (!success)
        {
            var errorMessage = "Addressable build error encountered: " + result.Error;
            Debug.LogError(errorMessage);

            throw new Exception(errorMessage);
        }

        Console.WriteLine($"--------------------");
        Console.WriteLine($"Finish building addressable");
        Console.WriteLine($"--------------------");
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
}