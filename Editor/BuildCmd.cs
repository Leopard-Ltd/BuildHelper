using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BuildCmd
{
    private static string bundleId = Application.identifier;

    [MenuItem("Build/Build Android")]
    static void Build()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, bundleId);
        AutoSigningPassword.SetKeyPass();

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes           = LoadSceneOnPath(),
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
            locationPathName = GetBuildPath()
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    static string[] LoadSceneOnPath()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        return scenes;
    }

    static string GetBuildPath() { return Path.GetFullPath($"../Build/Client/Android/{Application.identifier}.apk"); }
}