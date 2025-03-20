namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
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
    using UniTask = Cysharp.Threading.Tasks.UniTask;

    public class LocalServerLaunchControllerModel: IProgressSystemNotification, IService, IDisposable
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
        protected const string LOADING_CHECKPOINT_SHARDS = "Loading checkpoint shards:";
        protected const string ERROR = "Error: {0}";
        
        public event Action<string> onMessage = delegate { };
        public event Action<string, float> onMessageProgress = delegate { };

        protected SystemMessageService systemMessageService = default;
        protected ProcessService processService = default;
        protected readonly MonoBehaviour monoBehaviour = default;
        protected readonly string serverFileName = default;
        protected readonly string serverURL = default;
        protected readonly int maxAttempts;
        
        protected string pythonPath;
        protected string scriptPath;
        protected string logData;
        protected IntPtr processHeader;
        protected Match match;
        protected CancellationTokenSource cancellationToken = default;
        protected WindowsJobObjectApi.PROCESS_INFORMATION processInfo = default;
        protected StatusResponse status = default;
        protected UnityWebRequest request = default;
        protected UnityWebRequestAsyncOperation operation = default;
        protected string[] ignoreErrorFields = new string[]
        {
            "Error",
            "error"
        };
        protected string[] serverLogFields = new string[]
        {
            "Loading checkpoint shards"
        };
        
        IntPtr stdInputHandle;
        IntPtr stdoutRead, stdoutWrite;
        IntPtr stderrRead, stderrWrite;
        
        public virtual async Task<bool> TryRegister()
        {
            cancellationToken = new CancellationTokenSource();

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

            StartPythonScriptAsync();
            
            return await PollServerStatusAsync();
        }

        protected FileStream nulInputStream;

        protected virtual async Task<bool> StartPythonScriptAsync()
        {
            return await Task<bool>.Run(() =>
            {
                string workingDir = Path.GetDirectoryName(scriptPath);
                string commandLine = $"\"{pythonPath}\" \"{scriptPath}\"";

                #region Output

                // Create a SECURITY_ATTRIBUTES structure for inherited descriptors
                WindowsJobObjectApi.SECURITY_ATTRIBUTES sa = new WindowsJobObjectApi.SECURITY_ATTRIBUTES();
                sa.nLength = Marshal.SizeOf(typeof(WindowsJobObjectApi.SECURITY_ATTRIBUTES));
                sa.bInheritHandle = true;
                sa.lpSecurityDescriptor = IntPtr.Zero;

                // Create pipes to redirect stdout and stderr
                try
                {
                    nulInputStream = new FileStream("NUL", FileMode.Open, FileAccess.Read);
                }
                catch(Exception ex)
                {
                    Debug.LogError("Failed to open NUL device for stdin: " + ex.Message);
                    return false;
                }
                stdInputHandle = nulInputStream.SafeFileHandle.DangerousGetHandle();
  
                if (!WindowsJobObjectApi.CreatePipe(out stdoutRead, out stdoutWrite, ref sa, 0))
                {
                    Debug.LogError("Failed to create pipe for stdout.");
                    return false;
                }

                if (!WindowsJobObjectApi.CreatePipe(out stderrRead, out stderrWrite, ref sa, 0))
                {
                    Debug.LogError("Failed to create pipe for stderr.");
                    return false;
                }

                #endregion

                WindowsJobObjectApi.STARTUPINFO startupInfo = new WindowsJobObjectApi.STARTUPINFO();
                startupInfo.cb = Marshal.SizeOf(startupInfo);
                startupInfo.hStdInput = stdInputHandle;
                startupInfo.hStdOutput = stdoutWrite;
                startupInfo.hStdError = stderrWrite;
                startupInfo.dwFlags |= (int)WindowsJobObjectApi.STARTF_USESTDHANDLES;
                
                bool success = WindowsJobObjectApi.CreateProcess(
                    null, // Application is specified via command line
                    commandLine, // Command line
                    IntPtr.Zero, // Process security attributes
                    IntPtr.Zero, // Thread security attributes
                    true, // Do not inherit handles
                    WindowsJobObjectApi.CREATE_NO_WINDOW, // Creation flags
                    IntPtr.Zero, // Environment
                    workingDir, // Working directory
                    ref startupInfo, // STARTUPINFO
                    out processInfo // PROCESS_INFORMATION
                );

                processHeader = processInfo.hProcess;
                processService.RegisterProcess(processHeader);
                
                Task.Run(() => ReadPipe(stdoutRead));
                Task.Run(() => ReadPipe(stderrRead));

                if (success)
                {
                    WindowsJobObjectApi.WaitForSingleObject(processInfo.hProcess, WindowsJobObjectApi.INFINITE);
                    WindowsJobObjectApi.GetExitCodeProcess(processInfo.hProcess, out uint exitCode);
                    Debug.Log($"The script ended with the code: {exitCode}");
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.LogError($"Error starting process. Error code: {error}");
                }

                processService.RegisterProcess(stdoutRead);
                processService.RegisterProcess(stderrRead);
                processService.RegisterProcess(stdoutWrite);
                processService.RegisterProcess(stderrWrite);
                return success;
            });
        }
        
        protected virtual async Task ReadPipe(IntPtr pipeHandle)
        {
            try
            {
                FileStream fs = new FileStream
                (
                    new Microsoft.Win32.SafeHandles.SafeFileHandle(pipeHandle, false),
                    FileAccess.Read,
                    1024,
                    isAsync: true
                );
                using (fs)
                using (StreamReader reader = new StreamReader(fs))
                {
                    while (!cancellationToken.IsCancellationRequested && (logData = await reader.ReadLineAsync()) != null)
                    {
                        await UniTask.SwitchToMainThread(cancellationToken.Token);

                        if (IsContainsString(logData, ignoreErrorFields))
                        {
                            Debug.LogError($"{string.Format(ERROR, logData)}");
                        }
                        else if (IsContainsString(logData, serverLogFields))
                        {
                            match = Regex.Match(logData, @"(\d+)%");
                            if (int.TryParse(match.Groups[1].Value, out int progressValue))
                            {
                                onMessageProgress(LOADING_CHECKPOINT_SHARDS, progressValue / 100.0f);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Read pipe with еrror //=> {e}");
            }
            finally
            {
                if (pipeHandle != IntPtr.Zero)
                {
                    WindowsJobObjectApi.CloseHandle(pipeHandle);
                }
            }
        }

        protected virtual async Task<bool> PollServerStatusAsync()
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
                            onMessageProgress(SERVER_RUNNING_SUCCESS, 1f);
                            return true;
                        }
                        else
                        {
                            string message = status != null ? status.ServerRunning.ToString() : "null";
                            Debug.LogError($"SERVER NOT RUNNING OR NULL {message}");
                        }
                    }
                    else if(i == maxAttempts - 1)
                    {
                        Debug.LogError($"error result {request.result}");
                    }

                    await Task.Delay(1000);
                }
            }
            onMessageProgress(SERVER_RUNNING_FAILED, 1f);
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
                    Debug.LogError($"Error reading registry: {ex.Message}");
                }

                return null;
            });
        }
        
        protected virtual bool IsContainsString(string error, string[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (error.Contains(data[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            WindowsJobObjectApi.CloseHandle(stdoutRead);
            WindowsJobObjectApi.CloseHandle(stderrRead);
            WindowsJobObjectApi.CloseHandle(stdoutWrite);
            WindowsJobObjectApi.CloseHandle(stderrWrite);
            
            cancellationToken?.Cancel();
            cancellationToken?.Dispose();
            cancellationToken = null;
            
            GC.SuppressFinalize(this);
        }
    }
}