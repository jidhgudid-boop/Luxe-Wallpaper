using LuxeWallpaper.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LuxeWallpaper.Services
{
    public class LocalImportService
    {
        private readonly WallpaperService _wallpaperService;
        private readonly UserSettings _settings;
        private readonly string[] _supportedImageFormats = new[]
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff"
        };

        private readonly string[] _supportedVideoFormats = new[]
        {
            ".mp4", ".webm", ".avi", ".mkv", ".mov", ".wmv"
        };

        public event EventHandler<ImportProgressEventArgs> ImportProgressChanged;

        public LocalImportService(WallpaperService wallpaperService, UserSettings settings)
        {
            _wallpaperService = wallpaperService;
            _settings = settings;
        }

        public List<WallpaperItem> ImportFromDirectory(string directoryPath, bool includeSubdirectories = true)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            var wallpapers = new List<WallpaperItem>();
            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var allFiles = Directory.GetFiles(directoryPath, "*.*", searchOption)
                .Where(file => IsSupportedFormat(file))
                .ToList();

            int totalFiles = allFiles.Count;
            int processedFiles = 0;

            // 确保存储目录存在
            if (!Directory.Exists(_settings.DownloadPath))
            {
                Directory.CreateDirectory(_settings.DownloadPath);
            }

            foreach (var filePath in allFiles)
            {
                try
                {
                    // 直接使用原始文件路径
                    var wallpaper = CreateWallpaperItem(filePath);
                    if (wallpaper != null)
                    {
                        wallpapers.Add(wallpaper);
                        _wallpaperService.AddWallpaper(wallpaper);
                    }

                    processedFiles++;
                    double progress = (double)processedFiles / totalFiles * 100;
                    ImportProgressChanged?.Invoke(this, new ImportProgressEventArgs
                    {
                        CurrentFile = filePath,
                        ProcessedFiles = processedFiles,
                        TotalFiles = totalFiles,
                        Progress = progress
                    });
                }
                catch (Exception)
                {
                }
            }

            return wallpapers;
        }

        public WallpaperItem ImportSingleFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            if (!IsSupportedFormat(filePath))
            {
                throw new NotSupportedException($"File format not supported: {Path.GetExtension(filePath)}");
            }

            // 直接使用原始文件路径
            var wallpaper = CreateWallpaperItem(filePath);
            if (wallpaper != null)
            {
                _wallpaperService.AddWallpaper(wallpaper);
            }

            return wallpaper;
        }

        private WallpaperItem CreateWallpaperItem(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var wallpaper = new WallpaperItem
                {
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    ThumbnailPath = filePath,
                    Category = "本地导入",
                    Type = GetWallpaperType(filePath),
                    Resolution = GetImageResolution(filePath),
                    FileSize = fileInfo.Length,
                    CreateTime = fileInfo.CreationTime,
                    Tags = "本地,导入",
                    TagsList = new System.Collections.ObjectModel.ObservableCollection<string> { "本地", "导入" },
                    IsDownloaded = true,
                    IsLocal = true
                };

                // 获取图片原始尺寸
                GetImageDimensions(filePath, wallpaper);

                DetectColorScheme(wallpaper);

                return wallpaper;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void GetImageDimensions(string filePath, WallpaperItem wallpaper)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                if (_supportedVideoFormats.Contains(extension))
                {
                    wallpaper.OriginalWidth = 1920;
                    wallpaper.OriginalHeight = 1080;
                    return;
                }

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                        stream,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.Default);

                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        wallpaper.OriginalWidth = frame.PixelWidth;
                        wallpaper.OriginalHeight = frame.PixelHeight;
                    }
                }
            }
            catch (Exception)
            {
                // 如果获取失败，使用默认尺寸
                wallpaper.OriginalWidth = 1920;
                wallpaper.OriginalHeight = 1080;
            }
        }

        private bool IsSupportedFormat(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return _supportedImageFormats.Contains(extension) || _supportedVideoFormats.Contains(extension);
        }

        private WallpaperType GetWallpaperType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            if (_supportedVideoFormats.Contains(extension) || extension == ".gif")
            {
                return WallpaperType.Dynamic;
            }
            return WallpaperType.Static;
        }

        private string GetImageResolution(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                if (_supportedVideoFormats.Contains(extension))
                {
                    return "动态壁纸";
                }

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                        stream,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.Default);

                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        return $"{frame.PixelWidth} × {frame.PixelHeight}";
                    }
                }
            }
            catch (Exception)
            {
            }

            return "未知";
        }

        private void DetectColorScheme(WallpaperItem wallpaper)
        {
            wallpaper.ColorScheme = "未知";
        }

        public void DeleteWallpaper(WallpaperItem wallpaper, bool deleteFile = false)
        {
            if (wallpaper == null)
                throw new ArgumentNullException(nameof(wallpaper));

            if (deleteFile && File.Exists(wallpaper.FilePath))
            {
                try
                {
                    // 尝试多次删除，以防文件被占用
                    int retryCount = 0;
                    while (retryCount < 3)
                    {
                        try
                        {
                            File.Delete(wallpaper.FilePath);
                            break;
                        }
                        catch (IOException) when (retryCount < 2)
                        {
                            // 文件被占用，等待后重试
                            System.Threading.Thread.Sleep(500);
                            retryCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"删除文件失败：{ex.Message}", ex);
                }
            }

            // 如果有缩略图，也删除缩略图
            if (deleteFile && !string.IsNullOrEmpty(wallpaper.ThumbnailPath) &&
                File.Exists(wallpaper.ThumbnailPath) &&
                wallpaper.ThumbnailPath != wallpaper.FilePath)
            {
                try
                {
                    int retryCount = 0;
                    while (retryCount < 3)
                    {
                        try
                        {
                            File.Delete(wallpaper.ThumbnailPath);
                            break;
                        }
                        catch (IOException) when (retryCount < 2)
                        {
                            System.Threading.Thread.Sleep(500);
                            retryCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 缩略图删除失败不影响主流程
                    System.Diagnostics.Debug.WriteLine($"删除缩略图失败：{ex.Message}");
                }
            }

            _wallpaperService.RemoveWallpaper(wallpaper);
        }

        public void ExportWallpaper(WallpaperItem wallpaper, string exportPath)
        {
            if (!File.Exists(wallpaper.FilePath))
            {
                throw new FileNotFoundException($"Source file not found: {wallpaper.FilePath}");
            }

            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            string destFile = Path.Combine(exportPath, Path.GetFileName(wallpaper.FilePath));
            File.Copy(wallpaper.FilePath, destFile, true);
        }
    }

    public class ImportProgressEventArgs : EventArgs
    {
        public string CurrentFile { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public double Progress { get; set; }
    }
}
