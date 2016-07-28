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

        private readonly static Type t_Object = typeof(UnityEngine.Object);
        private readonly static Type t_Texture = typeof(Texture);
        private readonly static Type t_Texture2D = typeof(Texture2D);

        public static Dictionary<string, AssetMetadata> Map = new Dictionary<string, AssetMetadata>();

        public static string RemoveExtension(string file) {
            return file.Substring(0, file.LastIndexOf('.'));
        }

        public static void Crawl(string dir, string root = null) {
            root = root ?? dir;
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++) {
                string file = files[i];
                Map[RemoveExtension(file.Substring(root.Length + 1))] = new AssetMetadata(file);
            }
            files = Directory.GetDirectories(dir);
            for (int i = 0; i < files.Length; i++) {
                Crawl(files[i], root);
            }
        }

        public static void Crawl(Assembly asm) {
            string[] resourceNames = asm.GetManifestResourceNames();
            for (int i = 0; i < resourceNames.Length; i++) {
                string name = resourceNames[i];
                Debug.Log(name);
                int indexOfContent = name.IndexOf("Content");
                if (indexOfContent < 0) {
                    continue;
                }
                name = name.Substring(indexOfContent + 8).ToLowerInvariant();

                Type type = t_Object;

                if (name.EndsWith(".fmb") ||
                    name.EndsWith(".bin")) {
                    name = name.Substring(0, name.Length - 4);

                } else if (name.EndsWith(".png")) {
                    type = t_Texture2D;
                    name = name.Substring(0, name.Length - 4);

                } else {
                    name = name.Substring(0, name.LastIndexOf('.'));
                }

                // Folders are dots... for some reason.
                name = name.Replace('\\', '/').Replace('.', '/');

                // Good news: Embedded resources get their spaces replaced with underscores and folders are marked with dots.
                // As we don't know what was there and what not, add all combos!
                AssetMetadata metadata = new AssetMetadata(asm, resourceNames[i]) {
                    AssetType = type
                };
                string[] split = name.Split('_');
                int combos = (int) Math.Pow(2, split.Length - 1);
                for (int ci = 0; ci < combos; ci++) {
                    string rebuiltname = split[0];
                    for (int si = 1; si < split.Length; si++) {
                        rebuiltname += ci % (si + 1) == 0 ? "_" : " ";
                        rebuiltname += split[si];
                    }
                    Map[rebuiltname] = metadata;
                }
            }
        }

        public static void Hook() {
            ETGModUnityEngineHooks.Load = Load;
            // ETGModUnityEngineHooks.LoadAsync = LoadAsync;
            // ETGModUnityEngineHooks.LoadAll = LoadAll;
            // ETGModUnityEngineHooks.UnloadAsset = UnloadAsset;
        }

        public static UnityEngine.Object Load(string path, Type type) {
            if (path == "PlayerCoopCultist") {
                Debug.Log("LOADHOOK Loading resource \"" + path + "\" of (requested) type " + type);

                return Resources.Load(Player.CoopReplacement ?? (path + ETGModUnityEngineHooks.SkipSuffix), type) as GameObject;
            }

            AssetMetadata metadata;
                 if (Map.TryGetValue(path,                    out metadata)) { }
            else if (Map.TryGetValue(path.ToLowerInvariant(), out metadata)) { }
            else { return null; }

            // TODO load and parse data from metadata
            if (t_Texture.IsAssignableFrom(type) ||
                type == t_Texture2D ||
                (type == t_Object && metadata.AssetType == t_Texture2D)) {
                Texture2D tex = new Texture2D(2, 2);
                tex.name = path;
                tex.LoadImage(metadata.Data);
                return tex;
            }

            Debug.Log("Metadata:" + metadata);
            
            return null;
        }

        public static void HandleSprite(tk2dBaseSprite sprite) {
            Material[] materials = sprite.Collection.materials;
            for (int i = 0; i < materials.Length; i++) {
                Texture2D texOrig = materials[i].mainTexture as Texture2D;
                Texture2D tex = new Texture2D(texOrig.width, texOrig.height, texOrig.format, 1 < texOrig.mipmapCount, texOrig.filterMode != FilterMode.Point);
                Color[] data = tex.GetPixels();

                for (int y = 0; y < tex.height; y++) {
                    for (int x = 0; x < tex.width; x++) {
                        int p = x + y * tex.width;
                        data[p] = new Color(x / (float) tex.width, y / (float) tex.height, 1f, 1f);
                    }
                }

                tex.SetPixels(data);
                tex.Apply(true, false);
                materials[i].mainTexture = tex;
            }
        }
    }

    public static tk2dBaseSprite Handle(this tk2dBaseSprite sprite) {
        Assets.HandleSprite(sprite);
        return sprite;
    }

    public static void MapAssets(this Assembly asm) {
        Assets.Crawl(asm);
    }

}
