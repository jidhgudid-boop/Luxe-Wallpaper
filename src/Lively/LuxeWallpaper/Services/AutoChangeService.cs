using LuxeWallpaper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace LuxeWallpaper.Services
{
    public class AutoChangeService
    {
        private readonly WallpaperService _wallpaperService;
        private readonly UserSettings _settings;
        private readonly System.Timers.Timer _timer;
        private readonly Random _random;
        private List<WallpaperItem> _wallpaperQueue;
        private int _currentIndex;

        public event EventHandler<WallpaperItem> WallpaperChanged;

        public bool IsRunning => _timer.Enabled;

        public AutoChangeService(WallpaperService wallpaperService, UserSettings settings)
        {
            _wallpaperService = wallpaperService;
            _settings = settings;
            _random = new Random();
            _wallpaperQueue = new List<WallpaperItem>();
            _currentIndex = 0;

            _timer = new System.Timers.Timer();
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;

            UpdateInterval();
        }

        public void Start()
        {
            if (_settings.AutoChangeInterval <= 0)
                return;

            RefreshQueue();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void UpdateInterval()
        {
            _timer.Interval = _settings.AutoChangeInterval * 60 * 1000;
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_wallpaperQueue.Count == 0)
            {
                RefreshQueue();
            }

            if (_wallpaperQueue.Count > 0)
            {
                var wallpaper = _wallpaperQueue[_currentIndex];
                await _wallpaperService.SetWallpaperAsync(wallpaper);
                WallpaperChanged?.Invoke(this, wallpaper);

                _currentIndex++;
                if (_currentIndex >= _wallpaperQueue.Count)
                {
                    _currentIndex = 0;
                    RefreshQueue();
                }
            }
        }

        private void RefreshQueue()
        {
            _wallpaperQueue = _wallpaperService.GetAllWallpapers()
                .OrderBy(x => _random.Next())
                .ToList();
            _currentIndex = 0;
        }

        public void NextWallpaper()
        {
            if (_wallpaperQueue.Count == 0)
            {
                RefreshQueue();
            }

            if (_wallpaperQueue.Count > 0)
            {
                _currentIndex++;
                if (_currentIndex >= _wallpaperQueue.Count)
                {
                    _currentIndex = 0;
                }

                var wallpaper = _wallpaperQueue[_currentIndex];
                _wallpaperService.SetWallpaperAsync(wallpaper);
                WallpaperChanged?.Invoke(this, wallpaper);
            }
        }

        public void PreviousWallpaper()
        {
            if (_wallpaperQueue.Count == 0)
            {
                RefreshQueue();
            }

            if (_wallpaperQueue.Count > 0)
            {
                _currentIndex--;
                if (_currentIndex < 0)
                {
                    _currentIndex = _wallpaperQueue.Count - 1;
                }

                var wallpaper = _wallpaperQueue[_currentIndex];
                _wallpaperService.SetWallpaperAsync(wallpaper);
                WallpaperChanged?.Invoke(this, wallpaper);
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
