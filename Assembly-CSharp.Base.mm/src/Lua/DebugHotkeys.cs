using System;
using UnityEngine;
using System.Collections.Generic;
using Eluant;

namespace ETGMod.Lua {
    public class DebugHotkeyManager : IDisposable {
        public Dictionary<KeyCode, LuaFunction> FunctionMap = new Dictionary<KeyCode, LuaFunction>();

        public void AddFunction(KeyCode code, LuaFunction func) {
            func.DisposeAfterManagedCall = false;
            FunctionMap[code] = func;
        }

        public void Trigger(KeyCode code) {
            LuaFunction func;
            if (FunctionMap.TryGetValue(code, out func)) {
                func.Call();
            }
        }

        public void Dispose() {
            foreach (var pair in FunctionMap) {
                pair.Value.Dispose();
            }
        }
    }
}
