using UnityEngine;

namespace SGUI {
    public class SRect : SElement {

        public SRect()
            : this(Color.white) { }
        public SRect(Color color) {
            Foreground = color;
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            base.UpdateStyle();
        }

        public override void Render() {
            Draw.Rect(this, Vector2.zero, Size, Foreground);
        }

    }
}
