using IPA;
using IPA.Loader;
using PlaylistDownLoader.Installers;
using SiraUtil.Zenject;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace PlaylistDownLoader
{

    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin instance { get; private set; }
        internal static string Name => "PlaylistDownLoader";

        public static bool IsInGame { get; private set; }

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init(IPALogger logger, Zenjector zenjector)
        {
            instance = this;
            Logger.SetLogger(logger);
            Logger.Debug("Logger initialized.");
            zenjector.Install<PlaylistDownloaderInstaller>(Location.Menu);
        }

        #region BSIPA Config
        //Uncomment to use BSIPA's config
        /*
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Logger.log.Debug("Config loaded");
        }
        */
        #endregion

        [OnStart]
        public void OnApplicationStart()
        {
            Logger.Debug("OnApplicationStart");

        }

        [OnEnable]
        public void OnEnable()
        {
            SceneManager.activeSceneChanged += this.SceneManager_activeSceneChanged;
        }

        [OnDisable]
        public void OnDisable()
        {
            SceneManager.activeSceneChanged -= this.SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            IsInGame = arg1.name == "GameCore";
        }

        private bool IsInstallSyncSaber()
        {
            return PluginManager.GetPlugin("SyncSaber") != null;
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Logger.Debug("OnApplicationQuit");
        }
    }
}
