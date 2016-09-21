using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SFadeOutModifier : STimedModifier {

        public SFadeOutModifier()
            : this(0.3f) {
        }

        public SFadeOutModifier(float duration)
            : base(duration) {
            Duration = duration;
        }

        public Color OrigForeground;
        public Color OrigBackground;

        public Color TransparentForeground;
        public Color TransparentBackground;

        public override void OnStart() {
            OrigForeground = Elem.Foreground;
            OrigBackground = Elem.Background;
        }

        public override void Animate(float t) {
            Elem.Foreground = OrigForeground.WithAlpha(t * OrigForeground.a);
            Elem.Background = OrigBackground.WithAlpha(t * OrigBackground.a);
        }

    }
}
