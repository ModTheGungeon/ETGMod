#pragma warning disable RECS0018
using System;
using System.ComponentModel;
using UnityEngine;

namespace SGUI {
	public abstract class SElement {

        public SGUIRoot Root;
        public SElement Parent;

        public ISGUIBackend Backend {
            get {
                return Root.Backend;
            }
        }
        public ISGUIBackend Draw /* sounds better than Renderer, shorter than Backend, doesn't conflict with Render */ {
            get {
                return Root.Backend;
            }
        }

        public SElement ParentTop {
            get {
                SElement parent = this;
                while ((parent = parent.Parent) != null) { }
                return parent ?? this;
            }
        }

        public readonly BindingList<SElement> Children = new BindingList<SElement>();
        public SElement this[int id] {
            get {
                return Children[id];
            }
            set {
                Children[id] = value;
            }
        }

        public Vector2 Position = new Vector2(0f, 0f);
        public Vector2 Size = new Vector2(32f, 32f);

        public bool UpdateBounds = true;

        public Vector2 AbsoluteOffset {
            get {
                float x = 0f;
                float y = 0f;
                Vector2 offset = new Vector2(0f, 0f);
                for (SElement parent = Parent; parent != null; parent = parent.Parent) {
                    x += parent.Position.x;
                    y += parent.Position.y;
                }
                return offset;
            }
        }
        public Vector2 Centered {
            get {
                if (Parent == null) {
                    return Root.Size / 2f - Size / 2f;
                }
                return Parent.Size / 2f - Size / 2f;
            }
        }

        public virtual Color Foreground { get; set; }
        public virtual Color Background { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:SGUI.SElement"/> is interactive.
        /// </summary>
        /// <value><c>true</c> if is interactive; otherwise, <c>false</c>.</value>
        public virtual bool IsInteractive {
            get {
                return false;
            }
        }

        public bool IsFocused { get; protected set; }

        public Action<SElement> OnUpdateStyle;

        public SElement() {
            Children.ListChanged += HandleChange;
            if (SGUIRoot.Main != null) {
                SGUIRoot.Main.AdoptedChildren.Add(this);
                Foreground = SGUIRoot.Main.Foreground;
                Background = SGUIRoot.Main.Background;
            }
        }

        public virtual void HandleChange(object sender, ListChangedEventArgs e) {
            ParentTop.UpdateStyle();
        }

        protected bool _CenteredOnce;
        public void CenterOnce() {
            if (_CenteredOnce) {
                return;
            }
            _CenteredOnce = true;
            Position = Centered;
        }

        public virtual void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (
                Foreground.r == SGUIRoot.Main.Foreground.r &&
                Foreground.g == SGUIRoot.Main.Foreground.g &&
                Foreground.b == SGUIRoot.Main.Foreground.b
            ) {
                Foreground = SGUIRoot.Main.Foreground.WithAlpha(Foreground.a);
            }
            if (
                Background.r == SGUIRoot.Main.Background.r &&
                Background.g == SGUIRoot.Main.Background.g &&
                Background.b == SGUIRoot.Main.Background.b
            ) {
                Background = SGUIRoot.Main.Background.WithAlpha(Background.a);
            }

            OnUpdateStyle?.Invoke(this);
            UpdateChildrenStyles();
        }
        public virtual void UpdateChildrenStyles() {
            for (int i = 0; i < Children.Count; i++) {
                SElement child = Children[i];
                child.Root = Root;
                child.Parent = this;
                child.UpdateStyle();
            }
        }

        public virtual void Update() {
            UpdateChildren();
        }
        public void UpdateChildren() {
            for (int i = 0; i < Children.Count; i++) {
                SElement child = Children[i];
                child.Root = Root;
                child.Parent = this;
                child.Update();
            }
        }

        public virtual void RenderBackground() {
            Draw.Rect(this, Vector2.zero, Size, Background);
        }
        public abstract void Render();
        public void RenderChildren() {
            for (int i = 0; i < Children.Count; i++) {
                SElement child = Children[i];
                child.Root = Root;
                child.Parent = this;
                child.Render();
            }
        }

        public virtual void Dispose() {
        }

        public virtual void Focus() {
        }

	}
}
