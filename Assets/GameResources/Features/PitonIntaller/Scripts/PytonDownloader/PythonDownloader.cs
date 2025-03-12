namespace GameResources.Features.PitonIntaller.Scripts.PytonDownloader
{
    using FileDownloader.Scripts;

    public class PythonDownloader : BaseFileDownloader
    {
        public override string DownloadedSuccess { get; protected set; } = "Скачивание Python завершено";
        public override string DownloadedProgress { get; protected set; } = "Скачивание Python: {0}";

        public override string InstalledPath { get; protected set; } = "python_installer.exe";
    }
}
