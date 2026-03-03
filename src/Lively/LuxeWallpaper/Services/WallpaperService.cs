using LuxeWallpaper.Models;
using Lively.Core;
using Lively.Models;
using Lively.Models.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace LuxeWallpaper.Services
{
    public class WallpaperService : IDisposable
    {
        private readonly IDesktopCore _desktopCore;
        private readonly List<WallpaperItem> _wallpapers;
        private readonly UserSettings _settings;
        private bool _disposed = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public WallpaperService(UserSettings settings, IDesktopCore desktopCore = null)
        {
            _settings = settings;
            _desktopCore = desktopCore;
            _wallpapers = new List<WallpaperItem>();
            LoadWallpapers();
        }

        public List<WallpaperItem> GetAllWallpapers()
        {
            return _wallpapers.ToList();
        }

        public List<WallpaperItem> GetWallpapersByCategory(string category)
        {
            return _wallpapers.Where(w => w.Category == category).ToList();
        }

        public List<WallpaperItem> SearchWallpapers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return _wallpapers.ToList();

            keyword = keyword.ToLower();
            return _wallpapers.Where(w =>
                (w.Title?.ToLower().Contains(keyword) ?? false) ||
                (w.Tags?.ToLower().Contains(keyword) ?? false) ||
                (w.Category?.ToLower().Contains(keyword) ?? false)
            ).ToList();
        }

        public async Task SetWallpaperAsync(WallpaperItem wallpaper, MonitorInfo monitor = null)
        {
            if (wallpaper == null || !File.Exists(wallpaper.FilePath))
                return;

            try
            {
                if (_desktopCore != null)
                {
                    // 使用 Lively 的核心服务设置壁纸
                    var libraryModel = ConvertToLibraryModel(wallpaper);
                    var displayMonitor = monitor != null ? ConvertToDisplayMonitor(monitor) : null;
                    await _desktopCore.SetWallpaperAsync(libraryModel, displayMonitor);
                }
                else
                {
                    // 检查是否为动态壁纸
                    if (wallpaper.Type == LuxeWallpaper.Models.WallpaperType.Dynamic || 
                        wallpaper.Type == LuxeWallpaper.Models.WallpaperType.Live ||
                        IsDynamicWallpaper(wallpaper.FilePath))
                    {
                        // 尝试使用Lively.exe来设置动态壁纸
                        await TrySetDynamicWallpaperWithLively(wallpaper);
                    }
                    else
                    {
                        // 回退到传统方法设置静态壁纸
                        ApplyWallpaper(wallpaper.FilePath);
                    }
                }

                wallpaper.LastUsedTime = DateTime.Now;

                if (monitor != null)
                {
                    monitor.CurrentWallpaper = wallpaper;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置壁纸失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsDynamicWallpaper(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            // 常见的动态壁纸格式
            string[] dynamicExtensions = { ".mp4", ".webm", ".avi", ".mkv", ".gif", ".html", ".htm" };
            return dynamicExtensions.Contains(extension);
        }

        private async Task TrySetDynamicWallpaperWithLively(WallpaperItem wallpaper)
        {
            try
            {
                // 尝试找到Lively.exe
                string livelyExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lively.exe");
                if (!File.Exists(livelyExePath))
                {
                    // 尝试在上级目录查找
                    livelyExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Lively.exe");
                    if (!File.Exists(livelyExePath))
                    {
                        throw new Exception("找不到Lively.exe，无法设置动态壁纸");
                    }
                }

                // 构建命令行参数
                string args = $"--set-wallpaper \"{wallpaper.FilePath}\"";
                
                // 启动Lively.exe来设置动态壁纸
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = livelyExePath,
                        Arguments = args,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Lively.exe设置动态壁纸失败，退出代码：{process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                // 如果Lively.exe设置失败，尝试使用传统方法作为后备
                MessageBox.Show($"设置动态壁纸失败：{ex.Message}\n将尝试使用静态壁纸作为后备", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                ApplyWallpaper(wallpaper.FilePath);
            }
        }

        private void ApplyWallpaper(string filePath)
        {
            const int SPI_SETDESKWALLPAPER = 20;
            const int SPIF_UPDATEINIFILE = 0x01;
            const int SPIF_SENDCHANGE = 0x02;

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        public async Task SetLockScreenAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null || !File.Exists(wallpaper.FilePath))
                return;

            try
            {
                await Task.Run(() =>
                {
                    // 使用 Windows 10/11 推荐的 Personalization 设置路径
                    using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP"))
                    {
                        if (key != null)
                        {
                            key.SetValue("LockScreenImagePath", wallpaper.FilePath, RegistryValueKind.String);
                            key.SetValue("LockScreenImageUrl", wallpaper.FilePath, RegistryValueKind.String);
                        }
                    }

                    // 尝试使用 Windows Runtime API 设置锁屏壁纸
                    try
                    {
                        var lockScreenType = Type.GetType("Windows.System.UserProfile.LockScreen, Windows.System.UserProfile, ContentType=WindowsRuntime");
                        if (lockScreenType != null)
                        {
                            var setImageFileAsync = lockScreenType.GetMethod("SetImageFileAsync");
                            if (setImageFileAsync != null)
                            {
                                var storageFileType = Type.GetType("Windows.Storage.StorageFile, Windows.Storage, ContentType=WindowsRuntime");
                                if (storageFileType != null)
                                {
                                    var getFileFromPathAsync = storageFileType.GetMethod("GetFileFromPathAsync");
                                    if (getFileFromPathAsync != null)
                                    {
                                        var fileTask = getFileFromPathAsync.Invoke(null, new object[] { wallpaper.FilePath });
                                        if (fileTask != null)
                                        {
                                            var asTaskMethod = fileTask.GetType().GetMethod("AsTask");
                                            if (asTaskMethod != null)
                                            {
                                                var task = asTaskMethod.Invoke(fileTask, null);
                                                if (task is System.Threading.Tasks.Task t)
                                                {
                                                    t.Wait();
                                                    var resultProperty = task.GetType().GetProperty("Result");
                                                    var file = resultProperty?.GetValue(task);
                                                    if (file != null)
                                                    {
                                                        var setTask = setImageFileAsync.Invoke(null, new object[] { file });
                                                        if (setTask != null)
                                                        {
                                                            var setTaskAsTask = setTask.GetType().GetMethod("AsTask");
                                                            if (setTaskAsTask != null)
                                                            {
                                                                var setTaskResult = setTaskAsTask.Invoke(setTask, null);
                                                                if (setTaskResult is System.Threading.Tasks.Task st)
                                                                {
                                                                    st.Wait();
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Windows Runtime API 调用失败，注册表方式已足够
                    }
                });

                wallpaper.LastUsedTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置锁屏壁纸失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddWallpaper(WallpaperItem wallpaper)
        {
            _wallpapers.Add(wallpaper);
        }

        public void RemoveWallpaper(WallpaperItem wallpaper)
        {
            _wallpapers.Remove(wallpaper);
        }

        public async Task DeleteWallpaperAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null || !File.Exists(wallpaper.FilePath))
                return;

            await Task.Run(() =>
            {
                try
                {
                    // 删除文件
                    File.Delete(wallpaper.FilePath);

                    // 如果有缩略图，也删除缩略图
                    if (!string.IsNullOrEmpty(wallpaper.ThumbnailPath) && File.Exists(wallpaper.ThumbnailPath))
                    {
                        File.Delete(wallpaper.ThumbnailPath);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"删除文件失败：{ex.Message}");
                }
            });
        }

        public async Task UninstallWallpaperAsync(WallpaperItem wallpaper)
        {
            if (wallpaper == null)
                return;

            await Task.Run(() =>
            {
                try
                {
                    // 注销壁纸
                }
                catch (Exception ex)
                {
                    throw new Exception($"注销壁纸失败：{ex.Message}");
                }
            });
        }

        public async Task<bool> DownloadWallpaperAsync(string url, string fileName)
        {
            try
            {
                if (!Directory.Exists(_settings.DownloadPath))
                {
                    Directory.CreateDirectory(_settings.DownloadPath);
                }

                string filePath = Path.Combine(_settings.DownloadPath, fileName);

                using (var client = new System.Net.Http.HttpClient())
                {
                    var data = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(filePath, data);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadWallpapers()
        {
            // 加载示例壁纸数据
            var sampleWallpapers = new List<WallpaperItem>
            {
                new WallpaperItem
                {
                    Title = "动漫少女线稿",
                    Category = "动漫",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "黑白",
                    Tags = "动漫,少女,线稿",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "海边日落",
                    Category = "风景",
                    Resolution = "2560x1440",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "暖色",
                    Tags = "风景,海边,日落",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "雨天城市",
                    Category = "风景",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Dynamic,
                    ColorScheme = "冷色",
                    Tags = "风景,雨天,城市",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "银发少年",
                    Category = "动漫",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "冷色",
                    Tags = "动漫,少年,银发",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "绿色山丘",
                    Category = "风景",
                    Resolution = "3840x2160",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "绿色",
                    Tags = "风景,山丘,自然",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "像素游戏",
                    Category = "游戏",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Live,
                    ColorScheme = "彩色",
                    Tags = "游戏,像素,复古",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "红色信仰",
                    Category = "其他",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "红色",
                    Tags = "红色,信仰,中国",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "高速公路",
                    Category = "风景",
                    Resolution = "2560x1440",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "暖色",
                    Tags = "风景,公路,旅行",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "紫发少女",
                    Category = "动漫",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "紫色",
                    Tags = "动漫,少女,紫发",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "少女素描",
                    Category = "动漫",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "黑白",
                    Tags = "动漫,少女,素描",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "蓝天白云",
                    Category = "风景",
                    Resolution = "3840x2160",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "蓝色",
                    Tags = "风景,天空,白云",
                    IsDownloaded = true
                },
                new WallpaperItem
                {
                    Title = "可爱猫咪",
                    Category = "动物",
                    Resolution = "1920x1080",
                    Type = LuxeWallpaper.Models.WallpaperType.Static,
                    ColorScheme = "暖色",
                    Tags = "动物,猫咪,可爱",
                    IsDownloaded = true
                }
            };

            _wallpapers.AddRange(sampleWallpapers);
        }

        public List<string> GetAllCategories()
        {
            return _wallpapers.Select(w => w.Category).Distinct().ToList();
        }

        public List<string> GetAllResolutions()
        {
            return _wallpapers.Select(w => w.Resolution).Distinct().ToList();
        }

        public List<string> GetAllColorSchemes()
        {
            return _wallpapers.Select(w => w.ColorScheme).Distinct().ToList();
        }

        // 转换方法：将 LuxeWallpaper 的 WallpaperItem 转换为 Lively 的 LibraryModel
        private LibraryModel ConvertToLibraryModel(WallpaperItem wallpaper)
        {
            return new LibraryModel
            {
                Title = wallpaper.Title,
                FilePath = wallpaper.FilePath,
                ThumbnailPath = wallpaper.ThumbnailPath
            };
        }

        // 转换方法：将 LuxeWallpaper 的 MonitorInfo 转换为 Lively 的 DisplayMonitor
        private Lively.Models.DisplayMonitor ConvertToDisplayMonitor(MonitorInfo monitor)
        {
            return new Lively.Models.DisplayMonitor
            {
                DeviceId = monitor.DeviceName,
                DisplayName = monitor.DeviceName,
                IsPrimary = monitor.IsPrimary
            };
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WallpaperService()
        {
            Dispose(false);
        }

        #endregion
    }
}
