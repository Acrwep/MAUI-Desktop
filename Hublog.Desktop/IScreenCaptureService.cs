namespace Hublog.Desktop
{
    public interface IScreenCaptureService
    {
        byte[] CaptureScreen();

        byte [] NewCaptureScreen();
    }
}
