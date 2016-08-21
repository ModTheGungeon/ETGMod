using System;
using UnityEngine;

namespace SGUI {
    public class SGUITextField : SGUIElement {

        public string Text;

        public Action<SGUITextField, string> OnTextChanged;

        public SGUITextField()
            : this("") { }
        public SGUITextField(string text) {
            Text = text;
            Background = Color.white;
            Foreground = Color.black;
        }

        public override void UpdateStyle() {
            if (UpdateBounds) {
                Size.y = Backend.LineHeight;
            }

            base.UpdateStyle();
        }

        public override void Render() {
            string prevText = Text;
            Draw.TextField(this, Vector2.zero, ref Text);
            if (prevText != Text) {
                OnTextChanged?.Invoke(this, prevText);
            }
        }

    }
}
