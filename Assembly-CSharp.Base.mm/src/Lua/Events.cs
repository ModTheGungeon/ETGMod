using System;
using UnityEngine;
using NLua;

namespace ETGMod.Lua {
    public class ModException : Exception {
        public ModException(string msg) : base(msg) {}
    }

    public class EventContainer {
        public ModLoader.ModInfo Info;
       
        public LuaFunction MainMenuLoadedFirstTime;
        public LuaFunction Unloaded;

        public EventContainer(NLua.Lua state, ModLoader.ModInfo info) {
            Info = info;

            var events = (LuaTable)state["Events"];
            if (events["MainMenuLoadedFirstTime"] != null) {
                MainMenuLoadedFirstTime = (LuaFunction)events["MainMenuLoadedFirstTime"];
            }

            if (events["Unloaded"] != null) {
                Unloaded = (LuaFunction)events["Unloaded"];
            }
        }
    }
}
    