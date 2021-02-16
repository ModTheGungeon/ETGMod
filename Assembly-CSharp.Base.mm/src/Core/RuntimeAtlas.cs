using System;
using UnityEngine;
using System.Collections.Generic;

public class RuntimeAtlasPacker
{
    public List<RuntimeAtlasPage> Pages = new List<RuntimeAtlasPage>();

    public int Width;
    public int Height;
    public TextureFormat Format;
    public int Padding;

    public RuntimeAtlasPacker(int width = 0, int height = 0, TextureFormat format = TextureFormat.RGBA32, int padding = 2)
    {
        if (width == 0) width = RuntimeAtlasPage.DefaultSize;
        if (height == 0) height = RuntimeAtlasPage.DefaultSize;

        Width = width;
        Height = height;
        Format = format;
        Padding = padding;
    }

    public RuntimeAtlasSegment Pack(Texture2D tex, bool apply = false)
    {
        RuntimeAtlasSegment segment;

        for (int i = 0; i < Pages.Count; i++)
        {
            if ((segment = Pages[i].Pack(tex, apply)) != null)
            {
                return segment;
            }
        }

        return NewPage().Pack(tex, apply);
    }

    public Action<RuntimeAtlasPage> OnNewPage;
    public RuntimeAtlasPage NewPage()
    {
        RuntimeAtlasPage page = new RuntimeAtlasPage(Width, Height, Format, Padding);
        Pages.Add(page);
        OnNewPage?.Invoke(page);
        return page;
    }

    public void Apply()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].Apply();
        }
    }

    public bool IsPageTexture(Texture2D tex)
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            if (ReferenceEquals(Pages[i].Texture, tex)) return true;
        }
        return false;
    }
}

public class RuntimeAtlasPage
{
    public static int DefaultSize = Math.Min(SystemInfo.maxTextureSize, 4096);

    public List<RuntimeAtlasSegment> Segments = new List<RuntimeAtlasSegment>();
    public Texture2D Texture;

    public int Padding;

    protected Rect _MainRect;
    protected List<Rect> _Rects = new List<Rect>();
    protected int _Changes;

    private static readonly int BlockSize = 128;
    private static readonly Color32[] DefaultBlock = CreateDefaultBlock();
    private static readonly Color32 Default = new Color32(0, 0, 0, 0);

    private float _startingX, _currentY, _nextYLine;

    public RuntimeAtlasPage(int width = 0, int height = 0, TextureFormat format = TextureFormat.RGBA32, int padding = 2)
    {
        if (width == 0) width = DefaultSize;
        if (height == 0) height = DefaultSize;

        Texture = new Texture2D(width, height, format, false);
        Texture.wrapMode = TextureWrapMode.Clamp;
        Texture.filterMode = FilterMode.Point;
        Texture.anisoLevel = 0;

        for (int y = 0; y < height; y += BlockSize)
        {
            for (int x = 0; x < width; x += BlockSize)
            {
                Texture.SetPixels32(x, y, BlockSize, BlockSize, DefaultBlock);
            }
        }

        _MainRect = new Rect(0, 0, width, height);

        Padding = padding;
    }

    public RuntimeAtlasSegment Pack(Texture2D tex, bool apply = false)
    {
        var texRect = new Rect();
        texRect.Set(_startingX + Padding, _currentY + Padding, tex.width + Padding, tex.height + Padding);

        if (_MainRect.Contains(texRect))
        {
            _startingX = texRect.xMax;
            _nextYLine = Math.Max(texRect.yMax, _nextYLine);
        }
        else
        {
            _startingX = 0;
            _currentY = _nextYLine;

            texRect.Set(_startingX + Padding, _currentY + Padding, tex.width + Padding, tex.height + Padding);

            if (_MainRect.Contains(texRect))
            {
                _startingX = texRect.xMax;
                _nextYLine = Math.Max(texRect.yMax, _nextYLine);
            }
            else
            {
                return null;
            }
        }

        _Rects.Add(texRect);

        var segment = new RuntimeAtlasSegment()
        {
            texture = Texture,
            x = Mathf.RoundToInt(texRect.x),
            y = Mathf.RoundToInt(texRect.y),
            width = tex.width,
            height = tex.height
        };

        Segments.Add(segment);

        Texture.SetPixels32(segment.x, segment.y, segment.width, segment.height, tex.GetPixels32());

        ++_Changes;
        if (apply)
        {
            Apply();
        }
        return segment;
    }

    public void Apply()
    {
        if (_Changes == 0)
        {
            return;
        }
        _Changes = 0;
        Texture.Apply(false, false);
    }

    private static Color32[] CreateDefaultBlock()
    {
        Color32[] data = new Color32[BlockSize * BlockSize];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Default;
        }

        return data;
    }
}

public class RuntimeAtlasSegment
{
    public Texture2D texture;

    public int x;
    public int y;
    public int width;
    public int height;

    public Vector2[] uvs
    {
        get { return ETGMod.Assets.GenerateUVs(texture, x, y, width, height); }
    }
}
