namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.Diagnostics;
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
                    null,               // Путь к исполняемому файлу (null, так как указан в commandLine)
                    commandLine,        // Командная строка с аргументами
                    IntPtr.Zero,        // Атрибуты процесса (по умолчанию)
                    IntPtr.Zero,        // Атрибуты потока (по умолчанию)
                    false,              // Не наследовать дескрипторы
                    0,                  // Флаги создания (по умолчанию)
                    IntPtr.Zero,        // Среда окружения (по умолчанию)
                    workingDir,         // Рабочий каталог
                    ref startupInfo,    // Параметры запуска
                    out processInfo     // Информация о запущенном процессе
                );
                
                if (success)
                {
                    Debug.Log("Процесс Python успешно запущен.");
                    // Закрываем дескрипторы, чтобы избежать утечек ресурсов
                    WindowsJobObjectApi.CloseHandle(processInfo.hProcess);
                    WindowsJobObjectApi.CloseHandle(processInfo.hThread);
                    return true;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.LogError($"Не удалось запустить процесс. Код ошибки: {error}");
                    return false;
                }
            });
        }
    }
}