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
                return (Root ?? SGUIRoot.Main).Backend;
            }
        }
        public ISGUIBackend Draw /* sounds better than Renderer, shorter than Backend, doesn't conflict with Render */ {
            get {
                return (Root ?? SGUIRoot.Main).Backend;
            }
        }

        public SElement ParentTop {
            get {
                SElement parent = this;
                while ((parent = parent.Parent) != null) { }
                return parent ?? this;
            }
        }

        public SElement Previous {
            get {
                if (Parent == null)
                    return null;
                int i = Parent.Children.IndexOf(this) - 1;
                if (i < 0 || Parent.Children.Count <= i)
                    return null;
                return Parent.Children[i];
            }
        }

        public SElement Next {
            get {
                if (Parent == null)
                    return null;
                int i = Parent.Children.IndexOf(this) + 1;
                if (i < 0 || Parent.Children.Count <= i)
                    return null;
                return Parent.Children[i];
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

        public readonly BindingList<SModifier> Modifiers = new BindingList<SModifier>();
        // Alias for when creating an SElement
        public BindingList<SModifier> With {
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
                    return Root.Size * 0.5f - Size * 0.5f;
                }
                return Parent.InnerSize * 0.5f - Size * 0.5f;
            }
        }

        public Color[] Colors = new Color[2];
        public virtual int ColorCount {
            get {
                return Colors.Length;
            }
            set {
                Color[] newColors = new Color[value];
                Array.Copy(Colors, 0, newColors, 0, Math.Min(Colors.Length, value) - 1);
                newColors[value - 1] = Colors[Colors.Length - 1];
                Colors = newColors;
            }
        }
        public virtual Color Foreground {
            get {
                return Colors[0];
            }
            set {
                Colors[0] = value;
            }
        }
        public virtual Color Background {
            get {
                return Colors[Colors.Length - 1];
            }
            set {
                Colors[Colors.Length - 1] = value;
            }
        }
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

        public bool IsHovered { get; protected set; }
        public bool WasHovered { get; protected set; }
        public EMouseStatus MouseStatus { get; protected set; }
        public EMouseStatus MouseStatusPrev { get; protected set; }

        public Action<SElement> OnUpdateStyle;

        public Action<SElement, EMouseStatus, Vector2> OnMouse;

        public SElement() {
            IsHovered = true;
            WasHovered = true;
            MouseStatus = EMouseStatus.Inside;
            MouseStatusPrev = EMouseStatus.Inside;

            Children.ListChanged += HandleChange;
            Modifiers.ListChanged += HandleModifierChange;
            if (SGUIRoot.Main != null) {
                SGUIRoot.Main.AdoptedChildren.Add(this);
                Foreground = SGUIRoot.Main.Foreground;
                Background = SGUIRoot.Main.Background;
            }
        }

        public virtual void HandleChange(object sender, ListChangedEventArgs e) {
            if (Parent != null && Parent.Enabled) Parent.HandleChange(null, null);
            if (Enabled)
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
                    if (child.Enabled)
                        child.UpdateStyle();

                } else if (e.ListChangedType == ListChangedType.ItemDeleted) {
                    // TODO Dispose.
                    // Root.DisposingChildren.Add(null);
                }
            }
        }

        public virtual void HandleModifierChange(object sender, ListChangedEventArgs e) {
            if (sender == null || e == null) return;

            if (e.ListChangedType == ListChangedType.ItemAdded) {
                SModifier modifier = Modifiers[e.NewIndex];
                modifier.Elem = this;
                modifier.Init();
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
            Size = ((Parent == null ? new Vector2?() : Parent.InnerSize) ?? Root.Size) - Position * 2f;
        }

        public virtual void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (
                Foreground.r == SGUIRoot.Main.Foreground.r &&
                Foreground.g == SGUIRoot.Main.Foreground.g &&
                Foreground.b == SGUIRoot.Main.Foreground.b
            ) {
                Color color = SGUIRoot.Main.Foreground;
                color.a = Foreground.a;
                Foreground = color;
            }
            if (
                Background.r == SGUIRoot.Main.Background.r &&
                Background.g == SGUIRoot.Main.Background.g &&
                Background.b == SGUIRoot.Main.Background.b
            ) {
                Color color = SGUIRoot.Main.Background;
                color.a = Background.a;
                Background = color;
            }

            Modifiers.ForEach(_ModifierUpdateStyle);
            if (OnUpdateStyle != null) OnUpdateStyle(this);
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
                if (child.Enabled)
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
                if (child.Enabled)
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
            ((Parent == null ? null : Parent.Children) ?? Root.Children).Remove(this);
        }

        public virtual void Focus() {
        }

        public virtual void MouseStatusChanged(EMouseStatus e, Vector2 pos) {
            if (OnMouse != null) OnMouse(this, e, pos);
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
