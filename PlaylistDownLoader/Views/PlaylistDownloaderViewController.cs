using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Loader;
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
        private PlaylistDownLoaderController controller;
        private PlaylistDownLoaderController.PlaylistDownLoaderControllerFactory _factory;

        [Inject]
        void Constractor(PlaylistDownLoaderController.PlaylistDownLoaderControllerFactory factory)
        {
            this._factory = factory;
        }

        void FixedUpdate()
        {
            if (!string.IsNullOrEmpty(this.NotificationText) && lastUpdateTime.AddSeconds(5) <= DateTime.Now) {
                this.NotificationText = "";
            }
        }

        async void Start()
        {
            if (PluginManager.GetPlugin("SyncSaber") != null) {
                return;
            }
            this.controller = this._factory.Create();
            DontDestroyOnLoad(controller.gameObject);
            this._floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 30f), false, new Vector3(0f, 0.3f, 2.4f), new Quaternion(0, 0, 0, 0));
            this._floatingScreen.SetRootViewController(this, AnimationType.None);
            this.controller.ChangeNotificationText += this.ChangeNotificationText;
            await this.controller.CheckPlaylistsSong();
        }

        private void ChangeNotificationText(string obj)
        {
            this.lastUpdateTime = DateTime.Now;
            this.NotificationText = obj;
        }
    }
}
