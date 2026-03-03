using LuxeWallpaper.Models;
using LuxeWallpaper.Services;
using LuxeWallpaper.ViewModels;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace LuxeWallpaper.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly SettingsService _settingsService;
        private readonly UserSettings _settings;
        private Forms.NotifyIcon _notifyIcon;
        private Forms.ContextMenuStrip _trayMenu;
        private bool _isMinimizedToTray = false;

        public MainWindow()
        {
            InitializeComponent();
            var app = (App)Application.Current;
            _viewModel = new MainViewModel(app.DesktopCore);
            DataContext = _viewModel;

            _settingsService = new SettingsService();
            _settings = _settingsService.GetSettings();

            // 设置窗口图标，确保任务栏显示图标
            // 使用系统图标作为应用程序图标
            using (var iconStream = new System.IO.MemoryStream())
            {
                System.Drawing.SystemIcons.Application.Save(iconStream);
                iconStream.Position = 0;
                this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconStream);
            }

            InitializeTrayIcon();

            // 监听窗口状态变化
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;

            // 初始化页面状态
            Loaded += (s, e) =>
            {
                // 默认显示壁纸中心，隐藏搜索筛选区域
            };
        }

        /// <summary>
        /// 右键菜单删除壁纸事件处理
        /// </summary>
        private void DeleteWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is WallpaperItem wallpaper)
            {
                _viewModel.DeleteWallpaperCommand.Execute(wallpaper);
            }
        }

        /// <summary>
        /// 右键菜单打开文件目录事件处理
        /// </summary>
        private void OpenWallpaperFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is WallpaperItem wallpaper)
            {
                try
                {
                    if (string.IsNullOrEmpty(wallpaper.FilePath) || !File.Exists(wallpaper.FilePath))
                    {
                        MessageBox.Show("壁纸文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var folderPath = Path.GetDirectoryName(wallpaper.FilePath);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{wallpaper.FilePath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件目录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitializeTrayIcon()
        {
            // 创建托盘图标
            _notifyIcon = new Forms.NotifyIcon
            {
                Text = "Luxe Wallpaper",
                Visible = false,
                Icon = LoadTrayIcon()
            };

            // 创建右键菜单
            _trayMenu = new Forms.ContextMenuStrip();

            var showItem = new Forms.ToolStripMenuItem("显示主窗口", null, (s, e) => ShowMainWindow());
            showItem.Font = new Font(showItem.Font, System.Drawing.FontStyle.Bold);
            _trayMenu.Items.Add(showItem);

            _trayMenu.Items.Add(new Forms.ToolStripSeparator());

            var startWallpaperItem = new Forms.ToolStripMenuItem("开启壁纸", null, (s, e) => StartWallpaper());
            _trayMenu.Items.Add(startWallpaperItem);

            var settingsItem = new Forms.ToolStripMenuItem("设置", null, (s, e) => OpenSettings());
            _trayMenu.Items.Add(settingsItem);

            _trayMenu.Items.Add(new Forms.ToolStripSeparator());

            var exitItem = new Forms.ToolStripMenuItem("退出程序", null, (s, e) => ExitApplication());
            _trayMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = _trayMenu;

            // 双击托盘图标显示主窗口
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private Icon LoadTrayIcon()
        {
            try
            {
                // 尝试从应用程序资源加载图标
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                // 使用系统默认图标
                return System.Drawing.SystemIcons.Application;
            }
            catch
            {
                return System.Drawing.SystemIcons.Application;
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // 不再自动隐藏窗口到托盘，保持任务栏图标可见
            // 只在最小化时显示托盘图标，但不隐藏窗口
            if (WindowState == WindowState.Minimized)
            {
                _notifyIcon.Visible = true;
            }
            else
            {
                _notifyIcon.Visible = false;
            }
        }

        private void MinimizeToTray()
        {
            _isMinimizedToTray = true;
            Hide();
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(2000, "Luxe Wallpaper", "程序已最小化到系统托盘", Forms.ToolTipIcon.Info);
        }

        private void ShowMainWindow()
        {
            _isMinimizedToTray = false;
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _notifyIcon.Visible = false;
        }

        private void StartWallpaper()
        {
            // 调用 ViewModel 中的方法来开启壁纸
            // 这里可以根据实际需求实现
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Show();
            settingsWindow.Activate();
        }

        private void ExitApplication()
        {
            _notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理托盘图标
            _notifyIcon?.Dispose();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleWindowState();
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // 始终最小化到任务栏，不隐藏窗口
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果启用了最小化到托盘，点击关闭按钮时最小化到托盘而不是退出
            if (_settings.MinimizeToTray)
            {
                MinimizeToTray();
            }
            else
            {
                Close();
            }
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void MyLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到"我的库"标签页
            _viewModel.SelectedTabIndex = 0;
            UpdateTabButtonStyles(sender as Button);
            ShowTabContent(0);
        }

        private void WallpaperCenterButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到"壁纸中心"标签页
            _viewModel.SelectedTabIndex = 1;
            UpdateTabButtonStyles(sender as Button);
            ShowTabContent(1);
        }

        private void CreativeWorkshopButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到"创意工坊"标签页
            _viewModel.SelectedTabIndex = 2;
            UpdateTabButtonStyles(sender as Button);
            ShowTabContent(2);
        }

        private void ImportLocalWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开导入本地壁纸弹窗
            var dialog = new ImportWallpaperDialog((wallpaper) =>
            {
                // 导入完成后的回调
                if (wallpaper != null)
                {
                    _viewModel.Wallpapers.Add(wallpaper);
                    _viewModel.ApplyFilters();
                }
            });
            dialog.ShowDialog();
        }

        private void ShowTabContent(int tabIndex)
        {
            // 隐藏所有面板
            MyLibraryPanel.Visibility = Visibility.Collapsed;
            WallpaperCenterPanel.Visibility = Visibility.Collapsed;
            CreativeWorkshopPanel.Visibility = Visibility.Collapsed;

            // 显示选中的面板
            switch (tabIndex)
            {
                case 0:
                    MyLibraryPanel.Visibility = Visibility.Visible;
                    SearchFilterPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    WallpaperCenterPanel.Visibility = Visibility.Visible;
                    SearchFilterPanel.Visibility = Visibility.Visible;
                    break;
                case 2:
                    CreativeWorkshopPanel.Visibility = Visibility.Visible;
                    SearchFilterPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void UpdateTabButtonStyles(Button activeButton)
        {
            // 获取按钮所在的 StackPanel
            if (activeButton?.Parent is StackPanel stackPanel)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is Button button)
                    {
                        if (button == activeButton)
                        {
                            button.Style = (Style)FindResource("ActiveTabButton");
                        }
                        else
                        {
                            button.Style = (Style)FindResource("TabButton");
                        }
                    }
                }
            }
        }

        #region 定时切换配置面板

        private void ShowAutoChangeConfigPanel()
        {
            if (AutoChangeConfigOverlay != null)
            {
                AutoChangeConfigOverlay.Visibility = Visibility.Visible;

                // 添加淡入动画
                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                AutoChangeConfigOverlay.BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private void HideAutoChangeConfigPanel()
        {
            if (AutoChangeConfigOverlay != null)
            {
                // 添加淡出动画
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(150)
                };
                fadeOut.Completed += (s, e) =>
                {
                    AutoChangeConfigOverlay.Visibility = Visibility.Collapsed;
                };
                AutoChangeConfigOverlay.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void AutoChangeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAutoChangeConfigPanel();
        }

        private void CloseAutoChangeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            HideAutoChangeConfigPanel();
        }

        private void SaveAutoChangeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存定时切换配置
            HideAutoChangeConfigPanel();
        }

        private void CancelAutoChangeConfigButton_Click(object sender, RoutedEventArgs e)
        {
            HideAutoChangeConfigPanel();
        }

        #endregion

        private void MainContent_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 点击主内容区域时关闭详情面板
            _viewModel.CloseDetailPanelCommand.Execute(null);
        }
    }
}