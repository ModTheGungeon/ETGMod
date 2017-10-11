using System;
using UnityEngine;
using NLua;

namespace ETGMod.Lua {
    public static class Globals {
        public static PlayerController PrimaryPlayer { get { return GameManager.Instance.PrimaryPlayer; } }
        public static PlayerController SecondaryPlayer { get { return GameManager.Instance.SecondaryPlayer; } }
    }
}    