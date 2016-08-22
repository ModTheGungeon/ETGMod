using System;
using UnityEngine;

namespace SGUI {
    public class SGroup : SElement {

        public string Title;

        public EDirection ScrollDirection;
        public Vector2 ScrollPosition;
        public Vector2 InnerSize = new Vector2(128f, 128f);

        public Func<SGroup, Action<SElement>> AutoLayout;
        public float AutoLayoutPadding = 4f;
        protected Action<SElement> _AutoLayout;
        protected int _AutoLayoutIndex;

        public EDirection AutoGrowDirection = EDirection.None;

        public SGroup()
            : this(null) { }
        public SGroup(string title) {
            Title = title;
        }

        public override void UpdateStyle() {
            if (Size.x <= 0f) {
                AutoGrowDirection |= EDirection.Horizontal;
            }
            if (Size.y <= 0f) {
                AutoGrowDirection |= EDirection.Vertical;
            }

            if (AutoLayout != null) {
                _AutoLayout = AutoLayout(this);
                AutoLayout = null;
            }

            if (_AutoLayout != null) {
                _AutoLayoutIndex = 0;
                // Apply auto layout to any child that has no layout settings.
                for (int i = 0; i < Children.Count; i++) {
                    SElement child = Children[i];
                    if (child.OnUpdateStyle == null) {
                        _AutoLayout(child);
                    }
                }
            }

            base.UpdateStyle();
        }

        public override void Render() {
            Backend.StartGroup(this);
            RenderBackground();
            RenderChildren();
            Backend.EndGroup(this);
        }

        protected float _CurrentAutoLayoutRow;
        public void AutoLayoutRow(SElement elem) {
            if (elem == null) {
                return;
            }
            if (_AutoLayoutIndex == 0) {
                _CurrentAutoLayoutRow = 0f;
            }

            elem.UpdateBounds = false;

            if ((AutoGrowDirection & EDirection.Horizontal) != EDirection.Horizontal) {
                elem.Size.x = Size.x;
            }
            elem.Position.y = _CurrentAutoLayoutRow;
            _CurrentAutoLayoutRow += elem.Size.y + AutoLayoutPadding;

            GrowToFit(elem);

            ++_AutoLayoutIndex;
        }

        public void GrowToFit(SElement elem) {
            Vector2 max = elem.Position + elem.Size;
            if ((AutoGrowDirection & EDirection.Horizontal) == EDirection.Horizontal) {
                Size.x = max.x;
            }
            if ((AutoGrowDirection & EDirection.Vertical) == EDirection.Vertical) {
                Size.y = max.y;
            }
        }

        [Flags]
        public enum EDirection {
            None        = 0,
            Any         = 1,
            Horizontal  = Any | 2,
            Vertical    = Any | 4,
            Both        = Horizontal | Vertical
        }
    }
}
