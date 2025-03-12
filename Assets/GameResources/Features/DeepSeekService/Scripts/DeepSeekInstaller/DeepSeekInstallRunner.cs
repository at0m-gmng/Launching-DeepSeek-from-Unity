namespace GameResources.Features.DeepSeekService.Scripts.DeepSeekInstaller
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using DownloadedFileRunner.Scripts;
    using SystemNotification.Scripts;
    using SystemNotification.Scripts.Interfaces;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class DeepSeekInstallRunner : BaseFileRunner, IProgressSystemNotification
    {
        protected const string UNPUCKING_PROGRESS = "Распаковка: {0}";
        
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService)
        {
            systemMessageService = _systemMessageService;
            systemMessageService.RegisterMessage(this);
        }
        
        public DeepSeekInstallRunner(string _targetFolder, string _extractorPath)
        {
            targetFolder = _targetFolder;
            extractorPath = _extractorPath;
        }
        
        public event Action<string> onMessage = delegate { };
        public event Action<string, float> onMessageProgress = delegate { };

        protected SystemMessageService systemMessageService = default;
        protected readonly string targetFolder = default;
        protected readonly string extractorPath = default;
        protected string errorMessage = default;
        protected float progress = default;
        protected float progressMax = default;
        protected Process process = default;

        public override async Task<bool> RunAsync(string path)
        {
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            if (string.IsNullOrEmpty(extractorPath))
            {
                // Используем ZipArchive для пофайлового извлечения с прогрессом
                await ExtractWithProgress(path);
            }
            else
            {
                // Используем Process (например, 7z.exe) с мониторингом стандартного вывода
                await ExtractWithProcess();
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }

        protected virtual async Task ExtractWithProgress(string zipPath)
        {
            onMessage("Запуск распаковки через ZipArchive...");
            // Stopwatch stopwatch = Stopwatch.StartNew();
            
            // await UniTask.SwitchToMainThread();
            //
            await Task.Run(async () =>
            {
            
                // Переключаемся в пул потоков для выполнения тяжелой работы
                // await UniTask.SwitchToThreadPool();

                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    int totalFiles = archive.Entries.Count;
                    int extractedFiles = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(targetFolder, entry.FullName);

                        // Если это папка — создаём её
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        // Извлекаем файл
                        entry.ExtractToFile(destinationPath, overwrite: true);
                        extractedFiles++;

                        // Вычисляем прогресс
                        progress = (float)extractedFiles / totalFiles;
                        progressMax = progress * 100;
                        await UniTask.SwitchToMainThread();
                        onMessageProgress(string.Format(UNPUCKING_PROGRESS, progressMax), progressMax);
                        Debug.LogError($"Распаковка: {(progress * 100).ToString("0.0")}");
                        await UniTask.SwitchToThreadPool();
                        
                        // Немного уступаем главный поток, чтобы обновления UI могли обработаться
                        // await UniTask.Yield();
                        // Возвращаемся в пул потоков для продолжения работы
                        // await UniTask.SwitchToThreadPool();
                    }
                }
                
                // По завершении переключаемся на главный поток
                // await UniTask.SwitchToMainThread();
            });
            // stopwatch.Stop();
            // await UniTask.SwitchToMainThread();
        }

        protected virtual async Task ExtractWithProcess()
        {
            onMessage("Запуск распаковки через внешний инструмент...");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = targetFolder,
                Arguments = $"x \"{extractorPath}\" -o\"{targetFolder}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            await Task.Run(() =>
            {
                process = Process.Start(startInfo);
                using (process)
                {
                    process.WaitForExit();
                    errorMessage = process.StandardError.ReadToEnd();

                    if (process.ExitCode != 0)
                    {
                        onMessage($"Ошибка распаковки: {errorMessage}");
                    }
                }
            });
            onMessage("Распаковка завершена.");
        }
    }
}