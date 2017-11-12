using System;
using UnityEngine;
using Eluant;

namespace ETGMod.Lua {
    public class ModException : Exception {
        public ModException(string msg) : base(msg) {}
    }

    public class EventContainer : IDisposable {
        public ModLoader.ModInfo Info;
       
        public LuaFunction MainMenuLoadedFirstTime;
        public LuaFunction Unloaded;

        private void _Event(LuaTable env, string name, ref LuaFunction func) {
            var v = env[name];
            if (v != null) {
                if (v is LuaFunction) {
                    func = v as LuaFunction;
                } else {
                    v.Dispose();
                }
            }
        }

        public EventContainer(LuaTable env, ModLoader.ModInfo info) {
            Info = info;

            using (var events = env["Events"] as LuaTable) {
                _Event(env, "MainMenuLoadedFirstTime", ref MainMenuLoadedFirstTime);
                _Event(env, "Unloaded", ref Unloaded);
            }
        }

        public void Dispose() {
            MainMenuLoadedFirstTime.Dispose();
            Unloaded.Dispose();
        }

        public void SetupExternalHooks() {
            if (MainMenuLoadedFirstTime != null) {
                EventHooks.MainMenuLoadedFirstTime += (main_menu) => {
                    Info.RunLua(MainMenuLoadedFirstTime, "Events.MainMenuLoadedFirstTime", new LuaTransparentClrObject(main_menu, autobind: true));
                };
            }
        }
    }
}
    