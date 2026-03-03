using System;
using System.Windows;

namespace LuxeWallpaper.Models
{
    /// <summary>
    /// 布局配置类 - 定义网格布局的行列数和卡片尺寸
    /// </summary>
    public class LayoutConfig
    {
        public int Columns { get; set; }
        public int Rows { get; set; }
        public string Name { get; set; }

        // 计算每页显示的卡片数量
        public int CardsPerPage => Columns * Rows;

        // 默认布局配置
        // 2行 × 4列 = 8张卡片
        public static LayoutConfig Layout2x4 => new LayoutConfig
        {
            Columns = 4,
            Rows = 2,
            Name = "2×4"
        };

        // 3行 × 7列 = 21张卡片
        public static LayoutConfig Layout3x7 => new LayoutConfig
        {
            Columns = 7,
            Rows = 3,
            Name = "3×7"
        };

        /// <summary>
        /// 计算卡片尺寸（基于可用空间）
        /// </summary>
        public CardDimensions CalculateCardDimensions(double availableWidth, double availableHeight, double margin)
        {
            double totalHorizontalMargin = margin * (Columns + 1);
            double totalVerticalMargin = margin * (Rows + 1);

            double cardWidth = (availableWidth - totalHorizontalMargin) / Columns;
            double cardHeight = (availableHeight - totalVerticalMargin) / Rows;

            // 保持16:9的宽高比
            double aspectRatio = 16.0 / 9.0;
            double calculatedHeight = cardWidth / aspectRatio;

            // 如果计算的高度超过可用高度，则基于高度计算宽度
            if (calculatedHeight > cardHeight)
            {
                calculatedHeight = cardHeight;
                cardWidth = calculatedHeight * aspectRatio;
            }
            else
            {
                cardHeight = calculatedHeight;
            }

            // 动态计算圆角（基于卡片宽度）
            double cornerRadius = Math.Max(8, cardWidth * 0.04);

            return new CardDimensions
            {
                Width = cardWidth,
                Height = cardHeight,
                CornerRadius = cornerRadius,
                Margin = margin
            };
        }
    }

    /// <summary>
    /// 卡片尺寸结构
    /// </summary>
    public struct CardDimensions
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double CornerRadius { get; set; }
        public double Margin { get; set; }
    }
}
