using ImageMagick;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LuxeWallpaper.Views;

namespace LuxeWallpaper.Services
{
    /// <summary>
    /// GIF 动态壁纸窗口 - 使用桌面窗口覆盖法
    /// 创建始终置顶、无边框、全屏、WS_EX_LAYERED 透明属性的特殊窗口
    /// 置于桌面图标下方、原生壁纸上方
    /// </summary>
    public class GifWallpaperWindow : Window, IDisposable
    {
        // Windows API 常量
        private const int HWND_BOTTOM = 1;
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_NOSENDCHANGING = 0x0400;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const int WS_EX_LAYERED = unchecked((int)0x00080000);
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x00008000;
        private const int WS_CHILD = unchecked((int)0x40000000);
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_BORDER = unchecked((int)0x00800000);
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_POPUP = unchecked((int)0x80000000);

        // WorkerW 窗口类名
        private const string WORKERW_CLASS_NAME = "WorkerW";

        // GifImage 控件
        private readonly GifImageControl _gifImage;

        // 窗口句柄
        private IntPtr _hwnd;
        private IntPtr _workerWHandle;

        // 状态
        private bool _isPlaying;
        private bool _isDisposed;
        private CancellationTokenSource _cancellationTokenSource;

        // 当前 GIF 路径
        private string _currentGifPath;

        public GifWallpaperWindow()
        {
            // 无边框
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;

            // 全屏
            WindowState = WindowState.Normal;
            Top = -10000;
            Left = -10000;

            // 背景透明
            Background = Brushes.Transparent;
            AllowsTransparency = true;

            // 不显示在任务栏
            ShowInTaskbar = false;

            // 创建 GIF 图像控件
            _gifImage = new GifImageControl
            {
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Content = _gifImage;

            // 加载完成时获取句柄并设置窗口属性
            Loaded += GifWallpaperWindow_Loaded;
            Closing += GifWallpaperWindow_Closing;
            SizeChanged += GifWallpaperWindow_SizeChanged;

            // 关键：在窗口创建前就设置 Owner
            var helper = new WindowInteropHelper(this);
            helper.Owner = GetDesktopWindow();
        }

        private void GifWallpaperWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 窗口大小改变时重新定位
            RepositionWindow();
        }

        private void GifWallpaperWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Stop();
        }

        private void GifWallpaperWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            InitializeWorkerW();
        }

        /// <summary>
        /// 初始化 WorkerW 窗口作为父窗口
        /// </summary>
        private void InitializeWorkerW()
        {
            try
            {
                // 查找 Progman 窗口
                IntPtr progman = FindWindow("Progman", null);
                if (progman == IntPtr.Zero)
                {
                    Debug.WriteLine("找不到 Progman 窗口");
                    return;
                }

                // 发送消息让 Progman 创建 WorkerW 窗口
                SendMessage(progman, 0x052c, IntPtr.Zero, IntPtr.Zero);

                // 等待 WorkerW 创建
                Thread.Sleep(200);

                // 枚举所有窗口，找到正确的 WorkerW
                IntPtr workerw = IntPtr.Zero;
                EnumWindows((hwnd, lParam) =>
                {
                    // 获取窗口类名
                    StringBuilder sb = new StringBuilder(256);
                    GetClassName(hwnd, sb, sb.Capacity);

                    if (sb.ToString() == "WorkerW")
                    {
                        // 检查这个 WorkerW 是否有子窗口
                        IntPtr child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);

                        if (child == IntPtr.Zero)
                        {
                            // 这个 WorkerW 没有子窗口，这是我们需要的
                            workerw = hwnd;
                            Debug.WriteLine($"找到空的 WorkerW 窗口：{hwnd:X}");
                            return false; // 停止枚举
                        }
                    }

                    return true; // 继续枚举
                }, IntPtr.Zero);

                if (workerw == IntPtr.Zero)
                {
                    Debug.WriteLine("找不到合适的 WorkerW 窗口");
                    return;
                }

                // 将我们的窗口设置为 WorkerW 的子窗口
                SetParent(_hwnd, workerw);

                // 设置窗口扩展样式
                int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);

                // 设置窗口样式
                int style = GetWindowLong(_hwnd, GWL_STYLE);
                style &= ~WS_CAPTION;
                style &= ~WS_BORDER;
                style &= ~WS_THICKFRAME;
                style &= ~WS_POPUP;
                style |= WS_CHILD;
                SetWindowLong(_hwnd, GWL_STYLE, style);

                // 获取屏幕尺寸
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                // 设置窗口位置和大小
                SetWindowPos(_hwnd, new IntPtr(HWND_BOTTOM), 0, 0, screenWidth, screenHeight,
                    SWP_NOACTIVATE | SWP_NOSENDCHANGING | SWP_FRAMECHANGED);

                MoveWindow(_hwnd, 0, 0, screenWidth, screenHeight, true);

                Debug.WriteLine($"GIF 窗口初始化完成，尺寸：{screenWidth}x{screenHeight}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 播放 GIF 壁纸
        /// </summary>
        public void Play(string gifPath)
        {
            if (_isDisposed || string.IsNullOrEmpty(gifPath) || !File.Exists(gifPath))
                return;

            _currentGifPath = gifPath;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    _gifImage.Source = gifPath;
                    _isPlaying = true;

                    // 获取屏幕尺寸
                    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                    int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                    // 设置窗口尺寸和位置
                    Width = screenWidth;
                    Height = screenHeight;
                    Top = 0;
                    Left = 0;

                    // 显示窗口
                    Show();

                    // 确保窗口在正确的位置
                    RepositionWindow();

                    Debug.WriteLine($"开始播放 GIF: {gifPath}, 窗口尺寸：{screenWidth}x{screenHeight}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"播放 GIF 失败：{ex.Message}");
                }
            });
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _cancellationTokenSource?.Cancel();

            Dispatcher.Invoke(() =>
            {
                try
                {
                    _gifImage.Source = null;
                    Hide();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"停止 GIF 失败：{ex.Message}");
                }
            });
        }

        /// <summary>
        /// 重新定位窗口到桌面
        /// </summary>
        public void RepositionWindow()
        {
            if (_hwnd == IntPtr.Zero)
                return;

            try
            {
                // 查找 WorkerW 窗口
                _workerWHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, WORKERW_CLASS_NAME, null);

                if (_workerWHandle != IntPtr.Zero)
                {
                    // 重新设置父窗口
                    SetParent(_hwnd, _workerWHandle);

                    // 获取屏幕尺寸
                    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                    int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                    // 设置窗口位置为底部，并确保铺满屏幕
                    SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, screenWidth, screenHeight, SWP_NOACTIVATE);

                    Debug.WriteLine($"重新定位窗口成功，尺寸：{screenWidth}x{screenHeight}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"重新定位窗口失败：{ex.Message}");
            }
        }

        #region Windows API

        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
            string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Dispose();
                    _gifImage?.Dispose();
                }

                Stop();
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GifWallpaperWindow()
        {
            Dispose(false);
        }

        #endregion
    }
}
