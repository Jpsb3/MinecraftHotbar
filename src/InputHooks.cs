using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MinecraftHotbar
{
    internal class InputHooks : IDisposable
    {
        private readonly HotbarForm _form;
        private IntPtr _mouseHookId = IntPtr.Zero;
        private IntPtr _keyboardHookId = IntPtr.Zero;
        private Native.HookProc _mouseProc;
        private Native.HookProc _keyboardProc;

        public InputHooks(HotbarForm form)
        {
            _form = form;
            _mouseProc = MouseHookCallback;
            _keyboardProc = KeyboardHookCallback;

            InstallHooks();
        }

        private void InstallHooks()
        {
            try
            {
                IntPtr modHandle = Native.GetModuleHandle(null);

                _mouseHookId = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _mouseProc, modHandle, 0);
                _keyboardHookId = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _keyboardProc, modHandle, 0);

                Log.Write($"Hooks instalados: Mouse={_mouseHookId != IntPtr.Zero}, Teclado={_keyboardHookId != IntPtr.Zero}");
            }
            catch (Exception ex)
            {
                Log.Write($"Error en InstallHooks: {ex.Message}");
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && wParam == (IntPtr)Native.WM_MOUSEWHEEL)
                {
                    if (_form != null && !_form.IsDisposed && _form.Visible)
                    {
                        var hookStruct = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
                        var pt = new System.Drawing.Point(hookStruct.pt.x, hookStruct.pt.y);

                        // Solo si el cursor está sobre la parte VISIBLE de la hotbar
                        // (la ventana es más alta que la barra; encima suele haber otras ventanas)
                        if (_form.ContainsScreenPoint(pt))
                        {
                            short wheelDelta = (short)((hookStruct.mouseData >> 16) & 0xffff);
                            int direction = wheelDelta > 0 ? -1 : 1;
                            _form.OnMouseWheelScroll(direction);
                            return (IntPtr)1; // Consumir evento para no afectar a ventanas de fondo
                        }
                    }
                }
            }
            catch { }

            return Native.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && (wParam == (IntPtr)Native.WM_KEYDOWN || wParam == (IntPtr)Native.WM_SYSKEYDOWN))
                {
                    if (_form != null && !_form.IsDisposed)
                    {
                        var hookStruct = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
                        uint vk = hookStruct.vkCode;

                        bool isEsc = (vk == 0x1B);
                        bool isE = (vk == 0x45);
                        bool isNum = (vk >= 0x31 && vk <= 0x39) || (vk >= 0x61 && vk <= 0x69);

                        // Tecla ESC: Ocultar hotbar si estorba sobre otras aplicaciones
                        if (isEsc)
                        {
                            if (!Native.AnyModifierDown())
                            {
                                if (_form.IsModalOpen)
                                {
                                    _form.HideHotbar();
                                    return (IntPtr)1;
                                }

                                if (_form.Visible && !_form.IsManuallyHidden)
                                {
                                    bool wasInteracting = Native.IsOwnWindowForeground() || _form.ContainsScreenPoint(Cursor.Position);
                                    _form.HideHotbar();
                                    if (wasInteracting) return (IntPtr)1;
                                    // Dejar pasar Esc para que la app activa (Chrome, VS Code, Discord) reciba su Esc normalmente
                                    return Native.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
                                }
                                else if (!_form.Visible || _form.IsManuallyHidden)
                                {
                                    if (Native.IsDesktopFocusedWithoutTyping())
                                    {
                                        _form.ShowHotbar();
                                        return (IntPtr)1;
                                    }
                                }
                            }
                        }
                        else if (isE)
                        {
                            if (!Native.AnyModifierDown() && Native.IsDesktopFocusedWithoutTyping())
                            {
                                if (!_form.Visible || _form.IsManuallyHidden)
                                {
                                    _form.ShowHotbar();
                                }
                                _form.OnToggleInventoryKey();
                                return (IntPtr)1;
                            }
                        }
                        else if (isNum && _form.Visible && !_form.IsManuallyHidden)
                        {
                            // Si la hotbar o el inventario están activos, el HTML recibe las teclas
                            // por sí mismo (así se puede escribir en los campos del inventario).
                            if (Native.IsOwnWindowForeground())
                                return Native.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);

                            // Atajos como Ctrl+1 o Win+1 nunca se tocan
                            if (Native.AnyModifierDown())
                                return Native.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);

                            // Solo se capturan cuando estás en el escritorio y no estás escribiendo.
                            if (Native.IsDesktopFocusedWithoutTyping())
                            {
                                int slotIndex = -1;
                                if (vk >= 0x31 && vk <= 0x39) slotIndex = (int)(vk - 0x31);
                                else if (vk >= 0x61 && vk <= 0x69) slotIndex = (int)(vk - 0x61);

                                if (slotIndex >= 0)
                                {
                                    _form.OnSelectSlotKey(slotIndex);
                                    return (IntPtr)1;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return Native.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_mouseHookId != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }

            if (_keyboardHookId != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }
        }
    }
}
