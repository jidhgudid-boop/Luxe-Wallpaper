using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LuxeWallpaper.Converters
{
    /// <summary>
    /// 将宽度和高度转换为 Rect 的转换器
    /// </summary>
    public class RectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is double width &&
                values[1] is double height &&
                width > 0 && height > 0)
            {
                return new Rect(0, 0, width, height);
            }
            // 返回默认尺寸
            return new Rect(0, 0, 320, 180);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
