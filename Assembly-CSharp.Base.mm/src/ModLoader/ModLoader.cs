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
using NLua;
using ETGMod.Lua;

namespace ETGMod {
    public partial class ModLoader {
        public static Logger Logger = new Logger("ModLoader");
        private static ModuleDefinition _AssemblyCSharpModuleDefinition = ModuleDefinition.ReadModule(typeof(WingsItem).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));
        private static ModuleDefinition _UnityEngineModuleDefinition = ModuleDefinition.ReadModule(typeof(GameObject).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));

        const string METADATA_FILE_NAME = "mod.yml";

        public NLua.Lua LuaState { get; internal set; }

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

        private LuaTable _InitializeLuaState() {
            var f = LuaState.LoadFile(Path.Combine(Paths.ResourcesFolder, "lua/env.lua"));

            var prev_path = ((LuaTable)LuaState["package"])["path"];
            ((LuaTable)LuaState["package"])["path"] = Path.Combine(Paths.ResourcesFolder, "lua/?.lua");

            var ret = f.Call();

            if (ret.Length == 1) Logger.Debug($"Ran env.lua, got an environment");
            else if (ret.Length == 0) Logger.Error($"env.lua did not return anything", @throw: true);
            else Logger.Warn($"env.lua returned more than 1 result");

            ((LuaTable)LuaState["package"])["path"] = prev_path;

            return (LuaTable)(ret[0]);
        }

        private void _SetupSandbox(LuaTable env) {
            var f = LuaState.LoadFile(Path.Combine(Paths.ResourcesFolder, "lua/sandbox.lua"));
        }

        public void RefreshLuaState() {
            if (LuaState != null) LuaState.Dispose();
            LuaState = new NLua.Lua();
            LuaState.LoadCLRPackage();
            //_InitializeLuaState();
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

        private void _RunModScript(ModInfo info, ModInfo parent = null) {
            info.ScriptPath = Path.Combine(info.RealPath, info.ModMetadata.Script);
            Logger.Info($"Running Lua script at '{info.ScriptPath}'");

            if (parent != null) info.Parent = parent;

            var func = LuaState.LoadFile(info.ScriptPath);
            var env = _InitializeLuaState();

            env["Mod"] = info;
            env["Logger"] = info.Logger;

            env["package"] = LuaState.NewTable();
            var mt = LuaState.NewTable();
            var real_package = LuaState.NewTable();
                
            LuaState.DoString(@"
                local __etgmod_fake_package_mt = {}
                __etgmod_real_package = {
                    loaded = {}
                }
                __etgmod_fake_package = setmetatable({}, __etgmod_fake_package_mt)

                local real = __etgmod_real_package
                local _error = error

                function __etgmod_fake_package_mt.__index(self, key)
                    return real[key]
                end

                function __etgmod_fake_package_mt.__newindex(self, key, value)
                    _error('Modifying the package table is forbidden.')
                end

                function __etgmod_fake_package_mt.__metatable(self, key)
                    return nil
                end
            ");

            ((LuaTable)LuaState["__etgmod_real_package"])["path"] = Path.Combine(Paths.ResourcesFolder, "lua/libs/?.lua") + ";" + Path.Combine(Paths.ResourcesFolder, "lua/libs/?/init.lua") + ";" + Path.Combine(Paths.ResourcesFolder, "lua/libs/?/?.lua") + ";" + info.RealPath + "/?.lua";
            ((LuaTable)LuaState["__etgmod_real_package"])["cpath"] = "";

            env["package"] = LuaState["__etgmod_fake_package"];

            LuaState.DoString(@"
                __etgmod_real_package = nil
                __etgmod_fake_package = nil
            ");

            info.LuaEnvironment = env;

            info.RunLua(func, "the main script");

            info.Events = new EventContainer(info.LuaEnvironment, info);
            info.Events.SetupExternalHooks();
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
            if (info.HasScript) {
                try {
                    info.Events?.Unloaded?.Call();
                } catch (Exception e) {
                    Logger.Error($"Error while calling the Unloaded method in Lua mod: [{e.GetType().Name}] {e.Message}");
                    LuaError.Invoke(info, LuaEventMethod.Unloaded, e);
                    foreach (var l in e.StackTrace.Split('\n')) Logger.ErrorIndent(l);

                    if (e.InnerException != null) {
                        Logger.ErrorIndent($"Inner exception: [{e.InnerException.GetType().Name}] {e.InnerException.Message}");
                        foreach (var l in e.InnerException.StackTrace.Split('\n')) Logger.ErrorIndent(l);
                    }
                }
            }
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