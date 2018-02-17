using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using YamlDotNet.Serialization;
using Mono.Cecil;
using System.Security.Cryptography;
using System.Linq;
using Eluant;
using ETGMod.Lua;

namespace ETGMod {
    public partial class ModLoader {
        public static Logger Logger = new Logger("ModLoader");
        private static ModuleDefinition _AssemblyCSharpModuleDefinition = ModuleDefinition.ReadModule(typeof(WingsItem).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));
        private static ModuleDefinition _UnityEngineModuleDefinition = ModuleDefinition.ReadModule(typeof(GameObject).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));

        const string METADATA_FILE_NAME = "mod.yml";

        public LuaRuntime LuaState { get; internal set; }

        public Deserializer Deserializer = new DeserializerBuilder().Build();
        public string CachePath;
        public string ModsPath;
        public GameObject GameObject;

        public string RelinkCachePath;
        public string UnpackCachePath;

        public List<ModInfo> LoadedMods = new List<ModInfo>();

        public enum LuaEventMethod {
            Loaded,
            Unloaded
        }

        public Action<ModInfo> PostLoadMod = (obj) => { };
        public Action<ModInfo> PostUnloadMod = (obj) => { };
        public Action<ModInfo, LuaEventMethod, Exception> LuaError = (obj, method, ex) => { };

        private Dictionary<string, ModuleDefinition> _AssemblyRelinkMap;
        public Dictionary<string, ModuleDefinition> AssemblyRelinkMap {
            get {
                if (_AssemblyRelinkMap != null) return _AssemblyRelinkMap;

                _AssemblyRelinkMap = new Dictionary<string, ModuleDefinition>();

                var entries = Directory.GetFileSystemEntries(Paths.ManagedFolder);

                for (int i = 0; i < entries.Length; i++) {
                    var full_entry = entries[i];
                    var entry = Path.GetFileName(full_entry);

                    if (full_entry.EndsWithInvariant(".mm.dll")) {
                        if (entry.StartsWithInvariant("Assembly-CSharp.")) {
                            _AssemblyRelinkMap[entry.RemoveSuffix(".dll")] = _AssemblyCSharpModuleDefinition;
                        } else if (entry.StartsWithInvariant("UnityEngine.")) {
                            _AssemblyRelinkMap[entry.RemoveSuffix(".dll")] = _UnityEngineModuleDefinition;
                        } else {
                            Logger.Debug($"Found MonoMod patch assembly {entry}, but it's neither a patch for Assembly-CSharp nor UnityEngine. Ignoring.");
                        }
                    }
                }

                return _AssemblyRelinkMap;
            }
        }


        public ModLoader(string modspath, string cachepath) {
            CachePath = cachepath;
            UnpackCachePath = Path.Combine(cachepath, "Unpack");
            RelinkCachePath = Path.Combine(cachepath, "Relink");
            ModsPath = modspath;
            GameObject = new GameObject("ETGMod Mod Loader");
            RefreshLuaState();
        }

        private LuaTable _CreateNewEnvironment(ModInfo info) {
            var f = LuaState.CompileFile(Path.Combine(Paths.ResourcesFolder, "lua/env.lua"));

            LuaState.Globals["MOD"] = new LuaTransparentClrObject(info, autobind: true);

            string prev_path;
            using (var t = LuaState.Globals["package"] as LuaTable) {
                prev_path = t["path"].ToString();
                t["path"] = Path.Combine(Paths.ResourcesFolder, "lua/?.lua");
            }

            LuaTable env;
            var ret = f.Call();

            if (ret.Count == 1) Logger.Debug($"Ran env.lua, got an environment");
            else if (ret.Count == 0) Logger.Error($"env.lua did not return anything", @throw: true);
            else Logger.Warn($"env.lua returned more than 1 result");

            using (var t = LuaState.Globals["package"] as LuaTable) {
                t["path"] = prev_path;
            }

            LuaState.Globals["MOD"] = null;

            env = ret[0] as LuaTable;
            if (ret.Count > 1) {
                for (int i = 1; i < ret.Count; i++) {
                    ret[i].Dispose();
                }
            }

            return env;
        }

        private void _SetupSandbox(LuaTable env) {
            using (var sandbox = LuaState.CompileFile(Path.Combine(Paths.ResourcesFolder, "lua/sandbox.lua"))) {
                sandbox.Call(env).Dispose();
            }
        }

        internal void RefreshLuaState() {
            if (LuaState != null) LuaState.Dispose();
            LuaState = new LuaRuntime();
            LuaState.MonoStackTraceWorkaround = true;
            // The version of Unity that Gungeon uses uses Mono 2.6.5, released in 2009
            // Read the comment on MonoStackTraceWorkaround to learn more
            LuaState.InitializeClrPackage();
        }

        public ModInfo Load(string path) {
            return Load(path, null);
        }

        private ModInfo Load(string path, ModInfo parent) {
            ModInfo mod;
            if (Directory.Exists(path)) {
                mod = _LoadFromDir(path);
            } else if (path.EndsWithInvariant(".zip")) {
                mod = _LoadFromZip(path);
            } else {
                throw new InvalidOperationException($"Mod type not suppored: {path}");
            }

            if (parent == null) LoadedMods.Add(mod);
            else {
                parent.EmbeddedMods.Add(mod);
                mod.Parent = parent;
                mod.Name = $"[{parent.ModMetadata.Name}] {mod.ModMetadata.Name}";
            }

            if (mod.ModMetadata.IsModPack) {
                _HandleModPack(mod);
            }

            if (mod.ModMetadata.HasScript && !File.Exists(Path.Combine(path, mod.ModMetadata.Script))) {
                throw new FileNotFoundException($"{mod.ModMetadata.Script} doesn't exist in unpacked mod directory {path}");
            }

            if (mod.ModMetadata.HasScript) {
                Logger.Debug($"Mod has script ({mod.ModMetadata.Script}), running");
                _RunModScript(mod);
            }

            PostLoadMod.Invoke(mod);
                
            if (!mod.IsComplete) throw new InvalidOperationException($"Tried to return incomplete ModInfo when loading {path}");
            return mod;
        }

        private static Action<LuaTable, string, LuaValue> _FakePackageNewindex = (self, key, value) => {
            throw new LuaException("I'm sorry, Dave.");
        };

        private static Func<LuaTable, string> _FakePackageMetatable = (self) => {
            return "I'm afraid I can't let you do that.";
        };

        private void _RunModScript(ModInfo info, ModInfo parent = null) {
            info.ScriptPath = Path.Combine(info.RealPath, info.ModMetadata.Script);
            Logger.Info($"Running Lua script at '{info.ScriptPath}'");

            if (parent != null) info.Parent = parent;

            info.Hooks = new HookManager();
            info.DebugHotkeys = new DebugHotkeyManager();

            info.ItemsDB = new ItemsDB();

            var env = _CreateNewEnvironment(info);


            using (var func = LuaState.CompileFile(info.ScriptPath)) {
                info.RealPackageTable = LuaState.CreateTable();

                using (var fake_package = LuaState.CreateTable())
                using (var mt = LuaState.CreateTable()) {
                    func.Environment = env;

                    /* Setup the metatable */
                    Func<LuaTable, string, LuaValue> fake_package_index = (self, key) => {
                        return info.RealPackageTable[key];
                    };

                    using (var index = LuaState.CreateFunctionFromDelegate(fake_package_index)) {
                        mt["__index"] = index;
                    }

                    using (var newindex = LuaState.CreateFunctionFromDelegate(_FakePackageNewindex)) {
                        mt["__newindex"] = newindex;
                    }

                    using (var metatable = LuaState.CreateFunctionFromDelegate(_FakePackageMetatable)) {
                        mt["__metatable"] = metatable;
                    }

                    fake_package.Metatable = mt;

                    /* Setup the real package table */
                    using (var loaded = LuaState.CreateTable()) info.RealPackageTable["loaded"] = loaded;
                    info.RealPackageTable["path"] = Path.Combine(Paths.ResourcesFolder, "lua/libs/?.lua") + ";" + Path.Combine(Paths.ResourcesFolder, "lua/libs/?/init.lua") + ";" + Path.Combine(Paths.ResourcesFolder, "lua/libs/?/?.lua") + ";" + info.RealPath + "/?.lua";
                    info.RealPackageTable["cpath"] = "Really makes you think";

                    /* Add the fake package with a locked metatable to the env */
                    env["package"] = fake_package;

                }

                info.LuaEnvironment = env;
                info.RunLua(func, "the main script");

                info.Triggers = new TriggerContainer(info.LuaEnvironment, info);
                info.Triggers.SetupExternalHooks();
            }
        }

        private string _HashPath(string path) {
            using (var md5 = MD5.Create()) {
                return Convert.ToBase64String(
                    md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(path))
                ).Replace('/', '_');
            }
        }

        private byte[] _HashContent(string inpath) {
            using (var infile = File.OpenRead(inpath)) {
                using (var md5 = MD5.Create()) {
                    return md5.ComputeHash(infile);
                }
            }
        }

        private void _HashContentIntoFile(string inpath, string outpath) {
            using (var outfile = File.OpenWrite(outpath)) {
                var checksum = _HashContent(inpath);

                outfile.Write(checksum, 0, checksum.Length);
            }

        }

        private string _PrepareZipUnpackPath(string zippath) {
            if (!Directory.Exists(UnpackCachePath)) Directory.CreateDirectory(UnpackCachePath);
            var hashedpath = _HashPath(zippath);
            var hashedcontent = _HashContent(zippath);
            var moddir = Path.Combine(UnpackCachePath, $"{hashedpath}.unpack");
            var modchecksum = Path.Combine(UnpackCachePath, $"{hashedpath}.sum");

            if (!File.Exists(modchecksum) || !File.ReadAllBytes(modchecksum).SequenceEqual(hashedcontent)) {
                Logger.Debug($"[{zippath}] ZIP unpack checksum doesn't match or doesn't exist, updating");

                using (var file = File.OpenWrite(modchecksum)) {
                    file.Write(hashedcontent, 0, hashedcontent.Length);
                }

                if (Directory.Exists(moddir)) Directory.Delete(moddir, recursive: true);
                Directory.CreateDirectory(moddir);
            }

            return moddir;
        }

        private void _Relink(string input, string output) {
            using (var modder = new MonoModder() {
                InputPath = input,
                OutputPath = output
            }) {
                modder.CleanupEnabled = false;

                modder.RelinkModuleMap = AssemblyRelinkMap;

                modder.ReaderParameters.ReadSymbols = false;
                modder.WriterParameters.WriteSymbols = false;
                modder.WriterParameters.SymbolWriterProvider = null;

                modder.Read();
                modder.MapDependencies();
                modder.AutoPatch();
                modder.Write();
            }
        }

        private string _PrepareRelinkPath(string asmpath) {
            if (!Directory.Exists(RelinkCachePath)) Directory.CreateDirectory(RelinkCachePath);
            var hashedpath = _HashPath(asmpath);
            var hashedcontent = _HashContent(asmpath);
            var asm = Path.Combine(RelinkCachePath, $"{hashedpath}.dll");
            var checksum = Path.Combine(RelinkCachePath, $"{hashedpath}.sum");

            if (!File.Exists(checksum) || !File.ReadAllBytes(checksum).SequenceEqual(hashedcontent)) {
                Logger.Debug($"[{asmpath}] Relink checksum doesn't match or doesn't exist, updating");

                using (var file = File.OpenWrite(checksum)) {
                    file.Write(hashedcontent, 0, hashedcontent.Length);
                }

                _Relink(asmpath, output: asm);
            }

            if (!File.Exists(asm)) {
                Logger.Debug("Relinked checksum exists and is valid, but the assembly doesn't exist (probably crashed while relinking) - invalidating checksum");
                File.Delete(checksum);
                return _PrepareRelinkPath(asmpath);
            }

            return asm;
        }

        public void Unload(ModInfo info) {
            UnloadAll(info.EmbeddedMods);

            Logger.Info($"Unloading mod {info.Name}");
            if (info.HasScript) {
                try {
                    info.Triggers?.Unloaded?.Call();
                } catch (LuaException e) {
                    Logger.Error(e.Message);
                    LuaError.Invoke(info, LuaEventMethod.Unloaded, e);

                    for (int i = 0; i < e.TracebackArray.Length; i++) {
                        Logger.ErrorIndent("  " + e.TracebackArray[i]);
                    }
                }
            }

            info.Dispose();

            info.EmbeddedMods = new List<ModInfo>();
            PostUnloadMod.Invoke(info);
        }

        public void UnloadAll(List<ModInfo> list) {
            for (int i = 0; i < list.Count; i++) {
                var info = list[i];
                Unload(info);
            }
        }

        public void UnloadAll() {
            UnloadAll(LoadedMods);
            LoadedMods = new List<ModInfo>();
        }

        private ModInfo _LoadFromZip(string path) {
            string unpacked_path = _PrepareZipUnpackPath(path);
            using (var zip = System.IO.Compression.ZipStorer.Open(path, FileAccess.Read)) {
                var dir = zip.ReadCentralDir();

                for (int i = 0; i < dir.Count; i++) {
                    var entry = dir[i];

                    var dirname = Path.GetDirectoryName(entry.FilenameInZip);
                    var outdir = Path.Combine(unpacked_path, dirname);
                    if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);

                    zip.ExtractFile(entry, Path.Combine(unpacked_path, entry.FilenameInZip));
                }
            }

            return _LoadFromDir(unpacked_path, new ModInfo.Metadata {
                Name = Path.GetFileNameWithoutExtension(path)
            }, path);
        }

        private void _HandleModPack(ModInfo info) {
            var modpackpath = Path.Combine(info.RealPath, info.ModMetadata.ModPackDir);
            if (!Directory.Exists(modpackpath)) {
                Logger.Error($"Mod pack folder {info.ModMetadata.ModPackDir} doesn't exist (in {info.RealPath}). Ignoring.");
            } else {
                var modpackentries = Directory.GetFileSystemEntries(modpackpath);
                for (int i = 0; i < modpackentries.Length; i++) {
                    var full_entry = modpackentries[i];

                    Load(full_entry, parent: info);
                }
            }
        }

        private ModInfo _LoadFromDir(string path, ModInfo.Metadata default_metadata = null, string original_path = null) {
            var info = new ModInfo {
                ModMetadata = default_metadata ?? new ModInfo.Metadata {
                    Name = Path.GetFileNameWithoutExtension(path),
                },
                RealPath = path,
                Path = original_path ?? path
            };

            var entries = Directory.GetFileSystemEntries(path);
            for (int i = 0; i < entries.Length; i++) {
                var full_entry = entries[i];

                if (Path.GetFileName(full_entry) == METADATA_FILE_NAME) {
                    using (var file = File.OpenRead(full_entry)) {
                        using (var reader = new StreamReader(file)) {
                            info.ModMetadata = Deserializer.Deserialize<ModInfo.Metadata>(reader);
                        }
                    }
                }
            }

            return info;
        }
    }
}