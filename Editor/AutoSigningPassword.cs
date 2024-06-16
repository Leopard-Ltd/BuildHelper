using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AutoSigningPassword
{
#if UNITY_EDITOR
    static        string keystorePass = "123456";
    static        string keyaliasName = "hai";
    static        string keyaliasPass = "123456";
    static        bool   allowLoad;
    public static string filePath = "user.keystore";

    static AutoSigningPassword()
    {
        var dPath = Application.dataPath;
        dPath = dPath.Replace("Assets", "");
        dPath = $"{dPath}/{filePath}";

        var finalPath = dPath;
        allowLoad = EditorPrefs.GetInt("signing") == 1 ? true : false;
        allowLoad = true;

        if (!allowLoad)
        {
            PlayerSettings.Android.useCustomKeystore = false;

            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName      = finalPath;
        PlayerSettings.keystorePass              = keystorePass;
        PlayerSettings.Android.keyaliasName      = keyaliasName;
        PlayerSettings.keyaliasPass              = keyaliasPass;
    }

#endif

    [MenuItem("Tools/AutoSignPass")]
    public static void AllowAutoSign()
    {
        var dPath = Application.dataPath;
        dPath = dPath.Replace("Assets", "");
        dPath = $"{dPath}{filePath}";

        var finalPath = dPath;
        allowLoad = !allowLoad;

        if (allowLoad)
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName      = finalPath;
            PlayerSettings.keystorePass              = keystorePass;
            PlayerSettings.Android.keyaliasName      = keyaliasName;
            PlayerSettings.keyaliasPass              = keyaliasPass;
            EditorPrefs.SetInt("signing", 1);
        }
        else
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName      = finalPath;
            PlayerSettings.keystorePass              = "";
            PlayerSettings.Android.keyaliasName      = "";
            PlayerSettings.keyaliasPass              = "";
            EditorPrefs.SetInt("signing", 0);
        }

        Debug.Log("AutoSignPass " + allowLoad);
    }

    public static void SetKeyPass()
    {
        var dPath = Application.dataPath;
        dPath = dPath.Replace("Assets", "");
        dPath = $"{dPath}{filePath}";

        var finalPath = dPath;
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName      = finalPath;
        PlayerSettings.keystorePass              = keystorePass;
        PlayerSettings.Android.keyaliasName      = keyaliasName;
        PlayerSettings.keyaliasPass              = keyaliasPass;
        EditorPrefs.SetInt("signing", 1);
    }

    private static string SetPath()
    {
        var dPath = Application.dataPath;
        var temp  = dPath.Split(':');

        return temp[0];
    }
}