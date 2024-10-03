using System.Drawing;
using System.Windows.Forms;

namespace Hublog.Desktop.Platforms.Windows
{
    public class WindowsScreenCaptureService : IScreenCaptureService
    {
        public byte[] CaptureScreen()
        {
            return new byte[0];
        }
    }
}
