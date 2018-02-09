using UnityEngine;

public static class Texture2DExt {
    public static bool IsReadable(this Texture2D texture) {
#if DEBUG
        try {
            texture.GetPixels();
            return true;
        } catch {
            return false;
        }
#else
        return texture.GetRawTextureData().Length != 0; // spams log
#endif
    }

    public static Texture2D GetRW(this Texture2D texture) {
        if (texture == null) {
            return null;
        }
        if (texture.IsReadable()) {
            return texture;
        }
        return texture.Copy();
    }

    public static Texture2D Copy(this Texture2D texture, TextureFormat? format = TextureFormat.ARGB32) {
        if (texture == null) {
            return null;
        }
        RenderTexture copyRT = RenderTexture.GetTemporary(
            texture.width, texture.height, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Default
        );

        Graphics.Blit(texture, copyRT);

        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = copyRT;

        Texture2D copy = new Texture2D(texture.width, texture.height, format != null ? format.Value : texture.format, 1 < texture.mipmapCount);
        copy.name = texture.name;
        copy.ReadPixels(new Rect(0, 0, copyRT.width, copyRT.height), 0, 0);
        copy.Apply(true, false);

        RenderTexture.active = previousRT;
        RenderTexture.ReleaseTemporary(copyRT);

        return copy;
    }
}