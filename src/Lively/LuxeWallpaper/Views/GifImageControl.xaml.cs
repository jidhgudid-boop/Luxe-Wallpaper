using ImageMagick;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace LuxeWallpaper.Views
{
    public partial class GifImageControl : UserControl, IDisposable
    {
        private MagickImageCollection? _gifCollection;
        private int _currentFrameIndex;
        private bool _isPlaying;
        private readonly System.Windows.Threading.DispatcherTimer _frameTimer;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(GifImageControl),
                new PropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(GifImageControl),
                new PropertyMetadata(Stretch.UniformToFill, OnStretchChanged));

        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public GifImageControl()
        {
            InitializeComponent();
            _frameTimer = new System.Windows.Threading.DispatcherTimer();
            _frameTimer.Tick += FrameTimer_Tick;
            Unloaded += GifImageControl_Unloaded;
        }

        private void GifImageControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            Dispose();
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GifImageControl control)
            {
                control.LoadGif();
            }
        }

        private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GifImageControl control && control.GifImage != null)
            {
                control.GifImage.Stretch = (Stretch)e.NewValue;
            }
        }

        private void LoadGif()
        {
            StopAnimation();
            DisposeGifCollection();

            if (string.IsNullOrEmpty(Source) || !File.Exists(Source))
            {
                GifImage.Source = null;
                return;
            }

            try
            {
                var extension = Path.GetExtension(Source).ToLowerInvariant();
                if (extension != ".gif")
                {
                    // 非GIF文件，使用普通图片加载
                    GifImage.Source = new BitmapImage(new Uri(Source));
                    return;
                }

                _gifCollection = new MagickImageCollection(Source);
                
                if (_gifCollection.Count == 0)
                {
                    DisposeGifCollection();
                    return;
                }

                // 如果是单帧GIF，直接显示
                if (_gifCollection.Count == 1)
                {
                    DisplayFrame(0);
                    return;
                }

                // 开始动画播放
                StartAnimation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading GIF: {ex.Message}");
                // 回退到普通图片加载
                try
                {
                    GifImage.Source = new BitmapImage(new Uri(Source));
                }
                catch { }
            }
        }

        private void StartAnimation()
        {
            if (_gifCollection == null || _gifCollection.Count <= 1)
                return;

            _isPlaying = true;
            _currentFrameIndex = 0;
            
            // 显示第一帧
            DisplayFrame(0);
            
            // 设置定时器
            UpdateTimerInterval();
            _frameTimer.Start();
        }

        private void StopAnimation()
        {
            _isPlaying = false;
            _frameTimer?.Stop();
        }

        private void FrameTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isPlaying || _gifCollection == null || _gifCollection.Count <= 1)
                return;

            _currentFrameIndex++;
            if (_currentFrameIndex >= _gifCollection.Count)
            {
                _currentFrameIndex = 0; // 循环播放
            }

            DisplayFrame(_currentFrameIndex);
            UpdateTimerInterval();
        }

        private void UpdateTimerInterval()
        {
            if (_gifCollection == null || _currentFrameIndex >= _gifCollection.Count)
                return;

            var frame = _gifCollection[_currentFrameIndex];
            // 获取帧延迟，默认100ms（10fps）
            var delay = frame.AnimationDelay;
            if (delay <= 0)
                delay = 10; // ImageMagick使用1/100秒为单位

            // 转换为毫秒
            var interval = TimeSpan.FromMilliseconds(delay * 10);
            _frameTimer.Interval = interval;
        }

        private void DisplayFrame(int frameIndex)
        {
            if (_gifCollection == null || frameIndex >= _gifCollection.Count)
                return;

            try
            {
                var frame = _gifCollection[frameIndex];
                
                // 转换为BitmapSource
                using (var memoryStream = new MemoryStream())
                {
                    frame.Format = MagickFormat.Png;
                    frame.Write(memoryStream);
                    memoryStream.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = memoryStream;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    GifImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying frame {frameIndex}: {ex.Message}");
            }
        }

        private void DisposeGifCollection()
        {
            if (_gifCollection != null)
            {
                _gifCollection.Dispose();
                _gifCollection = null;
            }
        }

        public void Dispose()
        {
            StopAnimation();
            DisposeGifCollection();
            _frameTimer?.Stop();
        }
    }
}
