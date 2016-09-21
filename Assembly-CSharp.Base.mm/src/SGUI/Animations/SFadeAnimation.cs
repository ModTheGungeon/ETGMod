using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public abstract class SFadeAnimation : SAnimation {

        public SFadeAnimation()
            : this(0.3f) {
        }

        public SFadeAnimation(float duration)
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

    }
}
