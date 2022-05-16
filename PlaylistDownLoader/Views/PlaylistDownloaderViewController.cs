using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Loader;
using PlaylistDownLoader.Interfaces;
using System;
using UnityEngine;
using Zenject;

namespace PlaylistDownLoader.Views
{
    [HotReload]
    internal class PlaylistDownloaderViewController : BSMLAutomaticViewController, IInitializable
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
        private void Constractor(IPlaylistDownloader controller)
        {
            this._controller = controller;
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(this.NotificationText) && this.lastUpdateTime.AddSeconds(5) <= DateTime.Now) {
                this.NotificationText = "";
            }
        }
        private void ChangeNotificationText(string obj)
        {
            this.lastUpdateTime = DateTime.Now;
            this.NotificationText = obj;
        }

        public async void Initialize()
        {
            if (PluginManager.GetPlugin("SyncSaber") != null) {
                return;
            }
            this._floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 20f), false, new Vector3(0f, 0.3f, 2.8f), new Quaternion(0f, 0f, 0f, 0f));
            this._floatingScreen.SetRootViewController(this, AnimationType.None);
            this._controller.ChangeNotificationText += this.ChangeNotificationText;
            await this._controller.CheckPlaylistsSong();
        }
    }
}
