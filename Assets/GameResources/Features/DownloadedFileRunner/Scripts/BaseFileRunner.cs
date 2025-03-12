namespace GameResources.Features.DownloadedFileRunner.Scripts
{
    using System.Threading.Tasks;

    public class BaseFileRunner : IDonwloadedRunner
    {
        /// <summary>
        /// Asynchronous process launch (installation, unpacking, etc.)
        /// </summary>
        public virtual Task<bool> RunAsync(string path) => default;
    }
}