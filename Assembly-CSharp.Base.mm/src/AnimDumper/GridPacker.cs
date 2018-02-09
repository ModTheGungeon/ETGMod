using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


namespace ETGMod {
    public static class GridPacker {
        public struct Element {

            public Element(Texture2D texture, Vector2[] uvs, string name = null) {
                Texture = texture;
                UVs = uvs;
                Name = name ?? "Unknown";
                _RWTexture = null;
            }

            public string Name { get; private set; }

            public Vector2[] UVs { get; private set; }
            public Texture2D Texture { get; private set; }

            private Texture2D _RWTexture;
            public Texture2D RWTexture {
                get {
                    if (_RWTexture != null) return _RWTexture;
                    return _RWTexture = Texture.GetRW();
                }
            }

            public int Width {
                get {
                    double x1UV = 1D;
                    double y1UV = 1D;
                    double x2UV = 0D;
                    double y2UV = 0D;
                    for (int ii = 0; ii < UVs.Length; ii++) {
                        if (UVs[ii].x < x1UV) x1UV = UVs[ii].x;
                        if (UVs[ii].y < y1UV) y1UV = UVs[ii].y;
                        if (x2UV < UVs[ii].x) x2UV = UVs[ii].x;
                        if (y2UV < UVs[ii].y) y2UV = UVs[ii].y;
                    }

                    int x1 = (int)Math.Floor(x1UV * RWTexture.width);
                    int x2 = (int)Math.Ceiling(x2UV * RWTexture.width);
                    return x2 - x1;
                }
            }

            public int Height {
                get {
                    double x1UV = 1D;
                    double y1UV = 1D;
                    double x2UV = 0D;
                    double y2UV = 0D;
                    for (int ii = 0; ii < UVs.Length; ii++) {
                        if (UVs[ii].x < x1UV) x1UV = UVs[ii].x;
                        if (UVs[ii].y < y1UV) y1UV = UVs[ii].y;
                        if (x2UV < UVs[ii].x) x2UV = UVs[ii].x;
                        if (y2UV < UVs[ii].y) y2UV = UVs[ii].y;
                    }

                    int y1 = (int)Math.Floor(y1UV * RWTexture.height);
                    int y2 = (int)Math.Ceiling(y2UV * RWTexture.height);
                    return y2 - y1;
                }
            }

            private bool _IsCropped(double uv_x_high, double uv_y_high, double uv_x_low, double uv_y_low) {
                return uv_x_high != 1 || uv_y_high != 1 || uv_x_low != 0 || uv_y_low != 0;
            }

            public void DrawOn(Texture2D target, int xoffs, int yoffs) {
                var texRW = RWTexture;
                var texRWData = texRW.GetPixels();

                Texture2D texRegion;

                double x1UV = 1D;
                double y1UV = 1D;
                double x2UV = 0D;
                double y2UV = 0D;
                for (int ii = 0; ii < UVs.Length; ii++) {
                    if (UVs[ii].x < x1UV) x1UV = UVs[ii].x;
                    if (UVs[ii].y < y1UV) y1UV = UVs[ii].y;
                    if (x2UV < UVs[ii].x) x2UV = UVs[ii].x;
                    if (y2UV < UVs[ii].y) y2UV = UVs[ii].y;
                }

                int x1 = (int)Math.Floor(x1UV * texRW.width);
                int y1 = (int)Math.Floor(y1UV * texRW.height);
                int x2 = (int)Math.Ceiling(x2UV * texRW.width);
                int y2 = (int)Math.Ceiling(y2UV * texRW.height);
                int w = x2 - x1;
                int h = y2 - y1;

                if (
                    UVs[0].x == x1UV && UVs[0].y == y1UV &&
                    UVs[1].x == x2UV && UVs[1].y == y1UV &&
                    UVs[2].x == x1UV && UVs[2].y == y2UV &&
                    UVs[3].x == x2UV && UVs[3].y == y2UV
                ) {
                    // original
                    texRegion = new Texture2D(w, h);
                    texRegion.SetPixels(texRW.GetPixels(x1, y1, w, h));
                    target.SetPixels(xoffs, target.height - h - yoffs, w, h, texRW.GetPixels(x1, y1, w, h));
                } else {
                    // flipped
                    if (UVs[0].x == UVs[1].x) {
                        int t = h;
                        h = w;
                        w = t;
                    }
                    texRegion = new Texture2D(w, h);

                    // Flipping using GPU / GL / Quads / UV doesn't work (returns blank texture for some reason).
                    // RIP performance.

                    double fxX = UVs[1].x - UVs[0].x;
                    double fyX = UVs[2].x - UVs[0].x;
                    double fxY = UVs[1].y - UVs[0].y;
                    double fyY = UVs[2].y - UVs[0].y;

                    double wO = texRW.width * (UVs[3].x - UVs[0].x);
                    double hO = texRW.height * (UVs[3].y - UVs[0].y);

                    double e = 0.001D;
                    double fxX0w = fxX < e ? 0D : wO;
                    double fyX0w = fyX < e ? 0D : wO;
                    double fxY0h = fxY < e ? 0D : hO;
                    double fyY0h = fyY < e ? 0D : hO;

                    for (int y = 0; y < h; y++) {
                        double fy = y / (double)h;
                        for (int x = 0; x < w; x++) {
                            double fx = x / (double)w;

                            double fxUV0w = fx * fxX0w + fy * fyX0w;
                            double fyUV0h = fx * fxY0h + fy * fyY0h;

                            double p =
                                Math.Round(UVs[0].y * texRW.height + fyUV0h) * texRW.width +
                                Math.Round(UVs[0].x * texRW.width + fxUV0w);

                            texRegion.SetPixel(x, y, texRWData[(int)p]);

                            target.SetPixel(xoffs + x, target.height - h - yoffs + y, texRWData[(int)p]);
                        }
                    }

                    var path = Path.Combine(Paths.ManagedFolder, "test_output");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    File.WriteAllBytes(Path.Combine(path, $"{Name}.png"), texRegion.EncodeToPNG());
                }
            }
        }

