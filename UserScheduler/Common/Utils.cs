using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace UserScheduler.Common
{
    public static class Utils
    {
        public static BitmapImage ToBitmapImage(string iconString)
        {
            try
            {
                var bs = Convert.FromBase64String(iconString);

                using (var memory = new MemoryStream(bs))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch { }

            return null;
        }
    }
}
