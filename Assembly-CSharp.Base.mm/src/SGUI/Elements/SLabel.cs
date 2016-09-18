using UnityEngine;

namespace SGUI {
    public class SLabel : SElement {

        public string Text;
        public Texture Icon;

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
                    Size = Backend.MeasureText(ref Text);
                } else {
                    Size = Backend.MeasureText(ref Text, Parent.InnerSize);
                }
                if (Icon != null) {
                    Size = Size.WithX(Size.x + Size.y + 4f);
                }
            }

            base.UpdateStyle();
        }

        public override void Render() {
            RenderBackground();
            Draw.Text(this, Vector2.zero, Size, Text, Alignment, Icon);
        }

    }
}
