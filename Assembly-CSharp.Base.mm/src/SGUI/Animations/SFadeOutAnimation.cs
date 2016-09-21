using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SFadeOutAnimation : SFadeAnimation {

        public SFadeOutAnimation()
            : this(0.3f) {
        }

        public SFadeOutAnimation(float duration)
            : base(duration) {
            Duration = duration;
        }

        public override void Animate(float t) {
            t = 1f - t;
            Elem.Foreground = OrigForeground.WithAlpha(t * OrigForeground.a);
            Elem.Background = OrigBackground.WithAlpha(t * OrigBackground.a);
        }

    }
}
