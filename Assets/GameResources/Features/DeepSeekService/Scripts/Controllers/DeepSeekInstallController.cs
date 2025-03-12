namespace GameResources.Features.DeepSeekService.Scripts.Controllers
{
    using GameResources.Features.DownloadedFileRunner.Scripts;
    using GameResources.Features.FileChecker.Scripts;
    using GameResources.Features.FileDownloader.Scripts;
    using GameResources.Features.InstallController.Scripts;

    public class DeepSeekInstallController : BaseInstallController
    {
        public DeepSeekInstallController
        (
            BaseFileCheker faleChecker, 
            BaseFileDownloader fileDownloader, 
            BaseFileRunner fileRunner, 
            string installerUrl = ""
        ) : base(faleChecker, fileDownloader, fileRunner, installerUrl) { }
        
        protected override string FileNotFind { get; set; } = "DeepSeek model not found. Starting to download installer...";
        protected override string InstallerDownloaded { get; set; } = "Model downloaded. Starting unpacking...";
        protected override string InstallerEnded { get; set; } = "DeepSeek unboxing complete.";
        protected override string ProgrammInstalled { get; set; } = "DeepSeek found.";
    }
}