namespace GameResources.Features.FileDownloader.Scripts
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IDownloader
    {
        public Task<bool> DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default);
    }
}