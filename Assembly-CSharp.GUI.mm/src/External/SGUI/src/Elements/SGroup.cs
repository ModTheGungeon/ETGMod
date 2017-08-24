using System;
using System.ComponentModel;
using UnityEngine;

namespace SGUI {
    public class SGroup : SElement {

        public string WindowTitle;
        public readonly SWindowTitleBar WindowTitleBar;
        public bool IsWindow;
        public int WindowID { get; protected set; }

        public EDirection ScrollDirection;
        public Vector2 ScrollPosition;
        public Vector2 ScrollMomentum;
        public float ScrollInitialMomentum = 16f;
        public float ScrollDecayFactor = 0.45f;
        public Vector2 GrowExtra = new Vector2(0f, -2f);
        public Vector2 ContentSize = new Vector2(128f, 128f);

        public Func<SGroup, Action<int, SElement>> AutoLayout;
        public float AutoLayoutPadding = 2f;
        public float Border = 2f;
        public EDirection AutoGrowDirection = EDirection.None;

        protected Action<int, SElement> _AutoLayout;

        public override Vector2 InnerOrigin {
            get {
                return IsWindow ? Position : new Vector2(
                    Position.x + Border,
                    Position.y + Border
                );
            }
        }
        public override Vector2 InnerSize {
            get {
                return IsWindow ? Size : new Vector2(
                    Size.x - Border * 2f,
                    Size.y - Border * 2f
                ) - Backend.ScrollBarSizes;
            }
        }

        public SGroup() {
            WindowTitleBar = new SWindowTitleBar {
                Parent = this
            };
        }

        public override void HandleChange(object sender, ListChangedEventArgs e) {
            if (e != null && e.ListChangedType == ListChangedType.ItemAdded) {
                SElement elem = Children[e.NewIndex];
                if (elem is SWindowTitleBar) {
                    Children.RemoveAt(e.NewIndex);
                    return;
                }
            }

            base.HandleChange(sender, e);
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (Size.x <= 0f) {
                AutoGrowDirection |= EDirection.Horizontal;
            }
            if (Size.y <= 0f) {
                AutoGrowDirection |= EDirection.Vertical;
            }
            if (GrowExtra.y <= -0f) { // Yes, -0f is a thing.
                GrowExtra.y = Backend.LineHeight * -GrowExtra.y;
            }

            if (AutoLayout != null) {
                _AutoLayout = AutoLayout(this);
                AutoLayout = null;

                IsWindow = WindowTitle != null;
            }

            base.UpdateStyle();

            if (_AutoLayout != null) {
                for (int i = 0; i < Children.Count; i++) {
                    _AutoLayout(i, Children[i]);
                }
            }
        }
        public override void UpdateChildrenStyles() {
            WindowTitleBar.Root = Root;
            WindowTitleBar.Parent = this;
            if (WindowTitleBar.Enabled)
                WindowTitleBar.UpdateStyle();

            base.UpdateChildrenStyles();
        }

        public override void RenderBackground() {
            // SGroup is a relative context, so Position is required.
            Draw.Rect(this, Position, Size, Background);
        }
        public override void Render() {
            if (Backend.ScrollBarSizes.x == 0f && ScrollDirection != EDirection.None) {
                ScrollPosition = new Vector2(
                    Mathf.Clamp(ScrollPosition.x + ScrollMomentum.x, 0f, ContentSize.x - Size.x),
                    Mathf.Clamp(ScrollPosition.y + ScrollMomentum.y, 0f, ContentSize.y - Size.y + (IsWindow ? WindowTitleBar.Size.y : 0f))
                );
                ScrollMomentum *= ScrollDecayFactor;
            } else {
                ScrollPosition = ScrollMomentum = Vector2.zero;
            }

            if (IsWindow) {
                Draw.Window(this);

            } else {
                WindowID = -1;
                RenderBackground();
                Draw.StartGroup(this);
                RenderChildren();
                Draw.EndGroup(this);
            }
        }
        public virtual void RenderGroup(int windowID) {
            WindowID = windowID;
            Draw.StartWindow(this);
            RenderChildren();
            Draw.EndWindow(this);
        }


        protected float _CurrentAutoLayoutVerticalY;
        public bool AutoLayoutVerticalStretch = true;
        public void AutoLayoutVertical(int index, SElement elem) {
            if (elem == null || !elem.Visible) {
                return;
            }
            if (index == 0) {
                _CurrentAutoLayoutVerticalY = 0f;
            }

            if (AutoLayoutVerticalStretch) {
                elem.Size.x = InnerSize.x;
            }
            elem.Position = new Vector2(0f, _CurrentAutoLayoutVerticalY);
            _CurrentAutoLayoutVerticalY += elem.InnerSize.y + AutoLayoutPadding;

            GrowToFit(elem);
        }

        protected float _CurrentAutoLayoutHorizontalX;
        public bool AutoLayoutHorizontalStretch = true;
        public void AutoLayoutHorizontal(int index, SElement elem) {
            if (elem == null || !elem.Visible) {
                return;
            }
            if (index == 0) {
                _CurrentAutoLayoutHorizontalX = 0f;
            }

            if (AutoLayoutVerticalStretch) {
                elem.Size.y = InnerSize.y;
            }
            elem.Position = new Vector2(_CurrentAutoLayoutHorizontalX, 0f);
            _CurrentAutoLayoutHorizontalX += elem.InnerSize.x + AutoLayoutPadding;

            GrowToFit(elem);
        }


        public float AutoLayoutLabelWidth;
        public void AutoLayoutLabeledInput(int index, SElement elem) {
            if (elem == null || index >= 2) {
                return;
            }

            if (index == 0) {
                Background = Background * 0f;

                if ((AutoGrowDirection & EDirection.Horizontal) == EDirection.Horizontal) {
                    Size.x = Parent.InnerSize.x;
                }

                elem.Position = Vector2.zero;
                if (AutoLayoutLabelWidth <= 0f) {
                    elem.Size = Backend.MeasureText(((SLabel) elem).Text, font: elem.Font);
                } else {
                    elem.Size = new Vector2(AutoLayoutLabelWidth, Backend.LineHeight);
                }
                if ((AutoGrowDirection & EDirection.Vertical) == EDirection.Vertical) {
                    Size.y = elem.Size.y;
                }
            } else if (index == 1) {
                elem.Position = new Vector2(Children[0].Size.x + AutoLayoutPadding, 0f);
                elem.Size = new Vector2(Size.x - Children[0].Size.x - AutoLayoutPadding, Size.y);
            }
        }

        public void GrowToFit(SElement elem) {
            Vector2 max = elem.Position + elem.Size;

            ContentSize.x = Mathf.Max(ContentSize.x, max.x + GrowExtra.x);
            ContentSize.y = Mathf.Max(ContentSize.y, max.y + GrowExtra.y);

            if ((AutoGrowDirection & EDirection.Horizontal) == EDirection.Horizontal) {
                Size.x = ContentSize.x;
            }
            if ((AutoGrowDirection & EDirection.Vertical) == EDirection.Vertical) {
                Size.y = ContentSize.y;
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
