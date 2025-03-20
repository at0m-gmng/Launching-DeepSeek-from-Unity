namespace GameResources.Features.FileChecker.Scripts
{
    using System.Threading.Tasks;

    public interface IComponentChecker
    {
        public Task<bool> IsContains();
    }
}