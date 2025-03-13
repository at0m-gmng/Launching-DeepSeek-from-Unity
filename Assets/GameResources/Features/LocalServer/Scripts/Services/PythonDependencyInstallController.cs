namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using GameResources.Features.SystemNotification.Scripts;
    using GameResources.Features.SystemNotification.Scripts.Interfaces;
    using GameResources.Services.Scripts;
    using ProcessController;
    using UnityEngine;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class PythonDependencyInstallController: ISystemNotification, IService
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService, ProcessService _processService)
        {
            systemMessageService = _systemMessageService;
            processService = _processService;
            systemMessageService.RegisterMessage(this);
            processService.RegisterProcess(process);
        }

        [Inject]
        public PythonDependencyInstallController(string _dependenciesPath)
        {
            dependenciesPath = _dependenciesPath;
        }
     
        protected const string INSTALL_DEPENDENCIES = "Installing dependencies...";
        protected const string INSTALL_DEPENDENCIES_ENDED = "Installation of dependencies is complete";
        protected const string ERROR = "Error: {0}";
        
        public event Action<string> onMessage = delegate { };

        protected SystemMessageService systemMessageService = default;
        protected ProcessService processService = default;
        protected Process process;
        protected ProcessStartInfo data = default;
        
        protected readonly string dependenciesPath;
        
        protected string pythonPath;
        protected string requirementsPath;
        protected string[] ignoreErrorFields = new string[]
        {
            "WARNING"
        };

        public virtual async Task<bool> TryRegister()
        {
            requirementsPath = Path.Combine(Application.streamingAssetsPath, dependenciesPath);

            if (!File.Exists(requirementsPath))
            {
                Debug.LogError("Requirements.txt file not found at path:" + requirementsPath);
                return false;
            }
            
            pythonPath = await FindPythonPath();

            if (string.IsNullOrEmpty(pythonPath))
            {
                Debug.LogError("Python not found on the system.");
                return false;
            }

            return await InstallDependencies();
        }

        protected async Task<string> FindPythonPath()
        {
            return await Task.Run(() =>
            {
                data = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "python",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process = new Process { StartInfo = data };
                
                using (process)
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)[0];
                    }
                    else
                    {
                        Debug.LogError(string.Format(ERROR, error));
                        return null;
                    }
                }
            });
        }

        protected virtual async Task<bool> InstallDependencies()
        {
            await Task.Run(() =>
            {
                data = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-m pip install -r \"{requirementsPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process = new Process { StartInfo = data };

                using (process)
                {
                    process.Start();
                    onMessage(INSTALL_DEPENDENCIES);
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error) && !IsContainsString(error))
                    {
                        Debug.LogError(string.Format(ERROR, error));
                        return false;
                    }
                    else
                    {
                        onMessage(INSTALL_DEPENDENCIES_ENDED);
                        Debug.LogError(INSTALL_DEPENDENCIES_ENDED);
                        return true;
                    }
                }
            });

            return true;
        }

        protected virtual bool IsContainsString(string error)
        {
            for (int i = 0; i < ignoreErrorFields.Length; i++)
            {
                if (error.Contains(ignoreErrorFields[i]))
                {
                    return true;
                }
            }

            return false;
        }
        
        public virtual void OnApplicationQuit()
        {
            if (process != null && !process.HasExited)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error terminating process: {ex.Message}");
                }
            }
            else
            {
                if (process == null)
                {
                    Debug.LogError($"Process null");
                }
                if(process != null && process.HasExited)
                {
                    Debug.LogError($"Process not exist");
                }
            }
        }
    }
}