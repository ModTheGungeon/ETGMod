using System;
using UnityEngine;

namespace SGUI {
    public class SButton : SElement {

        public string Text;

        /// <summary>
        /// On button status event. Gets fired by the backend itself if it supports listening for this.
        /// </summary>
        public Action<SButton, bool> OnChange;
        public Action<SButton> OnClick;

        public override bool IsInteractive {
            get {
                return true;
            }
        }

        public bool IsClicked { get; protected set; }

        public SButton()
            : this("") { }
        public SButton(string text) {
            Text = text;
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (UpdateBounds) {
                Size = Backend.MeasureText(Text) + new Vector2(16f, 2f);
            }

            base.UpdateStyle();
        }

        public override void RenderBackground() { }
        public override void Render() {
            // Do not render background - background should be handled by Draw.Button

            if (IsClicked = Draw.Button(this, Vector2.zero, Size, Text)) {
                OnClick?.Invoke(this);
            }

            // Focusing should happen when the element has got a valid ID (after rendering the element).
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
