using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LuxeWallpaper.Converters
{
    /// <summary>
    /// 智能图片裁剪转换器
    /// 根据图片原始尺寸和显示区域高度计算裁剪比例
    /// 保持图片主体居中显示，维持原有比例不变
    /// </summary>
    public class SmartImageCropConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4)
                return new Rect(0, 0, 1, 1);

            // 获取图片原始尺寸
            int originalWidth = values[0] is int w ? w : 0;
            int originalHeight = values[1] is int h ? h : 0;

            // 获取显示区域尺寸
            double displayWidth = values[2] is double dw ? dw : 320;
            double displayHeight = values[3] is double dh ? dh : 180;

            if (originalWidth <= 0 || originalHeight <= 0)
                return new Rect(0, 0, 1, 1);

            // 计算图片和显示区域的比例
            double imageAspectRatio = (double)originalWidth / originalHeight;
            double displayAspectRatio = displayWidth / displayHeight;

            double scaleX, scaleY, offsetX, offsetY;

            if (imageAspectRatio > displayAspectRatio)
            {
                // 图片比显示区域更宽，需要裁剪左右两侧
                // 以高度为基准进行缩放
                scaleY = 1.0;
                scaleX = displayAspectRatio / imageAspectRatio;
                offsetX = (1.0 - scaleX) / 2.0; // 居中裁剪
                offsetY = 0;
            }
            else
            {
                // 图片比显示区域更高，需要裁剪上下两侧
                // 以宽度为基准进行缩放
                scaleX = 1.0;
                scaleY = imageAspectRatio / displayAspectRatio;
                offsetX = 0;
                offsetY = (1.0 - scaleY) / 2.0; // 居中裁剪
            }

            // 返回裁剪区域（相对于图片的0-1坐标系）
            return new Rect(offsetX, offsetY, scaleX, scaleY);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 图片高度比例转换器
    /// 根据图片高度计算显示时的缩放比例
    /// </summary>
    public class ImageHeightScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int originalHeight && originalHeight > 0)
            {
                // 标准显示高度为180
                const double standardHeight = 180.0;
                double scale = standardHeight / originalHeight;

                // 限制最小和最大缩放比例
                scale = Math.Max(0.5, Math.Min(2.0, scale));

                return scale;
            }

            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 图片裁剪视图框转换器
    /// 根据图片尺寸创建适当的Viewbox
    /// </summary>
    public class ImageClipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Geometry.Empty;

            int originalWidth = values[0] is int w ? w : 320;
            int originalHeight = values[1] is int h ? h : 180;

            // 创建裁剪几何体
            var clipGeometry = new RectangleGeometry(
                new Rect(0, 0, originalWidth, originalHeight));

            return clipGeometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
