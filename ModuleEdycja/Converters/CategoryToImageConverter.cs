using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ModuleEdycja.Converters
{
    public class CategoryToImageConverter : IValueConverter
    {
        public string ImagesFolder { get; set; } = "Images";
        public string DefaultImage { get; set; } = "default.png";

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var category = value as string;
            if (string.IsNullOrWhiteSpace(category))
                return null;

            var fileName = NormalizeFileName(category) + ".png";
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var imagePath = Path.Combine(baseDir, ImagesFolder, fileName);

            if (!File.Exists(imagePath))
            {
                imagePath = Path.Combine(baseDir, ImagesFolder, DefaultImage);
                if (!File.Exists(imagePath))
                    return null;
            }

            return CreateImage(imagePath);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();

        private static string NormalizeFileName(string category)
        {
            var normalized = category.Trim().ToUpperInvariant();
            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                normalized = normalized.Replace(ch, '_');
            }
            normalized = normalized.Replace(' ', '_').Replace('/', '_').Replace('\\', '_');
            return normalized;
        }

        private static BitmapImage CreateImage(string path)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
