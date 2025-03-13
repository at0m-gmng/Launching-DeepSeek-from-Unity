namespace GameResources.Features.DeepSeekService.Scripts.DeepSeekInstaller
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using DownloadedFileRunner.Scripts;
    using ProcessController;
    using SystemNotification.Scripts;
    using SystemNotification.Scripts.Interfaces;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class DeepSeekInstallRunner : BaseFileRunner, IProgressSystemNotification
    {
        protected const string UNPUCKING_PROGRESS = "Unpacking: {0}";
        
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService, ProcessService _processService)
        {
            systemMessageService = _systemMessageService;
            processService = _processService;
            systemMessageService.RegisterMessage(this);
            processService.RegisterProcess(process);
        }
        
        public DeepSeekInstallRunner(string _targetFolder, string _extractorPath)
        {
            targetFolder = _targetFolder;
            extractorPath = _extractorPath;
        }
        
        public event Action<string> onMessage = delegate { };
        public event Action<string, float> onMessageProgress = delegate { };

        protected SystemMessageService systemMessageService = default;
        protected ProcessService processService = default;
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
                // Use ZipArchive for file-by-file extraction with progress
                await ExtractWithProgress(path);
            }
            else
            {
                // Use a Process (e.g. 7z.exe) with standard output monitoring
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
            onMessage("Starting unpacking via ZipArchive...");

            await Task.Run(async () =>
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    int totalFiles = archive.Entries.Count;
                    int extractedFiles = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(targetFolder, entry.FullName);

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        entry.ExtractToFile(destinationPath, overwrite: true);
                        extractedFiles++;

                        progress = (float)extractedFiles / totalFiles;
                        progressMax = progress * 100;
                        await UniTask.SwitchToMainThread();
                        onMessageProgress(string.Format(UNPUCKING_PROGRESS, progressMax), progressMax);
                        Debug.LogError($"Unboxing: {(progress * 100).ToString("0.0")}");
                        await UniTask.SwitchToThreadPool();
                    }
                }
                
            });
        }

        protected virtual async Task ExtractWithProcess()
        {
            onMessage("Starting unpacking via external tool...");

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
                        onMessage($"Unpacking error: {errorMessage}");
                    }
                }
            });
            onMessage("Unpacking complete.");
        }
    }
}