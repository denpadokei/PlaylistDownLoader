using Newtonsoft.Json;
using PlaylistDownLoader.Interfaces;
using PlaylistDownLoader.Models;
using PlaylistDownLoader.Networks;
using PlaylistDownLoader.SimpleJson;
using PlaylistDownLoader.Utilites;
using SongCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PlaylistDownLoader
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class PlaylistDownLoaderController : MonoBehaviour, IPlaylistDownloader
    {
        private static readonly string _playlistsDirectory = Path.Combine(Environment.CurrentDirectory, "Playlists");
        private static readonly string _customLevelsDirectory = Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels");
        private static readonly HashSet<string> _downloadedSongHash = new HashSet<string>();
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(2, 2);

        public event Action<string> ChangeNotificationText;

        public const string ROOT_URL = "https://beatsaver.com/api";
        public const string ROOT_DL_URL = "https://cdn.beatsaver.com";

        public bool AnyDownloaded { get; private set; }

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            //DontDestroyOnLoad(this); // Don't destroy this object on scene changes

            Logger.Debug($"{name}: Awake()");
            this.StartCoroutine(this.CreateText());
        }
        #endregion

        private IEnumerator CreateText()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Any(t => t.name == "Teko-Medium SDF No Glow"));
            this.ChengeText("Finish PlaylistDownloader Initiaraize.");
        }

        public async Task CheckPlaylistsSong()
        {
            while (!Loader.AreSongsLoaded || Loader.AreSongsLoading) {
                await Task.Delay(200);
            }

            AnyDownloaded = false;
            List<FileInfo> playlists = new List<FileInfo>();
            List<Task> downloadTask = new List<Task>();
            playlists.AddRange(Directory.EnumerateFiles(_playlistsDirectory, "*.json").Select(x => new FileInfo(x)));
            playlists.AddRange(Directory.EnumerateFiles(_playlistsDirectory, "*.bplist").Select(x => new FileInfo(x)));
            try {
                foreach (var playlist in playlists.Select(x => JsonConvert.DeserializeObject<PlaylistEntity>(File.ReadAllText(x.FullName)))) {
                    foreach (var song in playlist.songs.Where(x => !string.IsNullOrEmpty(x.hash))) {
                        while (Plugin.IsInGame || !Loader.AreSongsLoaded || Loader.AreSongsLoading) {
                            await Task.Delay(200);
                        }
                        if (Loader.GetLevelByHash(song.hash.ToUpper()) != null || _downloadedSongHash.Any(x => x == song.hash.ToUpper())) {
                            _downloadedSongHash.Add(song.hash.ToUpper());
                            continue;
                        }
                        downloadTask.Add(this.DownloadSong(song.hash));
                        _downloadedSongHash.Add(song.hash.ToUpper());
                    }
                }
                await Task.WhenAll(downloadTask);
                if (this.AnyDownloaded) {
                    StartCoroutine(PlaylistManagerUtil.RefreshPlaylist());
                }
                ChengeText("Checked PlaylitsSongs");
            }
            catch (Exception e) {
                Logger.Error(e);
            }
        }



        private async Task DownloadSong(string hash)
        {
            var timer = new Stopwatch();
            WebResponse res = null;
            try {
                await semaphoreSlim.WaitAsync().ConfigureAwait(false);

                timer.Start();
                while (Plugin.IsInGame) {
                    await Task.Delay(200).ConfigureAwait(false);
                }
                if (HistoryManager.Contains(hash)) {
                    return;
                }
                res = await WebClient.GetAsync($"{ROOT_URL}/maps/hash/{hash.ToLower()}", CancellationToken.None);
                HistoryManager.Add(hash);
                if (!res.IsSuccessStatusCode) {
                    Logger.Info($"Beatmap is not find. {hash}");
                    return;
                }
                var json = res.ConvertToJsonNode();
                if (json == null) {
                    Logger.Info($"Beatmap is not find. {hash}");
                    return;
                }
                var meta = json["metadata"].AsObject;
                Logger.Info($"DownloadedSongInfo : {meta["songName"].Value} ({timer.ElapsedMilliseconds} ms)");                
                var version = json["versions"].AsArray.Children.FirstOrDefault(x => string.Equals(x["state"].Value, "Published", StringComparison.InvariantCultureIgnoreCase));
                if (version == null) {
                    Logger.Debug("this map is not published.");
                    // 後でパブリッシュになったらDLできるように外しておく。
                    HistoryManager.Remove(hash);
                    return;
                }
                var key = json["id"].Value;
                var songDirectoryPath = this.CreateSongDirectory(json);
                while (Plugin.IsInGame) {
                    await Task.Delay(200).ConfigureAwait(false);
                }
                if (File.Exists(songDirectoryPath)) {
                    return;
                }
                var dlurl = version.AsObject["downloadURL"].Value;
                if (string.IsNullOrEmpty(dlurl)) {
                    dlurl = $"{ROOT_DL_URL}/{hash.ToLower()}.zip";
                }
                using (var ms = new MemoryStream(await WebClient.DownloadSong($"{dlurl}", CancellationToken.None)))
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read)) {
                    Logger.Info($"DownloadedSongZip : {meta["songName"].Value}  ({timer.ElapsedMilliseconds} ms)");
                    try {
                        archive.ExtractToDirectory(songDirectoryPath);
                    }
                    catch (Exception e) {
                        Logger.Error($"{e}");
                    }
                    this.ChengeText($"Downloaded {meta["songName"].Value}");
                }

                AnyDownloaded = true;
            }
            catch (Exception e) {
                Logger.Error(e);
            }
            finally {
                HistoryManager.Save();
                if (timer.IsRunning) {
                    timer.Stop();
                }
                Logger.Info($"Downloaded : {res.ConvertToJsonNode()?["name"]}  ({timer.ElapsedMilliseconds} ms)");
                semaphoreSlim.Release();
            }
        }
        private void ChengeText(string message)
        {
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                Logger.Info(message);
                this.ChangeNotificationText?.Invoke($"PlaylistDownloader - {message}");
            });
        }

        private string CreateSongDirectory(JSONNode songNode)
        {
            var metaData = songNode["metadata"].AsObject;
            var songIndex = Regex.Replace($"{songNode["id"].Value} ({metaData["songName"].Value} - {metaData["levelAuthorName"].Value})", "[\\\\:*/?\"<>|]", "_");
            var result = Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels", songIndex);
            var count = 1;
            var resultLength = result.Length;
            while (Directory.Exists(result)) {
                result = $"{result.Substring(0, resultLength)}({count})";
                count++;
            }
            return result;
        }
    }
}
