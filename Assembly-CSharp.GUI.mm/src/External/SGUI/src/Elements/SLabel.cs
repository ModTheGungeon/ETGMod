using UnityEngine;

namespace SGUI {
    public class SLabel : SElement {

        public string Text;
        public Texture Icon;
        public FontStyle FontStyle = FontStyle.Normal;

        public Vector2 IconScale = Vector2.one;
        public Color IconColor {
            get {
                return Colors[1];
            }
            set {
                Colors[1] = value;
            }
        }

        public TextAnchor Alignment = TextAnchor.MiddleLeft;

        public SLabel()
            : this("") { }
        public SLabel(string text) {
            Text = text;
            ColorCount = 3;
            IconColor = UnityEngine.Color.white;
            Background = Background * 0f;
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (UpdateBounds) {
                if (Parent == null) {
                    Size = Backend.MeasureText(ref Text);
                } else {
                    Size = Backend.MeasureText(ref Text, Parent.InnerSize.x - (Icon == null ? 0 : Icon.width * IconScale.x + 1f + Backend.IconPadding), font: Font);
                }
                if (Icon != null) {
                    Size.y = Mathf.Max(Size.y, Icon.height * IconScale.y + Backend.IconPadding);
                }
            }

            base.UpdateStyle();
        }

        public override void Render() {
            RenderBackground();
            Draw.Text(this, Vector2.zero, Size, Text, Alignment, Icon, Foreground);
        }

    }
}
