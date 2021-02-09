using PlaylistManager.Utilities;
using SongCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlaylistDownLoader.Utilites
{
    public static class PlaylistManagerUtil
    {
        private static WaitWhile waitWhileLoadingAndIngame = new WaitWhile(() => Plugin.IsInGame || Loader.AreSongsLoading);

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
