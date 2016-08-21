using System;
using UnityEngine;

namespace SGUI {
    public class STextField : SElement {

        public string Text;

        public Action<STextField, string> OnTextChanged;

        public STextField()
            : this("") { }
        public STextField(string text) {
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
