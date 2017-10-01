#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;

namespace ETGMod.BasePatches {
    // hacky stuff for passing the scale and region of a parsed definition

    [MonoModPatch("global::tk2dSpriteDefinition")]
    public class tk2dSpriteDefinition : global::tk2dSpriteDefinition {
        public int ETGModOffsetX {
            get {
                return (int)position0.x;
            }
        }

        public int ETGModOffsetY {
            get {
                return (int)position0.y;
            }
        }

        public int ETGModCropWidth {
            get {
                return regionW;
            }
        }

        public int ETGModCropHeight {
            get {
                return regionH;
            }
        }

        public int ETGModCropX {
            get {
                return regionX;
            }
        }

        public int ETGModCropY {
            get {
                return regionY;
            }
        }

        public float ETGModScaleW {
            get {
                var w = position3.x - ETGModOffsetX;
                return w * 16f / regionW;
            }
        }

        public float ETGModScaleH {
            get {
                var w = position3.y - ETGModOffsetY;
                return w * 16f / regionH;
            }
        }

        public float ETGModScaledWidth {
            get {
                return (position3.x - ETGModOffsetX) * 16f;
            }
        }

        public float ETGModScaledHeight {
            get {
                return (position3.y - ETGModOffsetY) * 16f;
            }
        }
    }
}