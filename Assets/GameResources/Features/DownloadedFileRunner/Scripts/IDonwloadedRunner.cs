namespace GameResources.Features.DownloadedFileRunner.Scripts
{
    using System.Threading.Tasks;

    public interface IDonwloadedRunner
    {
        /// <summary>
        /// Asynchronous process launch (installation, unpacking, etc.)
        /// </summary>
        public Task<bool> RunAsync(string path);
    }
}