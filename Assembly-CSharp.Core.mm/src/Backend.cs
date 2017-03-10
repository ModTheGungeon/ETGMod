using System;
using System.Collections.Generic;
using System.Reflection;

namespace ETGMod {
    public class Backend {
        public struct Info {
            public string Name;
            public Version Version;
            public Type Type;
        }

        public static List<Info> AllBackends = new List<Info>();
        public static HashSet<string> AllBackendNames = new HashSet<string>();

        public static void AddBackendInfo(Info info) {
            AllBackends.Add(info);
            AllBackendNames.Add(info.Name);
        }

        public static Version Version = new Version(1, 0, 0);

        public static void Init() {
            DefaultLogger.Error("Backend does not override Init(), and therefore does not execute any code.");
        }
    }
}
