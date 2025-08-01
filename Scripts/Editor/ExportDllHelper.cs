namespace BuildHelper.Workflows
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEditor.Compilation;

    public class ExportDllHelper
    {
        public static async void ExportDll()
        {
            var sourLibs     = $"{CommonServicesHelper.GetProjectPath()}/Packages/BuildHelper/Libs/";
            var localGitPath = await CloneProject();
            var outputPath   = $"{localGitPath}/Editor";
            var assemblyName = "BuildHelper";
            var assemblies   = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            var buildMenuFile = $"{CommonServicesHelper.GetProjectPath()}/Packages/BuildHelper/Scripts/Editor/BuildMenu.cs";

            if (File.Exists(buildMenuFile))
            {
                File.Copy($"{buildMenuFile}", Path.Combine(outputPath, "BuildMenu.cs"), true);
                File.Copy($"{buildMenuFile}.meta", Path.Combine(outputPath, "BuildMenu.cs.meta"), true);
            }

            CopyAllFolderToTargetFolder(sourLibs, $"{localGitPath}/Libs");

            foreach (var asm in assemblies)
            {
                if (asm.name == assemblyName)
                {
                    File.Copy(asm.outputPath, Path.Combine(outputPath, $"{assemblyName}.dll"), true);
                    CommonServicesHelper.LogMessage($"Exported {assemblyName}.dll to {outputPath}");

                    break;
                }
            }

            CommitAndDelete(localGitPath);
        }

        private static async void CommitAndDelete(string localGitPath)
        {
            var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await CommonServicesHelper.RunTerminalCommandAsync("git add .", localGitPath);
            await CommonServicesHelper.RunTerminalCommandAsync($"git commit -m \"Update libs at {currentTime}\"", localGitPath);
            await CommonServicesHelper.RunTerminalCommandAsync($"git push origin develop", localGitPath);
            CommonServicesHelper.LogMessage("Update libs successfully, now deleting local git repository...");

            ForceDeleteDirectory(localGitPath);
        }

        private static void CopyAllFolderToTargetFolder(string sourceFolder, string targetFolder)
        {
            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                var fileName     = Path.GetFileName(file);
                var destFilePath = Path.Combine(targetFolder, fileName);
                File.Copy(file, destFilePath, true);
                CommonServicesHelper.LogMessage($"Copied {fileName} to {targetFolder}");
            }

            foreach (var dir in Directory.GetDirectories(sourceFolder))
            {
                var dirName    = Path.GetFileName(dir);
                var destSubDir = Path.Combine(targetFolder, dirName);

                Directory.CreateDirectory(destSubDir);

                CopyAllFolderToTargetFolder(dir, destSubDir);
            }
        }

        static async Task<string> CloneProject()
        {
            var gitUrl = "git@github.com:Leopard-Ltd/BuildHelperRuntime.git";

            var rootPath     = CommonServicesHelper.GetRootPath();
            var tmp          = rootPath.Split("/");
            var listProject  = string.Join("/", tmp, 0, tmp.Length - 2);
            var localGitPath = $"{listProject}/BuildHelperRuntime";
            ForceDeleteDirectory(localGitPath);

            CommonServicesHelper.LogMessage($"Start clone");
            await CommonServicesHelper.RunTerminalCommandAsync($"git clone {gitUrl} {localGitPath}");

            try
            {
                await CommonServicesHelper.RunTerminalCommandAsync("git checkout develop", localGitPath);
            }
            catch
            {
                await CommonServicesHelper.RunTerminalCommandAsync("git checkout -b develop origin/develop", localGitPath);
            }

            return localGitPath;
        }

        private static void ForceDeleteDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return;
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
    }
}