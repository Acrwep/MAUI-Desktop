using System.Drawing;
using System.Windows.Forms;

namespace Hublog.Desktop.Platforms.Windows
{
    public class WindowsScreenCaptureService : IScreenCaptureService
    {
        public byte[] CaptureScreen()
        {
            var screenSize = Screen.PrimaryScreen.Bounds;
            using var bitmap =    new Bitmap(screenSize.Width, screenSize.Height);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 1, 0, 0, bitmap.Size);

            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            return memoryStream.ToArray();
        }
    }
}
