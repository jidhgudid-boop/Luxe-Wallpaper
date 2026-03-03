using CommunityToolkit.Mvvm.ComponentModel;

namespace LuxeWallpaper.Models
{
    public partial class PageNumberItem : ObservableObject
    {
        [ObservableProperty]
        private int _pageNumber;

        [ObservableProperty]
        private bool _isCurrentPage;

        public override string ToString()
        {
            return PageNumber.ToString();
        }
    }
}
