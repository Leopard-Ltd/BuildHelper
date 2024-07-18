using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class CommonServices
{
    public static string GetPathBuildInformation(string fileName)
    {
        var filePath = Application.dataPath;
        filePath = filePath.Replace("Assets", "");
        filePath = $"{filePath}/{fileName}";

        return filePath;
    }

    public static T GetDataModel<T>(string filePath) where T : class
    {
        T data = null;

        var fileContents = File.ReadAllText(filePath, Encoding.UTF8);
        fileContents = fileContents.Replace("\n", "").Replace("\r", "");

        data = JsonUtility.FromJson<T>(fileContents);

        return data;
    }

    public static bool IsBatchMode()
    {
        var args = Environment.GetCommandLineArgs().ToList();
        var isBatchMode= args.Contains("-batchmode");
        Console.WriteLine($"Command Line Ne {string.Join(",", args)}, {isBatchMode}");

        return isBatchMode;
    }
}