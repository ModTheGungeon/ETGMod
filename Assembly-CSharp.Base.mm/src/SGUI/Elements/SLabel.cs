using UnityEngine;

namespace SGUI {
    public class SLabel : SElement {

        public string Text;

        public SLabel()
            : this("") { }
        public SLabel(string text) {
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
