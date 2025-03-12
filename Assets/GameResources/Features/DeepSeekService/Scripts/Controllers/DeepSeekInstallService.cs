namespace GameResources.Features.DeepSeekService.Scripts.Controllers
{
    using System;
    using System.Threading.Tasks;
    using DeepSeekChecker;
    using DeepSeekDownloader;
    using DeepSeekInstaller;
    using InstallService.Scripts;
    using Services.Scripts;
    using UnityEngine;

    public class DeepSeekInstallService: BaseInstallService, IService
    {
        protected override string[] RequiredFiles => requiredFiles;

        protected string installingPath = @$"{Application.streamingAssetsPath}/DeepSeek";

        public override void InstallBindings() 
            => Container.Bind<IService>().To<DeepSeekInstallService>().FromComponentOn(gameObject).AsTransient();

        public virtual async Task<bool> TryRegister()
        {
            fileChecker = new DeepSeekChecker(installingPath, RequiredFiles);
            fileDownloader = Container.Instantiate<DeepSeekDownloader>();
            installRunner = Container.Instantiate<DeepSeekInstallRunner>(new []
            {
                installingPath,
                string.Empty
            });
            
            installController = Container.Instantiate<DeepSeekInstallController>(new object[]
            {
                fileChecker,
                fileDownloader,
                installRunner,
                TryGetDownloadModelURL()
            });

            try
            {
                return await InitInstall();
            }
            catch (Exception e)
            {
                return false;
            }
        }

        protected virtual string TryGetDownloadModelURL()
        {
            if (!installerUrl.Contains("drive.usercontent.google.com"))
            {
                installerUrl = "https://drive.google.com/uc?export=download&id=" + installerUrl;
            }

            return installerUrl;
        }
    }
}