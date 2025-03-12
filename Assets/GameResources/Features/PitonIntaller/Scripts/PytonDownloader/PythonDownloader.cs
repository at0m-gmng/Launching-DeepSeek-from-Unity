namespace GameResources.Features.PitonIntaller.Scripts.PytonDownloader
{
    using FileDownloader.Scripts;

    public class PythonDownloader : BaseFileDownloader
    {
        public override string DownloadedSuccess { get; protected set; } = "Python download complete";
        public override string DownloadedProgress { get; protected set; } = "Python download: {0}";

        public override string InstalledPath { get; protected set; } = "python_installer.exe";
    }
}
