#pragma warning disable RECS0018

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;
using AttachPoint = tk2dSpriteDefinition.AttachPoint;

public static partial class ETGMod {
    /// <summary>
    /// ETGMod asset management.
    /// </summary>
    public static partial class Assets {

        public readonly static Type t_Object = typeof(UnityEngine.Object);
        public readonly static Type t_AssetDirectory = typeof(AssetDirectory);
        public readonly static Type t_Texture = typeof(Texture);
        public readonly static Type t_Texture2D = typeof(Texture2D);
        public readonly static Type t_tk2dSpriteCollectionData = typeof(tk2dSpriteCollectionData);
        public readonly static Type t_tk2dSpriteDefinition = typeof(tk2dSpriteDefinition);

        private readonly static FieldInfo f_tk2dSpriteCollectionData_spriteNameLookupDict =
            typeof(tk2dSpriteCollectionData).GetField("spriteNameLookupDict", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Asset map. All string - AssetMetadata entries here will cause an asset to be remapped. Use ETGMod.Assets.AddMapping to add an entry.
        /// </summary>
        public readonly static Dictionary<string, AssetMetadata> Map = new Dictionary<string, AssetMetadata>();
        /// <summary>
        /// Directories that would not fit into Map due to conflicts.
        /// </summary>
        public readonly static Dictionary<string, AssetMetadata> MapDirs = new Dictionary<string, AssetMetadata>();
        /// <summary>
        /// Texture remappings. This dictionary starts empty and will be filled as sprites get replaced. Feel free to add your own remapping here.
        /// </summary>
        public readonly static Dictionary<string, Texture2D> TextureMap = new Dictionary<string, Texture2D>();

        public static bool DumpResources = false;

        public static bool DumpSprites = false;
        public static bool DumpSpritesMetadata = false;
        public static int FramesToHandleAllSpritesIn = 16;
        private readonly static Vector2[] _DefaultUVs = {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        public static Shader DefaultSpriteShader;

        public static RuntimeAtlasPacker Packer = new RuntimeAtlasPacker();

        public static bool TryGetMapped(string path, out AssetMetadata metadata, bool dataless = false) {
            if (!dataless) {
                if (MapDirs.TryGetValue(path, out metadata)) { return true; }
                if (MapDirs.TryGetValue(path.ToLowerInvariant(), out metadata)) { return true; }
            }
            if (Map.TryGetValue(path, out metadata)) { return true; }
            if (Map.TryGetValue(path.ToLowerInvariant(), out metadata)) { return true; }

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
            if (metadata.AssetType == t_AssetDirectory) {
                return MapDirs[path] = metadata;
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
                string file = files[i];
                AddMapping(file.Substring(root.Length + 1), new AssetMetadata(file) {
                    AssetType = t_AssetDirectory
                });
                Crawl(file, root);
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
            string spritesDir = Path.Combine(ResourcesDirectory, "sprites");
            if (!Directory.Exists(spritesDir)) {
                Debug.Log("Sprites directory not existing, creating...");
                Directory.CreateDirectory(spritesDir);
            }

            ETGModUnityEngineHooks.Load = Load;
            // ETGModUnityEngineHooks.LoadAsync = LoadAsync;
            // ETGModUnityEngineHooks.LoadAll = LoadAll;
            // ETGModUnityEngineHooks.UnloadAsset = UnloadAsset;

            DefaultSpriteShader = Shader.Find("tk2d/BlendVertexColor");
        }

        public static UnityEngine.Object Load(string path, Type type) {
            if (path == "PlayerCoopCultist" && Player.CoopReplacement != null) {
                return Resources.Load(Player.CoopReplacement, type) as GameObject;
            }

            if (path.StartsWithInvariant("ITEMDB:")) {
                return UnityEngine.Object.Instantiate(Databases.Items.GetModItemByName(path.Substring(7)));
            }

#if DEBUG
            if (DumpResources) {
                Dump.DumpResource(path);
            }
#endif

            AssetMetadata metadata;
            bool isJson = false;
            bool isPatch = false;
                 if (TryGetMapped(path, out metadata, true)) { }
            else if (TryGetMapped(path + ".json", out metadata)) { isJson = true; }
            else if (TryGetMapped(path + ".patch.json", out metadata)) { isPatch = true; isJson = true; }

            if (metadata != null) {
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

                if (t_tk2dSpriteCollectionData == type) {
                    AssetMetadata json = GetMapped(path + ".json");
                    if (metadata.AssetType == t_Texture2D && json != null) {
                        // Atlas
                        string[] names;
                        Rect[] regions;
                        Vector2[] anchors;
                        AttachPoint[][] attachPoints;
                        AssetSpriteData.ToTK2D(JSONHelper.ReadJSON<List<AssetSpriteData>>(json.Stream), out names, out regions, out anchors, out attachPoints);
                        tk2dSpriteCollectionData sprites = tk2dSpriteCollectionData.CreateFromTexture(
                            Resources.Load<Texture2D>(path), tk2dSpriteCollectionSize.Default(), names, regions, anchors
                        );
                        for (int i = 0; i < attachPoints.Length; i++) {
                            sprites.SetAttachPoints(i, attachPoints[i]);
                        }
                        return sprites;
                    }

                    if (metadata.AssetType == t_AssetDirectory) {
                        // Separate textures
                        // TODO create collection from "children" assets
                        tk2dSpriteCollectionData data = new tk2dSpriteCollectionData();
                        tk2dSpriteCollectionSize size = tk2dSpriteCollectionSize.Default();

                        data.Transient = true;
                        data.version = 3;
                        data.invOrthoSize = 1f / size.OrthoSize;
                        data.halfTargetHeight = size.TargetHeight * 0.5f;
                        data.premultipliedAlpha = false;
                        data.material = new Material(DefaultSpriteShader);
                        data.materials = new Material[] { data.material };
                        data.buildKey = UnityEngine.Random.Range(0, int.MaxValue);

                        data.Handle(true);

                        data.textures = new Texture2D[data.spriteDefinitions.Length];
                        for (int i = 0; i < data.spriteDefinitions.Length; i++) {
                            data.textures[i] = data.spriteDefinitions[i].materialInst.mainTexture;
                        }

                        return data;
                    }
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

            }
            
            UnityEngine.Object orig = Resources.Load(path + ETGModUnityEngineHooks.SkipSuffix, type);
            if (orig is GameObject) {
                HandleGameObject((GameObject) orig);
                Packer.Apply();
            }
            return orig;
        }

        public static void HandleSprites(tk2dSpriteCollectionData sprites, bool replace = false) {
            if (sprites == null) {
                return;
            }
            string path = "sprites/" + sprites.spriteCollectionName;

            Texture2D replacement;
            AssetMetadata metadata;

            string atlasName = sprites.materials[0].mainTexture == null ? null : sprites.materials[0].mainTexture.name;
            if (sprites.materials[0].mainTexture != null && (atlasName == null || atlasName.Length == 0 || atlasName[0] != '~')) {
                     if (TextureMap.TryGetValue(path, out replacement)) { }
                else if (TryGetMapped          (path, out metadata))    { TextureMap[path] = replacement = Resources.Load<Texture2D>(path); }
                else {
                    foreach (KeyValuePair<string, AssetMetadata> mapping in Map) {
                        if (!mapping.Value.HasData) continue;
                        string resourcePath = mapping.Key;
                        if (!resourcePath.StartsWithInvariant("sprites/@")) continue;
                        string spriteName = resourcePath.Substring(9);
                        if (sprites.spriteCollectionName.Contains(spriteName)) {
                            string copyPath = Path.Combine(ResourcesDirectory, ("DUMP" + path).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".png");
                            if (mapping.Value.Container == AssetMetadata.ContainerType.Filesystem && !File.Exists(copyPath)) {
                                File.Copy(mapping.Value.File, copyPath);
                            }
                            TextureMap[path] = replacement = Resources.Load<Texture2D>(resourcePath);
                            break;
                        }
                    }
                }

                if (replacement != null) {
                    // Full atlas texture replacement.
                    replacement.name = '~' + atlasName;
                    for (int i = 0; i < sprites.materials.Length; i++) {
                        sprites.materials[i].mainTexture = replacement;
                    }
                }
            }

            if (DumpSprites) {
                Dump.DumpSpriteCollection(sprites);
            }
            if (DumpSpritesMetadata) {
                Dump.DumpSpriteCollectionMetadata(sprites);
            }

            List<tk2dSpriteDefinition> list = null;
            foreach (KeyValuePair<string, AssetMetadata> mapping in Map) {
                string assetPath = mapping.Key;
                if (assetPath.Length <= path.Length + 1) {
                    continue;
                }
                if (!assetPath.StartsWithInvariant(path) || mapping.Value.AssetType != t_Texture2D) {
                    continue;
                }

                string name = assetPath.Substring(path.Length + 1);
                tk2dSpriteDefinition frame = sprites.GetSpriteDefinition(name);

                if (frame != null && frame.materialInst != null) {
                    Texture2D origTex = (Texture2D) frame.materialInst.mainTexture;
                    if (Packer.IsPageTexture(origTex)) {
                        continue;
                    }
                }

                if (!TextureMap.TryGetValue(assetPath, out replacement))
                    replacement = TextureMap[assetPath] = Resources.Load<Texture2D>(assetPath);
                if (replacement == null) {
                    continue;
                }

                if (frame == null && name[0] == '@') {
                    name = name.Substring(1);
                    for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
                        tk2dSpriteDefinition frame_ = sprites.spriteDefinitions[i];
                        if (frame_.Valid && frame_.name.Contains(name)) {
                            frame = frame_;
                            name = frame_.name;
                            break;
                        }
                    }
                    if (frame == null) {
                        continue;
                    }
                }

                if (frame != null) {
                    // Replace old sprite.
                    frame.ReplaceTexture(replacement);

                } else {
                    // Add new sprite.
                    if (list == null) {
                        list = new List<tk2dSpriteDefinition>(sprites.spriteDefinitions.Length);
                        list.AddRange(sprites.spriteDefinitions);
                    }
                    frame = new tk2dSpriteDefinition();
                    frame.name = name;
                    frame.material = sprites.materials[0];
                    frame.ReplaceTexture(replacement);

                    AssetSpriteData frameData = new AssetSpriteData();
                    AssetMetadata jsonMetadata = GetMapped(assetPath + ".json");
                    if (jsonMetadata != null) {
                        frameData = JSONHelper.ReadJSON<AssetSpriteData>(jsonMetadata.Stream);
                    }

                    frame.normals = new Vector3[0];
                    frame.tangents = new Vector4[0];
                    frame.indices = new int[] { 0, 3, 1, 2, 3, 0 };

                    // TODO figure out this black magic
                    const float pixelScale = 0.0625f;
                    float w = replacement.width * pixelScale;
                    float h = replacement.height * pixelScale;
                    frame.position0 = new Vector3(0f, 0f, 0f);
                    frame.position1 = new Vector3(w, 0f, 0f);
                    frame.position2 = new Vector3(0f, h, 0f);
                    frame.position3 = new Vector3(w, h, 0f);
                    frame.boundsDataCenter = frame.untrimmedBoundsDataCenter = new Vector3(w / 2f, h / 2f, 0f);
                    frame.boundsDataExtents = frame.untrimmedBoundsDataExtents = new Vector3(w, h, 0f);

                    sprites.SetAttachPoints(list.Count, frameData.attachPoints);

                    list.Add(frame);
                }
            }
            if (list != null) {
                sprites.spriteDefinitions = list.ToArray();
                ReflectionHelper.SetValue(f_tk2dSpriteCollectionData_spriteNameLookupDict, sprites, null);
            }

            if (sprites.hasPlatformData) {
                sprites.inst.Handle(replace);
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
            Packer.Apply();
            yield return null;
        }

        public static void ReplaceTexture(tk2dSpriteDefinition frame, Texture2D replacement, bool pack = true) {
            frame.flipped = tk2dSpriteDefinition.FlipMode.None;
            frame.materialInst = new Material(frame.material);
            frame.texelSize = replacement.texelSize;
            frame.extractRegion = pack;
            if (pack) {
                RuntimeAtlasSegment segment = Packer.Pack(replacement);
                frame.materialInst.mainTexture = segment.texture;
                frame.uvs = segment.uvs;
            } else {
                frame.materialInst.mainTexture = replacement;
                frame.uvs = _DefaultUVs;
            }
        }

    }

    public static void Handle(this tk2dBaseSprite sprite) {
        Assets.HandleSprites(sprite.Collection);
    }

    public static void Handle(this tk2dSpriteCollectionData sprites, bool replace = false) {
        Assets.HandleSprites(sprites, replace);
    }

    public static void MapAssets(this Assembly asm) {
        Assets.Crawl(asm);
    }

    public static void ReplaceTexture(this tk2dSpriteDefinition frame, Texture2D replacement, bool pack = true) {
        Assets.ReplaceTexture(frame, replacement, pack);
    }

}
