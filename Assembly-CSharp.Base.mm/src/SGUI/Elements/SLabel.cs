using UnityEngine;

namespace SGUI {
    public class SLabel : SElement {

        public string Text;

        public TextAnchor Alignment = TextAnchor.MiddleLeft;

        public SLabel()
            : this("") { }
        public SLabel(string text) {
            Text = text;
            Background = Background.WithAlpha(0f);
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
            }

            base.UpdateStyle();
        }

        public override void Render() {
            RenderBackground();
            Draw.Text(this, Vector2.zero, Size, Text, Alignment);
        }

    }
}
