using System.Diagnostics;
using UnityEngine;
using System.Linq.Expressions;
using System.Linq;

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

        public abstract void CopyTo(SElement elem);

        public override void OnStart() {
            for (int i = 0; i < Elem.Children.Count; i++) {
                CopyTo(Elem.Children[i]);
            }
        }

        public override void Animate(float t) {
            for (int i = 0; i < OrigColors.Length; i++) {
                Elem.Colors[i] = Animate(t, OrigColors[i]);
            }
        }

        public abstract Color Animate(float t, Color c);
    }
}
