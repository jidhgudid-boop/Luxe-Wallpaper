using LuxeWallpaper.Models;
using LuxeWallpaper.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShapesPath = System.Windows.Shapes.Path;

namespace LuxeWallpaper.Views
{
    public partial class ImportWallpaperDialog : Window
    {
        private readonly List<string> _selectedFiles = new();
        private readonly LocalImportService _localImportService;
        private readonly Action<WallpaperItem> _onImportCompleted;

        // 支持的文件格式
        private readonly List<string> _supportedImageFormats = new() { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".avif" };
        private readonly List<string> _supportedVideoFormats = new() { ".mp4", ".mov", ".mkv", ".webm" };

        public ImportWallpaperDialog(Action<WallpaperItem> onImportCompleted)
        {
            InitializeComponent();
            var settings = new UserSettings();
            var wallpaperService = new WallpaperService(settings);
            _localImportService = new LocalImportService(wallpaperService, settings);
            _onImportCompleted = onImportCompleted;
            Owner = Application.Current.MainWindow;
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 选择文件按钮点击
        /// </summary>
        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择壁纸文件",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp;*.avif|视频文件|*.mp4;*.webm;*.mov;*.mkv|所有文件|*.*",
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                AddFiles(dialog.FileNames);
            }
        }

        /// <summary>
        /// 拖拽进入
        /// </summary>
        private void FileDropArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                var border = sender as Border;
                if (border != null)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(123, 91, 245)); // #7B5BF5
                    border.Background = new SolidColorBrush(Color.FromRgb(30, 30, 50)); // #1E1E32
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 拖拽悬停
        /// </summary>
        private void FileDropArea_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 拖拽放下
        /// </summary>
        private void FileDropArea_Drop(object sender, DragEventArgs e)
        {
            // 恢复样式
            var border = sender as Border;
            if (border != null)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 72)); // #2E2E48
                border.Background = new SolidColorBrush(Color.FromRgb(21, 21, 37)); // #151525
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddFiles(files);
            }
        }

        /// <summary>
        /// 添加文件到列表
        /// </summary>
        private void AddFiles(string[] files)
        {
            foreach (var file in files)
            {
                var extension = System.IO.Path.GetExtension(file).ToLower();

                // 检查是否是支持的格式
                if (_supportedImageFormats.Contains(extension) || _supportedVideoFormats.Contains(extension))
                {
                    if (!_selectedFiles.Contains(file))
                    {
                        _selectedFiles.Add(file);
                    }
                }
            }

            UpdateFileList();
        }

        /// <summary>
        /// 更新文件列表显示
        /// </summary>
        private void UpdateFileList()
        {
            FileCountText.Text = $"{_selectedFiles.Count} 个文件待导入";

            if (_selectedFiles.Count > 0)
            {
                EmptyState.Visibility = Visibility.Collapsed;
                FileListScrollViewer.Visibility = Visibility.Visible;
                ImportButton.IsEnabled = true;

                // 清空并重新填充列表
                FileListPanel.Children.Clear();

                foreach (var file in _selectedFiles)
                {
                    var fileItem = CreateFileItem(file);
                    FileListPanel.Children.Add(fileItem);
                }
            }
            else
            {
                EmptyState.Visibility = Visibility.Visible;
                FileListScrollViewer.Visibility = Visibility.Collapsed;
                ImportButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// 创建文件项控件
        /// </summary>
        private Border CreateFileItem(string filePath)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            var isImage = _supportedImageFormats.Contains(extension);

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 50)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 72)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 文件图标
            var iconPath = new ShapesPath
            {
                Width = 24,
                Height = 24,
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush(Color.FromRgb(123, 91, 245)), // #7B5BF5
                Data = isImage
                    ? Geometry.Parse("M21,19V5C21,3.89 20.1,3 19,3H5C3.89,3 3,3.89 3,5V19C3,20.1 3.89,21 5,21H19C20.1,21 21,20.1 21,19M8.5,13.5L11,16.5L14.5,12L19,18H5M19,5V19H5V5H19Z")
                    : Geometry.Parse("M17,10.5V7A1,1 0 0,0 16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5Z")
            };
            Grid.SetColumn(iconPath, 0);

            // 文件名
            var fileNameText = new TextBlock
            {
                Text = fileName,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 12, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 300
            };
            Grid.SetColumn(fileNameText, 1);

            // 删除按钮 - 红色垃圾桶图标，完全无悬停效果
            var deleteButton = new Button
            {
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Content = new ShapesPath
                {
                    Width = 16,
                    Height = 16,
                    Stretch = Stretch.Uniform,
                    Fill = new SolidColorBrush(Color.FromRgb(229, 115, 115)), // 红色 #E57373
                    Data = Geometry.Parse("M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z")
                },
                // 使用空模板完全移除默认悬停效果
                Template = new ControlTemplate(typeof(Button))
                {
                    VisualTree = new FrameworkElementFactory(typeof(ContentPresenter))
                }
            };

            deleteButton.Click += (s, e) =>
            {
                _selectedFiles.Remove(filePath);
                UpdateFileList();
            };
            Grid.SetColumn(deleteButton, 2);

            grid.Children.Add(iconPath);
            grid.Children.Add(fileNameText);
            grid.Children.Add(deleteButton);

            border.Child = grid;

            return border;
        }

        /// <summary>
        /// 导入按钮点击
        /// </summary>
        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFiles.Count == 0) return;

            ImportButton.IsEnabled = false;
            ImportButton.Content = "导入中...";

            var importedCount = 0;
            var failedCount = 0;

            foreach (var filePath in _selectedFiles.ToList())
            {
                try
                {
                    var wallpaper = _localImportService.ImportSingleFile(filePath);
                    if (wallpaper != null)
                    {
                        _onImportCompleted?.Invoke(wallpaper);
                        importedCount++;
                    }
                }
                catch (Exception)
                {
                    failedCount++;
                    // 可以在这里记录错误日志
                }
            }

            // 显示导入结果
            if (failedCount > 0)
            {
                MessageBox.Show($"导入完成！成功：{importedCount} 个，失败：{failedCount} 个",
                    "导入结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Close();
        }
    }
}
