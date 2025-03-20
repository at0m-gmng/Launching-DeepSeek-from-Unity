namespace GameResources.Features.PithonInstaller.Scripts.PytonDownloader
{
    using GameResources.Features.FileDownloader.Scripts;

    public class PythonDownloader : BaseFileDownloader
    {
        public override string DownloadedSuccess { get; protected set; } = "Python download complete";
        public override string DownloadedProgress { get; protected set; } = "Python download";

        public override string InstalledPath { get; protected set; } = "python_installer.exe";
    }
}
