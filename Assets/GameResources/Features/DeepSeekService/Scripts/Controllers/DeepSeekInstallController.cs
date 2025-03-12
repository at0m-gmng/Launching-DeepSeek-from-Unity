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
        
        protected override string FileNotFind { get; set; } = "Модель DeepSeek не найдена. Начинается загрузка установщика...";
        protected override string InstallerDownloaded { get; set; } = "Модель скачана. Запуск распаковки...";
        protected override string InstallerEnded { get; set; } = "Распаковка DeepSeek завершена.";
        protected override string ProgrammInstalled { get; set; } = "DeepSeek найден.";
    }
}