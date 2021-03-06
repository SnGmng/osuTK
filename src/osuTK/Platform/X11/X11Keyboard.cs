//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2010 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osuTK.Input;

namespace osuTK.Platform.X11
{
    // Standard keyboard driver that relies on xlib input events.
    // Only one keyboard supported.
    internal sealed class X11Keyboard : IKeyboardDriver2
    {
        private readonly static string name = "Core X11 keyboard";
        private readonly byte[] keys = new byte[32];
        private readonly int KeysymsPerKeycode;
        private readonly X11KeyMap KeyMap;
        private KeyboardState state = new KeyboardState();

        public X11Keyboard()
        {
            Debug.WriteLine("Using X11Keyboard.");
            state.IsConnected = true;

            IntPtr display = API.DefaultDisplay;
            using (new XLock(display))
            {
                // Find the number of keysyms per keycode.
                int first = 0, last = 0;
                API.DisplayKeycodes(display, ref first, ref last);
                IntPtr keysym_ptr =
                    API.GetKeyboardMapping(display,
                        (byte)first,
                        last - first + 1,
                        ref KeysymsPerKeycode);
                Functions.XFree(keysym_ptr);

                if (Xkb.IsSupported(display))
                {
                    // Request that auto-repeat is only set on devices that support it physically.
                    // This typically means that it's turned off for keyboards what we want).
                    // We prefer this method over XAutoRepeatOff/On, because the latter needs to
                    // be reset before the program exits.
                    bool supported;
                    Xkb.SetDetectableAutoRepeat(display, true, out supported);
                    KeyMap = new X11KeyMap(display);
                }
            }
        }

        public KeyboardState GetState()
        {
            ProcessEvents();
            return state;
        }

        public KeyboardState GetState(int index)
        {
            // X11Keyboard supports a single keyboard only
            ProcessEvents();
            if (index == 0)
            {
                return state;
            }
            else
            {
                return new KeyboardState();
            }
        }

        public void GetStates(List<KeyboardState> result)
        {
            result.Clear();
            for (int i = 0; i < 1; i++)
            {
                result.Add(GetState(i));
            }
        }

        public string GetDeviceName(int index)
        {
            if (index == 0)
            {
                return name;
            }
            else
            {
                return String.Empty;
            }
        }

        private void ProcessEvents()
        {
            IntPtr display = API.DefaultDisplay;
            using (new XLock(display))
            {
                Functions.XQueryKeymap(display, keys);
                for (int keycode = 0; keycode < 256; keycode++)
                {
                    bool pressed = (keys[keycode >> 3] >> (keycode & 0x07) & 0x01) != 0;
                    Key key;
                    if (KeyMap.TranslateKey(keycode, out key))
                    {
                        state[key] = pressed;
                    }
                }
            }
        }
    }
}

