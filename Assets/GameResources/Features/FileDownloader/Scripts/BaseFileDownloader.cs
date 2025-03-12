namespace GameResources.Features.FileDownloader.Scripts
{
    using System;
    using System.Threading.Tasks;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Threading;
    using SystemNotification.Scripts;
    using SystemNotification.Scripts.Interfaces;
    using UnityEngine;
    using Zenject;

    public class BaseFileDownloader : IDownloader, IProgressSystemNotification
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService)
        {
            systemMessageService = _systemMessageService;
            systemMessageService.RegisterMessage(this);
        }
        
        protected SystemMessageService systemMessageService = default;
        
        public event Action<string> onMessage = delegate {  };
        public event Action<string, float> onMessageProgress = delegate {  };

        public virtual string DownloadedSuccess { get; protected set; } = "Download complete";
        public virtual string DownloadedProgress { get; protected set; } = "Downloads: {0}";

        public virtual string InstalledPath { get; protected set; } = "NewFile_MascotProject";

        protected HttpResponseMessage response = default;
        protected HttpClient client = default;
        protected Stream contentStream = default;
        protected FileStream fileStream = default;
        protected byte[] buffer = default;
        protected long total = default;
        protected long totalRead = default;
        protected long existingLength = default;
        protected float progressPercentage = default;
        protected int bytesRead = default;
        protected bool isRequiredReportProgress = false;
        protected bool isMoreToRead = default;
        protected string tempFilePath = default;
        protected HttpRequestMessage requestFileSize;
        protected HttpClientHandler handler = new HttpClientHandler();

        public virtual async Task<bool> DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            try
            {
                existingLength = 0;
                if (File.Exists(destinationPath))
                {
                    existingLength = new FileInfo(destinationPath).Length;
                    UnityEngine.Debug.LogError($"An existing file was found, size = {existingLength} bytes.");

                    InitNewHandler(true);
                    total = await GetExpectedFileSizeAsync(url, cancellationToken);

                    if (total != -1 && existingLength == total)
                    {
                        UnityEngine.Debug.LogError("File already downloaded. Skipping download.");
                        onMessageProgress(string.Empty, 1f);
                        return true;
                    }
                    else
                    {
                        File.Delete(destinationPath);
                    }
                }

                InitNewHandler(true);
                using (client = new HttpClient(handler))
                {
                    requestFileSize = new HttpRequestMessage(HttpMethod.Get, url);
                    if (existingLength > 0)
                    {
                        requestFileSize.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);
                        onMessage($"Attempting to continue loading from position {existingLength}.");
                    }

                    using (response = await client.SendAsync(requestFileSize, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                        {
                            if (response.Headers.Location != null)
                            {
                                string newUrl = response.Headers.Location.ToString();
                                Debug.LogError($"Redirect to {newUrl}");
                                return await DownloadFileAsync(newUrl, destinationPath, cancellationToken);
                            }
                            else
                            {
                                Debug.LogError("Redirect without new URL");
                                return false;
                            }
                        }
                
                        if (response.StatusCode == System.Net.HttpStatusCode.OK && existingLength > 0)
                        {
                            onMessage("The server does not support resuming. Restarting the download.");
                            File.Delete(destinationPath);
                            existingLength = 0;
                        }
                
                        response.EnsureSuccessStatusCode();

                        if (existingLength > 0 && response.StatusCode == System.Net.HttpStatusCode.PartialContent && response.Content.Headers.ContentLength.HasValue)
                        {
                            total = existingLength + response.Content.Headers.ContentLength.Value;
                        }
                        else
                        {
                            total = await GetExpectedFileSizeAsync(url, cancellationToken);
                            if (total == -1)
                            {
                                total = response.Content.Headers.ContentLength ?? -1L;
                            }
                        }
                        isRequiredReportProgress = total != -1;
                        onMessage($"Total file size: {total} bytes.");

                        using (fileStream = new FileStream(destinationPath, existingLength > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            using (contentStream = await response.Content.ReadAsStreamAsync())
                            {
                                totalRead = existingLength;
                                buffer = new byte[8192];
                                isMoreToRead = true;
                                do
                                {
                                    bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                                    if (bytesRead == 0)
                                    {
                                        isMoreToRead = false;
                                        onMessageProgress(DownloadedSuccess, 1f);
                                        continue;
                                    }
                                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                    totalRead += bytesRead;

                                    if (isRequiredReportProgress)
                                    {
                                        progressPercentage = (float)totalRead / total;
                                        onMessageProgress(string.Format(DownloadedProgress, (progressPercentage * 100).ToString("0.0")), progressPercentage);
                                    }
                                }
                                while (isMoreToRead);
                            }
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                onMessage("Download cancelled by user");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Download with error: {ex}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Downloading the installer with repeated attempts if the file is damaged.
        /// </summary>
        public virtual async Task<string> DownloadInstallerAsync(string downloadUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                int attempts = 0;
                int maxAttempts = 3;
                bool valid = false;

                while (attempts < maxAttempts && !valid)
                {
                    attempts++;
                    tempFilePath = Path.Combine(Application.streamingAssetsPath, InstalledPath);
                    tempFilePath = tempFilePath.Replace('/', Path.DirectorySeparatorChar);

                    Debug.LogError($"Attempting to download installer: {attempts} of {maxAttempts}");
                    onMessage($"Attempting to download installer: {attempts} of {maxAttempts}");
                    if (!await DownloadFileAsync(downloadUrl, tempFilePath, cancellationToken))
                    {
                        tempFilePath = string.Empty;
                    }
                    else
                    {
                        valid = IsZipFileValid(tempFilePath);   
                    }
                }
                if (!valid)
                {
                    tempFilePath = string.Empty;
                    Debug.LogError("Failed to download valid installer after " + maxAttempts + " attempts.");
                }
                return tempFilePath;
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Check the integrity of the ZIP file.
        /// If the archive is corrupted (e.g. End of Central Directory not found), an exception will be thrown.
        /// </summary>
        protected virtual bool IsZipFileValid(string filePath)
        {
            // If the file does not have a .zip extension, skip the integrity check
            if (Path.GetExtension(filePath).ToLower() != ".zip")
            {
                return true;
            }

            try
            {
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    // If the archive opened without errors, we consider it valid
                }
                return true;
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected virtual async Task<long> GetExpectedFileSizeAsync(string url, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using (var headClient = new HttpClient(handler))
                    {
                        var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
                        var headResponse = await headClient.SendAsync(headRequest, cancellationToken);
            
                        if (headResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            if (headResponse.Content.Headers.ContentLength.HasValue)
                            {
                                long contentLength = headResponse.Content.Headers.ContentLength.Value;
                    
                                // Additional check: if the size is less than the minimum allowed (for example, 100 bytes), we consider it invalid
                                if (contentLength > 100)
                                {
                                    return contentLength;
                                }
                                else
                                {
                                    // If the size is too small, the server may have returned an HTML page or an error
                                    return -1L;
                                }
                            }
                            else
                            {
                                // If the Content-Length header is missing, return -1
                                return -1L;
                            }
                        }
                        else
                        {
                            // For other success codes (eg 206 Partial Content)
                            headResponse.EnsureSuccessStatusCode();
                            return headResponse.Content.Headers.ContentLength ?? -1L;
                        }
                    }
                }
                catch (Exception e)
                {
                    // In case of any error, return -1, signaling that the expected size is not defined
                    return -1L;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        protected virtual void InitNewHandler(bool autoRedirect = true)
        {
            handler = new HttpClientHandler();
            handler.AllowAutoRedirect = autoRedirect;
        }

        protected virtual void OnMessage(string message) => onMessage(message);
        protected virtual void OnMessageProgress(string message, float progress) => onMessageProgress(message, progress);
    }
}