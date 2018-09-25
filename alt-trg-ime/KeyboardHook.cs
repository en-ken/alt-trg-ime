using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace alt_trg_ime
{
    class KeyboardHook
    {
        class NativeMethods
        {
            #region === Keyboard hook ===
            /// <summary>
            /// Callback method of keyboard procesure.
            /// </summary>
            /// <param name="code">A hook code</param>
            /// <param name="wParam">A window message</param>
            /// <param name="lParam">A keyboard hook information</param>
            /// <returns></returns>
            public delegate IntPtr LowLevelKeyboardProc(int code, WM wParam, ref KBDLLHOOKSTRUCT lParam);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="code"></param>
            /// <param name="wParam"></param>
            /// <param name="lParam"></param>
            /// <returns></returns>
            public delegate IntPtr KeyboardProc(int code, VK wParam, int lParam);

            /// <summary>
            /// Install the specified hook procesure into a hook chain.
            /// </summary>
            /// <param name="idHook">The type of hook procesure(WH_KEYBOARD_LL)</param>
            /// <param name="lpfn">A pointer to the hook procesure</param>
            /// <param name="hMod">A handle to the DLL (must be null)</param>
            /// <param name="dwThreadId">The indentifier of the thread of the hook procesure</param>
            /// <returns></returns>
            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            /// <summary>
            /// Pass the hook information to the next hook procesure.
            /// </summary>
            /// <param name="hhk">(ignored parameter)</param>
            /// <param name="nCode">A hook code</param>
            /// <param name="wParam">A virtual-key code</param>
            /// <param name="lParam">A keyboard hook information</param>
            /// <returns></returns>
            [DllImport("user32.dll")]
            //public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, WM wParam, ref KBDLLHOOKSTRUCT lParam);
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, VK wParam, int lParam);

            /// <summary>
            /// Uninstall the hook procesure
            /// </summary>
            /// <param name="hhk">A handle to the hook procesure</param>
            /// <returns></returns>
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);
            #endregion

            #region === IME operation ===
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetCursorPos(out POINT lppoint);

            [DllImport("user32.dll")]
            public static extern IntPtr WindowFromPoint(POINT point);

            /// <summary>
            /// Activate/Deactivate IME
            /// </summary>
            /// <param name="hIMC">The handle of the input context</param>
            /// <param name="fOpen">Activate or not</param>
            /// <returns></returns>
            [DllImport("imm32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ImmSetOpenStatus(IntPtr hIMC, bool fOpen);

            [DllImport("imm32.dll")]
            public static extern IntPtr ImmGetContext(IntPtr hWnd);

            [DllImport("imm32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hImc);


            #endregion
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x;
            public int y;
        }

        struct KBDLLHOOKSTRUCT
        {
            /// <summary>
            /// Virtual-key Code.
            /// </summary>
            public VK vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// Low level keyboard
        /// </summary>
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_KEYBOARD = 2;

        enum VK
        {
            /// <summary>
            /// Left ALT key
            /// </summary>
            LMENU = 0xA4,

            /// <summary>
            /// Right ALT key
            /// </summary>
            RMENU = 0xA5,
        }

        enum WM
        {
            /// <summary>
            /// Key down
            /// </summary>
            KEYDOWN = 0x0100,
            /// <summary>
            /// Key up
            /// </summary>
            KEYUP = 0x0101,
            /// <summary>
            /// Alt down
            /// </summary>
            SYSKEYDOWN = 0x0104,
            /// <summary>
            /// Alt up
            /// </summary>
            SYSKEYUP = 0x105
        }
        
        private static void changeIME(bool isOn)
        {
            Console.WriteLine("Current PID" + Process.GetCurrentProcess().Id);

            POINT p;
            NativeMethods.GetCursorPos(out p);
            var hWnd = NativeMethods.WindowFromPoint(p);
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("Cannot get window handle");
                return;
            }

            var hImc = NativeMethods.ImmGetContext(hWnd);
            if(hImc == IntPtr.Zero)
            {
                Console.WriteLine("Cannot get IME handle");
                return;
            }

            var result = NativeMethods.ImmSetOpenStatus(handle, isOn);
            NativeMethods.ImmReleaseContext(hWnd, hImc);
            Console.WriteLine($">>>IME {isOn}:{result}");
        }


        private static IntPtr HookProc2(int code, VK wParam, int lParam)
        {
            if(code < 0)
            {
                return NativeMethods.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
            }

            Func<int, bool> isKeyDown = param => (param & 0x40000000) != 0;
            Func<int, int> getKeyCount = param => (param & 0xFFFF);

            switch (wParam)
            {
                case VK.LMENU:
                    if (isKeyDown(lParam))
                    {
                        Console.WriteLine("KeyDown" + getKeyCount(lParam));
                    }
                    else if(getKeyCount(lParam) > 0)
                    {
                        Console.WriteLine("KeyUp" + getKeyCount(lParam));

                        changeIME(false);
                        return IntPtr.Zero;
                    }
                    break;
                case VK.RMENU:
                    if (isKeyDown(lParam))
                    {
                        Console.WriteLine("KeyDown" + getKeyCount(lParam));
                    }
                    else if(getKeyCount(lParam) > 0)
                    {
                        Console.WriteLine("KeyUp" + getKeyCount(lParam));

                        changeIME(true);
                        return IntPtr.Zero;
                    }
                    break;
                default:
                    break;
            }

            return NativeMethods.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private static IntPtr handle;
        private static event NativeMethods.KeyboardProc hookCallback;

        /// <summary>
        /// Whether the left ALT stroke becomes a blank stroke
        /// </summary>
        private static bool isLAltBlankStroke = false;
        /// <summary>
        /// Whether the right ALT stroke becomes a blank stroke
        /// </summary>
        private static bool isRAltBlankStroke = false;

        public static void Start()
        {
            //Get this dll's handle
            var h = Marshal.GetHINSTANCE(typeof(KeyboardHook).Assembly.GetModules()[0]);

            hookCallback += HookProc2;
            handle = NativeMethods.SetWindowsHookEx(WH_KEYBOARD, HookProc2, h, 0);

            if(handle == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception();
            }
        }

        public static void Stop()
        {
            NativeMethods.UnhookWindowsHookEx(handle);
            handle = IntPtr.Zero;
            hookCallback -= HookProc2;
        }
    }
}
