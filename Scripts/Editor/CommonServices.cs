using System.IO;
using System.Text;
using UnityEngine;

public class CommonServices
{
    public static string GetPathBuildInformation()
    {
        var filePath = Application.dataPath;
        filePath = filePath.Replace("Assets", "");
        filePath = $"{filePath}/BuildInformation.json";

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
}