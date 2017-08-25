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

namespace ETGMod {
    public partial class ModLoader {
        public static Logger Logger = new Logger("ModLoader");
        public static string ModClassName = typeof(Mod).FullName;
        private static ModuleDefinition _AssemblyCSharpModuleDefinition = ModuleDefinition.ReadModule(typeof(WingsItem).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));
        private static ModuleDefinition _UnityEngineModuleDefinition = ModuleDefinition.ReadModule(typeof(GameObject).Assembly.Location, new ReaderParameters(ReadingMode.Immediate));

        const string METADATA_FILE_NAME = "mod.yml";

        public Deserializer Deserializer = new DeserializerBuilder().Build();
        public string CachePath;
        public string ModsPath;
        public GameObject GameObject;

        public string RelinkCachePath;
        public string UnpackCachePath;

        public List<ModInfo> LoadedMods = new List<ModInfo>();

        public Action<ModInfo> PostLoadMod = (obj) => {};
        public Action<ModInfo> PostUnloadMod = (obj) => { };

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
        }

        public ModInfo Load(string path, ModInfo parent = null) {
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

            if (mod.ModMetadata.HasDLL && !mod.AssemblyNameMap.ContainsPath(Path.Combine(path, mod.ModMetadata.DLL))) {
                throw new FileNotFoundException($"{mod.ModMetadata.DLL} doesn't exist in unpacked mod directory {path}");
            }

            if (mod.ModMetadata.HasDLL) {
                Logger.Debug($"Mod has DLL ({mod.ModMetadata.DLL}), injecting");
                _Inject(mod);
            }

            PostLoadMod.Invoke(mod);
                
            if (!mod.IsComplete) throw new InvalidOperationException($"Tried to return incomplete ModInfo when loading {path}");
            return mod;
        }

        private void _Inject(ModInfo info, ModInfo parent = null) {
            AppDomain.CurrentDomain.AssemblyResolve += info.AssemblyResolveHandler;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += info.AssemblyResolveHandler;

            info.AssemblyPath = _PrepareRelinkPath(Path.Combine(info.RealPath, info.ModMetadata.DLL));

            var types = info.Assembly.GetTypes();
            Logger.Debug("Scanning subclasses");
            for (int i = 0; i < types.Length; i++) {
                var type = types[i];

                if (type.BaseType.FullName == ModClassName) {
                    Logger.Debug($"Found Mod subclass: {type.FullName}");

                    var behaviour = (Mod)GameObject.AddComponent(type);
                    UnityEngine.Object.DontDestroyOnLoad(behaviour);
                    info.Behaviours.Add(behaviour);

                    behaviour.Info = info;
                }
            }

            if (parent != null) info.Parent = parent;
            for (int i = 0; i < info.Behaviours.Count; i++) {
                var behaviour = info.Behaviours[i];

                behaviour.Loaded();
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
            AppDomain.CurrentDomain.AssemblyResolve -= info.AssemblyResolveHandler;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= info.AssemblyResolveHandler;

            UnloadAll(info.EmbeddedMods);
            info.EmbeddedMods = new List<ModInfo>();

            for (int j = 0; j < info.Behaviours.Count; j++) {
                var behaviour = info.Behaviours[j];

                behaviour.Unloaded();
            }

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

                if (full_entry.EndsWithInvariant(".dll")) {
                    info.AssemblyNameMap.AddAssembly(full_entry);
                } else if (Path.GetFileName(full_entry) == METADATA_FILE_NAME) {
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