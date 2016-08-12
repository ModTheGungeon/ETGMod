#pragma warning disable RECS0018

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;

public static partial class ETGMod {
    /// <summary>
    /// ETGMod asset management.
    /// </summary>
    public static partial class Assets {

        private readonly static Type t_Object = typeof(UnityEngine.Object);
        private readonly static Type t_Texture = typeof(Texture);
        private readonly static Type t_Texture2D = typeof(Texture2D);

        private readonly static FieldInfo f_tk2dSpriteCollectionData_spriteNameLookupDict =
            typeof(tk2dSpriteCollectionData).GetField("spriteNameLookupDict", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Asset map. All string - AssetMetadata entries here will cause an asset to be remapped. Use ETGMod.Assets.AddMapping to add an entry.
        /// </summary>
        public readonly static Dictionary<string, AssetMetadata> Map = new Dictionary<string, AssetMetadata>();
        /// <summary>
        /// Texture remappings. This dictionary starts empty and will be filled as sprites get replaced. Feel free to add your own remapping here.
        /// </summary>
        public readonly static Dictionary<string, Texture2D> TextureMap = new Dictionary<string, Texture2D>();

        public static bool DumpResources = false;

        public static bool DumpSprites = false;
        public static int FramesToHandleAllSpritesIn = 10;
        private readonly static Vector2[] _DefaultUVs = {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        public static bool TryGetMapped(string path, out AssetMetadata metadata) {
            if (Map.TryGetValue(path, out metadata)) { return true; }
            if (Map.TryGetValue(path.ToLowerInvariant(), out metadata)) { return true; }

            // ETGMod now crawls through ResourcesDirectory.
            // TryGetMapped is not used everywhere and manual crawling every time is too expensive.
            // If you're feeling lucky, uncomment the following code.
            /*
            string diskPathRaw = Path.Combine(ResourcesDirectory, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            string diskPath = diskPathRaw;
            if (!File.Exists(diskPath)) {
                diskPath = diskPathRaw + ".png";
            }
            if (File.Exists(diskPath)) {
                metadata = AddMapping(path, new AssetMetadata(diskPath));
                return true;
            }
            ;
            /**/

            return false;
        }
        public static AssetMetadata GetMapped(string path) {
            AssetMetadata metadata;
            TryGetMapped(path, out metadata);
            return metadata;
        }

        public static AssetMetadata AddMapping(string path, AssetMetadata metadata) {
            path = path.Replace('\\', '/');
            if (metadata.AssetType == null) {
                path = RemoveExtension(path, out metadata.AssetType);
            }

            return Map[path] = metadata;
        }

        public static string RemoveExtension(string file, out Type type) {
            type = t_Object;

            if (file.EndsWithInvariant(".png")) {
                type = t_Texture2D;
                file = file.Substring(0, file.Length - 4);

            }

            return file;
        }

        public static void Crawl(string dir, string root = null) {
            if (root == null) root = dir;
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++) {
                string file = files[i];
                AddMapping(file.Substring(root.Length + 1), new AssetMetadata(file));
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
                int indexOfContent = name.IndexOfInvariant("Content");
                if (indexOfContent < 0) {
                    continue;
                }
                name = name.Substring(indexOfContent + 8);
                Console.WriteLine("Found resource " + name + " in .dll");
                AddMapping(name, new AssetMetadata(asm, resourceNames[i]));
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

            if (DumpResources) {
                Dump.DumpResource(path);
            }

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
                tex.filterMode = FilterMode.Point;
                return tex;
            }

            // TODO use tk2dSpriteCollectionData.CreateFromTexture

            UnityEngine.Object orig = Resources.Load(path + ETGModUnityEngineHooks.SkipSuffix);
            if (orig is GameObject) {
                HandleGameObject((GameObject) orig);
            }
            return orig;
        }

        public static void HandleSprite(tk2dSpriteCollectionData sprites) {
            if (TextureMap.ContainsValue((Texture2D) sprites.materials[0].mainTexture)) {
                return;
            }
            string path = "sprites/" + sprites.spriteCollectionName;

            Texture2D replacement;
            AssetMetadata metadata;
                 if (TextureMap.TryGetValue(path, out replacement)) { }
            else if (TryGetMapped          (path, out metadata))    { TextureMap[path] = replacement = Resources.Load<Texture2D>(path); }
            if (replacement != null) {
                // Full atlas texture replacement.
                for (int i = 0; i < sprites.materials.Length; i++) {
                    sprites.materials[i].mainTexture = replacement;
                }
            }

            if (DumpSprites) {
                Dump.DumpSpriteCollection(sprites);
            }

            // Old method: Crawl through sprites in collection, replace.
            // +: It just works.
            // -: It is slow.
            // -: Doesn't support adding new frames.
            /*
            for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
                tk2dSpriteDefinition frame = sprites.spriteDefinitions[i];
                Texture2D texOrig = (Texture2D) frame.material.mainTexture;
                if (!frame.Valid || (frame.materialInst != null && TextureMap.ContainsValue((Texture2D) frame.materialInst.mainTexture))) {
                    continue;
                }
                string pathFull = path + "/" + frame.name;
                // Console.WriteLine("Frame " + i + ": " + frame.name + " (" + pathFull + ")");

                     if (TextureMap.TryGetValue(pathFull, out replacement)) { }
                else if (TryGetMapped          (pathFull, out metadata))    { TextureMap[pathFull] = replacement = Resources.Load<Texture2D>(pathFull); }
                if (replacement != null) {
                    frame.flipped = tk2dSpriteDefinition.FlipMode.None;
                    frame.extractRegion = false;
                    frame.uvs = _DefaultUVs;
                    frame.materialInst = new Material(frame.material);
                    frame.materialInst.mainTexture = replacement;
                }
            }
            */

            // New method: Crawl through asset list, replace or add.
            // +: Adding.
            // +: Faster.
            // -: No Resources support.
            bool refresh = false;
            foreach (KeyValuePair<string, AssetMetadata> mapping in Map) {
                string assetPath = mapping.Key;
                if (assetPath.Length <= path.Length + 1) {
                    continue;
                }
                if (!assetPath.ToLowerInvariant().StartsWithInvariant(path.ToLowerInvariant()) || mapping.Value.AssetType != t_Texture2D) {
                    continue;
                }

                TextureMap[assetPath] = replacement = Resources.Load<Texture2D>(assetPath);
                if (replacement == null) {
                    continue;
                }

                string name = assetPath.Substring(path.Length + 1);
                tk2dSpriteDefinition frame = null;
                for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
                    tk2dSpriteDefinition frame_ = sprites.spriteDefinitions[i];
                    if (frame_.Valid && frame_.name.ToLowerInvariant() == name.ToLowerInvariant()) {
                        frame = frame_;
                        name = frame.name;
                        break;
                    }
                }

                if (frame != null) {
                    // Replace old sprite.
                    frame.ReplaceTexture(replacement);

                } else {
                    refresh = true;
                    // Add new sprite.
                    // FIXME TODO!
                    frame = new tk2dSpriteDefinition();
                    frame.name = name;
                    frame.material = sprites.materials[0];
                    frame.ReplaceTexture(replacement);
                }
            }

            if (refresh) {
                ReflectionHelper.SetValue(f_tk2dSpriteCollectionData_spriteNameLookupDict, sprites, null);
            }
        }

