using UnityEngine;

namespace SGUI {
    public class SWindowTitleBar : SElement {

        public bool Dragging;

        public SWindowTitleBar() {
            ColorCount = 1;
            Background = Background * 2f;

            Children.Add(new SButton {

            });
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            Size = new Vector2(
                Parent.Size.x + ((SGroup) Parent).Border * 2f,
                Backend.LineHeight
            );
            Position = Vector2.zero;

            base.UpdateStyle();

            float x = Size.x;
            for (int i = 0; i < Children.Count; i++) {
                SElement child = Children[i];
                x -= child.Size.x;
                child.Position.x = x;
            }
        }

        public override void RenderBackground() {
            // Backend renders background.
        }
        public override void Render() {
            // Handle title, buttons, dragging and other backend-specific window title bar magic.
            Draw.WindowTitleBar(this);
        }

    }
}
