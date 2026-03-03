using LuxeWallpaper.Models;
using LuxeWallpaper.Services;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LuxeWallpaper.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly PowerSavingService _powerSavingService;
        private readonly UserSettings _settings;
        private int _currentPage = 0;

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            _powerSavingService = PowerSavingServiceInstance.Instance;
            _settings = _settingsService.GetSettings();
            LoadSettings();
            
            // 订阅下拉框选择改变事件
            if (PowerThreshold != null)
                PowerThreshold.SelectionChanged += PowerThreshold_SelectionChanged;
            
            // 订阅节能模式开关事件
            if (PowerSavingMode != null)
                PowerSavingMode.Checked += PowerSavingMode_Changed;
            if (PowerSavingMode != null)
                PowerSavingMode.Unchecked += PowerSavingMode_Changed;
        }

        private void PowerSavingMode_Changed(object sender, RoutedEventArgs e)
        {
            // 实时通知PowerSavingService状态改变
            if (_powerSavingService != null)
            {
                bool isEnabled = PowerSavingMode.IsChecked ?? false;
                _powerSavingService.SetPowerSavingEnabled(isEnabled);
            }
            // 保存设置到文件
            SaveSettings();
        }

        private void PowerThreshold_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveSettings();
        }

        private void LoadSettings()
        {
            if (PowerSavingMode != null)
            {
                // 从设置文件加载节能模式状态
                PowerSavingMode.IsChecked = _settings.PowerSavingMode;
                // 同步到PowerSavingService
                if (_powerSavingService != null)
                {
                    _powerSavingService.SetPowerSavingEnabled(_settings.PowerSavingMode);
                }
            }
            if (PowerThreshold != null)
            {
                // 根据PowerThreshold值设置SelectedIndex (40=0, 45=1, ..., 100=12)
                int index = (_settings.PowerThreshold - 40) / 5;
                if (index >= 0 && index <= 12)
                    PowerThreshold.SelectedIndex = index;
                // 同步到PowerSavingService
                if (_powerSavingService != null)
                {
                    _powerSavingService.SetPowerThreshold(_settings.PowerThreshold);
                }
            }
            if (DownloadPath != null)
                DownloadPath.Text = _settings.DownloadPath;
            if (AutoStart != null)
                AutoStart.IsChecked = _settings.AutoStart;
        }

        private void SaveSettings()
        {
            if (PowerSavingMode != null)
                _settings.PowerSavingMode = PowerSavingMode.IsChecked ?? false;
            
            // 修复：正确获取ComboBox选中值
            if (PowerThreshold != null && PowerThreshold.SelectedIndex >= 0)
            {
                int threshold = 40 + (PowerThreshold.SelectedIndex * 5);
                _settings.PowerThreshold = threshold;
            }
            
            if (DownloadPath != null)
                _settings.DownloadPath = DownloadPath.Text;
            if (AutoStart != null)
                _settings.AutoStart = AutoStart.IsChecked ?? false;

            _settingsService.SaveSettings(_settings);
            SetAutoStart(_settings.AutoStart);
        }

        private void SetAutoStart(bool autoStart)
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                var appName = "LuxeWallpaper";
                var appPath = Environment.ProcessPath;

                if (autoStart && appPath != null)
                {
                    key?.SetValue(appName, appPath);
                }
                else
                {
                    key?.DeleteValue(appName, false);
                }
            }
            catch (Exception)
            {
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            base.OnClosing(e);
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                var tagValue = button.Tag.ToString();
                if (int.TryParse(tagValue, out int pageIndex))
                {
                    SwitchPage(pageIndex);
                }
            }
        }

        private void SwitchPage(int pageIndex)
        {
            _currentPage = pageIndex;

            for (int i = 0; i <= 7; i++)
            {
                var page = FindName($"Page{i}") as UIElement;
                if (page != null)
                {
                    page.Visibility = i == pageIndex ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            UpdateNavButtons();
        }

        private void UpdateNavButtons()
        {
            var navButtons = new Button[] { NavPower, NavEngine, NavWallpaper, NavDownload, NavUpdate, NavStartup, NavAbout, NavExit };
            
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] != null)
                {
                    navButtons[i].Style = (Style)FindResource(i == _currentPage ? "ActiveNavItemStyle" : "NavItemStyle");
                }
            }
        }

        private void BrowseDownloadPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                if (DownloadPath != null)
                {
                    DownloadPath.Text = dialog.FolderName;
                }
            }
        }

        private void RenderPerformance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Reset all buttons to inactive style
                RenderLow.Style = (Style)FindResource("RenderPerfButtonStyle");
                RenderMedium.Style = (Style)FindResource("RenderPerfButtonStyle");
                RenderHigh.Style = (Style)FindResource("RenderPerfButtonStyle");
                
                // Set clicked button to active style
                button.Style = (Style)FindResource("RenderPerfButtonActiveStyle");
                
                // Save the selection
                SaveSettings();
            }
        }

        private void FadeInOut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string tag = button.Tag?.ToString() ?? "";
                
                if (tag == "Enable")
                {
                    FadeInOutEnable.Style = (Style)FindResource("RenderPerfButtonActiveStyle");
                    FadeInOutDisable.Style = (Style)FindResource("RenderPerfButtonStyle");
                }
                else
                {
                    FadeInOutEnable.Style = (Style)FindResource("RenderPerfButtonStyle");
                    FadeInOutDisable.Style = (Style)FindResource("RenderPerfButtonActiveStyle");
                }
                
                SaveSettings();
            }
        }
    }
}
