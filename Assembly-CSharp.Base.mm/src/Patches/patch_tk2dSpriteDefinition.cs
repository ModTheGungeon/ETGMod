#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;

namespace ETGMod.BasePatches {
    [MonoModPatch("global::tk2dSpriteDefinition")]
    public class tk2dSpriteDefinition : global::tk2dSpriteDefinition {
        /*
         * Tk2d RE
         * 
         * PositionX pattern:
         *   [W_LOW, H_LOW]
         *   [W_HIGH, H_LOW]
         *   [W_LOW, H_HIGH]
         *   [W_HIGH, H_HIGH]
         * 
         *   W_LOW  is X offset
         *   W_HIGH is X offset + the width of the cropped image
         *   H_LOW  is Y offset - the height of the cropped image
         *   H_HIGH is Y offset
         *  
         *   Note how H_LOW starts from the far left instead of the origin
         *   and ends on the origin instead of the far right
         * 
         * Texel size ("the unit", scale your pixels by this):
         *   1/16; 1/16
         * 
         * Check tk2dRuntime.SpriteCollectionGenerator/CreateDefinitionForRegionInTexture
         * for more
         */

        public int GetCropWidth(UnityEngine.Texture tex) {
            if (extractRegion) return regionW;

            // I can't really guarantee the UV order.
            // But I can guarantee that there will be 2 different Xs
            // in the 4 UV vectors.
            // So I pick the first X, and then I search for an X
            // that's different to the first one. That'll be my second X.
            // If for some strange reason I don't find a different one
            // that means that the sprite is a 1D line, which doesn't
            // make much sense but we go with it anyway.
            float uvx1 = uvs[0].x;

            float uvx2 = uvx1;

            // Manually unrolled loop
            // I'm checking for the exact same number
            // so ignore the note about a float comparison
            if (uvs[1].x != uvx1) uvx2 = uvs[1].x;
            else if (uvs[2].x != uvx1) uvx2 = uvs[2].x;
            else if (uvs[3].x != uvx1) uvx2 = uvs[3].x;

            // We don't waste time and lines on checking
            // which number is bigger and which one smaller,
            // we just subtract and get the absolute.
            // The values in the UVs are divided by the width
            // of the texture so let's reverse that.
            return (int)(Math.Ceiling(Math.Abs(uvx1 - uvx2) * tex.width));
        }

        public int GetCropHeight(UnityEngine.Texture tex) {
            if (extractRegion) return regionH;

            // Pretty much the same thing as GetCropWidth

            float uvy1 = uvs[0].y;

            float uvy2 = uvy1;

            if (uvs[1].y != uvy1) uvy2 = uvs[1].y;
            else if (uvs[2].y != uvy1) uvy2 = uvs[2].y;
            else if (uvs[3].y != uvy1) uvy2 = uvs[3].y;

            return (int)(Math.Ceiling(Math.Abs(uvy1 - uvy2) * tex.height));
        }

        public int GetCropX(UnityEngine.Texture tex) {
            if (extractRegion) return regionX;

            // Much easier than GetCropWidth and GetCropHeight
            // We just have to find the smaller number

            var smallest = uvs[0].x;
            if (uvs[1].x < smallest) smallest = uvs[1].x;
            else if (uvs[2].x < smallest) smallest = uvs[2].x;
            else if (uvs[3].x < smallest) smallest = uvs[3].x;

            return (int)(smallest * tex.width);
        }

        public int GetCropY(UnityEngine.Texture tex) {
            if (extractRegion) return regionY;

            // Similiar to GetCropX, but we have to convert the
            // coordinate origin here since ETGMod uses top-left
            // origin and tk2d uses bottom-left

            var smallest = uvs[0].y;
            if (uvs[1].y < smallest) smallest = uvs[1].y;
            else if (uvs[2].y < smallest) smallest = uvs[2].y;
            else if (uvs[3].y < smallest) smallest = uvs[3].y;

            var crop_height = GetCropHeight(tex);
            var bottom_left_origin_y = smallest * tex.height;
            // top left -> bottom left conversion:
            //   y = texture.height - 2 - height - definition.Y
            // reversed:
            //   -definition.Y = y - texture.height + 2 + height
            //   definition.Y = -y + texture.height - 2 - height
            //   definition.Y = texture.height - 2 - height - y

            return (int)Math.Ceiling(tex.height - crop_height - bottom_left_origin_y);
        }

        public int GetETGModOffsetX() {
            // Deja vu?

            var smallest = position0.x;
            if (position1.x < smallest) smallest = position1.x;
            else if (position2.x < smallest) smallest = position2.x;
            else if (position3.x < smallest) smallest = position3.x;

            return (int)(smallest * 16f);
        }

        public int GetETGModOffsetY() {
            var smallest = position0.y;
            if (position1.y < smallest) smallest = position1.y;
            else if (position2.y < smallest) smallest = position2.y;
            else if (position3.y < smallest) smallest = position3.y;

            return (int)(smallest * 16f);
        }

        public float GetScaleW(UnityEngine.Texture tex) {
            var biggest = position0.x;
            if (position1.x > biggest) biggest = position1.x;
            else if (position2.x > biggest) biggest = position2.x;
            else if (position3.x > biggest) biggest = position3.x;

            // if it's flipped, we have to use the other image dimension
            float dimension = 0;
            if (flipped == FlipMode.None) dimension = GetCropWidth(tex);
            else dimension = GetCropHeight(tex);

            return (biggest * 16f - GetETGModOffsetX()) / dimension;
        }

        public float GetScaleH(UnityEngine.Texture tex) {
            // var h = height * hscale / 16f;

            var biggest = position0.y;
            if (position1.y > biggest) biggest = position1.y;
            else if (position2.y > biggest) biggest = position2.y;
            else if (position3.y > biggest) biggest = position3.y;

            float dimension = 0;
            if (flipped == FlipMode.None) dimension = GetCropHeight(tex);
            else dimension = GetCropWidth(tex);

            return (biggest * 16f - GetETGModOffsetY()) / dimension;
        }

        public bool IsGeneratedByETGMod {
            get {
                return !extractRegion && (
                    regionX == Animation.Collection.CollectionGenerator.ETGMOD_ID_X &&
                    regionY == Animation.Collection.CollectionGenerator.ETGMOD_ID_Y &&
                    regionW == Animation.Collection.CollectionGenerator.ETGMOD_ID_W &&
                    regionH == Animation.Collection.CollectionGenerator.ETGMOD_ID_H
                );
            }
        }
    }
}