using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using UnityEngine.SceneManagement;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;
using BS_Utils.Utilities;
using IPA.Loader;

namespace PlaylistDownLoader
{

    [Plugin(RuntimeOptions.SingleStartInit)]
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
        public void Init(IPALogger logger)
        {
            instance = this;
            Logger.log = logger;
            Logger.log.Debug("Logger initialized.");
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
            Logger.log.Debug("OnApplicationStart");
            SceneManager.activeSceneChanged += this.SceneManager_activeSceneChanged;
            BSEvents.lateMenuSceneLoadedFresh += this.BSEvents_lateMenuSceneLoadedFresh;
            new GameObject("PlaylistDownLoaderController").AddComponent<PlaylistDownLoaderController>();
        }

        private async void BSEvents_lateMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            if (this.IsInstallSyncSaber()) {
                return;
            }
            await PlaylistDownLoaderController.instance.CheckPlaylistsSong();
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
            Logger.log.Debug("OnApplicationQuit");
            SceneManager.activeSceneChanged -= this.SceneManager_activeSceneChanged;
        }
    }
}
