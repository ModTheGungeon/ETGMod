using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Ionic.Zip;

namespace ETGMod.ModLoader {
    public class ModLoader {
        public string ModBaseDir = Path.Combine(Application.dataPath, "..").ToPlatformPath();

        public ModLoader(string path) {
            ModBaseDir = path;
        }

        public ModLoader() {}

        public List<Mod> LoadedMods;

        public class ModLoaderException : Exception {
            public ModLoaderException(string msg) : base(msg) {}
        }

        public void InjectMod(string path) {
            path = Path.Combine(ModBaseDir, path.ToPlatformPath());
            DefaultLogger.Info($"Loading mod from '{path}'");
            if (Directory.Exists(path)) {
                // DIRECTORY MOD
                //InjectModFromDir(path);
            } else if (File.Exists(path) && path.EndsWithInvariant(".zip")) {
                // ZIPPED MOD
                InjectModFromZip(path);
            }
            throw new ModLoaderException($"Invalid mod path: '{path}'");
        }

        public void InjectModFromZip(string path) {
            var metadata = new Metadata(); // default metadata

            using (var zip = ZipFile.Read(path)) {
                foreach (var entry in zip.Entries) {
                    switch (entry.FileName) {
                    case "metadata.txt":
                        using (var stream = new MemoryStream()) {
                            entry.Extract(stream);
                            stream.Seek(0, SeekOrigin.Begin);
                            metadata = new Metadata(stream);
                        }
                        break;
                    }
                }
            }

            for (int i = 0; i < metadata.Dependencies.Count; i++) {
                var dep = metadata.Dependencies[i];

                if (!Backend.AllBackendNames.Contains(dep.Name)) {
                    DefaultLogger.Warn($"No backend dependency {dep.Name} found for {metadata.Name}");
                } else {
                    DefaultLogger.Debug($"Dependency {dep.Name} found for {metadata.Name}");
                }
            }
        }
    }
}
