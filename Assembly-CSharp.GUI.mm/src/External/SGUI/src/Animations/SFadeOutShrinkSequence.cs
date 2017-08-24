using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SFadeOutShrinkSequence : SAnimationSequence {

        public SFadeOutAnimation FadeOut;
        public SShrinkAnimation Shrink;

        public float FadeOutDuration;
        public float ShrinkDuration;

        public SFadeOutShrinkSequence()
            : this(0.3f, 0.5f) {
        }
        public SFadeOutShrinkSequence(float durationFadeOut, float durationShrink) {
            FadeOutDuration = durationFadeOut;
            ShrinkDuration = durationShrink;
        }

        public override void OnStart() {
            if (FadeOut == null) {
                Sequence.Insert(0, FadeOut = new SFadeOutAnimation(FadeOutDuration));
            }

            if (Shrink == null) {
                Sequence.Add(Shrink = new SShrinkAnimation(ShrinkDuration));
            }

            base.OnStart();
        }

    }
}
