#pragma warning disable RECS0018
using System;
using System.ComponentModel;
using UnityEngine;

namespace SGUI {
	public abstract class SGUIElement {

        public SGUIRoot Root;
        public SGUIElement Parent;

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

        public SGUIElement ParentTop {
            get {
                SGUIElement parent = this;
                while ((parent = parent.Parent) != null) { }
                return parent;
            }
        }

        public readonly BindingList<SGUIElement> Children = new BindingList<SGUIElement>();

        public Vector2 Position = new Vector2(0f, 0f);
        public Vector2 Size = new Vector2(32f, 32f);

        public bool UpdateBounds = true;

        public Vector2 AbsoluteOffset {
            get {
                float x = 0f;
                float y = 0f;
                Vector2 offset = new Vector2(0f, 0f);
                for (SGUIElement parent = Parent; parent != null; parent = parent.Parent) {
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

        public Color Foreground;
        public Color Background;

        public Action<SGUIElement> OnUpdateStyle;

        public SGUIElement() {
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

        public virtual void UpdateStyle() {

            if (
                Foreground.r == SGUIRoot.Main.Foreground.r &&
                Foreground.g == SGUIRoot.Main.Foreground.g &&
                Foreground.b == SGUIRoot.Main.Foreground.b
            ) {
                Foreground.r = SGUIRoot.Main.Foreground.r;
                Foreground.g = SGUIRoot.Main.Foreground.g;
                Foreground.b = SGUIRoot.Main.Foreground.b;
            }
            if (
                Background.r == SGUIRoot.Main.Background.r &&
                Background.g == SGUIRoot.Main.Background.g &&
                Background.b == SGUIRoot.Main.Background.b
            ) {
                Background.r = SGUIRoot.Main.Background.r;
                Background.g = SGUIRoot.Main.Background.g;
                Background.b = SGUIRoot.Main.Background.b;
            }

            OnUpdateStyle?.Invoke(this);
            UpdateChildrenStyles();
        }

        public void UpdateChildrenStyles() {
            for (int i = 0; i < Children.Count; i++) {
                SGUIElement child = Children[i];
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
                SGUIElement child = Children[i];
                child.Root = Root;
                child.Parent = this;
                child.Update();
            }
        }

        public abstract void Render();
        public void RenderChildren() {
            for (int i = 0; i < Children.Count; i++) {
                SGUIElement child = Children[i];
                child.Root = Root;
                child.Parent = this;
                child.Render();
            }
        }

        public virtual void Dispose() {

        }

	}
}
