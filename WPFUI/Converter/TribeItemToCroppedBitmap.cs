﻿using MainCore.UI.Models.Output;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WPFUI.Converter
{
    [ValueConversion(typeof(TribeItem), typeof(CroppedBitmap))]
    public class TribeItemToCroppedBitmap : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TribeItem item) return null;

            var path = item.ImageSource;
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();

                var rect = new Int32Rect(
                    TribeItem.ImageMask.X,
                    TribeItem.ImageMask.Y,
                    TribeItem.ImageMask.Width,
                    TribeItem.ImageMask.Height);

                if (bitmap.PixelWidth < rect.X + rect.Width || bitmap.PixelHeight < rect.Y + rect.Height)
                {
                    return bitmap; // fallback to full image
                }

                return new CroppedBitmap(bitmap, rect);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}