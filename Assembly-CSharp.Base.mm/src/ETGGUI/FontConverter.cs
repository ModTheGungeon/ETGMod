using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI {
    public static class FontConverter {

        public static Font GetFontFromdfFont(dfFont font, int scale = 1) {
            Font f = new Font(font.name);
            f.fontNames = new string[] { f.name };

            f.material = new Material(GUI.skin.font.material);
            f.material.mainTexture = font.Texture;
            f.material.mainTexture.wrapMode = TextureWrapMode.Repeat;

            CharacterInfo[] chars = new CharacterInfo[font.Glyphs.Count];

            for (int i = 0; i < chars.Length; i++) {
                CharacterInfo inf = new CharacterInfo();
                dfFont.GlyphDefinition glyphDefinition = font.Glyphs[i];
                dfAtlas.ItemInfo itemInfo = font.Atlas[font.Sprite];

                inf.glyphWidth = glyphDefinition.width * scale;
                inf.glyphHeight = glyphDefinition.height * scale;
                inf.size = 0; // Must be 0

                inf.index = glyphDefinition.id;

                float left = itemInfo.region.x + glyphDefinition.x * f.material.mainTexture.texelSize.x;
                float right = left + glyphDefinition.width * f.material.mainTexture.texelSize.x;
                float top = itemInfo.region.yMax - glyphDefinition.y * f.material.mainTexture.texelSize.y;
                float bottom = top - glyphDefinition.height * f.material.mainTexture.texelSize.y;

                inf.uvTopLeft = new Vector2(left, top);
                inf.uvTopRight = new Vector2(right, top);
                inf.uvBottomLeft = new Vector2(left, bottom);
                inf.uvBottomRight = new Vector2(right, bottom);

                inf.advance = glyphDefinition.xadvance * scale;

                inf.minY = scale * -glyphDefinition.height / 2;
                inf.maxY = scale * glyphDefinition.height / 2;

                chars[i] = inf;
            }

            f.characterInfo = chars;

            return f;
        }

    }
}
