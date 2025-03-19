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

    public class BaseInstallController: IProgressSystemNotification
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService)
        {
            systemMessageService = _systemMessageService;
            systemMessageService.RegisterMessage(this);
        }
        
        public BaseInstallController
        (
            BaseFileCheker _fileChecker,
            BaseFileDownloader _fileDownloader,
            BaseFileRunner _fileRunner,
            string _installerUrl = "")
        {
            fileChecker = _fileChecker;
            fileDownloader = _fileDownloader;
            fileRunner = _fileRunner;
            installerUrl = _installerUrl;
        }
        
        
        protected const string INSTALL_FAILED = "Installation not completed";

        public event Action<string> onMessage = delegate {};
        public event Action<string, float> onMessageProgress = delegate {};


        protected virtual string FileNotFind { get; set; } = "File not found. Starting installer download...";
        protected virtual string InstallerDownloaded { get; set; } = "Installer downloaded. Starting installation...";
        protected virtual string InstallerEnded { get; set; } = "The file installation is complete";
        protected virtual string ProgrammInstalled { get; set; } = "The file is already installed";
        protected virtual string ProgrammStartInstalled { get; set; } = "Starting file installation";

        
        protected SystemMessageService systemMessageService;
        protected readonly BaseFileCheker fileChecker;
        protected readonly BaseFileDownloader fileDownloader;
        protected readonly BaseFileRunner fileRunner;
        protected readonly string installerUrl;
        protected string installerPath = default;
               
        public virtual async Task<bool> InstallAsync(CancellationToken cancellationToken)
        {
            onMessageProgress(ProgrammStartInstalled, 0f);
            await Task.Delay(1000);
            
            if (!await fileChecker.IsContains())
            {
                onMessageProgress(FileNotFind, 1f);
                await Task.Delay(100);
                installerPath = await fileDownloader.DownloadInstallerAsync(TryGetInstallerUrl(), cancellationToken);

                if (!string.IsNullOrEmpty(installerPath))
                {
                    onMessage(InstallerDownloaded);
                    await Task.Delay(100);
                    onMessageProgress(InstallerDownloaded, 0f);

                    await fileRunner.RunAsync(installerPath);
                    onMessageProgress(InstallerEnded, 1f);

                    return true;
                }
                onMessage(INSTALL_FAILED);
                return false;
            }
            else
            {
                onMessageProgress(ProgrammInstalled, 1f);
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
        protected virtual void OnMessageProgress(string message, float progress) => onMessageProgress(message, progress);
    }
}