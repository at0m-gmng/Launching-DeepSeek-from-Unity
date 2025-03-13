namespace GameResources.Features.PitonIntaller.Scripts.PytonInstaller
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using DownloadedFileRunner.Scripts;
    using ProcessController;
    using Zenject;

    public class PythonInstallRunner : BaseFileRunner
    {
        [Inject]
        protected virtual void Construct(ProcessService _processService)
        {
            processService = _processService;
            processService.RegisterProcess(process);
        }
        
        protected ProcessService processService = default;
        protected Process process = default;
        
        public override async Task<bool> RunAsync(string path)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "/quiet InstallAllUsers=1 PrependPath=1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process = Process.Start(startInfo);
            
            using (process)
            {
                await Task.Run(() =>
                {
                    process?.WaitForExit();
                });
                
                return process != null && process.ExitCode == 0;
            }
        }
    }
}