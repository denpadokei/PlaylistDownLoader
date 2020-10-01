using BeatSaverSharp;
using Newtonsoft.Json;
using PlaylistDownLoader.Models;
using PlaylistDownLoader.Utilites;
using PlaylistLoaderLite.HarmonyPatches;
using SongCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
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
    public class PlaylistDownLoaderController : MonoBehaviour
    {
        private static readonly string _playlistsDirectory = Path.Combine(Environment.CurrentDirectory, "Playlists");
        private static readonly string _customLevelsDirectory = Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels");
        private static readonly HashSet<string> _downloadedSongHash = new HashSet<string>();
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(2, 2);
        private static DateTime lastUpdateTime;

        private TMP_Text progressText;

        public bool AnyDownloaded { get; private set; }

        public BeatSaver Current { get; private set; }

        public static PlaylistDownLoaderController instance { get; private set; }

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (instance != null) {
                Logger.log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            instance = this;
            Logger.log?.Debug($"{name}: Awake()");

            var httpOption = new HttpOptions() { ApplicationName = "PlaylistDownloader", Version = Assembly.GetExecutingAssembly().GetName().Version, Timeout = new TimeSpan(0, 0, 10), HandleRateLimits = false };
            this.Current = new BeatSaver(httpOption);

            this.StartCoroutine(this.CreateText());
            
        }

        private void FixedUpdate()
        {
            if (!string.IsNullOrEmpty(progressText?.text) && lastUpdateTime.AddSeconds(5) <= DateTime.Now) {
                progressText.text = "";
            }
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Logger.log?.Debug($"{name}: OnDestroy()");
            instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.
        }
        #endregion

        private IEnumerator CreateText()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Any(t => t.name == "Teko-Medium SDF No Glow"));
            this.progressText = Utility.CreateNotificationText("");
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
                        while (Plugin.IsInGame) {
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
                if (AnyDownloaded) {
                    StartCoroutine(RefreshSongsAndPlaylists());
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
            Beatmap beatmap = null;
            try {
                await semaphoreSlim.WaitAsync().ConfigureAwait(false);

                timer.Start();
                while (Plugin.IsInGame) {
                    await Task.Delay(200).ConfigureAwait(false);
                }
                beatmap = await this.Current.Hash(hash).ConfigureAwait(false);
                if (beatmap == null) {
                    Logger.log.Info($"Beatmap is not find. {hash}");
                    return;
                }
                Logger.log.Info($"DownloadedSongInfo : {beatmap.Metadata.SongName} ({timer.ElapsedMilliseconds} ms)");
                var songDirectoryPath = Path.Combine(_customLevelsDirectory, Regex.Replace($"{beatmap.Key}({beatmap.Metadata.SongName} - {beatmap.Metadata.SongAuthorName})", "[/:*<>|?\"]", "_"));
                while (Plugin.IsInGame) {
                    await Task.Delay(200).ConfigureAwait(false);
                }
                if (File.Exists(songDirectoryPath)) {
                    return;
                }

                using (var ms = new MemoryStream(await beatmap.DownloadZip().ConfigureAwait(false)))
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read)) {
                    Logger.log.Info($"DownloadedSongZip : {beatmap.Metadata.SongName}  ({timer.ElapsedMilliseconds} ms)");
                    archive.ExtractToDirectory(songDirectoryPath);
                    this.ChengeText($"Downloaded {beatmap.Metadata.SongName}");
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
                Logger.log.Info($"Downloaded : {beatmap?.Metadata.SongName}  ({timer.ElapsedMilliseconds} ms)");
                semaphoreSlim.Release();
            }
        }

        private IEnumerator RefreshSongsAndPlaylists()
        {
            yield return new WaitWhile(() => Plugin.IsInGame || Loader.AreSongsLoading);
            Loader.Instance.RefreshSongs(false);
            yield return new WaitWhile(() => Plugin.IsInGame || Loader.AreSongsLoading);
            PlaylistCollectionOverride.RefreshPlaylists();
        }

        private void ChengeText(string message)
        {
            HMMainThreadDispatcher.instance.Enqueue(() =>
            {
                Logger.log.Info(message);
                progressText.text = $"PlaylistDownloader - {message}";
                lastUpdateTime = DateTime.Now;
            });
        }
    }
}
