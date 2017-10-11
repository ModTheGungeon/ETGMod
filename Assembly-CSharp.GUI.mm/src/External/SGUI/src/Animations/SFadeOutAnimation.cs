using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SFadeOutAnimation : SFadeAnimation {

        public SFadeOutAnimation()
            : this(0.3f) {
        }

        public SFadeOutAnimation(float duration)
            : base(duration) {
        }

        public override Color Animate(float t, Color c) {
            c.a *= 1f - t;
            return c;
        }

        public override void CopyTo(SElement elem) {
            elem.Modifiers.Add(new SFadeOutAnimation(Duration));
        }

    }
}
