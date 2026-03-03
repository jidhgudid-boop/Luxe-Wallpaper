using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using LuxeWallpaper.Models;

namespace LuxeWallpaper.Services
{
    /// <summary>
    /// 响应式布局管理器 - 管理卡片布局、分页和重排
    /// </summary>
    public class ResponsiveLayoutManager
    {
        private LayoutConfig _currentLayout;
        private int _currentPage = 1;
        private List<WallpaperItem> _allItems = new List<WallpaperItem>();
        private DispatcherTimer _layoutUpdateTimer;

        public event EventHandler<LayoutChangedEventArgs> LayoutChanged;
        public event EventHandler<PageChangedEventArgs> PageChanged;
        public event EventHandler ItemsRefreshed;

        public LayoutConfig CurrentLayout
        {
            get => _currentLayout;
            private set
            {
                if (_currentLayout != value)
                {
                    var oldLayout = _currentLayout;
                    _currentLayout = value;
                    LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(oldLayout, value));
                    RefreshPagination();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value && value >= 1 && value <= TotalPages)
                {
                    var oldPage = _currentPage;
                    _currentPage = value;
                    PageChanged?.Invoke(this, new PageChangedEventArgs(oldPage, value));
                    ItemsRefreshed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public int TotalPages => (int)Math.Ceiling((double)_allItems.Count / CurrentLayout.CardsPerPage);

        public ObservableCollection<WallpaperItem> PagedItems { get; } = new ObservableCollection<WallpaperItem>();
        public ObservableCollection<int> PageNumbers { get; } = new ObservableCollection<int>();

        public CardDimensions CurrentCardDimensions { get; private set; }

        public ResponsiveLayoutManager()
        {
            _currentLayout = LayoutConfig.Layout2x4; // 默认2x4布局

            // 防抖定时器，用于延迟更新布局
            _layoutUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _layoutUpdateTimer.Tick += (s, e) =>
            {
                _layoutUpdateTimer.Stop();
                UpdateLayout();
            };
        }

        /// <summary>
        /// 切换布局
        /// </summary>
        public void ToggleLayout()
        {
            // 如果当前是2x4布局（4列2行），切换到3x7布局（7列3行）
            if (CurrentLayout.Columns == 4 && CurrentLayout.Rows == 2)
            {
                CurrentLayout = LayoutConfig.Layout3x7;
            }
            else
            {
                CurrentLayout = LayoutConfig.Layout2x4;
            }
        }

        /// <summary>
        /// 设置布局
        /// </summary>
        public void SetLayout(LayoutConfig layout)
        {
            CurrentLayout = layout;
        }

        /// <summary>
        /// 更新布局尺寸
        /// </summary>
        public void UpdateDimensions(double availableWidth, double availableHeight, double margin)
        {
            CurrentCardDimensions = CurrentLayout.CalculateCardDimensions(availableWidth, availableHeight, margin);
            _layoutUpdateTimer.Stop();
            _layoutUpdateTimer.Start();
        }

        /// <summary>
        /// 设置所有项目
        /// </summary>
        public void SetItems(IEnumerable<WallpaperItem> items)
        {
            _allItems = items?.ToList() ?? new List<WallpaperItem>();
            RefreshPagination();
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        public void AddItem(WallpaperItem item)
        {
            if (item != null)
            {
                _allItems.Add(item);
                RefreshPagination();
            }
        }

        /// <summary>
        /// 删除项目并自动重排
        /// </summary>
        public void RemoveItem(WallpaperItem item)
        {
            if (item != null && _allItems.Remove(item))
            {
                // 如果当前页没有项目了，回到上一页
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = Math.Max(1, TotalPages);
                }
                else
                {
                    RefreshPagination();
                }
            }
        }

        /// <summary>
        /// 刷新分页
        /// </summary>
        private void RefreshPagination()
        {
            PagedItems.Clear();

            var itemsForPage = _allItems
                .Skip((CurrentPage - 1) * CurrentLayout.CardsPerPage)
                .Take(CurrentLayout.CardsPerPage)
                .ToList();

            foreach (var item in itemsForPage)
            {
                PagedItems.Add(item);
            }

            UpdatePageNumbers();
            ItemsRefreshed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 更新页码列表
        /// </summary>
        private void UpdatePageNumbers()
        {
            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++)
            {
                PageNumbers.Add(i);
            }
        }

        /// <summary>
        /// 更新布局
        /// </summary>
        private void UpdateLayout()
        {
            ItemsRefreshed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 跳转到指定页
        /// </summary>
        public void GoToPage(int page)
        {
            CurrentPage = page;
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        public void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }
    }

    public class LayoutChangedEventArgs : EventArgs
    {
        public LayoutConfig OldLayout { get; }
        public LayoutConfig NewLayout { get; }

        public LayoutChangedEventArgs(LayoutConfig oldLayout, LayoutConfig newLayout)
        {
            OldLayout = oldLayout;
            NewLayout = newLayout;
        }
    }

    public class PageChangedEventArgs : EventArgs
    {
        public int OldPage { get; }
        public int NewPage { get; }

        public PageChangedEventArgs(int oldPage, int newPage)
        {
            OldPage = oldPage;
            NewPage = newPage;
        }
    }
}
