using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ETGMod {
    public partial class ETGMod : Backend {
        public override Version Version { get { return new Version(0, 3, 0); } }

        public static Logger Logger = new Logger("ETGMod");

#if DEBUG
        public static string VersionTag = "DEBUG";
#elif RELEASE
        public static string VersionTag = "";
#endif

        public override string StringVersion {
            get {
                return $"{Version}-{VersionTag}";
            }
        }


        public static ETGMod Instance;
        public static ModLoader ModLoader = new ModLoader(ModsFolder, CacheFolder);

        private static string _FullVersion;
        public static string FullVersion {
            // extremely tiny optimization
            get {
                if (_FullVersion != null) return _FullVersion;
                return _FullVersion = $"{Instance.Version}-{VersionTag}";
            }
        }

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
                return _ManagedFolder = Path.GetDirectoryName(typeof(ETGMod).Assembly.Location);
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

        private void _PrepareModsDirectory() {
            if (!Directory.Exists(ModsFolder)) {
                Logger.Debug($"Creating mods folder {ModsFolder}");
                try {
                    Directory.CreateDirectory(ModsFolder);
                } catch (IOException e) {
                    Logger.Error($"The mods folder ({ModsFolder}) already exists, but is a file. Please remove or rename it.");
                } catch (UnauthorizedAccessException e) {
                    Logger.Error($"Insufficient permissions to create mods folder ({ModsFolder}).");
                } catch (Exception e) {
                    Logger.Error($"Unknown error while creating mods folder ({ModsFolder}): {e.Message}");
                }
            }
        }

        private void _PrepareModLoadConfigFiles() {
            if (!File.Exists(ModsOrderFile)) {
                using (var file = File.Create(ModsOrderFile)) {
                    using (var writer = new StreamWriter(file)) {
                        writer.WriteLine("# Specify the order of loading mods here.");
                        writer.WriteLine("# First this file is read and all the mods specified here are loaded,");
                        writer.WriteLine("# then all the mods not in this file are loaded in an unspecified order.");
                        writer.WriteLine("# Lines starting with '#' are ignored.");
                    }
                }
            }
            if (!File.Exists(ModsBlacklistFile)) {
                using (var file = File.Create(ModsBlacklistFile)) {
                    using (var writer = new StreamWriter(file)) {
                        writer.WriteLine("# Specify blacklisted mods here.");
                        writer.WriteLine("# Any mods specified here will not be loaded, even if they are specified");
                        writer.WriteLine("# in order.txt.");
                        writer.WriteLine("# Lines starting with '#' are ignored.");
                    }
                }
            }
        }

        private void _LoadOrIgnoreIfBlacklisted(string mods_dir_entry, HashSet<string> blacklist) {
            var filename = Path.GetFileName(mods_dir_entry);
            if (blacklist.Contains(mods_dir_entry)) {
                Logger.Info($"Refusing to load blacklisted mod: {filename}");
                return;
            }
            Logger.Info($"Loading mod: {filename}");
            ModLoader.Load(mods_dir_entry);
        }

        private void _LoadMods() {
            ModLoader.UnloadAll();
            _PrepareModsDirectory();
            _PrepareModLoadConfigFiles();

            var entries = Directory.GetFileSystemEntries(ModsFolder);

            var order = new List<string>();
            var blacklist = new HashSet<string>();

            using (var file = File.Open(ModsOrderFile, FileMode.Open)) {
                using (var reader = new StreamReader(file)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        line = line.Trim();
                        if (line.StartsWithInvariant("#")) continue;
                        var file_path = Path.Combine(ModsFolder, line);
                        if (!File.Exists(file_path)) {
                            Logger.Warn($"Ordered mod {line} does not exist. Ignoring.");
                            continue;
                        }
                        order.Add(line);
                    }
                }
            }

            using (var file = File.Open(ModsBlacklistFile, FileMode.Open)) {
                using (var reader = new StreamReader(file)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        line = line.Trim();
                        if (line.StartsWithInvariant("#")) continue;
                        var file_path = Path.Combine(ModsFolder, line);
                        if (!File.Exists(file_path)) {
                            Logger.Warn($"Blacklisted mod {line} does not exist. Ignoring.");
                            continue;
                        }
                        blacklist.Add(file_path);
                    }
                }
            }

            // First load the mods in order
            for (int i = 0; i < order.Count; i++) {
                var entry = order[i];

                _LoadOrIgnoreIfBlacklisted(Path.Combine(ModsFolder, entry), blacklist);
            }

            for (int i = 0; i < entries.Length; i++) {
                var entry = entries[i];
                if (entry.EndsWithInvariant(".txt") || order.Contains(entry)) {
                    // if entry was in order.txt and is already loaded, or ends with .txt...
                    continue; // ...skip this iteration
                }

                _LoadOrIgnoreIfBlacklisted(entry, blacklist);
            }
        }

        public override void Loaded() {
            Instance = this;

            Logger.Info($"Core ETGMod init {FullVersion}");
            Logger.Info($"Game folder: {GameFolder}");
        }

        public override void AllBackendsLoaded() {
            Logger.Info($"Loading mods from {ModsFolder}");

            _LoadMods();
        }

        public void Awake() {
            System.Console.WriteLine("ENEMY OBJECTS");
            for (int i = 0; i < EnemyDatabase.Instance.Entries.Count; i++) {
                var e = EnemyDatabase.Instance.Entries[i];

                var name = "[ERROR]";

                if (e == null) {
                    name = "[NULL OBJECT]";
                } else {
                    try {
                        var o = EnemyDatabase.GetOrLoadByGuid(e.myGuid);
                        var pdn = o.encounterTrackable?.journalData?.PrimaryDisplayName;
                        name = pdn != null ? StringTableManager.GetEnemiesString(pdn) : o.ActorName ?? "[NULL NAME]"; 
                    } catch { }
                }

                System.Console.WriteLine($"{e.myGuid} {name}");
            }
        }

        public void Update() {
            
        }

        public void FixedUpdate() {
            
        }
    }
}
