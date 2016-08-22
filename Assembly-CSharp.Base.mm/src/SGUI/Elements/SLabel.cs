using UnityEngine;

namespace SGUI {
    public class SLabel : SElement {

        public string Text;

        public SLabel()
            : this("") { }
        public SLabel(string text) {
            Text = text;
            Background = Background.WithAlpha(0f);
        }

        public override void UpdateStyle() {
            if (UpdateBounds) {
                Size = Backend.MeasureText(Text);
            }

            base.UpdateStyle();
        }

        public override void Render() {
            RenderBackground();
            Draw.Text(this, Vector2.zero, Text);
        }

    }
}
