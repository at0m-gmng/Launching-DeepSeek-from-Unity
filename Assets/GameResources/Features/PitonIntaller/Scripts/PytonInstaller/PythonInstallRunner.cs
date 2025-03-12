namespace GameResources.Features.PitonIntaller.Scripts.PytonInstaller
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using DownloadedFileRunner.Scripts;

    public class PythonInstallRunner : BaseFileRunner
    {
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

            Process process = Process.Start(startInfo);
            
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