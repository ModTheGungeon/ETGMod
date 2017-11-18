using System;
using UnityEngine;
using Eluant;

namespace ETGMod.Lua {
    public class TriggerContainer : IDisposable {
        public ModLoader.ModInfo Info;
       
        public LuaFunction MainMenuLoadedFirstTime;
        public LuaFunction Unloaded;

        private Action<MainMenuFoyerController> _MainMenuLoadedFirstTime;

        private void _Trigger(LuaTable triggers, string name, ref LuaFunction func) {
            var v = triggers[name];
            if (v != null) {
                if (v is LuaFunction) {
                    func = v as LuaFunction;
                } else {
                    v.Dispose();
                }
            }
        }

        public TriggerContainer(LuaTable env, ModLoader.ModInfo info) {
            Info = info;

            using (var triggers = env["Triggers"] as LuaTable) {
                _Trigger(triggers, "MainMenuLoadedFirstTime", ref MainMenuLoadedFirstTime);
                _Trigger(triggers, "Unloaded", ref Unloaded);
            }
        }

        public void Dispose() {
            MainMenuLoadedFirstTime?.Dispose();
            Unloaded?.Dispose();

            RemoveExternalHooks();
        }

        public void RemoveExternalHooks() {
            if (_MainMenuLoadedFirstTime != null) EventHooks.MainMenuLoadedFirstTime -= _MainMenuLoadedFirstTime;

            _MainMenuLoadedFirstTime = null;
        }

        public void SetupExternalHooks() {
            if (MainMenuLoadedFirstTime != null && _MainMenuLoadedFirstTime == null) {
                _MainMenuLoadedFirstTime = (main_menu) => {
                    Info.RunLua(MainMenuLoadedFirstTime, "Events.MainMenuLoadedFirstTime", new LuaTransparentClrObject(main_menu, autobind: true));
                };
                EventHooks.MainMenuLoadedFirstTime += _MainMenuLoadedFirstTime;
            }
        }
    }
}
    