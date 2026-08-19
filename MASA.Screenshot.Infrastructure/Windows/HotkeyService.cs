using System.Windows.Forms;
using System.Windows.Interop;
using MASA.Screenshot.Core.Constants;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;
using MASA.Screenshot.Infrastructure.Windows;

namespace MASA.Screenshot.Infrastructure.Windows;

public sealed class HotkeyService : IHotkeyService, IDisposable
{
    private IntPtr _windowHandle = IntPtr.Zero;
    private HwndSource? _hwndSource;
    private bool _disposed;

    public event EventHandler<CaptureMode>? HotkeyPressed;
    public bool IsPaused { get; set; }

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(HwndHook);
    }

    public void RegisterHotkeys(AppSettings settings)
    {
        if (_windowHandle == IntPtr.Zero) return;
        UnregisterHotkeys();

        if (!settings.HotkeysEnabled || IsPaused) return;

        // Register default global hotkeys safely
        // 1. Area Capture (Default PrintScreen or custom)
        RegisterSingleHotkey(HotkeyConstants.HOTKEY_ID_AREA, HotkeyConstants.MOD_NOREPEAT, (uint)Keys.PrintScreen);

        // 2. Fullscreen (Ctrl + PrintScreen)
        RegisterSingleHotkey(HotkeyConstants.HOTKEY_ID_FULLSCREEN, HotkeyConstants.MOD_CONTROL | HotkeyConstants.MOD_NOREPEAT, (uint)Keys.PrintScreen);

        // 3. Active Window (Alt + PrintScreen)
        RegisterSingleHotkey(HotkeyConstants.HOTKEY_ID_ACTIVE_WINDOW, HotkeyConstants.MOD_ALT | HotkeyConstants.MOD_NOREPEAT, (uint)Keys.PrintScreen);

        // 4. Quick Capture (Win + Shift + S or fallback Ctrl + Shift + S)
        bool registeredWinShiftS = RegisterSingleHotkey(HotkeyConstants.HOTKEY_ID_QUICK_CAPTURE, HotkeyConstants.MOD_WIN | HotkeyConstants.MOD_SHIFT | HotkeyConstants.MOD_NOREPEAT, (uint)Keys.S);
        if (!registeredWinShiftS)
        {
            RegisterSingleHotkey(HotkeyConstants.HOTKEY_ID_QUICK_CAPTURE, HotkeyConstants.MOD_CONTROL | HotkeyConstants.MOD_SHIFT | HotkeyConstants.MOD_NOREPEAT, (uint)Keys.S);
        }
    }

    private bool RegisterSingleHotkey(int id, uint modifiers, uint vk)
    {
        return NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, vk);
    }

    public void UnregisterHotkeys()
    {
        if (_windowHandle == IntPtr.Zero) return;
        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyConstants.HOTKEY_ID_AREA);
        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyConstants.HOTKEY_ID_FULLSCREEN);
        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyConstants.HOTKEY_ID_ACTIVE_WINDOW);
        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyConstants.HOTKEY_ID_QUICK_CAPTURE);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == HotkeyConstants.WM_HOTKEY && !IsPaused)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HotkeyConstants.HOTKEY_ID_AREA:
                case HotkeyConstants.HOTKEY_ID_QUICK_CAPTURE:
                    HotkeyPressed?.Invoke(this, CaptureMode.SelectedArea);
                    handled = true;
                    break;
                case HotkeyConstants.HOTKEY_ID_FULLSCREEN:
                    HotkeyPressed?.Invoke(this, CaptureMode.FullScreen);
                    handled = true;
                    break;
                case HotkeyConstants.HOTKEY_ID_ACTIVE_WINDOW:
                    HotkeyPressed?.Invoke(this, CaptureMode.ActiveWindow);
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterHotkeys();
            _hwndSource?.RemoveHook(HwndHook);
            _hwndSource?.Dispose();
            _disposed = true;
        }
    }
}
