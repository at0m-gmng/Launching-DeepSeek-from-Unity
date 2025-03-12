namespace GameResources.Features.PitonIntaller.Scripts.Controllers
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using DownloadedFileRunner.Scripts;
    using FileChecker.Scripts;
    using FileDownloader.Scripts;
    using InstallController.Scripts;
    using UnityEngine;
    using PytonChecker = GameResources.Features.PitonIntaller.Scripts.PytonChecker.PythonChecker;

    public class PytonInstallController : BaseInstallController
    {
        public PytonInstallController
        (
            BaseFileCheker _faleChecker, 
            BaseFileDownloader _fileDownloader, 
            BaseFileRunner _fileRunner, 
            string _installerUrl = "",
            string _pythonVersion = ""
        ) : base(_faleChecker, _fileDownloader, _fileRunner, _installerUrl)
        {
            pythonVersion = _pythonVersion;
        }
        
        protected override string FileNotFind { get; set; } = "Python not found. Starting to download installer...";
        protected override string InstallerDownloaded { get; set; } = "Installer downloaded. Starting installation...";
        protected override string InstallerEnded { get; set; } = "Python installation is complete.";
        protected override string ProgrammInstalled { get; set; } = "Python is already installed.";
        
        protected readonly string pythonVersion = default;
        
        public override async Task<bool> InstallAsync(CancellationToken cancellationToken)
        {
            if (!faleChecker.IsContains() && !faleChecker.IsContainsInStreamingAssets())
            {
                OnMessage(FileNotFind);
                Debug.LogError(FileNotFind);
                installerPath = await fileDownloader.DownloadInstallerAsync(TryGetInstallerUrl(), cancellationToken);
                if (!string.IsNullOrEmpty(installerPath))
                {
                    OnMessage(InstallerDownloaded);
                    Debug.LogError(InstallerDownloaded);
                    await fileRunner.RunAsync(installerPath);
                    
                    return TryNotificateOnSuccessInstall();
                }
                else
                {
                    return false;
                }
            }
            else if(faleChecker.IsContains() && faleChecker.IsContainsInStreamingAssets())
            {
                OnMessage(InstallerDownloaded);
                Debug.LogError(InstallerDownloaded);

                installerPath = faleChecker.GetContainsStreamingAssetsPath();
                await fileRunner.RunAsync(installerPath);
                
                return TryNotificateOnSuccessInstall();
            }
            else if(faleChecker.IsContains())
            {
                OnMessage(ProgrammInstalled);
                Debug.LogError(ProgrammInstalled);
            }
            else
            {
                return false;   
            }

            return false;
        }

        protected virtual bool TryNotificateOnSuccessInstall()
        {
            bool isSuccess = false;
            if (faleChecker is PytonChecker pytonChecker)
            {
                isSuccess = !string.IsNullOrEmpty(pytonChecker.TryGetPythonExecutableFile());   
            } 
            if (isSuccess)
            {
                OnMessage(InstallerEnded);
                Debug.LogError(InstallerEnded);

                if (File.Exists(installerPath))
                {
                    File.Delete(installerPath);
                }
            }
            else
            {
                OnMessage(INSTALL_FAILED);
                Debug.LogError(INSTALL_FAILED);
            }

            return isSuccess;
        }

        protected override string TryGetInstallerUrl()
        {
            if (!string.IsNullOrEmpty(installerUrl))
            {
                return installerUrl;
            }

            if (!string.IsNullOrEmpty(pythonVersion))
            {
                return $"https://www.python.org/ftp/python/{pythonVersion}/python-{pythonVersion}-amd64.exe";
            }

            return "https://www.python.org/ftp/python/3.9.7/python-3.9.7-amd64.exe";
        }
    }
}