        public static void HandleGameObject(GameObject go) {
            go.GetComponent<tk2dBaseSprite>()?.Collection.Handle();
        }

        public static void HandleAll() {
            StartCoroutine(HandleAllSprites());
        }
        private static IEnumerator HandleAllSprites() {
            tk2dBaseSprite[] sprites = UnityEngine.Object.FindObjectsOfType<tk2dBaseSprite>();
            int handleUntilYield = sprites.Length / FramesToHandleAllSpritesIn;
            int handleUntilYieldM1 = handleUntilYield - 1;
            for (int i = 0; i < sprites.Length; i++) {
                sprites[i].Handle();
                if (i % handleUntilYield == handleUntilYieldM1) yield return null;
            }
            yield return null;
        }

        public static void ReplaceTexture(tk2dSpriteDefinition frame, Texture2D replacement) {
            frame.flipped = tk2dSpriteDefinition.FlipMode.None;
            frame.extractRegion = false;
            frame.uvs = _DefaultUVs;
            frame.materialInst = new Material(frame.material);
            frame.materialInst.mainTexture = replacement;
        }

    }

    public static void Handle(this tk2dBaseSprite sprite) {
        Assets.HandleSprite(sprite.Collection);
    }

    public static void Handle(this tk2dSpriteCollectionData sprites) {
        Assets.HandleSprite(sprites);
    }

    public static void MapAssets(this Assembly asm) {
        Assets.Crawl(asm);
    }

    public static void ReplaceTexture(this tk2dSpriteDefinition frame, Texture2D replacement) {
        Assets.ReplaceTexture(frame, replacement);
    }

}
