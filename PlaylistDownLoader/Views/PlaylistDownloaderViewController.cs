using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using BS_Utils.Utilities;
using IPA.Loader;
using PlaylistDownLoader.Interfaces;
using UnityEngine;
using Zenject;

namespace PlaylistDownLoader.Views
{
    [HotReload]
    internal class PlaylistDownloaderViewController : BSMLAutomaticViewController
    {
        /// <summary>説明 を取得、設定</summary>
        private string notificationText_;
        /// <summary>説明 を取得、設定</summary>
        [UIValue("notification-text")]
        public string NotificationText
        {
            get => this.notificationText_ ?? "";

            set
            {
                this.notificationText_ = value;
                this.NotifyPropertyChanged();
            }
        }

        private FloatingScreen _floatingScreen;
        private DateTime lastUpdateTime;
        private IPlaylistDownloader _controller;
        [Inject]
        void Constractor(IPlaylistDownloader controller)
        {
            this._controller = controller;
        }

        void Update()
        {
            if (!string.IsNullOrEmpty(this.NotificationText) && lastUpdateTime.AddSeconds(5) <= DateTime.Now) {
                this.NotificationText = "";
            }
        }

        void Awake()
        {
            DontDestroyOnLoad(this);
            BSEvents.lateMenuSceneLoadedFresh += this.BSEvents_lateMenuSceneLoadedFresh;
        }

        protected override void OnDestroy()
        {
            BSEvents.lateMenuSceneLoadedFresh -= this.BSEvents_lateMenuSceneLoadedFresh;
            base.OnDestroy();
        }

        private async void BSEvents_lateMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            if (PluginManager.GetPlugin("SyncSaber") != null) {
                return;
            }
            this._floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 20f), false, new Vector3(0f, 0.3f, 2.8f), new Quaternion(0f, 0f, 0f, 0f));
            this._floatingScreen.SetRootViewController(this, AnimationType.None);
            this._controller.ChangeNotificationText += this.ChangeNotificationText;
            await this._controller.CheckPlaylistsSong();
        }

        private void ChangeNotificationText(string obj)
        {
            this.lastUpdateTime = DateTime.Now;
            this.NotificationText = obj;
        }
    }
}
