using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod asset management.
    /// </summary>
    public static class Assets {

        private readonly static Type t_Object = typeof(UnityEngine.Object);
        private readonly static Type t_Texture = typeof(Texture);
        private readonly static Type t_Texture2D = typeof(Texture2D);

        public static Dictionary<string, AssetMetadata> Map = new Dictionary<string, AssetMetadata>();

        public static bool TryGetMapped(string path, out AssetMetadata metadata) {
            string diskPath = Path.Combine(ResourcesDirectory, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            if (File.Exists(diskPath)) {
                metadata = Map[path] = new AssetMetadata(diskPath);
                return true;
            }

            if (Map.TryGetValue(path                   , out metadata)) { return true; }
            if (Map.TryGetValue(path.ToLowerInvariant(), out metadata)) { return true; }

            return false;
        }

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
                int indexOfContent = name.IndexOfInvariant("Content");
                if (indexOfContent < 0) {
                    continue;
                }
                name = name.Substring(indexOfContent + 8).ToLowerInvariant();

                Type type = t_Object;

                if (name.EndsWithInvariant(".png")) {
                    type = t_Texture2D;
                    name = name.Substring(0, name.Length - 4);

                }

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
                    Console.WriteLine("REBUILT: " + rebuiltname);
                    Map[rebuiltname] = metadata;
                }
            }
        }

        public static void Hook() {
            if (!Directory.Exists(ResourcesDirectory)) {
                Debug.Log("Resources directory not existing, creating...");
                Directory.CreateDirectory(ResourcesDirectory);
            }

            ETGModUnityEngineHooks.Load = Load;
            // ETGModUnityEngineHooks.LoadAsync = LoadAsync;
            // ETGModUnityEngineHooks.LoadAll = LoadAll;
            // ETGModUnityEngineHooks.UnloadAsset = UnloadAsset;
        }

        public static UnityEngine.Object Load(string path, Type type) {
            if (path == "PlayerCoopCultist" && Player.CoopReplacement != null) {
                Debug.Log("LOADHOOK Loading resource \"" + path + "\" of (requested) type " + type);

                return Resources.Load(Player.CoopReplacement, type) as GameObject;
            }

            /*
            string dumpdir = Path.Combine(Application.streamingAssetsPath.Replace('/', Path.DirectorySeparatorChar), "DUMP");
            // JSONHelper.SharedDir = Path.Combine(dumpdir, "SHARED");
            string dumppath = Path.Combine(dumpdir, path.Replace('/', Path.DirectorySeparatorChar) + ".json");
            if (!File.Exists(dumppath)) {
                UnityEngine.Object obj = Resources.Load(path + ETGModUnityEngineHooks.SkipSuffix);
                if (obj != null) {
                    Directory.GetParent(dumppath).Create();
                    Console.WriteLine("JSON WRITING " + path);
                    obj.WriteJSON(dumppath);
                    Console.WriteLine("JSON READING " + path);
                    object testobj = JSONHelper.ReadJSON(dumppath);
                    JSONHelper.LOG = false;

                    dumpdir = Path.Combine(Application.streamingAssetsPath.Replace('/', Path.DirectorySeparatorChar), "DUMPA");
                    // JSONHelper.SharedDir = Path.Combine(dumpdir, "SHARED");
                    dumppath = Path.Combine(dumpdir, path.Replace('/', Path.DirectorySeparatorChar) + ".json");
                    Directory.GetParent(dumppath).Create();
                    Console.WriteLine("JSON REWRITING " + path);
                    testobj.WriteJSON(dumppath);
                    Console.WriteLine("JSON REREADING " + path);
                    testobj = JSONHelper.ReadJSON(dumppath);
                    JSONHelper.LOG = false;
                    Console.WriteLine("JSON DONE " + path);
                }
            }
            JSONHelper.SharedDir = null;
            */

            AssetMetadata metadata;
            bool isJson = false;
            bool isPatch = false;
                 if (TryGetMapped(path, out metadata)) { }
            else if (TryGetMapped(path + ".json", out metadata)) { isJson = true; }
            else if (TryGetMapped(path + ".patch.json", out metadata)) { isPatch = true; isJson = true; }
            else { return null; }

            if (isJson) {
                if (isPatch) {
                    UnityEngine.Object obj = Resources.Load(path + ETGModUnityEngineHooks.SkipSuffix);
                    using (JsonHelperReader json = JSONHelper.OpenReadJSON(metadata.Stream)) {
                        json.Read(); // Go to start;
                        return (UnityEngine.Object) json.FillObject(obj);
                    }
                }
                return (UnityEngine.Object) JSONHelper.ReadJSON(metadata.Stream);
            }

            if (t_Texture.IsAssignableFrom(type) ||
                type == t_Texture2D ||
                (type == t_Object && metadata.AssetType == t_Texture2D)) {
                Texture2D tex = new Texture2D(2, 2);
                tex.name = path;
                tex.LoadImage(metadata.Data);
                return tex;
            }

            UnityEngine.Object orig = Resources.Load(path + ETGModUnityEngineHooks.SkipSuffix);
            if (orig is GameObject) {
                Handle(((GameObject) orig).transform);
            }
            return orig;
        }

        public static void MakeSpriteRW(tk2dBaseSprite sprite) {
            Material[] materials = sprite.Collection.materials;
            for (int i = 0; i < materials.Length; i++) {
                materials[i].mainTexture = (materials[i].mainTexture as Texture2D)?.GetRW();
            }
        }

        public static tk2dBaseSprite Handle(tk2dBaseSprite sprite) {
            string assetPrefix = "sprites/" + sprite.transform.GetPath().Replace("(Clone)", "") + ".";

            if (sprite.Collection == null) {
                return sprite;
            }
            if (sprite.Collection.materials == null) {
                return sprite;
            }
            Material[] materials = sprite.Collection.materials;
            for (int i = 0; i < materials.Length; i++) {
                string assetPath = assetPrefix + i;

                /*string diskPath = Path.Combine(ResourcesDirectory, assetPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".png");
                if (!File.Exists(diskPath)) {
                    Console.WriteLine("DUMPING A SPRITE TO " + diskPath);
                    Directory.GetParent(diskPath).Create();
                    File.WriteAllBytes(diskPath, ((Texture2D) materials[i].mainTexture).GetRW().EncodeToPNG());
                }*/

                AssetMetadata metadata;
                if (TryGetMapped(assetPath, out metadata)) {
                    materials[i].mainTexture = Resources.Load<Texture2D>(assetPath);
                }
            }

            return sprite;
        }

        public static Transform[] Handle(Transform[] ts) {
            for (int i = 0; i < ts.Length; i++) {
                Handle(ts[i]);
            }
            return ts;
        }
        public static void Handle(Transform t) {
            GameObject go = t.gameObject;

            go.GetComponent<tk2dBaseSprite>()?.Handle();

            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++) {
                Handle(t.GetChild(i));
            }
        }

    }

    public static tk2dBaseSprite Handle(this tk2dBaseSprite sprite) {
        return Assets.Handle(sprite);
    }

    public static void MapAssets(this Assembly asm) {
        Assets.Crawl(asm);
    }

}
