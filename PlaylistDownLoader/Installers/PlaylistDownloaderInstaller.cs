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
            this.Container.BindInterfacesAndSelfTo<PlaylistDownLoaderController>().FromNewComponentOnNewGameObject(nameof(PlaylistDownLoaderController)).AsSingle();
            this.Container.BindInterfacesAndSelfTo<PlaylistDownloaderViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();
        }
    }
}
