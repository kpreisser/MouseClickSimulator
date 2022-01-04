using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TTMouseclickSimulator.Core.Environment
{
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

        [DllImport("user32.dll", EntryPoint = "GetClientRect", ExactSpelling = true, SetLastError = true)]
        public static unsafe extern BOOL GetClientRect(IntPtr hWnd, RECT* lpRect);

        [DllImport("user32.dll", EntryPoint = "ClientToScreen", ExactSpelling = true)]
        public static unsafe extern BOOL ClientToScreen(IntPtr hWnd, POINT* lpPoint);

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow", ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow", ExactSpelling = true)]
        public static extern BOOL SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", ExactSpelling = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", ExactSpelling = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

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
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return (IntPtr)SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
        }

        public static unsafe void SendInput(INPUT input)
        {
            if (SendInputNative(1, &input, sizeof(INPUT)) == 0)
                throw new Win32Exception();
        }

        public static unsafe void SendInputs(INPUT[] inputs)
        {
            fixed (INPUT* inputsPtr = inputs)
            {
                if (SendInputNative((uint)inputs.Length, inputsPtr, sizeof(INPUT)) == 0)
                    throw new Win32Exception();
            }
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
            internal AbstractWindowsEnvironment.VirtualKeyShort wVk;
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
}
