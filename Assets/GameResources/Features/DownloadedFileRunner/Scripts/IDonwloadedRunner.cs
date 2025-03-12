namespace GameResources.Features.DownloadedFileRunner.Scripts
{
    using System.Threading.Tasks;

    public interface IDonwloadedRunner
    {
        /// <summary>
        /// Асинхронный запуск процесса (установки, распаковки и т.п.)
        /// </summary>
        public Task<bool> RunAsync(string path);
    }
}