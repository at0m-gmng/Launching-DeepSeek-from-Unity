namespace GameResources.Features.ProcessController
{
    using System;
    using System.Diagnostics;
    using Zenject;
    using System.Threading.Tasks;
    using Services.Scripts;

    public class ProcessService : MonoInstaller, IService, IJobObjectService
    {
        protected IntPtr jobHandle;
        
        public override void InstallBindings() 
            => Container.Bind<IService>().To<ProcessService>().FromComponentOn(gameObject).AsTransient();

        public async Task<bool> TryRegister()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                jobHandle = WindowsJobObjectApi.CreateJob();
                if (jobHandle != IntPtr.Zero)
                {
                    WindowsJobObjectApi.SetKillOnJobClose(jobHandle);
                    return true;
                }
            }

            return false;
        }

        public bool AddProcess(Process process)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || process == null || process.HasExited)
                return false;

            return WindowsJobObjectApi.AssignProcessToJob(jobHandle, process);
        }
        
        protected virtual void Dispose()
        {
            if (jobHandle != IntPtr.Zero)
            {
                WindowsJobObjectApi.CloseJob(jobHandle);
                jobHandle = IntPtr.Zero;
            }
        }
    }
}