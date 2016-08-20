using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI {
    public static class FontConverter {

        public static Vector3 Offset = new Vector3 (0, 0, 0);
        public static Vector3 Size = new Vector3(0, 0, 0);

        public static Font GetFontFromdfFont(dfFont font) {
            Font f = new Font();

            f.material = new Material(GUI.skin.font.material);
            f.material.mainTexture = font.Texture;
            f.material.mainTexture.wrapMode = TextureWrapMode.Repeat;

            CharacterInfo[] chars = new CharacterInfo[font.Glyphs.Count];

            int maxHeight = 0;

            Debug.Log(font.Glyphs.Count);

            for (int i = 0; i<font.Glyphs.Count; i++) {

                CharacterInfo inf = new CharacterInfo();
                dfFont.GlyphDefinition glyphDefinition = font.Glyphs[i];

                dfAtlas.ItemInfo itemInfo = font.Atlas[font.Sprite];

                inf.glyphWidth = glyphDefinition.width;
                inf.glyphHeight = glyphDefinition.height;

                inf.size = (int) Size.x;

                if (inf.glyphHeight>maxHeight)
                    maxHeight = inf.glyphHeight;

                inf.index = glyphDefinition.id;

                float left = itemInfo.region.x + glyphDefinition.x * f.material.mainTexture.texelSize.x;
                float top = itemInfo.region.yMax - glyphDefinition.y * f.material.mainTexture.texelSize.y;
                float right = left + glyphDefinition.width * f.material.mainTexture.texelSize.x;
                float bottom = top - glyphDefinition.height * f.material.mainTexture.texelSize.y;

                inf.uvBottomLeft = new Vector2(left, bottom);
                inf.uvBottomRight = new Vector2(right, bottom);
                inf.uvTopRight = new Vector2(right, top);
                inf.uvTopLeft = new Vector2(left, top);

                inf.advance = glyphDefinition.xadvance;

                inf.minY=0;
                inf.maxY = inf.glyphHeight;
                inf.minX=0;
                inf.maxX = inf.glyphWidth;

                chars[i] = inf;
            }

            f.characterInfo = chars;

            return f;
        }

    }
}
