using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace LuxeWallpaper.Models
{
    public partial class WallpaperItem : ObservableObject
    {
        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _filePath;

        [ObservableProperty]
        private string _thumbnailPath;

        [ObservableProperty]
        private WallpaperType _type;

        [ObservableProperty]
        private string _category;

        [ObservableProperty]
        private string _resolution;

        [ObservableProperty]
        private int _originalWidth;

        [ObservableProperty]
        private int _originalHeight;

        [ObservableProperty]
        private long _fileSize;

        [ObservableProperty]
        private DateTime _createTime;

        [ObservableProperty]
        private DateTime _lastUsedTime;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private bool _isDownloaded;

        [ObservableProperty]
        private int _downloadCount;

        [ObservableProperty]
        private int _favoriteCount;

        [ObservableProperty]
        private string _colorScheme;

        [ObservableProperty]
        private string _tags;

        [ObservableProperty]
        private System.Collections.ObjectModel.ObservableCollection<string> _tagsList;

        [ObservableProperty]
        private bool _isLocal;

        [ObservableProperty]
        private string _source;

        [ObservableProperty]
        private string _sourceUrl;

        [ObservableProperty]
        private string _publisherUrl;

        public WallpaperItem()
        {
            _id = Guid.NewGuid().ToString();
            _createTime = DateTime.Now;
            _lastUsedTime = DateTime.MinValue;
            _tagsList = new System.Collections.ObjectModel.ObservableCollection<string>();
        }
    }

    public enum WallpaperType
    {
        Static,
        Dynamic,
        Live
    }
}
