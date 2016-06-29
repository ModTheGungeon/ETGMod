using System;
using System.Collections.Generic;

namespace UnityEngine {
    /// <summary>
    /// Hooks in the Unity Engine that are public and used by (at least) ETGMod.
    /// </summary>
    public static class ETGModUnityEngineHooks {

        public static string SkipSuffix = "/SKIPHOOK";

        public static Func<string, Type, Object> Load = (string path, Type type) => null;
        public static Func<string, Type, ResourceRequest> LoadAsync = (string path, Type type) => null;
        public static Func<string, Type, Object[]> LoadAll = (string path, Type type) => null;
        public static Func<Object, bool> UnloadAsset = (Object asset) => false;

        public static Func<Object, Object> Instantiate = (Object obj) => obj;

    }
}
