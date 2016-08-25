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
        public float ScrollInitialMomentum = 8f;
        public float ScrollDecayFactor = 0.45f;
        public Vector2 InnerSize = new Vector2(128f, 128f);

        public Func<SGroup, Action<int, SElement>> AutoLayout;
        public float AutoLayoutPadding = 2f;
        public float Border = 2f;
        public EDirection AutoGrowDirection = EDirection.None;

        protected Action<int, SElement> _AutoLayout;

        public SGroup() {
            WindowTitleBar = new SWindowTitleBar {
                Parent = this
            };
        }

        public override void HandleChange(object sender, ListChangedEventArgs e) {
            if (e.ListChangedType == ListChangedType.ItemAdded) {
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

            if (AutoLayout != null) {
                _AutoLayout = AutoLayout(this);
                AutoLayout = null;

                IsWindow = WindowTitle != null;
            }

            if (_AutoLayout != null) {
                // Apply auto layout to any child that has no layout settings.
                for (int i = 0; i < Children.Count; i++) {
                    SElement child = Children[i];
                    if (child.OnUpdateStyle == null) {
                        _AutoLayout(i, child);
                    }
                }
            }

            base.UpdateStyle();
        }
        public override void UpdateChildrenStyles() {
            WindowTitleBar.Root = Root;
            WindowTitleBar.Parent = this;
            WindowTitleBar.UpdateStyle();

            base.UpdateChildrenStyles();
        }

        public override void RenderBackground() {
            Draw.Rect(this, ScrollPosition, new Vector2(Size.x + Border * 2f, Size.y + Border * 2f), Background);
        }
        public override void Render() {
            if (ScrollDirection != EDirection.None) {
                ScrollPosition = new Vector2(
                    Mathf.Clamp(ScrollPosition.x + ScrollMomentum.x, 0f, InnerSize.x - Size.x),
                    Mathf.Clamp(ScrollPosition.y + ScrollMomentum.y, 0f, InnerSize.y - Size.y + (IsWindow ? WindowTitleBar.Size.y : 0f))
                );
                ScrollMomentum *= ScrollDecayFactor;
            } else {
                ScrollPosition = ScrollMomentum = Vector2.zero;
            }

            if (IsWindow) {
                Draw.Window(this);

            } else {
                WindowID = -1;
                Draw.StartGroup(this);
                RenderBackground();
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


        protected float _CurrentAutoLayoutRow;
        public bool AutoLayoutRowsStretch = true;
        public void AutoLayoutRows(int index, SElement elem) {
            if (elem == null) {
                return;
            }
            if (index == 0) {
                _CurrentAutoLayoutRow = 0f;
            }

            if (AutoLayoutRowsStretch) {
                elem.UpdateBounds = false;
                elem.Size.x = Size.x;
            }
            elem.Position = new Vector2(0f, _CurrentAutoLayoutRow);
            _CurrentAutoLayoutRow += elem.Size.y + AutoLayoutPadding;

            GrowToFit(elem);
        }


        public float AutoLayoutLabelWidth;
        public void AutoLayoutLabeledInput(int index, SElement elem) {
            if (elem == null || index >= 2) {
                return;
            }

            if (index == 0) {
                Background = Background.WithAlpha(0f);

                if ((AutoGrowDirection & EDirection.Horizontal) == EDirection.Horizontal) {
                    Size.x = Parent.Size.x;
                }

                elem.Position = Vector2.zero;
                if (AutoLayoutLabelWidth <= 0f) {
                    elem.Size = Backend.MeasureText(((SLabel) elem).Text);
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

            InnerSize.x = Mathf.Max(InnerSize.x, max.x);
            InnerSize.y = Mathf.Max(InnerSize.y, max.y);

            if ((AutoGrowDirection & EDirection.Horizontal) == EDirection.Horizontal) {
                Size.x = InnerSize.x;
            }
            if ((AutoGrowDirection & EDirection.Vertical) == EDirection.Vertical) {
                Size.y = InnerSize.y;
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
