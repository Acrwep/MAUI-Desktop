using System.Runtime.InteropServices;
using Hublog.Desktop.Components.Pages;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Hublog.Desktop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                window.Width = 390;
                window.Height = 670;
            }

            return window;
        }
    }
}
