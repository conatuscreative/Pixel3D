using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Common.GlobalInput // Put this in a different namespace so it doesn't get pulled into network-sensitive gameplay state stuff
{
    [Flags]
    public enum Modifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4,
    }


    public static class Input
    {
        static bool active, lastActive;

        static KeyboardState ks, lastKs;
        static MouseState ms, lastMs;

        static GamePadState[] gps = new GamePadState[4];
        static GamePadState[] lastGps = new GamePadState[4];

        public static Modifiers Modifiers { get; private set; }
        
        public static void Update(bool isActive)
        {
            lastActive = isActive;
            active = isActive;

            lastKs = ks;
            ks = Keyboard.GetState();

            lastMs = ms;
            ms = Mouse.GetState();

            Array.Copy(gps, lastGps, gps.Length);
            for(int i = 0; i < 4; i++)
            {
                gps[i] = GamePad.GetState((PlayerIndex)i);
            }

            Modifiers = ((ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift))  ? Modifiers.Shift : 0)
                    | ((ks.IsKeyDown(Keys.LeftControl) || ks.IsKeyDown(Keys.RightControl)) ? Modifiers.Control : 0)
                    | ((ks.IsKeyDown(Keys.LeftAlt) || ks.IsKeyDown(Keys.RightAlt)) ? Modifiers.Alt : 0);
        }


        #region Accessors

        public static bool IsActive { get { return active; } }
        public static KeyboardState KeyboardState { get { return ks; } }
        public static GamePadState GamePadState(int playerIndex) { return gps[playerIndex]; }

        // Required for using buffered input for keyboard-based UI
        public static void SetKeyboardState(KeyboardState current, KeyboardState last)
        {
            ks = current;
            lastKs = last;
        }

        #endregion


        #region Keyboard

        public static bool IsKeyDown(Keys key)
        {
            return ks.IsKeyDown(key);
        }

        public static bool KeyWentDown(Keys key)
        {
            return ks.IsKeyDown(key) && lastKs.IsKeyUp(key) && active && lastActive;
        }

        public static bool KeyWentUp(Keys key)
        {
            return ks.IsKeyUp(key) && lastKs.IsKeyDown(key) && active && lastActive;
        }


        // With Modifiers:

        public static bool IsKeyDown(Keys key, Modifiers modifiers)
        {
            return IsKeyDown(key) && Modifiers == modifiers;
        }

        public static bool KeyWentDown(Keys key, Modifiers modifiers)
        {
            return KeyWentDown(key) && Modifiers == modifiers;
        }

        public static bool KeyWentUp(Keys key, Modifiers modifiers)
        {
            return KeyWentUp(key) && Modifiers == modifiers;
        }

        #endregion


        #region Mouse

        /// <summary>Mouse position in client space</summary>
        public static Point MousePosition { get { return new Point(ms.X, ms.Y); } }

        public static Point MouseDeltaPosition { get { return new Point(ms.X - lastMs.X, ms.Y - lastMs.Y); } }

        public static int MouseWheel { get { return (ms.ScrollWheelValue - lastMs.ScrollWheelValue) / 120; } }


        #region Left

        public static bool IsLeftMouseDown
        {
            get { return ms.LeftButton == ButtonState.Pressed; }
        }

        public static bool LeftMouseWentDown
        {
            get { return ms.LeftButton == ButtonState.Pressed && lastMs.LeftButton == ButtonState.Released; }
        }

        public static bool LeftMouseWentUp
        {
            get { return ms.LeftButton == ButtonState.Released && lastMs.LeftButton == ButtonState.Pressed; }
        }

        #endregion

        #region Right

        public static bool IsRightMouseDown
        {
            get { return ms.RightButton == ButtonState.Pressed; }
        }

        public static bool RightMouseWentDown
        {
            get { return ms.RightButton == ButtonState.Pressed && lastMs.RightButton == ButtonState.Released; }
        }

        public static bool RightMouseWentUp
        {
            get { return ms.RightButton == ButtonState.Released && lastMs.RightButton == ButtonState.Pressed; }
        }

        #endregion

        #region Middle

        public static bool IsMiddleMouseDown
        {
            get { return ms.MiddleButton == ButtonState.Pressed; }
        }

        public static bool MiddleMouseWentDown
        {
            get { return ms.MiddleButton == ButtonState.Pressed && lastMs.MiddleButton == ButtonState.Released; }
        }

        public static bool MiddleMouseWentUp
        {
            get { return ms.MiddleButton == ButtonState.Released && lastMs.MiddleButton == ButtonState.Pressed; }
        }

        #endregion

        #endregion


        #region Gamepad

        public static bool GamePadButtonWentDown(int playerIndex, Buttons button)
        {
            return gps[playerIndex].IsButtonDown(button) && !lastGps[playerIndex].IsButtonDown(button);
        }

        public static bool GamePadButtonWentUp(int playerIndex, Buttons button)
        {
            return !gps[playerIndex].IsButtonDown(button) && lastGps[playerIndex].IsButtonDown(button);
        }

        #endregion


        #region Helpful Input Interpreters

        public static bool Shift { get { return (Modifiers & Modifiers.Shift) != 0; } }
        public static bool Control { get { return (Modifiers & Modifiers.Control) != 0; } }
        public static bool Alt { get { return (Modifiers & Modifiers.Alt) != 0; } }


        public static Point GetSimpleMovement()
        {
            Point retval = new Point();

            if(IsKeyDown(Keys.A) || IsKeyDown(Keys.Left))
                retval.X -= 1;
            if(IsKeyDown(Keys.D) || IsKeyDown(Keys.Right))
                retval.X += 1;
            if(IsKeyDown(Keys.W) || IsKeyDown(Keys.Up))
                retval.Y += 1;
            if(IsKeyDown(Keys.S) || IsKeyDown(Keys.Down))
                retval.Y -= 1;

            return retval;
        }

        #endregion


        #region Character Input

        private static bool acceptingCharacters;
        private static readonly Queue<char> characterInputQueue = new Queue<char>();

        public static void StartCharacterInput()
        {
            acceptingCharacters = true;
            characterInputQueue.Clear();
        }

        public static void EndCharacterInput()
        {
            acceptingCharacters = false;
            characterInputQueue.Clear();
        }

        public static char GetNextCharacter()
        {
            if(characterInputQueue.Count > 0)
                return characterInputQueue.Dequeue();
            else
                return (char)0;
        }

        public static void EnqueueCharacter(char c)
        {
            if(c != 0 && acceptingCharacters)
                characterInputQueue.Enqueue(c);
        }

        public static void EnqueueCharacter(SignalChar c)
        {
            EnqueueCharacter((char)c);
        }

        #endregion


    }


    public enum SignalChar : ushort
    {
        None = 0,

        // Legitimate ASCII/Unicode Characters we'll actually get from WM_CHAR:
        Backspace = 8,
        Tab = 9,
        /// <summary>Carriage Return</summary>
        Enter = 13,
        /// <summary>Linefeed</summary>
        ShiftEnter = 10,
        Escape = 27,


        // Our own mapping, which is equal to the VK_ minus 22 (although probably noone should rely on that)
        PageUp = 11,
        PageDown = 12,
        End = 13,
        Home = 14,
        Left = 15,
        Up = 16,
        Right = 17,
        Down = 18,
        Insert = 23,
        Delete = 24,

        /// <summary>The last valid value for signal characters</summary>
        Last = 31 // <- useful characters start with space at 32
    }

}


