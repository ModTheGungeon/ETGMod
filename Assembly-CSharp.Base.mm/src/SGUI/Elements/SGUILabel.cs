using UnityEngine;

namespace SGUI {
    public class SGUILabel : SGUIElement {

        public string Text;

        public SGUILabel()
            : this("") { }
        public SGUILabel(string text) {
            Text = text;
            Background.a = 0f;
        }

        public override void UpdateStyle() {
            if (UpdateBounds) {
                Size = Backend.MeasureText(Text);
            }

            base.UpdateStyle();
        }

        public override void Render() {
            Draw.Text(this, Vector2.zero, Text);
        }

    }
}