        public class PackedData {
            public struct Entry {
                public int Index;
                public int X;
                public int Y;
                public int GridX;
                public int GridY;
                public int Width;
                public int Height;
            }

            public Texture2D Output;
            public Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
            public int TileWidth;
            public int TileHeight;
        }

        public static int GetBiggestWidth(IList<Element> grid_elements) {
            if (grid_elements.Count == 0) throw new InvalidOperationException("Empty collection");

            var biggest = grid_elements[0].Width;
            for (int i = 0; i < grid_elements.Count; i++) {
                if (grid_elements[i].Width > biggest) biggest = grid_elements[i].Width;
            }

            return biggest;
        }

        public static int GetBiggestHeight(IList<Element> grid_elements) {
            if (grid_elements.Count == 0) throw new InvalidOperationException("Empty collection");

            var biggest = grid_elements[0].Height;
            for (int i = 0; i < grid_elements.Count; i++) {
                if (grid_elements[i].Height > biggest) biggest = grid_elements[i].Height;
            }

            return biggest;
        }

        private static Color _BlankColor = new Color(0, 0, 0, 0);

        private static uint _NextPowerOfTwo(uint v) {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        public static PackedData Pack (IList<Element> grid_elements, bool force_pot = false, bool force_pot_per_element = false) {
            var data = new PackedData();
            var count = grid_elements.Count;

            var tile_width = GetBiggestWidth(grid_elements);
            var tile_height = GetBiggestHeight(grid_elements);

            if (force_pot_per_element) {
                var tile_width_pot = _NextPowerOfTwo((uint)tile_width);
                var tile_height_pot = _NextPowerOfTwo((uint)tile_height);

                var max = Math.Max(tile_width_pot, tile_height_pot);
                tile_width = tile_height = (int)max;
            }

            data.TileWidth = tile_width;
            data.TileHeight = tile_height;

            var sqrt = Math.Sqrt(count);

            // if count is 4 then the setup will be 2x2
            //   (sqrt  4 = 2)
            //   (floor 4 = 2)
            //   (ceil  4 = 2)
            // if count is 5 then the setup will be 3x3
            //   (sqrt  5    ≈ 2.24)
            //   (ceil  2.24 = 3)
            // etc.

            var horiz_count = (int)Math.Ceiling(sqrt);
            var vert_count = (int)Math.Ceiling(sqrt);

            var total_width = horiz_count * tile_width;
            var total_height = vert_count * tile_height;

            if (force_pot) {
                var total_width_pot = _NextPowerOfTwo((uint)total_width);
                var total_height_pot = _NextPowerOfTwo((uint)total_height);

                var max = Math.Max(total_width_pot, total_height_pot);
                total_width = total_height = (int)max;
            }

            var texture = new Texture2D(total_width, total_height);
            for (int x = 0; x < total_width; x++) {
                for (int y = 0; y < total_height; y++) {
                    texture.SetPixel(x, y, _BlankColor);
                }
            }

            var i = 0;
            for (int y = 0; y < vert_count; y++) {
                if (i >= grid_elements.Count) break;

                for (int x = 0; x < horiz_count; x++) {
                    if (i >= grid_elements.Count) break;

                    var el = grid_elements[i];

                    Console.WriteLine($"({x}, {y}) :: {el.Name} [{i}]");

                    var tx = x * tile_width;
                    var ty = y * tile_height;

                    //Console.WriteLine($"{texture.Width} {texture.Height}");

                    el.DrawOn(texture, tx, ty);

                    data.Entries[el.Name] = new PackedData.Entry {
                        Index = i,
                        X = tx,
                        Y = ty,
                        GridX = x,
                        GridY = y,
                        Width = el.Width,
                        Height = el.Height
                    };

                    i += 1;
                }
            }

            data.Output = texture;

            return data;
        }
    }
}
