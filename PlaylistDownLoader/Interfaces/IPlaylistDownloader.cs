using System;
using System.Threading.Tasks;

namespace PlaylistDownLoader.Interfaces
{
    public interface IPlaylistDownloader
    {
        event Action<string> ChangeNotificationText;
        Task CheckPlaylistsSong();
    }
}
