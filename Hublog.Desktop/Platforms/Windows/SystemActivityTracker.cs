using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

public class SystemActivityTracker
{
    private readonly NavigationManager _navigationManager;
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    private const int InactivityThreshold = 10000; // 10 seconds
    private DispatcherTimer _timer;
    private uint _lastInputTime;

    public event Action OnInactivityDetected;

    public SystemActivityTracker()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += CheckInactivity;
        _timer.Start();
    }

    private async void CheckInactivity(object sender, object e)
    {
        var lastInputInfo = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (GetLastInputInfo(ref lastInputInfo))
        {
            var idleTime = Environment.TickCount - lastInputInfo.dwTime;
            if (idleTime > InactivityThreshold)
            {
                OnInactivityDetected?.Invoke();
                await Task.Delay(9000); // Delay of 5000 milliseconds (5 seconds)
                _navigationManager.NavigateTo("/Dashboard"); // Replace with the actual route
            }
        }
    }

    public void Stop() => _timer.Stop();
}
