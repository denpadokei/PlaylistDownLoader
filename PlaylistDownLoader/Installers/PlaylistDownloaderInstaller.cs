using PlaylistDownLoader.Models;
using PlaylistDownLoader.Views;
using SiraUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace PlaylistDownLoader.Installers
{
    public class PlaylistDownloaderInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.Bind<PlaylistDownLoaderController.PlaylistDownLoaderControllerFactory>().AsSingle();
            this.Container.BindViewController<PlaylistDownloaderViewController>();
            this.Container.Bind<DownloaderManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        }
    }
}
