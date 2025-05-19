using System.Runtime.InteropServices;
using System.Text;

namespace WindowPlacementManager;

public static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


    public static string GetWindowTitle(IntPtr hWnd)
    {
        const int nChars = 256;
        StringBuilder buff = new StringBuilder(nChars);
        if(GetWindowText(hWnd, buff, nChars) > 0)
        {
            return buff.ToString();
        }
        return string.Empty;
    }
}


public struct RECT
{
    public int Left, Top, Right, Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
