using System;
using System.Globalization;
using System.Windows.Data;

namespace LuxeWallpaper.Converters
{
    /// <summary>
    /// 宽高比转换器 - 根据宽度和比例计算高度
    /// </summary>
    public class AspectRatioConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is double width &&
                values[1] is double ratio &&
                width > 0 && ratio > 0)
            {
                return width * ratio;
            }
            return 180; // 默认高度
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
