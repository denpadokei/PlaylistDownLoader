using PlaylistManager.Utilities;
using SongCore;
using System.Collections;
using UnityEngine;

namespace PlaylistDownLoader.Utilites
{
    public static class PlaylistManagerUtil
    {
        private static readonly WaitWhile waitWhileLoadingAndIngame = new WaitWhile(() => Plugin.IsInGame || Loader.AreSongsLoading);

        public static BeatSaberPlaylistsLib.PlaylistManager Current => PlaylistLibUtils.playlistManager;

        public static IEnumerator RefreshPlaylist()
        {
            yield return waitWhileLoadingAndIngame;
            Loader.Instance.RefreshSongs(false);
            yield return waitWhileLoadingAndIngame;
            _ = Current.GetAllPlaylists();
        }
    }
}
