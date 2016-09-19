#pragma warning disable RECS0018
using System;
using System.Collections.Generic;
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

        public readonly List<SModifier> Modifiers = new List<SModifier>();
        // Alias for when creating an SElement
        public List<SModifier> With {
            get {
                return Modifiers;
            }
            set {
                Modifiers.AddRange(value);
            }
        }

        public Vector2 Position = new Vector2(0f, 0f);
        public Vector2 Size = new Vector2(32f, 32f);

        public virtual Vector2 InnerOrigin {
            get {
                return Position;
            }
        }
        public virtual Vector2 InnerSize {
            get {
                return Size;
            }
        }

        public bool UpdateBounds = true;
        public bool Enabled = true;
        public bool Visible = true;

        public Vector2 AbsoluteOffset {
            get {
                float x = 0f;
                float y = 0f;
                for (SElement parent = Parent; parent != null; parent = parent.Parent) {
                    x += parent.InnerOrigin.x;
                    y += parent.InnerOrigin.y;
                    SGroup group = parent as SGroup;
                    if (group != null) {
                        if (group.ScrollDirection != SGroup.EDirection.None) {
                            x -= group.ScrollPosition.x;
                            y -= group.ScrollPosition.y;
                        }
                        if (group.IsWindow) {
                            x += group.Border;
                            y += group.Border;
                            y += group.WindowTitleBar.Size.y;
                        }
                    }
                }
                return new Vector2(x, y);
            }
        }
        public Vector2 Centered {
            get {
                if (Parent == null) {
                    return Root.Size / 2f - Size / 2f;
                }
                return Parent.InnerSize / 2f - Size / 2f;
            }
        }

        public virtual Color Foreground { get; set; }
        public virtual Color Background { get; set; }
        public object Font;

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

        public bool IsHovered { get; protected set; } = true;
        public bool WasHovered { get; protected set; } = true;
        public EMouseStatus MouseStatus { get; protected set; } = EMouseStatus.Inside;
        public EMouseStatus MouseStatusPrev { get; protected set; } = EMouseStatus.Inside;

        public Action<SElement> OnUpdateStyle;

        public Action<SElement, EMouseStatus, Vector2> OnMouse;

        public SElement() {
            Children.ListChanged += HandleChange;
            if (SGUIRoot.Main != null) {
                SGUIRoot.Main.AdoptedChildren.Add(this);
                Foreground = SGUIRoot.Main.Foreground;
                Background = SGUIRoot.Main.Background;
            }
        }

        public virtual void HandleChange(object sender, ListChangedEventArgs e) {
            Parent?.HandleChange(null, null);
            UpdateStyle();

            if (sender == null || e == null) return;

            // TODO Also send event to backend.
            if (Root != null) {
                if (e.ListChangedType == ListChangedType.ItemAdded) {
                    SElement child = Children[e.NewIndex];
                    int disposeIndex = Root.DisposingChildren.IndexOf(child);
                    if (0 <= disposeIndex) {
                        Root.DisposingChildren.RemoveAt(disposeIndex);
                    }
                    child.UpdateStyle();

                } else if (e.ListChangedType == ListChangedType.ItemDeleted) {
                    // TODO Dispose.
                    // Root.DisposingChildren.Add(null);
                }
            }
        }

        protected bool _CenteredOnce;
        public void CenterOnce() {
            if (_CenteredOnce) {
                return;
            }
            _CenteredOnce = true;
            Position = Centered;
        }
        public void Fill(float padding = 16f) {
            Position = new Vector2(padding, padding);
            Size = (Parent?.Size ?? Root.Size) - Position * 2f;
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

            Modifiers.ForEach(_ModifierUpdateStyle);
            OnUpdateStyle?.Invoke(this);
            UpdateChildrenStyles();
        }
        protected void _ModifierUpdateStyle(SModifier modifier) {
            modifier.Elem = this;
            modifier.UpdateStyle();
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
            Modifiers.ForEach(_ModifierUpdate);
            UpdateChildren();
        }
        protected void _ModifierUpdate(SModifier modifier) {
            modifier.Elem = this;
            modifier.Update();
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
                if (!child.Visible) continue;
                child.Render();
            }
        }

        public virtual void Dispose() {
            Backend.Dispose(this);
        }

        [Obsolete("Typo. Use Remove instead. Kept to keep mods relying on old API intact.")]
        public void Detatch() {
            Remove();
        }
        public virtual void Remove() {
            (Parent?.Children ?? Root.Children).Remove(this);
        }

        public virtual void Focus() {
        }

        public virtual void MouseStatusChanged(EMouseStatus e, Vector2 pos) {
            OnMouse?.Invoke(this, e, pos);
        }

        public virtual void SetFocused(int secret, bool value) {
            Backend.VerifySecret(secret);
            IsFocused = value;
        }

        public virtual void SetMouseStatus(int secret, EMouseStatus e, Vector2 pos) {
            Backend.VerifySecret(secret);
            if (MouseStatus != e) {
                if (e != EMouseStatus.Inside && e != EMouseStatus.Outside) {
                    MouseStatusChanged(e, pos);
                } else {
                    bool inside = e == EMouseStatus.Inside;
                    if (IsHovered != inside) {
                        MouseStatusChanged(e, pos);
                    }
                    WasHovered = IsHovered;
                    IsHovered = inside;
                }
            }
            MouseStatusPrev = MouseStatus;
            MouseStatus = e;
        }

	}
}
