namespace GameResources.Services.Scripts
{
    using System.Threading.Tasks;

    public interface IService
    {
        public Task<bool> TryRegister();
    }
}