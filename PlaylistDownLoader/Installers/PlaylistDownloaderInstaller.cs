using PlaylistDownLoader.Views;
using Zenject;

namespace PlaylistDownLoader.Installers
{
    public class PlaylistDownloaderInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<PlaylistDownLoaderController>().FromNewComponentOnNewGameObject().AsSingle();
            this.Container.BindInterfacesAndSelfTo<PlaylistDownloaderViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();
        }
    }
}
