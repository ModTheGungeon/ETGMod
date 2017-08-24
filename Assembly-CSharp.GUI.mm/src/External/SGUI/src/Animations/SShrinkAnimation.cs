using System;
using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    public class SShrinkAnimation : SAnimation {

        public SShrinkAnimation()
            : this(0.3f) {
        }
        public SShrinkAnimation(float duration)
            : base(duration) {
        }

        protected Vector2 _OriginalSize;

        public override void OnInit() {
            AutoStart = false;
            Elem.UpdateBounds = false;
        }

        public override void OnStart() {
            Loop = false;

            _OriginalSize = Elem.Size;
        }

        public override void Animate(float t) {
            Elem.Size = _OriginalSize * (1f - t);
            if (Elem.Parent != null) Elem.Parent.UpdateStyle();
        }

        public override void OnEnd() {
            Elem.Remove();
            base.OnEnd();
        }

    }
}
