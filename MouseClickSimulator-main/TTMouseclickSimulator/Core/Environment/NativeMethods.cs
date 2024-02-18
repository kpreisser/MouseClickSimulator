using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TTMouseClickSimulator.Core.Environment;

internal static class NativeMethods
{
    /// <summary>
    /// Synthesizes keystrokes, mouse motions, and button clicks.
    /// </summary>
    [DllImport("user32.dll", EntryPoint = "SendInput", ExactSpelling = true, SetLastError = true)]
    private static unsafe extern uint SendInputNative(
        uint nInputs,
        INPUT* pInputs,
        int cbSize);

    [DllImport("user32.dll", EntryPoint = "SendMessageW", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr SendMessageW(
        IntPtr hWnd,
        WM Msg,
        nint wParam = default,
        nint lParam = default);

    [DllImport("user32.dll", EntryPoint = "SendNotifyMessageW", ExactSpelling = true, SetLastError = true)]
    public static extern BOOL SendNotifyMessageW(
        IntPtr hWnd,
        WM Msg,
        nint wParam = default,
        nint lParam = default);

    [DllImport("user32.dll", EntryPoint = "PostMessageW", ExactSpelling = true, SetLastError = true)]
    public static extern BOOL PostMessageW(
        IntPtr hWnd,
        WM Msg,
        nint wParam = default,
        nint lParam = default);

    [DllImport("kernel32.dll", EntryPoint = "SetLastError", ExactSpelling = true)]
    public static extern void SetLastError(uint dwErrCode);

    [DllImport("user32.dll", EntryPoint = "GetClientRect", ExactSpelling = true, SetLastError = true)]
    public static unsafe extern BOOL GetClientRect(IntPtr hWnd, RECT* lpRect);

    [DllImport("user32.dll", EntryPoint = "ClientToScreen", ExactSpelling = true)]
    public static unsafe extern BOOL ClientToScreen(IntPtr hWnd, POINT* lpPoint);

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow", ExactSpelling = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "SetForegroundWindow", ExactSpelling = true)]
    public static extern BOOL SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", ExactSpelling = true, SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos", ExactSpelling = true, SetLastError = true)]
    public static extern BOOL SetWindowPos(
        IntPtr hWnd,
        nint hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int xy,
        SWP flags);

    [DllImport("user32.dll", EntryPoint = "EnableWindow", ExactSpelling = true)]
    public static extern BOOL EnableWindow(
        IntPtr hWnd,
        BOOL bEnable);

    [DllImport("user32.dll", EntryPoint = "GetDC", ExactSpelling = true)]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "ReleaseDC", ExactSpelling = true)]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", EntryPoint = "BitBlt", ExactSpelling = true, SetLastError = true)]
    public static extern BOOL BitBlt(
        IntPtr hdc,
        int x,
        int y,
        int cx,
        int cy,
        IntPtr hdcSrc,
        int x1,
        int y1,
        uint rop);

    public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size is 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return (IntPtr)SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
    }

    public static unsafe void SendInput(ReadOnlySpan<INPUT> inputs)
    {
        fixed (INPUT* inputsPtr = inputs)
        {
            if (SendInputNative((uint)inputs.Length, inputsPtr, sizeof(INPUT)) is 0)
                throw new Win32Exception();
        }
    }

    public static IntPtr SendMessageManaged(
        IntPtr hWnd,
        WM Msg,
        nint wParam = default,
        nint lParam = default)
    {
        // Before calling SendMessageW, set the last Win32 error code to 0, so that
        // we can later detect whether an error occured. This is because SendMessageW
        // only sets the last Win32 error if an error actually occured; however we
        // have no way to detect whether this was the case from the return value.
        // Note: Strictly speaking, this may not be reliable because the .NET runtime
        // may call other Win32 APIs before actually calling SendMessageW (e.g.
        // LoadLibraryW, GetProcAddress), but currently there's no other way to do
        // this, so we rely on the current runtime behavior. Also, if these other
        // Win32 calls would set an error code, it should mean our call to the
        // native SendMessageW function will also fail.
        SetLastError(0);

        var result = SendMessageW(
            hWnd,
            Msg,
            wParam,
            lParam);

        if (Marshal.GetLastWin32Error() != 0)
            throw new Win32Exception();

        return result;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        internal InputType type;
        internal InputUnion InputUnion;
    }

    internal enum InputType : uint
    {
        INPUT_MOUSE = 0,
        INPUT_KEYBOARD = 1,
        INPUT_HARDWARE = 2
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)]
        internal MOUSEINPUT mi;
        [FieldOffset(0)]
        internal KEYBDINPUT ki;
        [FieldOffset(0)]
        internal HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        internal int dx;
        internal int dy;
        internal uint mouseData;
        internal MOUSEEVENTF dwFlags;
        internal uint time;
        internal UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        internal WindowsEnvironment.VirtualKey wVk;
        internal ushort wScan;
        internal KEYEVENTF dwFlags;
        internal uint time;
        internal UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        internal uint uMsg;
        internal ushort wParamL;
        internal ushort wParamH;
    }

    [Flags]
    internal enum MOUSEEVENTF : uint
    {
        ABSOLUTE = 0x8000,
        HWHEEL = 0x01000,
        MOVE = 0x0001,
        MOVE_NOCOALESCE = 0x2000,
        LEFTDOWN = 0x0002,
        LEFTUP = 0x0004,
        RIGHTDOWN = 0x0008,
        RIGHTUP = 0x0010,
        MIDDLEDOWN = 0x0020,
        MIDDLEUP = 0x0040,
        VIRTUALDESK = 0x4000,
        WHEEL = 0x0800,
        XDOWN = 0x0080,
        XUP = 0x0100
    }

    [Flags]
    internal enum KEYEVENTF : uint
    {
        EXTENDEDKEY = 0x0001,
        KEYUP = 0x0002,
        SCANCODE = 0x0008,
        UNICODE = 0x0004
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int x;
        public int y;
    }

    public enum WM : uint
    {
        MOUSEMOVE = 0x0200,
        LBUTTONDOWN = 0x0201,
        LBUTTONUP = 0x0202,
        RBUTTONDOWN = 0x0204,
        RBUTTONUP = 0x0205,

        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102
    }

    [Flags]
    public enum MK : int
    {
        LBUTTON = 0x0001
    }

    public enum HWND : int
    {
        NOTOPMOST = -2,

        TOPMOST = -1
    }

    [Flags]
    public enum SWP : uint
    {
        NOSIZE = 0x0001,

        NOMOVE = 0x0002,

        NOACTIVATE = 0x0010,

        ASYNCWINDOWPOS = 0x4000
    }

    public struct BOOL
    {
        public int Value;

        public static implicit operator bool(BOOL @bool)
        {
            return @bool.Value != 0;
        }

        public static implicit operator BOOL(bool @bool)
        {
            return new BOOL()
            {
                Value = @bool ? 1 : 0
            };
        }
    }
}
