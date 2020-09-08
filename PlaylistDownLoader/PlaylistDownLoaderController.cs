using BeatSaverSharp;
using Newtonsoft.Json;
using PlaylistDownLoader.Models;
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
using System.Threading.Tasks;
using UnityEngine;

namespace PlaylistDownLoader
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class PlaylistDownLoaderController : MonoBehaviour
    {
        private static readonly string _tempPath = $@"{Path.GetTempPath()}\PlaylistDownloader";
        private static readonly string _playlistsDirectory = Path.Combine(Environment.CurrentDirectory, "Playlists");
        private static readonly string _customLevelsDirectory = Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels");
        private static readonly HashSet<string> _downloadedSongHash = new HashSet<string>();

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

            var httpOption = new HttpOptions() { ApplicationName = "PlaylistDownloader", Version = Assembly.GetExecutingAssembly().GetName().Version };
            this.Current = new BeatSaver(httpOption);
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

        public async Task CheckPlaylistsSong()
        {
            while (!Loader.AreSongsLoaded || Loader.AreSongsLoading) {
                await Task.Delay(200);
            }

            AnyDownloaded = false;
            List<FileInfo> playlists = new List<FileInfo>();
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
                        await this.DownloadSong(song.hash);
                        _downloadedSongHash.Add(song.hash.ToUpper());
                    }
                }

                if (AnyDownloaded) {
                    StartCoroutine(RefreshSongsAndPlaylists());
                }
            }
            catch (Exception e) {
                Logger.log.Error(e);
            }
        }

        private async Task DownloadSong(string hash)
        {
            try {
                var timer = new Stopwatch();
                timer.Start();
                var beatmap = await this.Current.Hash(hash);
                if (beatmap == null) {
                    return;
                }
                Logger.log.Info($"DownloadedSongInfo : {beatmap.Metadata.SongName} ({timer.ElapsedMilliseconds} ms)");
                var songDirectoryPath = Path.Combine(_customLevelsDirectory, $"{beatmap.Key}({Regex.Replace(beatmap.Metadata.SongName, "[/:*<>|?\"]", "")} - {Regex.Replace(beatmap.Metadata.SongAuthorName, "[/:*<>|?\"]", "")})");
                while (Plugin.IsInGame) {
                    await Task.Delay(200);
                }
                if (File.Exists(songDirectoryPath)) {
                    return;
                }
                var buff = await beatmap.DownloadZip();
                Logger.log.Info($"DownloadedSongZip : {beatmap.Metadata.SongName}  ({timer.ElapsedMilliseconds} ms)");
                using (var ms = new MemoryStream(buff))
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read)) {
                    archive.ExtractToDirectory(songDirectoryPath);
                }
                timer.Stop();
                Logger.log.Info($"Downloaded : {beatmap.Metadata.SongName}  ({timer.ElapsedMilliseconds} ms)");
                AnyDownloaded = true;
            }
            catch (Exception e) {
                Logger.log.Error(e);
            }
        }

        private IEnumerator RefreshSongsAndPlaylists()
        {
            yield return new WaitWhile(() => Plugin.IsInGame || Loader.AreSongsLoading);
            Loader.Instance.RefreshSongs(false);
            yield return new WaitWhile(() => Plugin.IsInGame || Loader.AreSongsLoading);
            PlaylistCollectionOverride.refreshPlaylists();
        }
    }
}
