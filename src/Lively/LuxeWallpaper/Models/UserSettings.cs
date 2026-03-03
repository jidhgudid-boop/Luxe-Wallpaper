using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace LuxeWallpaper.Models
{
    public partial class UserSettings : ObservableObject
    {
        [ObservableProperty]
        private bool _isDarkTheme = true;

        [ObservableProperty]
        private bool _autoStart;

        [ObservableProperty]
        private bool _autoChangeWallpaper;

        [ObservableProperty]
        private int _autoChangeInterval = 30;

        [ObservableProperty]
        private bool _applyToAllMonitors = true;

        [ObservableProperty]
        private WallpaperFitMode _fitMode = WallpaperFitMode.Fill;

        [ObservableProperty]
        private bool _enableAnimation = true;

        [ObservableProperty]
        private int _animationDuration = 500;

        [ObservableProperty]
        private string _downloadPath;

        [ObservableProperty]
        private List<string> _favoriteCategories = new();

        [ObservableProperty]
        private bool _showNotification = true;

        [ObservableProperty]
        private DateTime _lastUpdateTime;

        [ObservableProperty]
        private bool _powerSavingMode = true;

        [ObservableProperty]
        private int _powerThreshold = 80;

        [ObservableProperty]
        private bool _hardwareAcceleration = true;

        [ObservableProperty]
        private WallpaperEngineType _engineType = WallpaperEngineType.HighPerformance;

        [ObservableProperty]
        private LayoutMode _layoutMode = LayoutMode.Grid2x4;

        [ObservableProperty]
        private bool _fadeInOut = true;

        [ObservableProperty]
        private bool _setStaticWithDynamic = false;

        [ObservableProperty]
        private bool _setLockScreen = false;

        [ObservableProperty]
        private int _fadeInOutDuration = 300;

        [ObservableProperty]
        private bool _minimizeToTray = true;

        public UserSettings()
        {
            _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\HaoWallpaper";
        }
    }

    public enum WallpaperFitMode
    {
        Fill,
        Fit,
        Stretch,
        Center,
        Tile
    }

    public enum WallpaperEngineType
    {
        HighPerformance,
        HighCompatibility
    }

    public enum LayoutMode
    {
        Grid2x4,
        Grid3x7
    }
}
