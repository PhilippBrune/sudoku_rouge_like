using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.Platform
{
    /// <summary>
    /// Window frame for the window display modes: removes (Frameless Window) or restores (Window) the title bar and
    /// border, and centres the window on its monitor's work area. Unity has no API for a frameless window of a chosen
    /// size, so the Windows player sets the window style itself. Elsewhere this is a no-op and Frameless Window behaves
    /// like Window.
    /// </summary>
    // [REQ: DISPLAY-MODE-003] centred window; [REQ: DISPLAY-MODE-004] frameless window (Windows player only)
    // [REQ: DISPLAY-MODE-007] window title "Run of the Nine" (Windows player)
    public static class WindowFrameService
    {
        /// <summary>
        /// Title of the game window. The Player Settings product name stays <c>sudoku_rouge_like</c>, because Unity
        /// derives the save folder from it (LocalLow/Kratossos/sudoku_rouge_like: the live 1.0 build and the Steam
        /// Auto-Cloud root, SAVE-FOLDER-001), so the Windows player sets the store name as the title itself.
        /// </summary>
        public const string WindowTitle = "Run of the Nine";

        public static bool IsSupported =>
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            true;
#else
            false;
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void SetTitleBeforeSplash() => Win32.SetTitle(WindowTitle);

        // The runner re-applies the title once the first frame is up and after every fullscreen/window switch.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartRunner() => Runner.Get();
#endif

        /// <summary>Applies the frame for <paramref name="mode"/> once Unity has resized the window (a few frames
        /// after Screen.SetResolution). Fullscreen modes are left to Unity.</summary>
        public static void Apply(DisplayModeOption mode, int clientWidth, int clientHeight)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (mode != DisplayModeOption.Windowed && mode != DisplayModeOption.FramelessWindow) return;
            Runner.Get().Schedule(mode == DisplayModeOption.FramelessWindow, clientWidth, clientHeight);
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private sealed class Runner : MonoBehaviour
        {
            private static Runner _instance;
            private Coroutine _pending;
            private bool _frameless;
            private int _width;
            private int _height;
            private FullScreenMode _lastMode;
            private bool _titleSet;

            public static Runner Get()
            {
                if (_instance != null) return _instance;
                var go = new GameObject("WindowFrameService");
                DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                _instance = go.AddComponent<Runner>();
                return _instance;
            }

            public void Schedule(bool frameless, int width, int height)
            {
                _frameless = frameless;
                _width = width;
                _height = height;
                if (_pending != null) StopCoroutine(_pending);
                _pending = StartCoroutine(ApplyAfterResize());
            }

            private IEnumerator ApplyAfterResize()
            {
                // Screen.SetResolution takes effect at the end of the frame; Unity sets its own window style then.
                for (var i = 0; i < 3; i++) yield return null;
                if (Screen.fullScreenMode == FullScreenMode.Windowed)
                {
                    Win32.ApplyFrame(_frameless, _width, _height);
                    // Unity can restore the previous outer size after a style change (seen when switching from
                    // Window to Frameless Window), which leaves a larger client area. Check and re-apply.
                    for (var attempt = 0; attempt < 3; attempt++)
                    {
                        yield return new WaitForSecondsRealtime(0.25f);
                        if (Screen.fullScreenMode != FullScreenMode.Windowed) break;
                        if (Win32.Matches(_frameless, _width, _height)) break;
                        Win32.ApplyFrame(_frameless, _width, _height);
                    }
                }
                _lastMode = Screen.fullScreenMode;
                _pending = null;
            }

            private void Update()
            {
                if (!_titleSet) _titleSet = Win32.SetTitle(WindowTitle);

                // Alt+Enter (or Unity) can switch to fullscreen and back; Unity then restores its framed style.
                var mode = Screen.fullScreenMode;
                if (mode == _lastMode) return;
                _lastMode = mode;
                _titleSet = false;
                if (mode == FullScreenMode.Windowed && _width > 0 && _height > 0 && _pending == null)
                    _pending = StartCoroutine(ApplyAfterResize());
            }
        }

        private static class Win32
        {
            private const int GWL_STYLE = -16;
            private const long WS_POPUP = 0x80000000L;
            private const long WS_CAPTION = 0x00C00000L;
            private const long WS_THICKFRAME = 0x00040000L;
            private const long WS_SYSMENU = 0x00080000L;
            private const long WS_MINIMIZEBOX = 0x00020000L;
            private const long WS_MAXIMIZEBOX = 0x00010000L;
            private const uint SWP_NOZORDER = 0x0004;
            private const uint SWP_FRAMECHANGED = 0x0020;
            private const uint SWP_SHOWWINDOW = 0x0040;
            private const uint MONITOR_DEFAULTTONEAREST = 2;

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT { public int Left, Top, Right, Bottom; }

            [StructLayout(LayoutKind.Sequential)]
            private struct MONITORINFO
            {
                public int cbSize;
                public RECT rcMonitor;
                public RECT rcWork;
                public uint dwFlags;
            }

            private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
            [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder name, int max);
            [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
            [DllImport("kernel32.dll")] private static extern uint GetCurrentProcessId();
            [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);
            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr value);
            [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong32(IntPtr hWnd, int index);
            [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong32(IntPtr hWnd, int index, int value);
            [DllImport("user32.dll")] private static extern bool AdjustWindowRectEx(ref RECT rect, uint style, bool menu, uint exStyle);
            [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
            [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
            [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
            [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SetWindowTextW")] private static extern bool SetWindowText(IntPtr hWnd, string text);

            /// <summary>Sets the game window's title; false while the window is not up yet.</summary>
            public static bool SetTitle(string title)
            {
                try
                {
                    var hWnd = FindGameWindow();
                    return hWnd != IntPtr.Zero && SetWindowText(hWnd, title);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WindowFrame] Could not set the window title: {ex.Message}");
                    return true; // do not retry every frame
                }
            }

            /// <summary>True when the window has the wanted frame and client size.</summary>
            public static bool Matches(bool frameless, int clientWidth, int clientHeight)
            {
                var hWnd = FindGameWindow();
                if (hWnd == IntPtr.Zero || !GetClientRect(hWnd, out var client)) return true; // nothing to fix
                var hasCaption = (GetStyle(hWnd) & WS_CAPTION) == WS_CAPTION;
                return hasCaption != frameless
                    && client.Right - client.Left == clientWidth
                    && client.Bottom - client.Top == clientHeight;
            }

            private static IntPtr _found;
            private static uint _pid;

            [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
            private static bool OnWindow(IntPtr hWnd, IntPtr lParam)
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != _pid || !IsWindowVisible(hWnd)) return true;
                var name = new StringBuilder(64);
                GetClassName(hWnd, name, name.Capacity);
                if (name.ToString() != "UnityWndClass") return true;
                _found = hWnd;
                return false;
            }

            private static IntPtr FindGameWindow()
            {
                _found = IntPtr.Zero;
                _pid = GetCurrentProcessId();
                EnumWindows(OnWindow, IntPtr.Zero);
                return _found;
            }

            private static long GetStyle(IntPtr hWnd) =>
                IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, GWL_STYLE).ToInt64() : GetWindowLong32(hWnd, GWL_STYLE);

            private static void SetStyle(IntPtr hWnd, long style)
            {
                if (IntPtr.Size == 8) SetWindowLongPtr64(hWnd, GWL_STYLE, new IntPtr(style));
                else SetWindowLong32(hWnd, GWL_STYLE, unchecked((int)style));
            }

            public static void ApplyFrame(bool frameless, int clientWidth, int clientHeight)
            {
                try
                {
                    var hWnd = FindGameWindow();
                    if (hWnd == IntPtr.Zero || clientWidth <= 0 || clientHeight <= 0)
                    {
                        Debug.LogWarning("[WindowFrame] Game window not found; frame unchanged.");
                        return;
                    }

                    var frameBits = WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
                    var style = GetStyle(hWnd) & 0xFFFFFFFFL;
                    // Window: the non-resizable frame Unity uses (resizableWindow is off).
                    style = frameless
                        ? (style & ~frameBits) | WS_POPUP
                        : (style & ~(WS_POPUP | WS_THICKFRAME | WS_MAXIMIZEBOX)) | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX;
                    SetStyle(hWnd, style);

                    var outer = new RECT { Left = 0, Top = 0, Right = clientWidth, Bottom = clientHeight };
                    if (!frameless) AdjustWindowRectEx(ref outer, unchecked((uint)style), false, 0);
                    var width = outer.Right - outer.Left;
                    var height = outer.Bottom - outer.Top;

                    var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                    var work = GetMonitorInfo(MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST), ref info)
                        ? info.rcWork
                        : new RECT { Left = 0, Top = 0, Right = Display.main.systemWidth, Bottom = Display.main.systemHeight };
                    var x = work.Left + Math.Max(0, (work.Right - work.Left - width) / 2);
                    var y = work.Top + Math.Max(0, (work.Bottom - work.Top - height) / 2);

                    SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                    Debug.Log($"[WindowFrame] {(frameless ? "Frameless window" : "Window")} {clientWidth}x{clientHeight} at {x},{y}.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WindowFrame] Could not set the window frame: {ex.Message}");
                }
            }
        }
#endif
    }
}
