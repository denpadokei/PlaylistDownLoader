﻿using System;
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
using SiraUtil.Zenject;
using PlaylistDownLoader.Installers;

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
            Logger.log = logger;
            Logger.log.Debug("Logger initialized.");
            zenjector.OnMenu<PlaylistDownloaderInstaller>();
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
