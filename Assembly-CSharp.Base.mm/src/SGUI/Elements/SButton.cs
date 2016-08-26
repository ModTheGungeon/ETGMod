using System;
using UnityEngine;

namespace SGUI {
    public class SButton : SElement {

        public string Text;
        public Texture Icon;

        public TextAnchor Alignment = TextAnchor.MiddleLeft;

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
                if (Parent == null) {
                    Size = Backend.MeasureText(Text);
                } else {
                    Size = Backend.MeasureText(Text, Parent.Size);
                }
                Size += new Vector2(16f, 2f);
                if (Icon != null) {
                    Size = Size.WithX(Size.x + Size.y + 4f);
                }
            }

            base.UpdateStyle();
        }

        public override void RenderBackground() { }
        public override void Render() {
            // Do not render background - background should be handled by Draw.Button

            if (IsClicked = Draw.Button(this, Vector2.zero, Size, Text, Alignment, Icon)) {
                OnClick?.Invoke(this);
            }

            if (_StatusChanged) {
                OnChange?.Invoke(this, _Status);
            }
        }

        protected bool _StatusChanged;
        protected bool _Status;
        public virtual void SetStatus(int secret, bool status) {
            Backend.VerifySecret(secret);
            _StatusChanged = _Status != status;
            _Status = status;
        }

    }
}
