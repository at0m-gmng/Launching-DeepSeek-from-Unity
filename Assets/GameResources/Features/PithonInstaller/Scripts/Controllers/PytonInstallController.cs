namespace GameResources.Features.PithonInstaller.Scripts.Controllers
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using GameResources.Features.DownloadedFileRunner.Scripts;
    using GameResources.Features.FileChecker.Scripts;
    using GameResources.Features.FileDownloader.Scripts;
    using GameResources.Features.InstallController.Scripts;
    using PythonChecker;
    using UnityEngine;

    public class PytonInstallController : BaseInstallController
    {
        public PytonInstallController
        (
            BaseFileCheker fileChecker, 
            BaseFileDownloader _fileDownloader, 
            BaseFileRunner _fileRunner, 
            string _installerUrl = "",
            string _pythonVersion = ""
        ) : base(fileChecker, _fileDownloader, _fileRunner, _installerUrl)
        {
            pythonVersion = _pythonVersion;
            if (_fileRunner == null)
            {
                Debug.LogError("_fileRunner null");
            }
        }
        
        protected override string FileNotFind { get; set; } = "Python not found. Starting to download installer...";
        protected override string InstallerDownloaded { get; set; } = "Installer downloaded. Starting installation...";
        protected override string InstallerEnded { get; set; } = "Python installation is complete.";
        protected override string ProgrammInstalled { get; set; } = "Python is already installed.";
        
        protected override string ProgrammStartInstalled { get; set; } = "Starting installation Python";

        protected readonly string pythonVersion = default;
        protected PythonChecker PythonChecker => fileChecker as PythonChecker;
        
        public override async Task<bool> InstallAsync(CancellationToken cancellationToken)
        {
            OnMessageProgress(ProgrammStartInstalled, 0f);
            await Task.Delay(100);
            
            if (!await IsContainsPythonOrInstaller() && !PythonChecker.IsInstall)
            {
                OnMessageProgress(FileNotFind, 1f);
                await Task.Delay(100);
                OnMessageProgress(InstallerDownloaded, 0f);
                
                installerPath = await fileDownloader.DownloadInstallerAsync(TryGetInstallerUrl(), cancellationToken);
            }
            
            if(await IsContainsPythonOrInstaller() && !PythonChecker.IsInstall)
            {

                installerPath = fileChecker.FoundPath;
                await fileRunner.RunAsync(installerPath);
                
                return await TryNotificateOnSuccessInstall();
            }
            else if(await TryNotificateOnSuccessInstall())
            {
                return true;
            }

            OnMessageProgress(INSTALL_FAILED, 1f);
            return false;
        }

        protected virtual async Task<bool> IsContainsPythonOrInstaller()
        {
            if (!await fileChecker.IsContains())
            {
                return fileChecker.IsContainsInStreamingAssets() && fileDownloader.IsDownloadSuccess(fileChecker.GetContainsStreamingAssetsPath());
            }
            else if(!fileChecker.IsContainsInStreamingAssets() || 
                    fileChecker.IsContainsInStreamingAssets() && fileDownloader.IsDownloadSuccess(await PythonChecker.TryGetPythonPath()))
            {
                return true;
            }

            return false;
        }

        protected virtual async Task<bool> TryNotificateOnSuccessInstall()
        {
            bool isSuccess = await PythonChecker.IsContains();
            
            if (isSuccess)
            {
                OnMessageProgress(InstallerEnded, 1f);

                if (File.Exists(installerPath))
                {
                    File.Delete(installerPath);
                }
            }
            else
            {
                OnMessageProgress(INSTALL_FAILED, 1f);
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