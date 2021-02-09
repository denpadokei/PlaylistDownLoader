using Newtonsoft.Json;
using PlaylistDownLoader.Interfaces;
using PlaylistDownLoader.Models;
using PlaylistDownLoader.Networks;
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

        public bool AnyDownloaded { get; private set; }

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            DontDestroyOnLoad(this); // Don't destroy this object on scene changes

            Logger.log?.Debug($"{name}: Awake()");
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
                Logger.log.Error(e);
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
                res = await WebClient.GetAsync($"https://beatsaver.com/api/maps/by-hash/{hash}", CancellationToken.None);
                if (!res.IsSuccessStatusCode) {
                    Logger.log.Info($"Beatmap is not find. {hash}");
                    return;
                }
                var json = res.ConvertToJsonNode();
                if (json == null) {
                    Logger.log.Info($"Beatmap is not find. {hash}");
                    return;
                }
                var meta = json["metadata"].AsObject;
                Logger.log.Info($"DownloadedSongInfo : {meta["songName"].Value} ({timer.ElapsedMilliseconds} ms)");
                var songDirectoryPath = Path.Combine(_customLevelsDirectory, Regex.Replace($"{json["key"].Value}({meta["songName"].Value} - {meta["songAuthorName"].Value})", "[\\\\/:*<>|?\"]", "_"));
                while (Plugin.IsInGame) {
                    await Task.Delay(200).ConfigureAwait(false);
                }
                if (File.Exists(songDirectoryPath)) {
                    return;
                }

                using (var ms = new MemoryStream(await WebClient.DownloadSong($"https://beatsaver.com{json["downloadURL"].Value}", CancellationToken.None)))
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read)) {
                    Logger.log.Info($"DownloadedSongZip : {meta["songName"].Value}  ({timer.ElapsedMilliseconds} ms)");
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
                Logger.log.Error(e);
            }
            finally {
                if (timer.IsRunning) {
                    timer.Stop();
                }
                Logger.log.Info($"Downloaded : {res.ConvertToJsonNode()?["name"]}  ({timer.ElapsedMilliseconds} ms)");
                semaphoreSlim.Release();
            }
        }
        private void ChengeText(string message)
        {
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                Logger.log.Info(message);
                this.ChangeNotificationText?.Invoke($"PlaylistDownloader - {message}");
            });
        }
    }
}
