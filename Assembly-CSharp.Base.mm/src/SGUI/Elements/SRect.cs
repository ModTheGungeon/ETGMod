using UnityEngine;

namespace SGUI {
    public class SRect : SElement {

        public bool Filled = true;
        public float Thickness = 1f;

        public SRect()
            : this(Color.white) { }
        public SRect(Color color) {
            ColorCount = 1;
            Foreground = color;
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            base.UpdateStyle();
        }

        public override void Render() {
            if (Filled) {
                Draw.Rect(this, Vector2.zero, Size, Foreground);
            } else {
                Draw.Rect(this, new Vector2(0f, 0f), new Vector2(Thickness, Size.y), Foreground);
                Draw.Rect(this, new Vector2(Thickness, 0f), new Vector2(Size.x - Thickness, Thickness), Foreground);
                Draw.Rect(this, new Vector2(Size.x - 1f - Thickness, Thickness), new Vector2(Thickness, Size.y - Thickness), Foreground);
                Draw.Rect(this, new Vector2(Thickness, Size.y - 1f - Thickness), new Vector2(Size.x - Thickness * 2f, Thickness), Foreground);
            }

            RenderChildren();
        }

    }
}
