using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI {
    static class FontConverter {

        public static Vector3 offset, size;

        public static Font GetFontFromdfFont(dfFont font) {
            Font f = new Font();

            f.material=new Material(GUI.skin.font.material);
            f.material.mainTexture=font.Texture;
            f.material.mainTexture.wrapMode=TextureWrapMode.Repeat;

            CharacterInfo[] chars = new CharacterInfo[font.Glyphs.Count];

            int maxHeight = 0;

            Debug.Log(font.Glyphs.Count);

            for (int i = 0; i<font.Glyphs.Count; i++) {

                CharacterInfo inf = new CharacterInfo();
                dfFont.GlyphDefinition glyphDefinition = font.Glyphs[i];

                dfAtlas.ItemInfo itemInfo = font.Atlas[font.Sprite];

                inf.glyphWidth=glyphDefinition.width;
                inf.glyphHeight=glyphDefinition.height;

                inf.size=(int)size.x;

                if (inf.glyphHeight>maxHeight)
                    maxHeight=inf.glyphHeight;

                inf.index=glyphDefinition.id;

                float num9 = itemInfo.region.x+(float)glyphDefinition.x*f.material.mainTexture.texelSize.x;
                float num10 = itemInfo.region.yMax-(float)glyphDefinition.y*f.material.mainTexture.texelSize.y;
                float num11 = num9+(float)glyphDefinition.width*f.material.mainTexture.texelSize.x;
                float num12 = num10-(float)glyphDefinition.height*f.material.mainTexture.texelSize.y;

                inf.uvBottomLeft=( new Vector2(num9, num12) );
                inf.uvBottomRight=( new Vector2(num11, num12) );
                inf.uvTopRight=( new Vector2(num11, num10) );
                inf.uvTopLeft=( new Vector2(num9, num10) );

                inf.advance=glyphDefinition.xadvance;

                inf.minY=0;
                inf.maxY=inf.glyphHeight;
                inf.minX=0;
                inf.maxX=inf.glyphWidth;

                chars[i]=inf;
            }

            f.characterInfo=chars;

            return f;
        }

    }
}
