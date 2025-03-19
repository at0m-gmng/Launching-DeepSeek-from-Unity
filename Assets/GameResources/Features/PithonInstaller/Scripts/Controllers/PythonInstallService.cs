namespace GameResources.Features.PithonInstaller.Scripts.Controllers
{
    using System.Threading.Tasks;
    using GameResources.Features.InstallService.Scripts;
    using GameResources.Services.Scripts;
    using PythonChecker;
    using PytonDownloader;
    using PytonInstaller;
    using UnityEngine;

    public class PythonInstallService : BaseInstallService, IService
    {
        [Header("Specify the Python version to install (e.g. 3.9.7)")]
        [SerializeField] private string pythonVersion = "3.9.7";

        public override void InstallBindings() 
            => Container.Bind<IService>().To<PythonInstallService>().FromComponentOn(gameObject).AsTransient();

        public virtual async Task<bool> TryRegister()
        {
            fileChecker = new PythonChecker
            (
                Application.streamingAssetsPath, 
                RequiredFiles
            );
            
            fileDownloader = Container.Instantiate<PythonDownloader>();
            installRunner = Container.Instantiate<PythonInstallRunner>();

            installController = Container.Instantiate<PytonInstallController>(new object[]
            {
                fileChecker,
                fileDownloader,
                installRunner,
                installerUrl,
                pythonVersion
            });

            return await InitInstall();
        }
    }
}