// Boss 1 (Yan: the parry is a "dedicated key, customizable in options but default can be F"): the keys a player can change in
// Options (MainMenuUI), saved in PlayerPrefs. Parry took F, so search moved to G. ponytail: only these two are rebindable;
// the rest stay fixed until the options screen grows a full controls list.
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCore
{
    public static class Keybinds
    {
        public static Key Parry = Load("parry", Key.F), Search = Load("search", Key.G);

        static Key Load(string name, Key fallback) => (Key)PlayerPrefs.GetInt("key_" + name, (int)fallback);

        public static void Set(string name, Key key)
        {
            if (name == "parry") Parry = key; else Search = key;
            PlayerPrefs.SetInt("key_" + name, (int)key); PlayerPrefs.Save();
        }

        public static bool Pressed(Keyboard k, Key key) => k != null && key != Key.None && k[key] != null && k[key].wasPressedThisFrame;

        /// <summary>The key pressed this frame, if any (for rebinding).</summary>
        public static Key AnyPressed(Keyboard k)
        {
            if (k == null) return Key.None;
            foreach (var c in k.allKeys) if (c != null && c.wasPressedThisFrame) return c.keyCode;
            return Key.None;
        }
    }
}
