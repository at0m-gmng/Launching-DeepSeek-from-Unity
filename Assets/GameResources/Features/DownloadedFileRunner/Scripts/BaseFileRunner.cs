namespace GameResources.Features.DownloadedFileRunner.Scripts
{
    using System.Threading.Tasks;

    public class BaseFileRunner : IDonwloadedRunner
    {
        /// <summary>
        /// Асинхронный запуск процесса (установки, распаковки и т.п.)
        /// </summary>
        public virtual Task<bool> RunAsync(string path) => default;
    }
}