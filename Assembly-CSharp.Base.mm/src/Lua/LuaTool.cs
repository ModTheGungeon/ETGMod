using System;
using UnityEngine;
using NLua;

namespace ETGMod.Lua {
    public static class LuaTool {
        public static bool IsType<T>(object o) {
            return o is T;
        }

        public static T AssertType<T>(object o, string name) {
            if (!(o is T)) {
                throw new NLua.Exceptions.LuaException($"Expected type {typeof(T).Name} for {name}, got {o.GetType().Name}");
            }
            return (T)o;
        }
    }
}