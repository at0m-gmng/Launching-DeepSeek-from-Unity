namespace GameResources.Features.ProcessController
{
    using System.Diagnostics;

    public interface IJobObjectService
    {
        public bool RegisterProcess(Process process);
    }
}