using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SFadeInModifier : STimedModifier {

        public SFadeInModifier()
            : this(0.3f) {
        }

        public SFadeInModifier(float duration)
            : base(duration) {
            Duration = duration;
        }

        public Color OrigForeground;
        public Color OrigBackground;

        public Color TransparentForeground;
        public Color TransparentBackground;

        public override void Start() {
            OrigForeground = Elem.Foreground;
            OrigBackground = Elem.Background;
        }

        public override void Animate(float t) {
            Elem.Foreground = OrigForeground.WithAlpha(t * OrigForeground.a);
            Elem.Background = OrigBackground.WithAlpha(t * OrigBackground.a);
        }

    }
}
