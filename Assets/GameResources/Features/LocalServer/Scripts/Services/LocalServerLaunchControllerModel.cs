namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Data;
    using GameResources.Features.SystemNotification.Scripts;
    using GameResources.Features.SystemNotification.Scripts.Interfaces;
    using GameResources.Services.Scripts;
    using Microsoft.Win32;
    using ProcessController;
    using UnityEngine;
    using UnityEngine.Networking;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class LocalServerLaunchControllerModel: ISystemNotification, IService
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService, ProcessService _processService)
        {
            systemMessageService = _systemMessageService;
            processService = _processService;
            systemMessageService.RegisterMessage(this);
        }

        public LocalServerLaunchControllerModel(string _serverFileName, string _serverURL, int _maxAttempts)
        {
            serverFileName = _serverFileName;
            serverURL = _serverURL;
            maxAttempts = _maxAttempts;
        }
        
        protected const string SERVER_RUNNING_SUCCESS = "DeepSeek server is running!";
        protected const string SERVER_RUNNING_FAILED = "DeepSeek server failed to start or timed out!";
        protected const string ERROR = "Error: {0}";
        protected const string SHUTDOWN_URL = "http://127.0.0.1:5000/shutdown";
        
        public event Action<string> onMessage = delegate { };

        protected SystemMessageService systemMessageService = default;
        protected ProcessService processService = default;
        protected readonly MonoBehaviour monoBehaviour = default;
        protected readonly string serverFileName = default;
        protected readonly string serverURL = default;
        protected readonly int maxAttempts;
        
        protected string pythonPath;
        protected string scriptPath;
        protected IntPtr processHeader;
        protected ProcessStartInfo data = default;
        protected StatusResponse status = default;
        protected UnityWebRequest request = default;
        protected UnityWebRequestAsyncOperation operation = default;
        protected string[] ignoreErrorFields = new string[]
        {
            "Error",
            "error"
        };
        
        protected DataReceivedEventHandler errorHandler;
        protected EventHandler exitHandler;
        
        public virtual async Task<bool> TryRegister()
        {
            scriptPath = Path.Combine(Application.streamingAssetsPath, serverFileName);

            if (!File.Exists(scriptPath))
            {
                Debug.LogError("The file to start the server was not found.");
                return false;
            }
            
            pythonPath = await GetPythonPathFromRegistry();
            

            if (string.IsNullOrEmpty(pythonPath))
            {
                Debug.LogError("Python not found on the system.");
                return false;
            }

            exitHandler = new EventHandler(Unsibscribe);
            errorHandler = new DataReceivedEventHandler(CheckErrors);
            
            StartPythonScriptAsync();

            return await PollServerStatusAsync();
        }

        protected virtual void StartPythonScriptAsync()
        {
            string workingDir = Path.GetDirectoryName(scriptPath);
            string commandLine = $"\"{pythonPath}\" \"{scriptPath}\"";

            var startupInfo = new  WindowsJobObjectApi.STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            var processInfo = new WindowsJobObjectApi.PROCESS_INFORMATION();

            bool success = WindowsJobObjectApi.CreateProcess(
                null, // Application is specified via command line
                commandLine, // Command line
                IntPtr.Zero, // Process security attributes
                IntPtr.Zero, // Thread security attributes
                false, // Do not inherit handles
                WindowsJobObjectApi.CREATE_NO_WINDOW, // Creation flags
                IntPtr.Zero, // Environment
                workingDir, // Working directory
                ref startupInfo, // STARTUPINFO
                out processInfo // PROCESS_INFORMATION
            );
            
            if (success)
            {
                processHeader = processInfo.hProcess;
                processService.RegisterProcess(processHeader);
                
                Debug.LogError($"Server Success Launch");
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Debug.LogError($"Error starting process. Error code: {error}");
            }
            
            // data = new ProcessStartInfo
            // {
            //     FileName = pythonPath,
            //     Arguments = $"\"{scriptPath}\"",
            //     RedirectStandardOutput = true,
            //     RedirectStandardError = true,
            //     UseShellExecute = false,
            //     CreateNoWindow = true
            // };
            //
            // process = new Process { StartInfo = data, EnableRaisingEvents = true };
            //
            // process.ErrorDataReceived += errorHandler;
            // process.Exited += exitHandler;
            //     
            // try
            // {
            //     process.Start();
            //     process.BeginOutputReadLine();
            //     process.BeginErrorReadLine();
            // }
            // catch (Exception ex)
            // {
            //     Debug.LogError($"Error starting Python process: {ex.Message}");
            // }
        }

        protected async Task<bool> PollServerStatusAsync()
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                request = UnityWebRequest.Get(serverURL);
                using (request)
                {
                    operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

#if UNITY_2020_1_OR_NEWER
                    if (request.result == UnityWebRequest.Result.Success)
#else
                    if (!request.isNetworkError && !request.isHttpError)
#endif
                    {
                        status = JsonUtility.FromJson<StatusResponse>(request.downloadHandler.text);
                        if (request.responseCode == 200 || status != null && status.ServerRunning)
                        {
                            onMessage(SERVER_RUNNING_SUCCESS);
                            Debug.LogError(SERVER_RUNNING_SUCCESS);
                            return true;
                        }
                        else
                        {
                            string message = status != null ? status.ServerRunning.ToString() : "null";
                            Debug.LogError($"SERVER NOT RUNNING OR NULL {message}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"error result {request.result}");
                    }

                    await Task.Delay(1000);
                }
            }
            onMessage(SERVER_RUNNING_FAILED);
            Debug.LogError(SERVER_RUNNING_FAILED);
            return false;
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

        protected virtual void Unsibscribe(object sender, EventArgs eventArgs)
        {
            Debug.LogError("Unsibscribe");
            // process.ErrorDataReceived -= errorHandler;
            // process.Exited -= exitHandler;
        }
        
        protected virtual void CheckErrors(object sender, DataReceivedEventArgs e)
        {
            Debug.LogError($"ErrorsDelegate: {e.Data}");
            if (!string.IsNullOrEmpty(e.Data) && IsContainsString(e.Data))
            {
                Unsibscribe(sender, e);
            }
        }
        
        
        private async Task ShutdownPythonServerAsync()
        {
            request = UnityWebRequest.PostWwwForm(SHUTDOWN_URL, "");
            using (request)
            {
                await request.SendWebRequest();
            
#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
                {
                    Debug.LogError("Error trying to terminate Python server:" + request.error);
                }
                else
                {
                    Debug.Log("The request to terminate the Python server was sent successfully.");
                }
            }
        }
    }
}