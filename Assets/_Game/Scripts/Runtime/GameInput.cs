using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Tiny shared input state written by the on-screen controls and read by gameplay code.</summary>
    public static class GameInput
    {
        public static Vector2 Move;
        public static bool Blocked;      // true while a dialog / menu is open
        private static bool interact;

        public static void RequestInteract() { interact = true; }

        public static bool ConsumeInteract()
        {
            bool v = interact;
            interact = false;
            return v;
        }

        public static void Reset()
        {
            Move = Vector2.zero;
            Blocked = false;
            interact = false;
        }
    }
}
