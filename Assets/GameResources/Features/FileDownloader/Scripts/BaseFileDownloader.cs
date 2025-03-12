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

        public virtual string DownloadedSuccess { get; protected set; } = "Скачивание завершено";
        public virtual string DownloadedProgress { get; protected set; } = "Скачивание: {0}";

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
                    UnityEngine.Debug.LogError($"Обнаружен существующий файл, размер = {existingLength} байт.");

                    InitNewHandler(true);
                    total = await GetExpectedFileSizeAsync(url, cancellationToken);

                    if (total != -1 && existingLength == total)
                    {
                        UnityEngine.Debug.LogError("Файл уже скачан. Пропуск скачивания.");
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
                        onMessage($"Попытка продолжить загрузку с позиции {existingLength}.");
                    }

                    using (response = await client.SendAsync(requestFileSize, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                        {
                            if (response.Headers.Location != null)
                            {
                                string newUrl = response.Headers.Location.ToString();
                                Debug.LogError($"Редирект на {newUrl}");
                                return await DownloadFileAsync(newUrl, destinationPath, cancellationToken);
                            }
                            else
                            {
                                Debug.LogError("Редирект без нового URL");
                                return false;
                            }
                        }
                
                        if (response.StatusCode == System.Net.HttpStatusCode.OK && existingLength > 0)
                        {
                            onMessage("Сервер не поддерживает возобновление. Начинаем загрузку заново.");
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
                        onMessage($"Общий размер файла: {total} байт.");

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
                onMessage("Скачивание отменено пользователем");
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
        /// Загрузка установщика с повторными попытками, если файл повреждён.
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

                    Debug.LogError($"Попытка загрузки установщика: {attempts} из {maxAttempts}");
                    onMessage($"Попытка загрузки установщика: {attempts} из {maxAttempts}");
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
                    Debug.LogError("Не удалось скачать корректный установщик после " + maxAttempts + " попыток.");
                }
                return tempFilePath;
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Проверка целостности ZIP-файла.
        /// Если архив повреждён (например, не найден End of Central Directory), будет выброшено исключение.
        /// </summary>
        protected virtual bool IsZipFileValid(string filePath)
        {
            // Если файл не имеет расширения .zip, пропускаем проверку целостности
            if (Path.GetExtension(filePath).ToLower() != ".zip")
            {
                return true;
            }

            try
            {
                // Пытаемся открыть файл как ZIP-архив
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    // Если архив открылся без ошибок, считаем его валидным
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
                    // Создаем HttpClient с тем же обработчиком (handler)
                    using (var headClient = new HttpClient(handler))
                    {
                        var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
                        var headResponse = await headClient.SendAsync(headRequest, cancellationToken);
            
                        // Проверяем статус ответа
                        if (headResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // Если HEAD-запрос возвращает 200 OK, пытаемся получить Content-Length
                            if (headResponse.Content.Headers.ContentLength.HasValue)
                            {
                                long contentLength = headResponse.Content.Headers.ContentLength.Value;
                    
                                // Дополнительная проверка: если размер меньше минимально допустимого (например, 100 байт), считаем его некорректным
                                if (contentLength > 100)
                                {
                                    return contentLength;
                                }
                                else
                                {
                                    // Если размер слишком мал, возможно, сервер вернул HTML-страницу или ошибку
                                    return -1L;
                                }
                            }
                            else
                            {
                                // Если заголовок Content-Length отсутствует, возвращаем -1
                                return -1L;
                            }
                        }
                        else
                        {
                            // Для других успешных кодов (например, 206 Partial Content)
                            headResponse.EnsureSuccessStatusCode();
                            return headResponse.Content.Headers.ContentLength ?? -1L;
                        }
                    }
                }
                catch (Exception e)
                {
                    // В случае любой ошибки возвращаем -1, сигнализируя, что ожидаемый размер не определен
                    return -1L;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        protected virtual void InitNewHandler(bool autoRedirect = true)
        {
            handler = new HttpClientHandler();
            handler.AllowAutoRedirect = autoRedirect;
            // handler.MaxRequestContentBufferSize = long.MaxValue;
        }

        protected virtual void OnMessage(string message) => onMessage(message);
        protected virtual void OnMessageProgress(string message, float progress) => onMessageProgress(message, progress);
    }
}