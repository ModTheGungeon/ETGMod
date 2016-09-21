using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SFadeInAnimation : SFadeAnimation {

        public SFadeInAnimation()
            : this(0.3f) {
        }

        public SFadeInAnimation(float duration)
            : base(duration) {
            Duration = duration;
        }

        public override void Animate(float t) {
            Elem.Foreground = OrigForeground.WithAlpha(t * OrigForeground.a);
            Elem.Background = OrigBackground.WithAlpha(t * OrigBackground.a);
        }

    }
}
