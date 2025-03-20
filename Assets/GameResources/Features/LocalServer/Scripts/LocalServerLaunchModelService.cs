namespace GameResources.Features.LocalServer.Scripts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using GameResources.Services.Scripts;
    using PithonInstaller.Scripts.PythonChecker;
    using PithonInstaller.Scripts.PytonDownloader;
    using Services;
    using UnityEngine;
    using Zenject;

    public class LocalServerLaunchModelService : MonoInstaller, IService
    {
        [Header("Dependencies Properties")]
        [SerializeField]
        protected string dependenciesPath = "requirements.txt";
        
        [Header("Server Properties")]
        [SerializeField]
        protected string serverFileName = "start_deepseek.py";
        
        [SerializeField]
        protected string serverURL = @"http://127.0.0.1:5000/status";

        [Min(1)]
        [SerializeField]
        protected int maxInitWaitTime = 600;

        protected PythonDependencyInstallController pythonDependencyInstallController = default;
        protected LocalServerLaunchControllerModel localServerLaunchControllerModel = default;

        protected List<IService> initializeServices = new List<IService>();
        protected PythonChecker fileChecker = default;
        protected PythonDownloader fileDownloader = default;
        protected bool isComplete = false;
        protected string pythonPath = default;

        public override void InstallBindings() 
            => Container.Bind<IService>().To<LocalServerLaunchModelService>().FromComponentOn(gameObject).AsTransient();

        public virtual async Task<bool> TryRegister()
        {
            #region CheckPythonIntall

            fileChecker = new PythonChecker
            (
                Application.streamingAssetsPath, 
                new string[] {}
            );
            fileDownloader = Container.Instantiate<PythonDownloader>();
            
            #endregion
            
            if (await IsContainsPythonOrInstaller())
            {
                pythonPath = await fileChecker.TryGetPythonPath();
            
                pythonDependencyInstallController = Container.Instantiate<PythonDependencyInstallController>(new object[]
                {
                    dependenciesPath,
                    pythonPath
                });
                initializeServices.Add(pythonDependencyInstallController);
            
                localServerLaunchControllerModel = Container.Instantiate<LocalServerLaunchControllerModel>(new object[]
                {
                    serverFileName,
                    serverURL,
                    maxInitWaitTime
                });
                initializeServices.Add(localServerLaunchControllerModel);

                for (int i = 0; i < initializeServices.Count; i++)
                {
                    isComplete = await initializeServices[i].TryRegister();
                    if (!isComplete)
                    {
                        Debug.LogError($"Service Error {initializeServices[i].GetType()}");
                        return isComplete;
                    }
                }
            }
            else
            {
                return false;
            }
            
            return isComplete;
        }
        
        protected virtual async Task<bool> IsContainsPythonOrInstaller()
        {
            if (!await fileChecker.IsContains())
            {
                return fileChecker.IsContainsInStreamingAssets() && fileDownloader.IsDownloadSuccess(fileChecker.GetContainsStreamingAssetsPath());
            }
            else if(!fileChecker.IsContainsInStreamingAssets() || 
                    fileChecker.IsContainsInStreamingAssets() && fileDownloader.IsDownloadSuccess(await fileChecker.TryGetPythonPath()))
            {
                return true;
            }

            return false;
        }
    }
}