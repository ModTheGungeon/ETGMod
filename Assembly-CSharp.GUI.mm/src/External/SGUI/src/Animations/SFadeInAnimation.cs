using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SFadeInAnimation : SFadeAnimation {

        public SFadeInAnimation()
            : this(0.3f) {
        }

        public SFadeInAnimation(float duration)
            : base(duration) {
        }

        public override Color Animate(float t, Color c) {
            c.a *= t;
            return c;
        }

    }
}
