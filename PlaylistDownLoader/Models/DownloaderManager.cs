using PlaylistDownLoader.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace PlaylistDownLoader.Models
{
    public class DownloaderManager : MonoBehaviour
    {
        [Inject]
        private PlaylistDownloaderViewController _viewController;
    }
}
