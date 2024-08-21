using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Hublog.Desktop.Platforms.Windows
{
    public static class WindowManager
    {
        public static void OpenBreakPage()
        {
            var newWindow = new Microsoft.Maui.Controls.Window
            {
                Page = new ContentPage
                {
                    Content = new BlazorWebView
                    {
                        HostPage = "wwwroot/index.html",
                        RootComponents =
                        {
                            new RootComponent { ComponentType = Type.GetType("Hublog.Desktop.Components.Pages.BreakPage, Hublog.Desktop"), Selector = "#app" }
                        }
                    }
                }
            };

            Microsoft.Maui.Controls.Application.Current.OpenWindow(newWindow);
        }
    }
}
