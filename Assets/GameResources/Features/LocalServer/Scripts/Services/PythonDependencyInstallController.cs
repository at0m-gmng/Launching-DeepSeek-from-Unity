namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using GameResources.Features.SystemNotification.Scripts;
    using GameResources.Features.SystemNotification.Scripts.Interfaces;
    using GameResources.Services.Scripts;
    using UnityEngine;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class PythonDependencyInstallController: ISystemNotification, IService
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService)
        {
            systemMessageService = _systemMessageService;
            systemMessageService.RegisterMessage(this);
        }

        [Inject]
        public PythonDependencyInstallController(string _dependenciesPath)
        {
            dependenciesPath = _dependenciesPath;
        }
     
        protected const string INSTALL_DEPENDENCIES = "Установка зависимостей...";
        protected const string INSTALL_DEPENDENCIES_ENDED = "Установка зависимостей завершена";
        protected const string ERROR = "Ошибка: {0}";
        
        public event Action<string> onMessage = delegate { };

        protected readonly string dependenciesPath;
        protected string pythonPath;
        protected string requirementsPath;
        protected Process process;
        protected ProcessStartInfo data = default;
        protected SystemMessageService systemMessageService = default;
        protected string[] ignoreErrorFields = new string[]
        {
            "WARNING"
        };

        public virtual async Task<bool> TryRegister()
        {
            requirementsPath = Path.Combine(Application.streamingAssetsPath, dependenciesPath);

            if (!File.Exists(requirementsPath))
            {
                Debug.LogError("Не найден файл requirements.txt по пути: " + requirementsPath);
                return false;
            }
            
            pythonPath = await FindPythonPath();

            if (string.IsNullOrEmpty(pythonPath))
            {
                Debug.LogError("Python не найден в системе.");
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
                    Debug.LogError($"Ошибка при завершении процесса: {ex.Message}");
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