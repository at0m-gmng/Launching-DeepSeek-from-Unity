namespace GameResources.Features.PithonInstaller.Scripts.PythonChecker
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using GameResources.Features.FileChecker.Scripts;
    using Microsoft.Win32;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public class PythonChecker : BaseFileCheker
    {
        public PythonChecker(string _targetFolder, string[] _requiredFiles) : base(_targetFolder, _requiredFiles)
        {
            requiredFiles = _requiredFiles
            .Concat(new []
            {
                $"{Application.streamingAssetsPath}/python-3.9.7-amd64.exe",
                $"{Application.streamingAssetsPath}/python_installer.exe",
                
            }).ToArray();
            
            targetFolder = Application.streamingAssetsPath;
        }

        public bool IsInstall { get; protected set; } = false;
        
        
        public override async Task<bool> IsContains() 
            => !string.IsNullOrEmpty(FoundPath) || !string.IsNullOrEmpty(await TryGetPythonPath());

        public virtual async Task<string> TryGetPythonPath()
        {
            foundPath = await TryFindPython();

            IsInstall = !string.IsNullOrEmpty(foundPath) && !foundPath.Contains(Application.streamingAssetsPath);
            return foundPath;
        }

        protected virtual async Task<string> TryFindPython()
        {
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (File.Exists(requiredFiles[i]))
                {
                    return requiredFiles[i];
                }
            }

            return await TryGetPythonExecutableFile();
        }
        
        protected virtual async Task<string> TryGetPythonExecutableFile()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Python\PythonCore"))
                    {
                        var versions = key?.GetSubKeyNames();
                        var latestVersion = versions?.OrderByDescending(v => v).FirstOrDefault();
                        if (latestVersion != null)
                        {
                            using (var installKey = key.OpenSubKey($@"{latestVersion}\InstallPath"))
                            {
                                return installKey?.GetValue("ExecutablePath")?.ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ошибка чтения реестра: {ex.Message}");
                }

                return string.Empty;
            });
        }
    }
}