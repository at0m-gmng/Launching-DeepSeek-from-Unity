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
        {
            Container.Bind<IService>().To<ProcessService>().FromComponentOn(gameObject).AsTransient();
            Container.Bind<ProcessService>().FromInstance(this);
        }

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

        public bool RegisterProcess(Process process) 
            => Environment.OSVersion.Platform == PlatformID.Win32NT && process != null &&
               !process.HasExited && WindowsJobObjectApi.AssignProcessToJob(jobHandle, process);
        public bool RegisterProcess(IntPtr processHandle) 
            => Environment.OSVersion.Platform == PlatformID.Win32NT &&  
               WindowsJobObjectApi.AssignProcessToJob(jobHandle, processHandle);

        protected virtual void Dispose()
        {
            if (jobHandle != IntPtr.Zero)
            {
                WindowsJobObjectApi.CloseJob(jobHandle);
                jobHandle = IntPtr.Zero;
            }
        }

        protected virtual void OnApplicationQuit() => Dispose();
    }
}