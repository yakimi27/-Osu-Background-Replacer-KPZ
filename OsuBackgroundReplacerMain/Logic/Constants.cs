using System;
using System.IO;
using System.Linq;

namespace OsuBackgroundReplacerMain.Logic
{
    public static class Constants
    {
        public static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png" };

        public static bool IsSupportedImage(string filePath)
        {
            string? extension = Path.GetExtension(filePath)?.ToLower();
            return extension != null && SupportedImageExtensions.Contains(extension);
        }
    }
}
