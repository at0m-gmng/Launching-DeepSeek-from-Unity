namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using GameResources.Features.SystemNotification.Scripts;
    using GameResources.Features.SystemNotification.Scripts.Interfaces;
    using GameResources.Services.Scripts;
    using ProcessController;
    using UnityEngine;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class PythonDependencyInstallController: IProgressSystemNotification, IService
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService, ProcessService _processService)
        {
            systemMessageService = _systemMessageService;
            processService = _processService;
            systemMessageService.RegisterMessage(this);
        }

        [Inject]
        public PythonDependencyInstallController(string _dependenciesPath, string _pythonPath)
        {
            dependenciesPath = _dependenciesPath;
            pythonPath = _pythonPath;
        }
     
        protected const string INSTALL_DEPENDENCIES = "Installing dependencies...";
        protected const string INSTALL_DEPENDENCIES_ENDED = "Installation of dependencies is complete";
        protected const string ERROR = "Error: {0}";
        
        public event Action<string> onMessage = delegate { };
        public event Action<string, float> onMessageProgress = delegate { };

        protected SystemMessageService systemMessageService = default;
        protected ProcessService processService = default;
        protected IntPtr processHeader;
        
        protected readonly string dependenciesPath;
        protected readonly string pythonPath;

        protected string requirementsPath;

        public virtual async Task<bool> TryRegister()
        {
            requirementsPath = Path.Combine(Application.streamingAssetsPath, dependenciesPath);

            if (!File.Exists(requirementsPath))
            {
                Debug.LogError("Requirements.txt file not found at path:" + requirementsPath);
                return false;
            }

            onMessageProgress("Start install dependencies", 0f);
            await Task.Delay(100);
            
            if (await InstallDependencies())
            {
                onMessageProgress("Dependencies installation complete", 1f);
                return true;
            }
            else
            {
                return false;
            } 
        }

        protected virtual async Task<bool> InstallDependencies()
        {
            return await Task<bool>.Run(() =>
            {
                string workingDir = @$"{Path.GetDirectoryName(requirementsPath)}";
                string commandLine = $"\"{pythonPath}\" -m pip install -r \"{requirementsPath}\"";
                
                var startupInfo = new  WindowsJobObjectApi.STARTUPINFO();
                startupInfo.cb = Marshal.SizeOf(startupInfo);
                var processInfo = new WindowsJobObjectApi.PROCESS_INFORMATION();

                bool success = WindowsJobObjectApi.CreateProcess(
                    null,               // Path to executable (null, since specified in commandLine)
                    commandLine,        // Command line with arguments
                    IntPtr.Zero,        // Process attributes (default)
                    IntPtr.Zero,        // Thread attributes (default)
                    false,              // Do not inherit handles
                    WindowsJobObjectApi.CREATE_NO_WINDOW, // Creation flags (default)
                    IntPtr.Zero,        // Environment (default)
                    workingDir,         // Working directory
                    ref startupInfo,    // Startup parameters
                    out processInfo     // Information about the running process
                );
                
                processHeader = processInfo.hProcess;
                processService.RegisterProcess(processHeader);
                
                if (success)
                {
                    // Ожидание завершения процесса
                    uint result = WindowsJobObjectApi.WaitForSingleObject(processInfo.hProcess, WindowsJobObjectApi.INFINITE);

                    if (result == WindowsJobObjectApi.WAIT_OBJECT_0)
                    {
                        // Процесс завершился успешно
                        uint exitCode;
                        WindowsJobObjectApi.GetExitCodeProcess(processInfo.hProcess, out exitCode);

                        if (exitCode == 0)
                        {
                            Debug.LogError("Процесс завершился успешно");
                            return true;
                        }
                        else
                        {
                            Debug.LogError($"Процесс завершился с ошибкой, код выхода: {exitCode}");
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogError($"WaitForSingleObject завершился с ошибкой, результат: {result}");
                        return false;
                    }
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.LogError($"{string.Format(ERROR, error)}");
                    return false;
                }
            });
        }
    }
}