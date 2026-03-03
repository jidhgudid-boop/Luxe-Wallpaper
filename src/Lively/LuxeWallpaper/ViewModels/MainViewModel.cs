using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuxeWallpaper.Models;
using LuxeWallpaper.Services;
using Lively.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LuxeWallpaper.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WallpaperService _wallpaperService;
        private readonly SettingsService _settingsService;
        private readonly MonitorService _monitorService;
        private readonly AutoChangeService _autoChangeService;
        private readonly PowerSavingService _powerSavingService;
        private readonly LocalImportService _localImportService;
        private readonly ResponsiveLayoutManager _layoutManager;
        private readonly UserSettings _settings;
        private readonly IDesktopCore _desktopCore;

        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _wallpapers;

        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _filteredWallpapers;

        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _pagedWallpapers;

        [ObservableProperty]
        private WallpaperItem _selectedWallpaper;

        [ObservableProperty]
        private string _searchKeyword;

        [ObservableProperty]
        private string _selectedCategory;

        [ObservableProperty]
        private string _selectedResolution;

        [ObservableProperty]
        private string _selectedColorScheme;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _itemsPerPage = 8; // 默认2×4布局，每页8张

        [ObservableProperty]
        private ObservableCollection<string> _categories;

        [ObservableProperty]
        private ObservableCollection<string> _resolutions;

        [ObservableProperty]
        private ObservableCollection<string> _colorSchemes;

        [ObservableProperty]
        private ObservableCollection<MonitorInfo> _monitors;

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private bool _isDarkTheme = true;

        [ObservableProperty]
        private bool _isAutoChangeEnabled;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private ObservableCollection<PageNumberItem> _pageNumbers;

        [ObservableProperty]
        private bool _isPowerSavingEnabled;

        [ObservableProperty]
        private bool _isPowerSavingActive;

        [ObservableProperty]
        private LayoutMode _currentLayoutMode;

        [ObservableProperty]
        private int _gridColumns = 4;

        [ObservableProperty]
        private int _gridRows = 2;

        [ObservableProperty]
        private double _cardWidth = 320;

        [ObservableProperty]
        private double _cardHeight = 180;

        [ObservableProperty]
        private double _cardCornerRadius = 12;

        [ObservableProperty]
        private double _cardMargin = 10;

        [ObservableProperty]
        private bool _isDetailPanelVisible = false;

        [ObservableProperty]
        private double _detailPanelWidth = 350;

        [ObservableProperty]
        private double _gridOpacity = 1.0;

        [ObservableProperty]
        private string _importProgress;

        [ObservableProperty]
        private bool _isImporting;

        [ObservableProperty]
        private bool _showStoredOnly;

        [ObservableProperty]
        private bool _isSystemStaticEnabled;

        [ObservableProperty]
        private bool _isLockScreenStaticEnabled;

        public MainViewModel(IDesktopCore desktopCore = null)
        {
            _desktopCore = desktopCore;
            _settingsService = new SettingsService();
            _settings = _settingsService.GetSettings();
            _wallpaperService = new WallpaperService(_settings, _desktopCore);
            _monitorService = new MonitorService();
            _autoChangeService = new AutoChangeService(_wallpaperService, _settings);
            _powerSavingService = PowerSavingServiceInstance.Instance;
            _localImportService = new LocalImportService(_wallpaperService, _settings);
            _layoutManager = new ResponsiveLayoutManager();

            // 订阅布局管理器事件
            _layoutManager.LayoutChanged += OnLayoutChanged;
            _layoutManager.PageChanged += OnPageChanged;
            _layoutManager.ItemsRefreshed += OnItemsRefreshed;

            // 同步初始布局状态
            GridColumns = _layoutManager.CurrentLayout.Columns;
            GridRows = _layoutManager.CurrentLayout.Rows;

            Wallpapers = new ObservableCollection<WallpaperItem>();
            FilteredWallpapers = new ObservableCollection<WallpaperItem>();
            PagedWallpapers = new ObservableCollection<WallpaperItem>();
            Categories = new ObservableCollection<string>();

            // 监听壁纸集合变化，自动更新分页
            Wallpapers.CollectionChanged += (s, e) =>
            {
                UpdatePaging();
            };
            Resolutions = new ObservableCollection<string>();
            ColorSchemes = new ObservableCollection<string>();
            Monitors = new ObservableCollection<MonitorInfo>();
            PageNumbers = new ObservableCollection<PageNumberItem>();

            IsDarkTheme = _settings.IsDarkTheme;
            IsAutoChangeEnabled = _settings.AutoChangeWallpaper;
            IsPowerSavingEnabled = _settings.PowerSavingMode;
            CurrentLayoutMode = _settings.LayoutMode;

            _powerSavingService.PowerSavingStateChanged += OnPowerSavingStateChanged;
            _localImportService.ImportProgressChanged += OnImportProgressChanged;

            if (IsPowerSavingEnabled)
            {
                _powerSavingService.Start(true);
            }

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                var wallpapers = _wallpaperService.GetAllWallpapers();
                var categories = _wallpaperService.GetAllCategories();
                var resolutions = _wallpaperService.GetAllResolutions();
                var colorSchemes = _wallpaperService.GetAllColorSchemes();
                var monitors = _monitorService.GetAllMonitors();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Wallpapers.Clear();
                    foreach (var wallpaper in wallpapers)
                    {
                        Wallpapers.Add(wallpaper);
                    }

                    Categories.Clear();
                    Categories.Add("全部");
                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }

                    Resolutions.Clear();
                    Resolutions.Add("全部");
                    foreach (var resolution in resolutions)
                    {
                        Resolutions.Add(resolution);
                    }

                    ColorSchemes.Clear();
                    ColorSchemes.Add("全部");
                    foreach (var colorScheme in colorSchemes)
                    {
                        ColorSchemes.Add(colorScheme);
                    }

                    Monitors.Clear();
                    foreach (var monitor in monitors)
                    {
                        Monitors.Add(monitor);
                    }

                    ApplyFilters();
                    UpdatePaging();
                });
            });
        }

        [RelayCommand]
        public void ApplyFilters()
        {
            var filtered = Wallpapers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var keyword = SearchKeyword.ToLower();
                filtered = filtered.Where(w =>
                    (w.Title?.ToLower().Contains(keyword) ?? false) ||
                    (w.Tags?.ToLower().Contains(keyword) ?? false));
            }

            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "全部")
            {
                filtered = filtered.Where(w => w.Category == SelectedCategory);
            }

            if (!string.IsNullOrEmpty(SelectedResolution) && SelectedResolution != "全部")
            {
                filtered = filtered.Where(w => w.Resolution == SelectedResolution);
            }

            if (!string.IsNullOrEmpty(SelectedColorScheme) && SelectedColorScheme != "全部")
            {
                filtered = filtered.Where(w => w.ColorScheme == SelectedColorScheme);
            }

            // 仅展示已存储的壁纸（本地上传或已下载）
            if (ShowStoredOnly)
            {
                filtered = filtered.Where(w => w.IsLocal || w.IsDownloaded);
            }

            var filteredList = filtered.ToList();
            TotalPages = Math.Max(1, (int)Math.Ceiling(filteredList.Count / (double)ItemsPerPage));

            if (CurrentPage > TotalPages)
                CurrentPage = 1;

            var pagedList = filteredList
                .Skip((CurrentPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage)
                .ToList();

            FilteredWallpapers.Clear();
            PagedWallpapers.Clear();
            foreach (var wallpaper in pagedList)
            {
                FilteredWallpapers.Add(wallpaper);
                PagedWallpapers.Add(wallpaper);
            }

            UpdatePageNumbers();
        }

        private void UpdatePageNumbers()
        {
            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++)
            {
                PageNumbers.Add(new PageNumberItem
                {
                    PageNumber = i,
                    IsCurrentPage = i == CurrentPage
                });
            }
        }

        [RelayCommand]
        private async Task SetWallpaperAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;

            if (IsPowerSavingActive)
            {
                MessageBox.Show("节能模式已激活，无法设置壁纸。请退出游戏或办公应用后再试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await _wallpaperService.SetWallpaperAsync(wallpaper);
            SelectedWallpaper = wallpaper;

            if (_settings.ShowNotification)
            {
                MessageBox.Show($"壁纸已设置为：{wallpaper.Title}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePaging();
            }
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePaging();
            }
        }

        [RelayCommand]
        private void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
                UpdatePaging();
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
            _settings.IsDarkTheme = IsDarkTheme;
            _settingsService.SaveSettings(_settings);

            Application.Current.Dispatcher.Invoke(() =>
            {
                var themeUri = IsDarkTheme
                    ? new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative)
                    : new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative);

                var dict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null &&
                        (d.Source.OriginalString.Contains("DarkTheme") || d.Source.OriginalString.Contains("LightTheme")));

                if (dict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(dict);
                }

                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
            });
        }

        [RelayCommand]
        private void ToggleAutoChange()
        {
            IsAutoChangeEnabled = !IsAutoChangeEnabled;
            _settings.AutoChangeWallpaper = IsAutoChangeEnabled;
            _settingsService.SaveSettings(_settings);

            if (IsAutoChangeEnabled)
            {
                _autoChangeService.Start();
            }
            else
            {
                _autoChangeService.Stop();
            }
        }

        [RelayCommand]
        private void TogglePlayPause()
        {
            IsPlaying = !IsPlaying;
            if (IsPlaying)
            {
                _autoChangeService.Start();
            }
            else
            {
                _autoChangeService.Stop();
            }
        }

        [RelayCommand]
        private void PreviousWallpaper()
        {
            _autoChangeService.PreviousWallpaper();
        }

        [RelayCommand]
        private void NextWallpaper()
        {
            _autoChangeService.NextWallpaper();
        }

        [RelayCommand]
        private void AddToFavorites(WallpaperItem wallpaper)
        {
            if (wallpaper != null)
            {
                wallpaper.IsFavorite = !wallpaper.IsFavorite;
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow();
            settingsWindow.Owner = Application.Current.MainWindow;
            settingsWindow.ShowDialog();
        }

        [RelayCommand]
        private void SelectTab(string index)
        {
            if (int.TryParse(index, out int tabIndex))
            {
                SelectedTabIndex = tabIndex;
            }
        }

        [RelayCommand]
        private void TogglePowerSaving()
        {
            IsPowerSavingEnabled = !IsPowerSavingEnabled;
            _settings.PowerSavingMode = IsPowerSavingEnabled;
            _settingsService.SaveSettings(_settings);

            _powerSavingService.SetPowerSavingEnabled(IsPowerSavingEnabled);
        }

        [RelayCommand]
        private void ToggleSystemStatic()
        {
            IsSystemStaticEnabled = !IsSystemStaticEnabled;
        }

        [RelayCommand]
        private void ToggleLockScreenStatic()
        {
            IsLockScreenStaticEnabled = !IsLockScreenStaticEnabled;
        }

        [RelayCommand]
        private async Task SetLockScreenAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;

            if (IsPowerSavingActive)
            {
                MessageBox.Show("节能模式已激活，无法设置壁纸。请退出游戏或办公应用后再试。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await _wallpaperService.SetLockScreenAsync(wallpaper);
            SelectedWallpaper = wallpaper;

            if (_settings.ShowNotification)
            {
                MessageBox.Show($"锁屏壁纸已设置为：{wallpaper.Title}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private async Task PreviewWallpaperAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = wallpaper.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"预览壁纸失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DownloadWallpaperAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null || wallpaper.IsLocal) return;

            try
            {
                // 这里实现下载逻辑，假设 wallpaper.FilePath 是下载 URL
                // 实际项目中需要根据具体 API 实现
                MessageBox.Show($"下载壁纸：{wallpaper.Title}\n（需要实现具体的下载逻辑）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载壁纸失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task FindSimilarAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null || wallpaper.IsLocal) return;

            try
            {
                // 实现找相似功能，可以根据标签、分类等搜索相似壁纸
                MessageBox.Show($"查找与\"{wallpaper.Title}\"相似的壁纸\n（需要实现具体的找相似逻辑）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查找相似壁纸失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task OpenInWebsiteAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null || wallpaper.IsLocal) return;

            try
            {
                // 打开壁纸的官网链接
                var url = wallpaper.SourceUrl ?? "https://example.com";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开官网失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task OpenPublisherAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null || wallpaper.IsLocal) return;

            try
            {
                // 打开发布者详情页面
                var publisherUrl = wallpaper.PublisherUrl ?? "https://example.com/publisher";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = publisherUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开发布者页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ToggleLayoutMode()
        {
            // 淡出动画
            await AnimateGridOpacity(1.0, 0.0, 200);

            // 切换布局
            CurrentLayoutMode = CurrentLayoutMode == LayoutMode.Grid2x4 ? LayoutMode.Grid3x7 : LayoutMode.Grid2x4;
            _settings.LayoutMode = CurrentLayoutMode;
            _settingsService.SaveSettings(_settings);
            UpdateLayoutSettings();

            // 等待一小段时间让布局更新
            await Task.Delay(50);

            // 淡入动画
            await AnimateGridOpacity(0.0, 1.0, 200);
        }

        private async Task AnimateGridOpacity(double from, double to, int durationMs)
        {
            var steps = 20;
            var stepDuration = durationMs / steps;
            var stepValue = (to - from) / steps;

            for (int i = 0; i <= steps; i++)
            {
                GridOpacity = from + (stepValue * i);
                await Task.Delay(stepDuration);
            }

            GridOpacity = to;
        }

        private void UpdateLayoutSettings()
        {
            // 根据当前布局模式更新网格设置
            if (CurrentLayoutMode == LayoutMode.Grid2x4)
            {
                GridColumns = 4;
                GridRows = 2;
            }
            else
            {
                GridColumns = 5;
                GridRows = 3;
            }
            OnPropertyChanged(nameof(GridColumns));
            OnPropertyChanged(nameof(GridRows));
        }

        [RelayCommand]
        private async Task ImportLocalWallpaper()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择壁纸文件",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|视频文件|*.mp4;*.webm;*.avi;*.mkv|所有文件|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                IsImporting = true;
                ImportProgress = "正在导入...";

                await Task.Run(() =>
                {
                    foreach (var fileName in dialog.FileNames)
                    {
                        try
                        {
                            var wallpaper = _localImportService.ImportSingleFile(fileName);
                            if (wallpaper != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Wallpapers.Add(wallpaper);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    }
                });

                IsImporting = false;
                ImportProgress = "导入完成";
                ApplyFilters();
            }
        }

        [RelayCommand]
        private async Task ImportFromFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择壁纸文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                IsImporting = true;
                ImportProgress = "正在扫描文件夹...";

                await Task.Run(() =>
                {
                    try
                    {
                        var wallpapers = _localImportService.ImportFromDirectory(dialog.FolderName, true);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var wallpaper in wallpapers)
                            {
                                Wallpapers.Add(wallpaper);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });

                IsImporting = false;
                ImportProgress = "导入完成";
                ApplyFilters();
            }
        }

        [RelayCommand]
        private void ExportWallpaper(WallpaperItem wallpaper)
        {
            if (wallpaper == null || !File.Exists(wallpaper.FilePath))
            {
                MessageBox.Show("壁纸文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new OpenFolderDialog
            {
                Title = "选择导出位置"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _localImportService.ExportWallpaper(wallpaper, dialog.FolderName);
                    MessageBox.Show("导出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void DeleteWallpaper(WallpaperItem wallpaper)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteWallpaper called, wallpaper={wallpaper?.Title}, IsLocal={wallpaper?.IsLocal}");

            if (wallpaper == null)
            {
                System.Diagnostics.Debug.WriteLine("Wallpaper is null");
                return;
            }

            var result = MessageBox.Show(
                $"确定要从库中移除壁纸 \"{wallpaper.Title}\" 吗？\n\n注意：此操作仅从库中移除，不会删除源文件。",
                "确认移除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            System.Diagnostics.Debug.WriteLine($"Delete result: {result}");

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 仅从库中移除，不删除源文件
                    _localImportService.DeleteWallpaper(wallpaper, false);
                    Wallpapers.Remove(wallpaper);
                    ApplyFilters();
                    MessageBox.Show($"壁纸 \"{wallpaper.Title}\" 已从库中移除", "移除成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"移除壁纸失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void OpenWallpaperFolder(WallpaperItem wallpaper)
        {
            System.Diagnostics.Debug.WriteLine($"OpenWallpaperFolder called, wallpaper={wallpaper?.Title}, FilePath={wallpaper?.FilePath}");

            if (wallpaper == null || !File.Exists(wallpaper.FilePath))
            {
                MessageBox.Show($"壁纸文件不存在\nFilePath: {wallpaper?.FilePath ?? "null"}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var folderPath = Path.GetDirectoryName(wallpaper.FilePath);
                System.Diagnostics.Debug.WriteLine($"Opening folder: {folderPath}");
                // 使用 /select 参数打开文件夹并选中文件
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{wallpaper.FilePath}\"",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件目录失败：{ex.Message}\n\nStackTrace: {ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPowerSavingStateChanged(object sender, bool isActive)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsPowerSavingActive = isActive;
            });
        }

        private void OnImportProgressChanged(object sender, ImportProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ImportProgress = $"正在导入：{e.ProcessedFiles}/{e.TotalFiles} ({e.Progress:F1}%)";
            });
        }

        partial void OnCurrentPageChanged(int value)
        {
            UpdatePageNumbers();
            UpdatePaging();
        }

        private void UpdatePaging()
        {
            // 计算总页数
            TotalPages = (int)Math.Ceiling((double)Wallpapers.Count / ItemsPerPage);
            if (TotalPages < 1) TotalPages = 1;

            // 确保当前页在有效范围内
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            // 获取当前页的壁纸
            var pagedItems = Wallpapers
                .Skip((CurrentPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage)
                .ToList();

            // 更新分页集合
            PagedWallpapers.Clear();
            foreach (var item in pagedItems)
            {
                PagedWallpapers.Add(item);
            }

            UpdatePageNumbers();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilters();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilters();
        }

        partial void OnSelectedResolutionChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilters();
        }

        partial void OnSelectedColorSchemeChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilters();
        }

        partial void OnShowStoredOnlyChanged(bool value)
        {
            CurrentPage = 1;
            ApplyFilters();
        }

        #region 筛选命令

        [ObservableProperty]
        private string _selectedTimeFilter = "昨日热门";

        [ObservableProperty]
        private string _selectedTypeFilter = "种类";

        [RelayCommand]
        private void FilterByTime(string timeFilter)
        {
            SelectedTimeFilter = timeFilter switch
            {
                "Latest" => "最新",
                "Recommended" => "推荐的",
                "Yesterday" => "昨日热门",
                "Last3Days" => "近三天热门",
                "LastWeek" => "上周热门",
                "LastMonth" => "上月热门",
                "Last3Months" => "近3月热门",
                "Last6Months" => "近6月热门",
                "LastYear" => "去年热门",
                _ => "昨日热门"
            };
            CurrentPage = 1;
            ApplyFilters();
        }

        [RelayCommand]
        private void FilterByType(string typeFilter)
        {
            SelectedTypeFilter = typeFilter switch
            {
                "Static" => "静态",
                "Dynamic" => "动态",
                _ => "种类"
            };
            CurrentPage = 1;
            ApplyFilters();
        }

        [RelayCommand]
        private void FilterByCategory(string category)
        {
            SelectedCategory = category;
            CurrentPage = 1;
            ApplyFilters();
        }

        [RelayCommand]
        private void FilterByResolution(string resolution)
        {
            SelectedResolution = resolution;
            CurrentPage = 1;
            ApplyFilters();
        }

        [RelayCommand]
        private void FilterByColor(string color)
        {
            SelectedColorScheme = color;
            CurrentPage = 1;
            ApplyFilters();
        }

        #endregion

        #region 响应式布局管理

        /// <summary>
        /// 切换布局命令
        /// </summary>
        [RelayCommand]
        private void ToggleLayout()
        {
            _layoutManager.ToggleLayout();
        }

        /// <summary>
        /// 设置布局命令
        /// </summary>
        [RelayCommand]
        private void SetLayout(string layoutName)
        {
            var layout = layoutName switch
            {
                "2x4" => LayoutConfig.Layout2x4,
                "3x7" => LayoutConfig.Layout3x7,
                _ => LayoutConfig.Layout2x4
            };
            _layoutManager.SetLayout(layout);
        }

        /// <summary>
        /// 更新布局尺寸
        /// </summary>
        public void UpdateLayoutDimensions(double availableWidth, double availableHeight)
        {
            _layoutManager.UpdateDimensions(availableWidth, availableHeight, CardMargin);

            // 更新卡片尺寸属性
            var dimensions = _layoutManager.CurrentCardDimensions;
            CardWidth = dimensions.Width;
            CardHeight = dimensions.Height;
            CardCornerRadius = dimensions.CornerRadius;
        }

        /// <summary>
        /// 布局改变事件处理
        /// </summary>
        private void OnLayoutChanged(object sender, LayoutChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                GridColumns = e.NewLayout.Columns;
                GridRows = e.NewLayout.Rows;

                // 更新每页显示数量
                ItemsPerPage = e.NewLayout.Columns * e.NewLayout.Rows;

                // 重置到第一页
                CurrentPage = 1;

                // 刷新分页
                ApplyFilters();

                // 触发动画
                _ = AnimateLayoutChange();
            });
        }

        /// <summary>
        /// 页面改变事件处理
        /// </summary>
        private void OnPageChanged(object sender, PageChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentPage = e.NewPage;
                UpdatePageNumbers();
            });
        }

        /// <summary>
        /// 项目刷新事件处理
        /// </summary>
        private void OnItemsRefreshed(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 同步分页数据
                PagedWallpapers.Clear();
                foreach (var item in _layoutManager.PagedItems)
                {
                    PagedWallpapers.Add(item);
                }

                // 同步页码
                UpdatePageNumbersFromManager();
            });
        }

        /// <summary>
        /// 从布局管理器更新页码
        /// </summary>
        private void UpdatePageNumbersFromManager()
        {
            PageNumbers.Clear();
            for (int i = 1; i <= _layoutManager.TotalPages; i++)
            {
                PageNumbers.Add(new PageNumberItem
                {
                    PageNumber = i,
                    IsCurrentPage = i == _layoutManager.CurrentPage
                });
            }
        }

        /// <summary>
        /// 布局切换动画
        /// </summary>
        private async Task AnimateLayoutChange()
        {
            // 淡出
            GridOpacity = 0;
            await Task.Delay(150);

            // 更新分页
            _layoutManager.SetItems(FilteredWallpapers);

            // 淡入
            GridOpacity = 1;
        }

        /// <summary>
        /// 删除壁纸并重排
        /// </summary>
        [RelayCommand]
        private void DeleteWallpaperAndReflow(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;

            // 先关闭详情面板
            if (SelectedWallpaper == wallpaper)
            {
                IsDetailPanelVisible = false;
                SelectedWallpaper = null;
            }

            // 从集合中移除
            _layoutManager.RemoveItem(wallpaper);
            Wallpapers.Remove(wallpaper);

            // 重新应用过滤器并刷新分页
            ApplyFilters();
        }

        /// <summary>
        /// 选择壁纸并显示详情
        /// </summary>
        [RelayCommand]
        private void SelectWallpaper(WallpaperItem wallpaper)
        {
            SelectedWallpaper = wallpaper;

            // 解析 Tags 字符串到 TagsList
            if (wallpaper != null && !string.IsNullOrEmpty(wallpaper.Tags))
            {
                wallpaper.TagsList.Clear();
                var tags = wallpaper.Tags.Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags)
                {
                    wallpaper.TagsList.Add(tag.Trim());
                }
            }

            IsDetailPanelVisible = true;
        }

        /// <summary>
        /// 关闭详情面板
        /// </summary>
        [RelayCommand]
        private void CloseDetailPanel()
        {
            IsDetailPanelVisible = false;
            SelectedWallpaper = null;
        }

        /// <summary>
        /// 刷新分页（供布局管理器调用）
        /// </summary>
        public void RefreshLayoutPagination()
        {
            _layoutManager.SetItems(FilteredWallpapers);
        }

        #endregion
    }
}
