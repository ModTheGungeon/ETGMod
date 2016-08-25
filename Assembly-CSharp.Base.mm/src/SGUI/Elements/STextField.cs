using System;
using UnityEngine;

namespace SGUI {
    public class STextField : SElement {

        public string Text;
        public string TextOnSubmit = "";

        public Action<STextField, string> OnTextUpdate;
        public Action<STextField, bool, KeyCode> OnKey;
        public Action<STextField, string> OnSubmit;

        public override bool IsInteractive {
            get {
                return true;
            }
        }

        public STextField()
            : this("") { }
        public STextField(string text) {
            Text = text;
            Background = Color.white;
            Foreground = Color.black;
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (UpdateBounds) {
                Size.y = Backend.LineHeight;
            }

            base.UpdateStyle();
        }

        public override void RenderBackground() { }
        public override void Render() {
            // Do not render background - background should be handled by Draw.TextField

            Draw.TextField(this, Vector2.zero, Size, ref Text);

            // Focusing should happen when the element has got a valid ID (after rendering the element) and after checking for it.
            if (_ScheduleFocus) {
                _ScheduleFocus = false;
                Backend.Focus(this);
            }
        }

        protected bool _ScheduleFocus;
        public override void Focus() {
            _ScheduleFocus = true;
        }

    }
}
