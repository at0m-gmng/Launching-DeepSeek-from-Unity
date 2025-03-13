namespace GameResources.Features.ProcessController
{
    using System.Diagnostics;

    public interface IJobObjectService
    {
        bool AddProcess(Process process);
    }
}