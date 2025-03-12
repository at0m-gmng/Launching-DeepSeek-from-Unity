namespace GameResources.Features.InstallService.Scripts
{
    using System;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using DownloadedFileRunner.Scripts;
    using FileChecker.Scripts;
    using FileDownloader.Scripts;
    using InstallController.Scripts;
    using UnityEngine;
    using Zenject;

    public class BaseInstallService: MonoInstaller
    {
        [Header("Optional: Specify the installer URL if you want to use a different source")]
        [SerializeField] 
        protected string installerUrl = "";

        protected virtual string[] RequiredFiles => requiredFiles;

        [SerializeField]
        protected string[] requiredFiles = new string[]
        {
            @"C:\Python39\python.exe",
            @"C:\Python38\python.exe",
            @"C:\Program Files\Python39\python.exe",
            @"C:\Program Files\Python38\python.exe",
            "/usr/bin/python3",
            "/usr/local/bin/python3"
        };
        
        [Header("Delay in milliseconds")]
        [Min(0)]
        [SerializeField]
        protected int delayTicks = 1500;
        
        protected BaseFileCheker fileChecker = default;
        protected BaseFileDownloader fileDownloader = default;
        protected BaseFileRunner installRunner = default;
        protected BaseInstallController installController = default;

        public virtual async Task<bool> InitInstall()
        {
            await Task.Delay(delayTicks);
            try
            {
                await installController.InstallAsync(gameObject.GetCancellationTokenOnDestroy());
            }
            catch (Exception e)
            {
                Debug.LogError($"InitInstall with Error: {e}");
                return false;
            }

            return true;
        }
    }
}