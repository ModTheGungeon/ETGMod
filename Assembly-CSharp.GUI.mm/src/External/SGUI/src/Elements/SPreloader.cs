using System;
using UnityEngine;

namespace SGUI {
    public class SPreloader : SElement {

        public Vector2 IndividualSize = new Vector2(1f, 1f);
        public Vector2 Padding = new Vector2(0f, 0f);
        public Vector2 Count = new Vector2(32, 32);

        public bool IsCentered = true;

        public Func<float, float, float, float> Function = DefaultFunction;

        public override Vector2 InnerSize {
            get {
                return new Vector2(
                    (IndividualSize.x + Padding.x) * Count.x - Padding.x,
                    (IndividualSize.y + Padding.y) * Count.y - Padding.y
                );
            }
        }

        public override Vector2 InnerOrigin {
            get {
                if (!IsCentered) {
                    return base.InnerOrigin;
                }
                return base.InnerOrigin + Size * 0.5f - InnerSize * 0.5f;
            }
        }

        public float TimeStart;

        public SPreloader()
            : this(Color.white) { }
        public SPreloader(Color color) {
            ColorCount = 1;
            Foreground = color;
            TimeStart = SGUIRoot.TimeUnscaled;
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (UpdateBounds) {
                Size = InnerSize;
                if (Parent != null) Size.x = Parent.InnerSize.x;
            }

            base.UpdateStyle();
        }

        public override void Render() {
            float t = SGUIRoot.TimeUnscaled - TimeStart;
            float dx = IndividualSize.x + Padding.x;
            float dy = IndividualSize.y + Padding.y;
            for (int y = 0; y < Count.y; y++) {
                for (int x = 0; x < Count.x; x++) {
                    Color color = Foreground;
                    color.a *= Function((x + 0.5f) / Count.x, (y + 0.5f) / Count.y, t);
                    Draw.Texture(this, new Vector2(
                        dx * x,
                        dy * y
                    ), IndividualSize, SGUIRoot.White, color);
                }
            }
        }

        internal readonly static Vector2 _V2_05_05 = new Vector2(0.5f, 0.5f);
        internal static float _PSIN(float a) { return 0.5f + 0.5f * Mathf.Sin(a); }
        internal static float _SmoothStep(float a, float b, float x) {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - (2f * t));
        }
        internal static float _SS(float s, float r, float v) { return _SmoothStep(s - r    , s + r    , v); }
        internal static float _SS(float s, float v         ) { return _SmoothStep(s - 0.02f, s + 0.02f, v); }


        internal static float _Circle(Vector2 xy, float or, float ir) {
            float r = (xy - _V2_05_05).magnitude;
            return (1f - _SS(or, r)) * _SS(or - ir, r);
        }

        public static float LoadRot(Vector2 xy, float f) {
            f = ((f * 0.5f) % 1f) * -Mathf.PI * 2f;
            xy -= _V2_05_05;
            xy /= xy.magnitude;
            return _SS(0.5f, xy.x * Mathf.Cos(f) + xy.y * Mathf.Sin(f));
        }

        public static float LoadJump(Vector2 xy, float f) {
            f = (f % 1f) * 2f - 2f;
            xy -= _V2_05_05;
            xy /= xy.magnitude;

            float rf = 0f;
            float ss = 0f;

            rf += _SS(ss, xy.y * Mathf.Cos(Mathf.PI * Mathf.Min(0f, f)));
            rf += _SS(ss, xy.x * Mathf.Sin(Mathf.PI * Mathf.Min(0.5f, f)));
            rf += _SS(ss, -xy.y * -Mathf.Cos(Mathf.PI * Mathf.Min(1f, f)));
            rf += _SS(ss, -xy.x * -Mathf.Sin(Mathf.PI * Mathf.Min(1.5f, f)));

            return 1f - Mathf.Clamp01(rf);
        }

        public static float DefaultFunction(float x, float y, float t) {
            y = 1f - y;
            Vector2 xy = new Vector2(x, y);
            float a = _Circle(xy, 0.45f, 0.1f);

            a *= Mathf.Abs(LoadRot(xy, t));

            return a;
        }

    }
}
