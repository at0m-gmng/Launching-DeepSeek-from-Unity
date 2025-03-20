namespace GameResources.Features.DeepSeekService.Scripts.DeepSeekDownloader
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using FileDownloader.Scripts;
    using UnityEngine;

    public class DeepSeekDownloader : BaseFileDownloader
    {
        protected const string DEEPSEEK_PATH_FOLDER = "DeepSeek";
        public override string DownloadedSuccess { get; protected set; } = "DeepSeek download complete. Unpacking archive...";
        public override string DownloadedProgress { get; protected set; } = "DeepSeek download: {0}";
        public override string InstalledPath { get; protected set; } = "DeepSeek.zip";
        
        protected string extractDirectory = default;
        
        public override async Task<string> DownloadInstallerAsync(string downloadUrl, CancellationToken cancellationToken = default)
        {
            extractDirectory = Path.Combine(Application.streamingAssetsPath, DEEPSEEK_PATH_FOLDER);
            extractDirectory = extractDirectory.Replace('/', Path.DirectorySeparatorChar);
            try
            {
                await base.DownloadInstallerAsync(downloadUrl, cancellationToken);
            }
            catch (Exception e)
            {
                
            }
            return tempFilePath;
        }

        public override bool IsDownloadSuccess(string filePath)
        {
            
            FileInfo fileInfo = new FileInfo(filePath);
            if (Total != -1 && fileInfo.Length < Total)
            {
                return false;
            }

            return base.IsZipFileValid(filePath);
        }
    }
}