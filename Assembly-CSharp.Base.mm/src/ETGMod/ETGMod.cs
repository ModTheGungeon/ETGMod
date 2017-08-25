using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ETGMod {
    public partial class ETGMod : Backend {
        const KeyCode MOD_RELOAD_KEY = KeyCode.F5;

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
        public static ModLoader ModLoader = new ModLoader(Paths.ModsFolder, Paths.CacheFolder);

        private static string _FullVersion;
        public static string FullVersion {
            // extremely tiny optimization
            get {
                if (_FullVersion != null) return _FullVersion;
                return _FullVersion = $"{Instance.Version}-{VersionTag}";
            }
        }

        private void _PrepareModsDirectory() {
            if (!Directory.Exists(Paths.ModsFolder)) {
                Logger.Debug($"Creating mods folder {Paths.ModsFolder}");
                try {
                    Directory.CreateDirectory(Paths.ModsFolder);
                } catch (IOException e) {
                    Logger.Error($"The mods folder ({Paths.ModsFolder}) already exists, but is a file. Please remove or rename it.");
                } catch (UnauthorizedAccessException e) {
                    Logger.Error($"Insufficient permissions to create mods folder ({Paths.ModsFolder}).");
                } catch (Exception e) {
                    Logger.Error($"Unknown error while creating mods folder ({Paths.ModsFolder}): {e.Message}");
                }
            }
        }

        private void _PrepareModLoadConfigFiles() {
            if (!File.Exists(Paths.ModsOrderFile)) {
                using (var file = File.Create(Paths.ModsOrderFile)) {
                    using (var writer = new StreamWriter(file)) {
                        writer.WriteLine("# Specify the order of loading mods here.");
                        writer.WriteLine("# First this file is read and all the mods specified here are loaded,");
                        writer.WriteLine("# then all the mods not in this file are loaded in an unspecified order.");
                        writer.WriteLine("# Lines starting with '#' are ignored.");
                    }
                }
            }
            if (!File.Exists(Paths.ModsBlacklistFile)) {
                using (var file = File.Create(Paths.ModsBlacklistFile)) {
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

            var entries = Directory.GetFileSystemEntries(Paths.ModsFolder);

            var order = new List<string>();
            var blacklist = new HashSet<string>();

            using (var file = File.Open(Paths.ModsOrderFile, FileMode.Open)) {
                using (var reader = new StreamReader(file)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        line = line.Trim();
                        if (line.StartsWithInvariant("#")) continue;
                        var file_path = Path.Combine(Paths.ModsFolder, line);
                        if (!File.Exists(file_path)) {
                            Logger.Warn($"Ordered mod {line} does not exist. Ignoring.");
                            continue;
                        }
                        order.Add(line);
                    }
                }
            }

            using (var file = File.Open(Paths.ModsBlacklistFile, FileMode.Open)) {
                using (var reader = new StreamReader(file)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        line = line.Trim();
                        if (line.StartsWithInvariant("#")) continue;
                        var file_path = Path.Combine(Paths.ModsFolder, line);
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

                _LoadOrIgnoreIfBlacklisted(Path.Combine(Paths.ModsFolder, entry), blacklist);
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
            Logger.Info($"Game folder: {Paths.GameFolder}");
        }

        public override void AllBackendsLoaded() {
            Logger.Info($"Loading mods from {Paths.ModsFolder}");

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
            if (Input.GetKeyDown(MOD_RELOAD_KEY)) {
                Logger.Info($"Reloading all mods");

                _LoadMods(); 
            }
        }

        public void FixedUpdate() {
            
        }
    }
}
