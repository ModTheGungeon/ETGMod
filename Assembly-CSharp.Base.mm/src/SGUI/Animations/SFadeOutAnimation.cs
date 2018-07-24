﻿using System.Diagnostics;
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
            return c.WithAlpha((1f - t) * c.a);
        }

    }
}
