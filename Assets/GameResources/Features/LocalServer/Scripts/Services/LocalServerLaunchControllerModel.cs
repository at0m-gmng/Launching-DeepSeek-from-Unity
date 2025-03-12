namespace GameResources.Features.LocalServer.Scripts.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Data;
    using GameResources.Features.SystemNotification.Scripts;
    using GameResources.Features.SystemNotification.Scripts.Interfaces;
    using GameResources.Services.Scripts;
    using UnityEngine;
    using UnityEngine.Networking;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class LocalServerLaunchControllerModel: ISystemNotification, IService
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService)
        {
            systemMessageService = _systemMessageService;
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
        protected readonly MonoBehaviour monoBehaviour = default;
        protected readonly string serverFileName = default;
        protected readonly string serverURL = default;
        protected readonly int maxAttempts;
        
        protected string pythonPath;
        protected string scriptPath;
        protected Process process;
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
            
            pythonPath = await FindPythonPath();
            

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
            data = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = new Process { StartInfo = data, EnableRaisingEvents = true };
    
            process.ErrorDataReceived += errorHandler;
            process.Exited += exitHandler;
                
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error starting Python process: {ex.Message}");
            }
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
                        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    }
                    else
                    {
                        Debug.LogError(string.Format(ERROR, error));
                        return null;
                    }
                }
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
            process.ErrorDataReceived -= errorHandler;
            process.Exited -= exitHandler;
        }
        
        protected virtual void CheckErrors(object sender, DataReceivedEventArgs e)
        {
            Debug.LogError($"ErrorsDelegate: {e.Data}");
            if (!string.IsNullOrEmpty(e.Data) && IsContainsString(e.Data))
            {
                Unsibscribe(sender, e);
            }
        }
        
        
        public virtual async void OnApplicationQuit() => await ShutdownPythonServerAsync();

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