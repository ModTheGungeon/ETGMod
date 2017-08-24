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

            for (int i = 0; i < Elem.Children.Count; i++) {
                SElement child = Elem.Children[i];
                if (child.Modifiers.Where(m => m is SFadeAnimation) != null) continue;
                SFadeAnimation clone = (SFadeAnimation) MemberwiseClone();
                clone.Elem = child;
                child.Modifiers.Add(clone);
            }
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
