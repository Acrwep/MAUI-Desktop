using Android.App;
using Android.Runtime;

namespace Hublog.Desktop
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public static string OnlineURL = "https://localhost:44322/";
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
