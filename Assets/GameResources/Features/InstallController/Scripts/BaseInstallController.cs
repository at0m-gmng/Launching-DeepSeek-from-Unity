namespace GameResources.Features.InstallController.Scripts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DownloadedFileRunner.Scripts;
    using FileChecker.Scripts;
    using FileDownloader.Scripts;
    using SystemNotification.Scripts;
    using SystemNotification.Scripts.Interfaces;
    using Zenject;
    using UnityEngine;

    public class BaseInstallController: ISystemNotification
    {
        [Inject]
        public virtual void Construct(SystemMessageService _systemMessageService)
        {
            systemMessageService = _systemMessageService;
            systemMessageService.RegisterMessage(this);
        }
        
        public BaseInstallController
        (
            BaseFileCheker _faleChecker,
            BaseFileDownloader _fileDownloader,
            BaseFileRunner _fileRunner,
            string _installerUrl = "")
        {
            faleChecker = _faleChecker;
            fileDownloader = _fileDownloader;
            fileRunner = _fileRunner;
            installerUrl = _installerUrl;
        }
        
        
        protected const string INSTALL_FAILED = "Installation not completed";

        public event Action<string> onMessage = delegate {};

        protected virtual string FileNotFind { get; set; } = "File not found. Starting installer download...";
        protected virtual string InstallerDownloaded { get; set; } = "Installer downloaded. Starting installation...";
        protected virtual string InstallerEnded { get; set; } = "The file installation is complete.";
        protected virtual string ProgrammInstalled { get; set; } = "The file is already installed.";

        
        protected SystemMessageService systemMessageService;
        protected readonly BaseFileCheker faleChecker;
        protected readonly BaseFileDownloader fileDownloader;
        protected readonly BaseFileRunner fileRunner;
        protected readonly string installerUrl;
        protected string installerPath = default;
               
        public virtual async Task<bool> InstallAsync(CancellationToken cancellationToken)
        {
            if (!faleChecker.IsContains())
            {
                onMessage(FileNotFind);
                Debug.LogError(FileNotFind);
                installerPath = await fileDownloader.DownloadInstallerAsync(TryGetInstallerUrl(), cancellationToken);

                if (!string.IsNullOrEmpty(installerPath))
                {
                    onMessage(InstallerDownloaded);
                    Debug.LogError(InstallerDownloaded);
                    await fileRunner.RunAsync(installerPath);
                    onMessage(InstallerEnded);
                    Debug.LogError(InstallerEnded);

                    return true;
                }
                onMessage(INSTALL_FAILED);
                Debug.LogError(INSTALL_FAILED);
                return false;
            }
            else
            {
                onMessage(ProgrammInstalled);
                Debug.LogError(ProgrammInstalled);
                return true;
            }
        }
        
        
        protected virtual string TryGetInstallerUrl()
        {
            if (!string.IsNullOrEmpty(installerUrl))
            {
                return installerUrl;
            }

            return string.Empty;
        }

        protected virtual void OnMessage(string message) => onMessage(message);
    }
}