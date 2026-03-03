using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace LuxeWallpaper.Models
{
    public partial class MonitorInfo : ObservableObject
    {
        [ObservableProperty]
        private string _deviceName;

        [ObservableProperty]
        private Rect _screenBounds;

        [ObservableProperty]
        private Rect _workingArea;

        [ObservableProperty]
        private bool _isPrimary;

        [ObservableProperty]
        private double _dpiScale;

        [ObservableProperty]
        private WallpaperItem _currentWallpaper;

        [ObservableProperty]
        private bool _isEnabled = true;

        public override string ToString()
        {
            return $"{DeviceName} ({(IsPrimary ? "主显示器" : "副显示器")})";
        }
    }
}
