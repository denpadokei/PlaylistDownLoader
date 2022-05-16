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
        private static readonly string s_playlistsDirectory = Path.Combine(Environment.CurrentDirectory, "Playlists");
        //private static readonly string s_customLevelsDirectory = Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels");
        private static readonly HashSet<string> s_downloadedSongHash = new HashSet<string>();
        private static readonly SemaphoreSlim s_semaphoreSlim = new SemaphoreSlim(2, 2);
        private static readonly Regex s_invalidDirectoryAndFileChars = new Regex($@"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]");

        public event Action<string> ChangeNotificationText;

#pragma warning disable IDE1006 // 命名スタイル
        public const string ROOT_URL = "https://api.beatsaver.com";
        public const string ROOT_DL_URL = "https://cdn.beatsaver.com";
#pragma warning restore IDE1006 // 命名スタイル

        public bool AnyDownloaded { get; private set; }

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        protected void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            //DontDestroyOnLoad(this); // Don't destroy this object on scene changes

            Logger.Debug($"{this.name}: Awake()");
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

            this.AnyDownloaded = false;
            var playlists = new List<FileInfo>();
            var downloadTask = new List<Task>();
            playlists.AddRange(Directory.EnumerateFiles(s_playlistsDirectory, "*.json").Select(x => new FileInfo(x)));
            playlists.AddRange(Directory.EnumerateFiles(s_playlistsDirectory, "*.bplist").Select(x => new FileInfo(x)));
            try {
                foreach (var playlist in playlists.Select(x => JsonConvert.DeserializeObject<PlaylistEntity>(File.ReadAllText(x.FullName)))) {
                    foreach (var song in playlist.songs.Where(x => !string.IsNullOrEmpty(x.hash))) {
                        while (Plugin.IsInGame || !Loader.AreSongsLoaded || Loader.AreSongsLoading) {
                            await Task.Delay(200);
                        }
                        if (Loader.GetLevelByHash(song.hash.ToUpper()) != null || s_downloadedSongHash.Any(x => x == song.hash.ToUpper())) {
                            s_downloadedSongHash.Add(song.hash.ToUpper());
                            continue;
                        }
                        downloadTask.Add(this.DownloadSong(song.hash));
                        s_downloadedSongHash.Add(song.hash.ToUpper());
                    }
                }
                await Task.WhenAll(downloadTask);
                if (this.AnyDownloaded) {
                    this.StartCoroutine(PlaylistManagerUtil.RefreshPlaylist());
                }
                this.ChengeText("Checked PlaylitsSongs");
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
                await s_semaphoreSlim.WaitAsync().ConfigureAwait(false);

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

                this.AnyDownloaded = true;
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
                s_semaphoreSlim.Release();
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
            var songIndex = s_invalidDirectoryAndFileChars.Replace($"{songNode["id"].Value} ({metaData["songName"].Value} - {metaData["levelAuthorName"].Value})", "_");
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
