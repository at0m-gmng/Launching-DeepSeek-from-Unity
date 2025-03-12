namespace GameResources.Features.LocalServer.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using GameResources.Services.Scripts;
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
        protected bool isComplete = false;

        public override void InstallBindings() 
            => Container.Bind<IService>().To<LocalServerLaunchModelService>().FromComponentOn(gameObject).AsTransient();

        public virtual async Task<bool> TryRegister()
        {
            pythonDependencyInstallController = new PythonDependencyInstallController(dependenciesPath);
            initializeServices.Add(pythonDependencyInstallController);
            
            localServerLaunchControllerModel = new LocalServerLaunchControllerModel(serverFileName, serverURL, maxInitWaitTime);
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
            
            return isComplete;
        }

        protected void OnApplicationQuit()
        {
            Debug.LogError("OnApplicationQuit", gameObject);
            localServerLaunchControllerModel.OnApplicationQuit();
        }
    }
}