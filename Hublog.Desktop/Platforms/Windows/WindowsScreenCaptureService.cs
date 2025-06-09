using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Linq;
// Remove this if not used:
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Hublog.Desktop.Platforms.Windows
{
    public class WindowsScreenCaptureService : IScreenCaptureService
    {
        public byte[] CaptureScreen()
        {
            var screenSize = Screen.PrimaryScreen.Bounds;
            using var bitmap =    new Bitmap(screenSize.Width, screenSize.Height);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);

            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            return memoryStream.ToArray();
        }

        public byte[] NewCaptureScreen()
        {
            var screenBounds = Screen.PrimaryScreen.Bounds;

            using var fullBitmap = new Bitmap(screenBounds.Width, screenBounds.Height);
            using (var graphics = Graphics.FromImage(fullBitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, screenBounds.Size);
            }

            int targetWidth = 500;
            int targetHeight = screenBounds.Height * targetWidth / screenBounds.Width;
            using var resizedBitmap = new Bitmap(fullBitmap, new System.Drawing.Size(targetWidth, targetHeight));

            using var ms = new MemoryStream();
            var jpegEncoder = GetEncoder(DrawingImageFormat.Jpeg);

            if (jpegEncoder == null)
            {
                throw new InvalidOperationException("JPEG encoder not found.");
            }

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 65L);

            resizedBitmap.Save(ms, jpegEncoder, encoderParams);

            double sizeInKB = ms.Length / 1024.0;
            Console.WriteLine($"Compressed screenshot size: {sizeInKB:F2} KB");

            return ms.ToArray();
        }

        // Helper to get JPEG encoder
        private ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(codec => codec.FormatID == format.Guid);
        }


    }
}
