using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using NLua;

namespace ETGMod {
    public partial class ETGMod : Backend {
        public const KeyCode MOD_RELOAD_KEY = KeyCode.F5;

        public override Version Version { get { return new Version(0, 3, 0); } }

        public static bool AutoReloadMods = true;
        private static bool _ShouldAutoReload = false;

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

        public static Action<string, Exception> ErrorLoadingMod = (filename, ex) => { };
        public static Action<Exception> ErrorCreatingModsDirectory = (ex) => { };

        private static string _FullVersion;
        public static string FullVersion {
            // extremely tiny optimization
            get {
                if (_FullVersion != null) return _FullVersion;
                return _FullVersion = $"{Instance.Version}-{VersionTag}";
            }
        }

        private FileSystemWatcher _FSWatcher;

        private bool _PrepareModsDirectory() {
            if (!Directory.Exists(Paths.ModsFolder)) {
                Logger.Debug($"Creating mods folder {Paths.ModsFolder}");
                try {
                    Directory.CreateDirectory(Paths.ModsFolder);
                } catch (Exception e) {
                    ErrorCreatingModsDirectory.Invoke(e);
                    if (e is IOException) {
                        Logger.Error($"The mods folder ({Paths.ModsFolder}) already exists, but is a file. Please remove or rename it.");
                    } else if (e is UnauthorizedAccessException) {
                        Logger.Error($"Insufficient permissions to create mods folder ({Paths.ModsFolder}).");
                    } else {
                        Logger.Error($"Unknown error while creating mods folder ({Paths.ModsFolder}): {e.Message}");
                    }
                    return false;
                }
            }
            return true;
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

        private ModLoader.ModInfo _LoadOrIgnoreIfBlacklisted(string mods_dir_entry, HashSet<string> blacklist) {
            var filename = Path.GetFileName(mods_dir_entry);
            if (blacklist.Contains(mods_dir_entry)) {
                Logger.Info($"Refusing to load blacklisted mod: {filename}");
                return null;
            }
            Logger.Info($"Loading mod: {filename}");
            try {
                return ModLoader.Load(mods_dir_entry);
            } catch (Exception e) {
                Logger.Error($"Exception while loading mod {filename}: [{e.GetType().Name}] {e.Message}");
                ErrorLoadingMod.Invoke(filename, e);

                foreach (var l in e.StackTrace.Split('\n')) {
                    Logger.ErrorIndent(l);
                }

                if (e.InnerException != null) {
                    Logger.ErrorIndent($"Inner exception: [{e.InnerException.GetType().Name}] {e.InnerException.Message}");
                    foreach (var l in e.InnerException.StackTrace.Split('\n')) Logger.ErrorIndent(l);
                }
            }
            return null;
        }

        private void _LoadMods() {
            ModLoader.UnloadAll();
            ModLoader.RefreshLuaState();
            if (!_PrepareModsDirectory()) return;
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

                var info = _LoadOrIgnoreIfBlacklisted(Path.Combine(Paths.ModsFolder, entry), blacklist);
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

        public void OnApplicationFocus() {
            if (_ShouldAutoReload) {
                Logger.Debug($"Focused, auto-reloading mods");
                _ShouldAutoReload = false;
                _ReloadMods();
            }
        }

        public override void Loaded() {
            Instance = this;

            Logger.Info($"Core ETGMod init {FullVersion}");
            Logger.Info($"Game folder: {Paths.GameFolder}");

            if (AutoReloadMods) {
                _FSWatcher = new FileSystemWatcher {
                    Path = Paths.ModsFolder,
                    NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime
                                                    | NotifyFilters.DirectoryName | NotifyFilters.FileName
                                                    | NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                                    | NotifyFilters.Security | NotifyFilters.Size,
                    IncludeSubdirectories = true,
                };

                var deleg = new FileSystemEventHandler((source, e) => _ShouldAutoReload = true);
                _FSWatcher.Changed += deleg;
                _FSWatcher.Created += deleg;
                _FSWatcher.Deleted += deleg;
                _FSWatcher.Renamed += (source, e) => _ShouldAutoReload = true;

                _FSWatcher.EnableRaisingEvents = true;
            }
        }

        public override void AllBackendsLoaded() {
            Logger.Info("Initializing ID pools");
            _InitIDs();

            Logger.Info("Initializing APIs");
            _InitAPIs();

            Logger.Info($"Loading mods from '{Paths.ModsFolder}'");
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

                Console.WriteLine($"{e.myGuid} {name}");
            }
        }

        private void _ReloadMods() {
            Logger.Info($"Reloading all backends and mods");

            foreach (var backend in AllBackends) {
                backend.Type.GetMethod("Reload").Invoke(backend.Instance, _EmptyObjectArray);
            }
            _LoadMods();
            foreach (var backend in AllBackends) {
                backend.Type.GetMethod("ReloadAfterMods").Invoke(backend.Instance, _EmptyObjectArray);
            }
        }

        private static object[] _EmptyObjectArray = { };
        public void Update() {
            if (Input.GetKeyDown(MOD_RELOAD_KEY)) _ReloadMods();
        }
    }
}
