using System;
using System.IO;
using UnityEngine;

namespace ETGMod {
    public static class Paths {
        private static string _GameFolder;
        public static string GameFolder {
            get {
                if (_GameFolder != null) return _GameFolder;
                return _GameFolder = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            }
        }

        private static string _ManagedFolder;
        public static string ManagedFolder {
            get {
                if (_ManagedFolder != null) return _ManagedFolder;
                return _ManagedFolder = Path.GetDirectoryName(typeof(ETGMod.Paths).Assembly.Location);
            }
        }

        private static string _ResourcesFolder;
        public static string ResourcesFolder {
            get {
                if (_ResourcesFolder != null) return _ResourcesFolder;
                return _ResourcesFolder = Path.Combine(ManagedFolder, "ETGMod/Resources");
            }
        }

        private static string _ModsFolder;
        public static string ModsFolder {
            get {
                if (_ModsFolder != null) return _ModsFolder;
                return _ModsFolder = Path.Combine(GameFolder, "Mods");
            }
        }

        private static string _CacheFolder;
        public static string CacheFolder {
            get {
                if (_CacheFolder != null) return _CacheFolder;
                return _CacheFolder = Path.Combine(GameFolder, ".ETGModCache");
            }
        }

        private static string _ModsOrderFile;
        public static string ModsOrderFile {
            get {
                if (_ModsOrderFile != null) return _ModsOrderFile;
                return _ModsOrderFile = Path.Combine(ModsFolder, "order.txt");
            }
        }

        private static string _ModsBlacklistFile;
        public static string ModsBlacklistFile {
            get {
                if (_ModsBlacklistFile != null) return _ModsBlacklistFile;
                return _ModsBlacklistFile = Path.Combine(ModsFolder, "blacklist.txt");
            }
        }

        private static string _ModsCacheFolder;
        public static string ModsCacheFolder {
            get {
                if (_ModsCacheFolder != null) return _ModsCacheFolder;
                return _ModsCacheFolder = Path.Combine(GameFolder, ".ModRelinkCache");
            }
        }
    }
}
