using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public abstract class SFadeAnimation : SAnimation {

        public SFadeAnimation()
            : this(0.3f) {
        }

        public SFadeAnimation(float duration)
            : base(duration) {
        }

        public Color[] OrigColors;

        public override void OnInit() {
            OrigColors = new Color[Elem.Colors.Length];
            Elem.Colors.CopyTo(OrigColors, 0);
        }

        public override void Animate(float t) {
            for (int i = 0; i < OrigColors.Length; i++) {
                Elem.Colors[i] = Animate(t, OrigColors[i]);
            }
        }

        public abstract Color Animate(float t, Color c);

        public override void OnStart() {
        }

    }
}
