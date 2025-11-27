using System.Runtime.InteropServices;
using HttpMataki.NET.Auto;

public static class StartupHook
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    public static void Initialize()
    {
        if (GetConsoleWindow() == IntPtr.Zero)
        {
            AllocConsole();
        }

        HttpClientAutoInterceptor.StartInterception();
    }
}