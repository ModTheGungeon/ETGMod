using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod asset management.
    /// </summary>
    public static class Assets {

        public static Dictionary<string, ETGModAssetMetadata> Map = new Dictionary<string, ETGModAssetMetadata>();

        public static string RemoveExtension(string file) {
            return file.Substring(0, file.LastIndexOf('.'));
        }

        public static void Crawl(string dir, string root = null) {
            root = root ?? dir;
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++) {
                string file = files[i];
                Map[RemoveExtension(file.Substring(root.Length + 1))] = new ETGModAssetMetadata(file);
            }
            files = Directory.GetDirectories(dir);
            for (int i = 0; i < files.Length; i++) {
                Crawl(files[i], root);
            }
        }

        public static void Hook() {
            ETGModUnityEngineHooks.Load = Load;
            // ETGModUnityEngineHooks.LoadAsync = LoadAsync;
            // ETGModUnityEngineHooks.LoadAll = LoadAll;
            // ETGModUnityEngineHooks.UnloadAsset = UnloadAsset;
        }

        private readonly static Type t_Texture2D = typeof(Texture2D);
        public static UnityEngine.Object Load(string path, Type type) {
            ETGModAssetMetadata metadata;
            if (!Map.TryGetValue(path, out metadata)) {
                return null;
            }
            
            if (type == t_Texture2D) {
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(metadata.Data);
                return tex;
            }

            return null;
        }

    }

}
