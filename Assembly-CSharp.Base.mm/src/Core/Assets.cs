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
    public static class Assets {

        private readonly static Type t_Object = typeof(UnityEngine.Object);
        private readonly static Type t_Texture = typeof(Texture);
        private readonly static Type t_Texture2D = typeof(Texture2D);

        public readonly static Dictionary<string, AssetMetadata> Map = new Dictionary<string, AssetMetadata>();
        public readonly static Dictionary<string, Texture2D> TextureMap = new Dictionary<string, Texture2D>();

        public static bool DumpSprites = false;
        public static int FramesToHandleAllSpritesIn = 10;
        private readonly static Vector2[] _DefaultUVs = {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        public static bool TryGetMapped(string path, out AssetMetadata metadata) {
            string diskPathRaw = Path.Combine(ResourcesDirectory, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            string diskPath = diskPathRaw;
            if (!File.Exists(diskPath)) {
                diskPath = diskPathRaw + ".png";
            }
            if (File.Exists(diskPath)) {
                metadata = Map[path] = new AssetMetadata(diskPath);
                return true;
            }

            if (Map.TryGetValue(path                   , out metadata)) { return true; }
            if (Map.TryGetValue(path.ToLowerInvariant(), out metadata)) { return true; }

            return false;
        }
        public static AssetMetadata GetMapped(string path) {
            AssetMetadata metadata;
            TryGetMapped(path, out metadata);
            return metadata;
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

                Map[name] = new AssetMetadata(asm, resourceNames[i]) {
                    AssetType = type
                };
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

        public static void HandleSprite(tk2dBaseSprite sprite) {
            string diskPath;
            tk2dSpriteCollectionData sprites = sprite.Collection;
            if (TextureMap.ContainsValue((Texture2D) sprites.materials[0].mainTexture)) {
                return;
            }
            // _Replaced.Add(sprites);

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
                return;
            }

            Texture2D texRWOrig = null;
            Texture2D texRW = null;
            Color[] texRWData = null;
            for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
                tk2dSpriteDefinition frame = sprites.spriteDefinitions[i];
                Texture2D texOrig = (Texture2D) frame.material.mainTexture;
                if (!frame.Valid || (frame.materialInst != null && frame.materialInst.mainTexture != null && TextureMap.ContainsValue((Texture2D) frame.materialInst.mainTexture))) {
                    continue;
                }
                string pathFull = path + "/" + frame.name;
                // Console.WriteLine("Frame " + i + ": " + frame.name);

                /*
                for (int ii = 0; ii < frame.uvs.Length; ii++) {
                    Console.WriteLine("UV " + ii + ": " + frame.uvs[ii].x + ", " + frame.uvs[ii].y);
                }

                /**/
                /*
                Console.WriteLine("P0 " + frame.position0.x + ", " + frame.position0.y);
                Console.WriteLine("P1 " + frame.position1.x + ", " + frame.position1.y);
                Console.WriteLine("P2 " + frame.position2.x + ", " + frame.position2.y);
                Console.WriteLine("P3 " + frame.position3.x + ", " + frame.position3.y);
                /**/

                if (DumpSprites) {
                    if (texRWOrig != texOrig) {
                        texRWOrig = texOrig;
                        texRW = texOrig.GetRW();
                        texRWData = texRW.GetPixels();

                        Color[] fuck = texRWData;
                        Color hard = new Color(0f, 0f, 0f, 0f);
                        bool shit = true;
                        for (int me = 0; me < fuck.Length; me += 8) {
                            if (fuck[me] != hard) {
                                shit = false;
                                break;
                            }
                        }
                        if (shit) {
                            return;
                        }

                        diskPath = Path.Combine(ResourcesDirectory, ("DUMP" + path).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".png");
                        if (!File.Exists(diskPath)) {
                            Directory.GetParent(diskPath).Create();
                            File.WriteAllBytes(diskPath, texRW.EncodeToPNG());
                        }
                    }

                    Texture2D texRegion;

                    double x1UV = 1D;
                    double y1UV = 1D;
                    double x2UV = 0D;
                    double y2UV = 0D;
                    for (int ii = 0; ii < frame.uvs.Length; ii++) {
                        if (frame.uvs[ii].x < x1UV) x1UV = frame.uvs[ii].x;
                        if (frame.uvs[ii].y < y1UV) y1UV = frame.uvs[ii].y;
                        if (x2UV < frame.uvs[ii].x) x2UV = frame.uvs[ii].x;
                        if (y2UV < frame.uvs[ii].y) y2UV = frame.uvs[ii].y;
                    }

                    int x1 = (int) Math.Floor(x1UV * texOrig.width);
                    int y1 = (int) Math.Floor(y1UV * texOrig.height);
                    int x2 = (int) Math.Ceiling(x2UV * texOrig.width);
                    int y2 = (int) Math.Ceiling(y2UV * texOrig.height);
                    int w = x2 - x1;
                    int h = y2 - y1;

                    if (
                        frame.uvs[0].x == x1UV && frame.uvs[0].y == y1UV &&
                        frame.uvs[1].x == x2UV && frame.uvs[1].y == y1UV &&
                        frame.uvs[2].x == x1UV && frame.uvs[2].y == y2UV &&
                        frame.uvs[3].x == x2UV && frame.uvs[3].y == y2UV
                    ) {
                        // original
                        texRegion = new Texture2D(w, h);
                        texRegion.SetPixels(texRW.GetPixels(x1, y1, w, h));
                    } else {
                        // flipped
                        if (frame.uvs[0].x == frame.uvs[1].x) {
                            int t = h;
                            h = w;
                            w = t;
                        }
                        texRegion = new Texture2D(w, h);

                        // Flipping using GPU / GL / Quads / UV doesn't work (returns blank texture for some reason).
                        // RIP performance.

                        double fxX = frame.uvs[1].x - frame.uvs[0].x;
                        double fyX = frame.uvs[2].x - frame.uvs[0].x;
                        double fxY = frame.uvs[1].y - frame.uvs[0].y;
                        double fyY = frame.uvs[2].y - frame.uvs[0].y;

                        double wO = texOrig.width * (frame.uvs[3].x - frame.uvs[0].x);
                        double hO = texOrig.height * (frame.uvs[3].y - frame.uvs[0].y);

                        double e = 0.001D;
                        double fxX0w = fxX < e ? 0D : wO;
                        double fyX0w = fyX < e ? 0D : wO;
                        double fxY0h = fxY < e ? 0D : hO;
                        double fyY0h = fyY < e ? 0D : hO;

                        for (int y = 0; y < h; y++) {
                            double fy = y / (double) h;
                            for (int x = 0; x < w; x++) {
                                double fx = x / (double) w;

                                double fxUV0w = fx * fxX0w + fy * fyX0w;
                                double fyUV0h = fx * fxY0h + fy * fyY0h;

                                double p =
                                    Math.Round(frame.uvs[0].y * texOrig.height + fyUV0h) * texOrig.width +
                                    Math.Round(frame.uvs[0].x * texOrig.width + fxUV0w);

                                texRegion.SetPixel(x, y, texRWData[(int) p]);

                            }
                        }

                    }

                    diskPath = Path.Combine(ResourcesDirectory, ("DUMP" + pathFull).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".png");
                    if (!File.Exists(diskPath)) {
                        Directory.GetParent(diskPath).Create();
                        File.WriteAllBytes(diskPath, texRegion.EncodeToPNG());
                    }
                }

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
        }

        public static void HandleGameObject(GameObject go) {
            go.GetComponent<tk2dBaseSprite>()?.Handle();
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

    }

    public static void Handle(this tk2dBaseSprite sprite) {
        Assets.HandleSprite(sprite);
    }

    public static void MapAssets(this Assembly asm) {
        Assets.Crawl(asm);
    }

}
