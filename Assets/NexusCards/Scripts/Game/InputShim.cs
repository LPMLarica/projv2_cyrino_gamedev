using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Nexus
{
    public enum GameKey { Space, Escape, H, M, R, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8 }

    public static class InputShim
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        public static Vector2 MousePosition
        {
            get { return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero; }
        }

        public static bool LeftClick
        {
            get { return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; }
        }

        public static bool RightClick
        {
            get { return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame; }
        }

        public static bool KeyDown(GameKey key)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (key)
            {
                case GameKey.Space: return kb.spaceKey.wasPressedThisFrame;
                case GameKey.Escape: return kb.escapeKey.wasPressedThisFrame;
                case GameKey.H: return kb.hKey.wasPressedThisFrame;
                case GameKey.M: return kb.mKey.wasPressedThisFrame;
                case GameKey.R: return kb.rKey.wasPressedThisFrame;
                case GameKey.Digit1: return kb.digit1Key.wasPressedThisFrame;
                case GameKey.Digit2: return kb.digit2Key.wasPressedThisFrame;
                case GameKey.Digit3: return kb.digit3Key.wasPressedThisFrame;
                case GameKey.Digit4: return kb.digit4Key.wasPressedThisFrame;
                case GameKey.Digit5: return kb.digit5Key.wasPressedThisFrame;
                case GameKey.Digit6: return kb.digit6Key.wasPressedThisFrame;
                case GameKey.Digit7: return kb.digit7Key.wasPressedThisFrame;
                case GameKey.Digit8: return kb.digit8Key.wasPressedThisFrame;
            }
            return false;
        }
#else
        public static Vector2 MousePosition
        {
            get { return Input.mousePosition; }
        }

        public static bool LeftClick
        {
            get { return Input.GetMouseButtonDown(0); }
        }

        public static bool RightClick
        {
            get { return Input.GetMouseButtonDown(1); }
        }

        public static bool KeyDown(GameKey key)
        {
            switch (key)
            {
                case GameKey.Space: return Input.GetKeyDown(KeyCode.Space);
                case GameKey.Escape: return Input.GetKeyDown(KeyCode.Escape);
                case GameKey.H: return Input.GetKeyDown(KeyCode.H);
                case GameKey.M: return Input.GetKeyDown(KeyCode.M);
                case GameKey.R: return Input.GetKeyDown(KeyCode.R);
                case GameKey.Digit1: return Input.GetKeyDown(KeyCode.Alpha1);
                case GameKey.Digit2: return Input.GetKeyDown(KeyCode.Alpha2);
                case GameKey.Digit3: return Input.GetKeyDown(KeyCode.Alpha3);
                case GameKey.Digit4: return Input.GetKeyDown(KeyCode.Alpha4);
                case GameKey.Digit5: return Input.GetKeyDown(KeyCode.Alpha5);
                case GameKey.Digit6: return Input.GetKeyDown(KeyCode.Alpha6);
                case GameKey.Digit7: return Input.GetKeyDown(KeyCode.Alpha7);
                case GameKey.Digit8: return Input.GetKeyDown(KeyCode.Alpha8);
            }
            return false;
        }
#endif

        public static int DigitPressed()
        {
            for (int i = 0; i < 8; i++)
                if (KeyDown((GameKey)((int)GameKey.Digit1 + i))) return i;
            return -1;
        }
    }
}
