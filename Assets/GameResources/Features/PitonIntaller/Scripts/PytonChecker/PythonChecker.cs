﻿namespace GameResources.Features.PitonIntaller.Scripts.PytonChecker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using UnityEngine;

    public class PythonChecker : GameResources.Features.FileChecker.Scripts.BaseFileCheker
    {
        public PythonChecker(string _targetFolder, string[] _requiredFiles) : base(_targetFolder, _requiredFiles)
        {
            requiredFiles = _requiredFiles
            .Concat(new []
            {
                $"{Application.streamingAssetsPath}/python-3.9.7-amd64.exe",
                $"{Application.streamingAssetsPath}/python_installer.exe",
                
            }).ToArray();
            
            targetFolder = PlayerPrefs.GetString(pythonPathKey, string.Empty);
        }

        protected readonly string pythonPathKey = "PythonPath";
        protected string foundPath;
        
        public override bool IsContains() => !string.IsNullOrEmpty(TryGetPythonPath());

        public virtual string TryGetPythonExecutableFile()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "python",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        string[] paths = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (paths.Length > 0 && File.Exists(paths[0]))
                        {
                            return paths[0];
                        }
                    }
                }
            }
            catch { }

            return string.Empty;
        }
        
        protected virtual string TryGetPythonPath()
        {
            // 1. Check the cached path
            if (!string.IsNullOrEmpty(targetFolder) && File.Exists(targetFolder))
            {
                return targetFolder;
            }

            // 2. Check in the system
            foundPath = TryFindPython();
            if (!string.IsNullOrEmpty(foundPath))
            {
                targetFolder = foundPath;
                PlayerPrefs.SetString(pythonPathKey, foundPath);
                PlayerPrefs.Save();
            }

            return foundPath;
        }

        protected virtual string TryFindPython()
        {
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (File.Exists(requiredFiles[i]))
                {
                    return requiredFiles[i];
                }
            }

            return TryGetPythonExecutableFile();
        }
    }
}