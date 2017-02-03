using System;
using UnityEngine;
using System.Collections.Generic;

public class RuntimeAtlasPacker {

    public List<RuntimeAtlasPage> Pages = new List<RuntimeAtlasPage>();

    public int Width;
    public int Height;
    public TextureFormat Format;
    public int Padding;

    public RuntimeAtlasPacker(int width = 0, int height = 0, TextureFormat format = TextureFormat.RGBA32, int padding = 2) {
        if (width == 0) width = RuntimeAtlasPage.DefaultSize;
        if (height == 0) height = RuntimeAtlasPage.DefaultSize;

        Width = width;
        Height = height;
        Format = format;
        Padding = padding;
    }

    public RuntimeAtlasSegment Pack(Texture2D tex, bool apply = false) {
        RuntimeAtlasSegment segment;

        for (int i = 0; i < Pages.Count; i++) {
            if ((segment = Pages[i].Pack(tex, apply)) != null) {
                return segment;
            }
        }

        return NewPage().Pack(tex, apply);
    }

    public Action<RuntimeAtlasPage> OnNewPage;
    public RuntimeAtlasPage NewPage() {
        RuntimeAtlasPage page = new RuntimeAtlasPage(Width, Height, Format, Padding);
        Pages.Add(page);
        OnNewPage?.Invoke(page);
        return page;
    }

    public void Apply() {
        for (int i = 0; i < Pages.Count; i++) {
            Pages[i].Apply();
        }
    }

    public bool IsPageTexture(Texture2D tex) {
        for (int i = 0; i < Pages.Count; i++) {
            if (ReferenceEquals(Pages[i].Texture, tex)) return true;
        }
        return false;
    }

}

public class RuntimeAtlasPage {

    public static int DefaultSize = Math.Min(SystemInfo.maxTextureSize, 4096);

    public List<RuntimeAtlasSegment> Segments = new List<RuntimeAtlasSegment>();
    public Texture2D Texture;

    public int Padding;

    protected Rect _MainRect;
    protected List<Rect> _Rects = new List<Rect>();
    protected int _Changes;

    public RuntimeAtlasPage(int width = 0, int height = 0, TextureFormat format = TextureFormat.RGBA32, int padding = 2) {
        if (width == 0) width = DefaultSize;
        if (height == 0) height = DefaultSize;

        Texture = new Texture2D(width, height, format, false);
        Texture.wrapMode = TextureWrapMode.Clamp;
        Texture.filterMode = FilterMode.Point;
        Texture.anisoLevel = 0;
        Color[] data = new Color[128 * 128];
        Color blank = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < data.Length; i++) {
            data[i] = blank;
        }

        for (int y = 0; y < height; y += 128) {
            for (int x = 0; x < width; x += 128) {
                Texture.SetPixels(x, y, 128, 128, data);
            }
        }

        _MainRect = new Rect(0, 0, width, height);

        Padding = padding;
    }

    private Rect texRect = new Rect();
    private bool right = true;
    public RuntimeAtlasSegment Pack(Texture2D tex, bool apply = false) {
        texRect.Set(Padding, Padding, tex.width + Padding, tex.height + Padding);
        bool fit = _Rects.Count == 0;

        if (right) {
            if (!fit)
                for (int i = _Rects.Count - 1; 0 <= i; i--) {
                    Rect existing = _Rects[i];

                    texRect.x = existing.xMax + Padding;
                    texRect.y = existing.y;
                    if (_Feasible(texRect, i)) {
                        fit = true;
                        right = true;
                        break;
                    }
                }

            if (!fit)
                for (int i = 0; i < _Rects.Count; i++) {
                    Rect existing = _Rects[i];

                    texRect.x = existing.x;
                    texRect.y = existing.yMax + Padding;
                    if (_Feasible(texRect, i)) {
                        fit = true;
                        right = false;
                        break;
                    }
                }
        } else {
            if (!fit)
                for (int i = 0; i < _Rects.Count; i++) {
                    Rect existing = _Rects[i];

                    texRect.x = existing.x;
                    texRect.y = existing.yMax + Padding;
                    if (_Feasible(texRect, i)) {
                        fit = true;
                        right = false;
                        break;
                    }
                }

            if (!fit)
                for (int i = _Rects.Count - 1; 0 <= i; i--) {
                    Rect existing = _Rects[i];

                    texRect.x = existing.xMax + Padding;
                    texRect.y = existing.y;
                    if (_Feasible(texRect, i)) {
                        fit = true;
                        right = true;
                        break;
                    }
                }
        }

        if (!fit) {
            return null;
        }

        _Rects.Add(texRect);

        RuntimeAtlasSegment segment = new RuntimeAtlasSegment() {
            texture = Texture,
            x = Mathf.RoundToInt(texRect.x),
            y = Mathf.RoundToInt(texRect.y),
            width = tex.width,
            height = tex.height
        };
        Segments.Add(segment);

        Texture.SetPixels(segment.x, segment.y, segment.width, segment.height, tex.GetPixels());

        ++_Changes;
        if (apply) {
            Apply();
        }
        return segment;
    }

    protected bool _Feasible(Rect rect, int skip) {
        if (!_MainRect.Contains(rect)) {
            return false;
        }
        for (int i = 0; i < _Rects.Count; i++) {
            if (i == skip) continue;
            Rect existing = _Rects[i];
            if (existing.Overlaps(rect)) {
                return false;
            }
        }
        return true;
    }

    public void Apply() {
        if (_Changes == 0) {
            return;
        }
        _Changes = 0;
        Texture.Apply(false, false);
    }

}

public class RuntimeAtlasSegment {

    public Texture2D texture;

    public int x;
    public int y;
    public int width;
    public int height;

    public Vector2[] uvs {
        get { return ETGMod.Assets.GenerateUVs(texture, x, y, width, height); }
    }

}
