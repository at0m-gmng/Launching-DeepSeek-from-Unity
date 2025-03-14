namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using GameResources.Features.SystemNotification.Scripts;
    using GameResources.Features.SystemNotification.Scripts.Interfaces;
    using GameResources.Services.Scripts;
    using ProcessController;
    using UnityEngine;
    using Zenject;
    using Debug = UnityEngine.Debug;
    using Microsoft.Win32;

    public class PythonDependencyInstallController: ISystemNotification, IService
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService, ProcessService _processService)
        {
            systemMessageService = _systemMessageService;
            processService = _processService;
            systemMessageService.RegisterMessage(this);
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
        protected IntPtr processHeader;
        
        protected readonly string dependenciesPath;
        
        protected string pythonPath;
        protected string requirementsPath;

        public virtual async Task<bool> TryRegister()
        {
            requirementsPath = Path.Combine(Application.streamingAssetsPath, dependenciesPath);

            if (!File.Exists(requirementsPath))
            {
                Debug.LogError("Requirements.txt file not found at path:" + requirementsPath);
                return false;
            }

            pythonPath = await GetPythonPathFromRegistry();
            Debug.LogError($"pythonPath {pythonPath}");
            Debug.LogError($"requirementsPath {requirementsPath}");

            if (string.IsNullOrEmpty(pythonPath))
            {
                Debug.LogError("Python not found on the system.");
                return false;
            }

            return await InstallDependencies();
        }
        
        protected async Task<string> GetPythonPathFromRegistry()
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

                return null;
            });
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
                
                if (success)
                {
                    processHeader = processInfo.hProcess;
                    processService.RegisterProcess(processHeader);

                    WindowsJobObjectApi.CloseHandle(processInfo.hProcess);
                    WindowsJobObjectApi.CloseHandle(processInfo.hThread);
                    return true;
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