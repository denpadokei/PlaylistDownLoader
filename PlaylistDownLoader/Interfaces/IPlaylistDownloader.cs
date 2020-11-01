using BeatSaverSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaylistDownLoader.Interfaces
{
    public interface IPlaylistDownloader
    {
        event Action<string> ChangeNotificationText;
        BeatSaver Current { get; }
        Task CheckPlaylistsSong();
    }
}
