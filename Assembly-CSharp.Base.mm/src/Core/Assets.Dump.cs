using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;

public static partial class ETGMod {
    public static partial class Assets {
        public static class Dump {
            public static void DumpResource(string path) {
                string dumpdir = Path.Combine(Application.streamingAssetsPath.Replace('/', Path.DirectorySeparatorChar), "DUMP");
                JSONHelper.SharedDir = Path.Combine(dumpdir, "SHARED");
                string dumppath = Path.Combine(dumpdir, path.Replace('/', Path.DirectorySeparatorChar) + ".json");
                if (!File.Exists(dumppath)) {
                    UnityEngine.Object obj = Resources.Load(path + ETGModUnityEngineHooks.SkipSuffix);
                    if (obj != null) {
                        Directory.GetParent(dumppath).Create();
                        Console.WriteLine("JSON WRITING " + path);
                        obj.WriteJSON(dumppath);
                        //Console.WriteLine("JSON READING " + path);
                        //object testobj = JSONHelper.ReadJSON(dumppath);
                        JSONHelper.LOG = false;

                        dumpdir = Path.Combine(Application.streamingAssetsPath.Replace('/', Path.DirectorySeparatorChar), "DUMPA");
                        // JSONHelper.SharedDir = Path.Combine(dumpdir, "SHARED");
                        dumppath = Path.Combine(dumpdir, path.Replace('/', Path.DirectorySeparatorChar) + ".json");
                        Directory.GetParent(dumppath).Create();
                        //Console.WriteLine("JSON REWRITING " + path);
                        //testobj.WriteJSON(dumppath);
                        //Console.WriteLine("JSON REREADING " + path);
                        //testobj = JSONHelper.ReadJSON(dumppath);
                        JSONHelper.LOG = false;
                        Console.WriteLine("JSON DONE " + path);
                    }
                }
                JSONHelper.SharedDir = null;
            }

            public static void DumpSpriteCollection(tk2dSpriteCollectionData sprites) {
                string path = "DUMPsprites/" + sprites.spriteCollectionName;
                string diskPath = null;

                diskPath = Path.Combine(ResourcesDirectory, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".png");
                if (File.Exists(diskPath)) {
                    return;
                }

                Texture2D texRWOrig = null;
                Texture2D texRW = null;
                Color[] texRWData = null;
                for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
                    tk2dSpriteDefinition frame = sprites.spriteDefinitions[i];
                    Texture2D texOrig = frame.material.mainTexture as Texture2D;
                    if (texOrig == null || !frame.Valid || (frame.materialInst != null && TextureMap.ContainsValue((Texture2D) frame.materialInst.mainTexture))) {
                        continue;
                    }
                    string pathFull = path + "/" + frame.name;
                    // Console.WriteLine("Frame " + i + ": " + frame.name + " (" + pathFull + ")");

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

                    if (texRWOrig != texOrig) {
                        texRWOrig = texOrig;
                        texRW = texOrig.GetRW();
                        texRWData = texRW.GetPixels();
                        diskPath = Path.Combine(ResourcesDirectory, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".png");

                        Directory.GetParent(diskPath).Create();
                        File.WriteAllBytes(diskPath, texRW.EncodeToPNG());
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

                    diskPath = Path.Combine(ResourcesDirectory, pathFull.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".png");
                    if (!File.Exists(diskPath)) {
                        Directory.GetParent(diskPath).Create();
                        File.WriteAllBytes(diskPath, texRegion.EncodeToPNG());
                    }

                }
            }

            public static void DumpSpriteCollectionMetadata(tk2dSpriteCollectionData sprites) {
                string path = "DUMPsprites/" + sprites.spriteCollectionName;
                string diskPath = Path.Combine(ResourcesDirectory, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".json");
                if (File.Exists(diskPath)) {
                    return;
                }
                Directory.GetParent(diskPath).Create();
                JSONHelper.WriteJSON(AssetSpriteData.FromTK2D(sprites), diskPath);
                for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
                    tk2dSpriteDefinition frame = sprites.spriteDefinitions[i];
                    Texture2D texOrig = (Texture2D) frame.material.mainTexture;
                    if (!frame.Valid || (frame.materialInst != null && TextureMap.ContainsValue((Texture2D) frame.materialInst.mainTexture))) {
                        continue;
                    }
                    string pathFull = path + "/" + frame.name;
                    diskPath = Path.Combine(ResourcesDirectory, pathFull.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + ".json");
                    if (!File.Exists(diskPath)) {
                        Directory.GetParent(diskPath).Create();
                        JSONHelper.WriteJSON(AssetSpriteData.FromTK2D(sprites, frame, true), diskPath);
                    }
                }
            }

            public static void DumpPacker() {
                DumpPacker(Packer, "main");
            }
            public static void DumpPacker(RuntimeAtlasPacker packer, string name) {
                string dir = Path.Combine(ResourcesDirectory, ("DUMPpacker_" + name).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir, true);
                }
                Directory.CreateDirectory(dir);

                for (int i = 0; i < packer.Pages.Count; i++) {
                    RuntimeAtlasPage page = packer.Pages[i];
                    string diskPath = Path.Combine(dir, i + ".png");
                    File.WriteAllBytes(diskPath, page.Texture.EncodeToPNG());
                }
            }

        }
    }
}
