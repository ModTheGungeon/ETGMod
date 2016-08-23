using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGUI {
    public sealed class SGUIIMBackend : ISGUIBackend {

        public static int DefaultDepth = 0x0ade;
        public int Depth = DefaultDepth;

        public static Func<SGUIIMBackend, Font> GetFont;

        private readonly static Color _Transparent = new Color(0f, 0f, 0f, 0f);

        // IDisposable, yet cannot be disposed. Let's keep one reference for the lifetime of all backend instances.
        private readonly static TextGenerator _TextGenerator = new TextGenerator();
        private TextGenerationSettings _TextGenerationSettings = new TextGenerationSettings();

        private readonly static List<SElement> _Elements = new List<SElement>();
        private static int _GlobalElementSemaphore;
        private int _ElementSemaphore;

        private readonly List<int> _ClickedButtons = new List<int>();

        public SGUIRoot CurrentRoot { get; private set; }

        public bool UpdateStyleOnRender {
            get {
                return true;
            }
        }

        public bool RenderOnGUI {
            get {
                return true;
            }
        }

        public float LineHeight { get; set; }

        private Font _Font;
        public Font Font {
            get {
                return _Font;
            }
            set {
                if (_Font != value) {
                    LineHeight = value.dynamic ? value.lineHeight * 2f : (value.characterInfo[0].glyphHeight + 4f);
                }
                _Font = value;
            }
        }

        public bool Initialized { get; private set; }
        public void Init() {
            if (Font == null) {
                if (GetFont != null) {
                    GUI.skin.font = GetFont(this);
                }
                Font = GUI.skin.font;

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.skin.textField.alignment = TextAnchor.MiddleLeft;

                GUI.skin.verticalScrollbar.fixedWidth = 0f;
                GUI.skin.verticalScrollbarThumb.fixedWidth = 0f;
            }

            Initialized = true;
        }

        public void StartRender(SGUIRoot root) {
            if (CurrentRoot != null) {
                throw new InvalidOperationException("StartRender already called! Call EndRender first!");
            }
            CurrentRoot = root;

            GUI.skin.textField.normal.background = CurrentRoot.TextFieldBackground[0];
            GUI.skin.textField.active.background = CurrentRoot.TextFieldBackground[1];
            GUI.skin.textField.hover.background = CurrentRoot.TextFieldBackground[2];
            GUI.skin.textField.focused.background = CurrentRoot.TextFieldBackground[3];

            GUI.skin.settings.selectionColor = new Color(0.3f, 0.6f, 0.9f, 1f);

            GUI.depth = Depth;

            _ClickedButtons.Clear();
        }

        public void EndRender(SGUIRoot root) {
            if (CurrentRoot == null) {
                throw new InvalidOperationException("EndRender already called! Call StartRender first!");
            }
            CurrentRoot = null;

            _GlobalElementSemaphore -= _ElementSemaphore;
            _ElementSemaphore = 0;
            if (_GlobalElementSemaphore == 0) {
                _Elements.Clear();
            }
        }

        private string _NewElementName(SElement elem) {
            ++_GlobalElementSemaphore;
            ++_ElementSemaphore;
            _Elements.Add(elem);
            return (_Elements.Count - 1).ToString();
        }
        private bool _RegisteredNextElement;
        /// <summary>
        /// Registers the next element, mapping it to the element.
        /// </summary>
        /// <param name="elem">Element containing the next element.</param>
        private int _RegisterNextElement(SElement elem) {
            _RegisteredNextElement = true;
            GUI.SetNextControlName(_NewElementName(elem));
            return CurrentElementID;
        }
        /// <summary>
        /// Registers the next element when not already registered.
        /// </summary>
        private int _RegisterNextElement() {
            if (!_RegisteredNextElement) {
                GUI.SetNextControlName(_NewElementName(null));
            }
            _RegisteredNextElement = false;
            return CurrentElementID;
        }

        public int CurrentElementID {
            get {
                return _Elements.Count - 1;
            }
        }
        public int GetElementID(SElement elem) {
            return _Elements.IndexOf(elem);
        }

        public void Focus(SElement elem) {
            string id = GetElementID(elem).ToString();
            // if window, use GUI.FocusWindow
            GUI.FocusControl(id);
        }
        public void Focus(int id) {
            GUI.FocusControl(id.ToString());
        }
        public bool IsFocused(SElement elem) {
            return GUI.GetNameOfFocusedControl() == GetElementID(elem).ToString();
        }
        public bool IsFocused(int id) {
            return GUI.GetNameOfFocusedControl() == id.ToString();
        }

        public bool IsClicked(SElement elem) {
            return IsClicked(GetElementID(elem));
        }
        public bool IsClicked(int id) {
            return _ClickedButtons.Contains(id);
        }

        public bool IsRelativeContext(SElement elem) {
            if (elem == null) {
                // Root is absolute.
                return true;
            }

            if (elem is SGroup) {
                return true;
            }
            // SWindowTitleBar gets rendered with the SGroup
            if (elem is SWindowTitleBar) {
                return true;
            }

            return false;
        }

        public void PreparePosition(SElement elem, ref Vector2 position) {
            if (elem == null) {
                return;
            }
            // IMGUI draws relative to the current window.
            // If this is a window or similar, don't add the absolute offset.
            // If not, add the element position to draw relative to *that*.
            if (IsRelativeContext(elem)) {
                return;
            }
            position += elem.Position;

            PreparePosition(elem.Parent, ref position);
        }


        public void Rect(SElement elem, Vector2 position, Vector2 size, Color color) {
            PreparePosition(elem, ref position);
            Rect(new Rect(position, size), color);
        }
        public void Rect(Rect bounds, Color color) {
            Color prevGUIColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(bounds, SGUIRoot.White, ScaleMode.StretchToFill, color.a < 1f);
            GUI.color = prevGUIColor;
        }


        public void StartClip(SElement elem) {
            Vector2 position = elem.Position;
            PreparePosition(elem, ref position);
            StartClip(new Rect(position, elem.Size));
        }
        public void StartClip(SElement elem, Rect bounds) {
            Vector2 position = Vector2.zero;
            PreparePosition(elem, ref position);
            bounds.position += position;
            StartClip(bounds);
        }
        public void StartClip(Rect bounds) {
            GUI.BeginClip(bounds);
        }
        public void EndClip() {
            GUI.EndClip();
        }


        public void Text(SElement elem, Vector2 position, string text) {
            PreparePosition(elem, ref position);

            GUI.skin.label.normal.textColor = elem.Foreground;
            // Direct texts in anything with an unfitting size will have some text bounds issues - use SGUILabel or the Rect variant.
            _RegisterNextElement(elem);
            GUI.Label(new Rect(position, elem != null ? elem.Size : Vector2.zero), text);
        }
        public void Text(SElement elem, Rect bounds, string text) {
            Vector2 position = Vector2.zero;
            PreparePosition(elem, ref position);
            bounds.position += position;

            GUI.skin.label.normal.textColor = elem.Foreground;
            _RegisterNextElement(elem);
            GUI.Label(bounds, text);
        }

        public bool TextField(SElement elem, Vector2 position, Vector2 size, ref string text) {
            PreparePosition(elem, ref position);

            if (elem != null) {
                GUI.skin.textField.normal.textColor = elem.Foreground * 0.8f;
                GUI.skin.textField.active.textColor = elem.Foreground;
                GUI.skin.textField.hover.textColor = elem.Foreground;
                GUI.skin.textField.focused.textColor = elem.Foreground;
                GUI.skin.settings.cursorColor = elem.Foreground;
                GUI.backgroundColor = elem.Background;
            }

            _RegisterNextElement(elem);
            return TextField(new Rect(position, size), ref text);
        }
        public bool TextField(Rect bounds, ref string text) {
            _RegisterNextElement();
            text = GUI.TextField(bounds, text);
            bool focused = IsFocused(CurrentElementID);

            if (!Font.dynamic && focused) {
                TextEditor editor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
#pragma warning disable CS0618
                // TextEditor.content is obsolete, yet it must be accessed.
                // Alternatively access TextEditor.m_Content via reflection.
                GUIContent content = editor.content;
#pragma warning restore CS0618
                Color prevGUIColor = GUI.color;

                if (editor.cursorIndex != editor.selectIndex) {
                    Rect boundsRelative = new Rect(0, 0, bounds.width, bounds.height);
                    Vector2 selectA = editor.style.GetCursorPixelPosition(boundsRelative, content, editor.cursorIndex) - editor.scrollOffset;
                    Vector2 selectB = editor.style.GetCursorPixelPosition(boundsRelative, content, editor.selectIndex) - editor.scrollOffset;
                    Vector2 selectFrom, selectTo;
                    if (selectA.x <= selectB.x) {
                        selectFrom = selectA;
                        selectTo = selectB;
                    } else {
                        selectFrom = selectB;
                        selectTo = selectA;
                    }

                    GUI.BeginClip(bounds);

                    GUI.color = GUI.skin.settings.selectionColor;
                    GUI.DrawTexture(new Rect(
                        selectFrom.x,
                        editor.style.padding.top,
                        selectTo.x - selectFrom.x,
                        bounds.height - editor.style.padding.top - editor.style.padding.bottom
                    ), SGUIRoot.White, ScaleMode.StretchToFill, false);

                    GUI.color = prevGUIColor;
                    GUI.skin.label.normal.textColor = GUI.backgroundColor;
                    // Draw over the text field. Again. Because.
                    GUI.Label(new Rect(
                        selectFrom.x,
                        0f,
                        selectTo.x - selectFrom.x,
                        bounds.height
                    ), editor.SelectedText);

                    GUI.EndClip();
                }

                Vector2 cursor = editor.style.GetCursorPixelPosition(bounds, content, editor.cursorIndex) - editor.scrollOffset;
                GUI.color = GUI.skin.settings.cursorColor;
                GUI.DrawTexture(new Rect(
                    cursor.x - 2f,
                    bounds.y + editor.style.padding.top,
                    1f,
                    bounds.height - editor.style.padding.top - editor.style.padding.bottom
                ), SGUIRoot.White, ScaleMode.StretchToFill, false);

                GUI.color = prevGUIColor;
            }

            return focused;
        }


        // IMGUI doesn't seem to have something similar to TextEditor for buttons... so SButton.OnChange stays untouched.
        public bool Button(SElement elem, Vector2 position, Vector2 size, string text) {
            PreparePosition(elem, ref position);

            if (elem != null) {
                GUI.skin.button.normal.textColor = elem.Foreground * 0.9f;
                GUI.skin.button.active.textColor = elem.Foreground;
                GUI.skin.button.hover.textColor = elem.Foreground;
                GUI.skin.button.focused.textColor = elem.Foreground;
                GUI.backgroundColor = elem.Background;
            }

            _RegisterNextElement(elem);
            return Button(new Rect(position, size), text);
        }
        public bool Button(Rect bounds, string text) {
            _RegisterNextElement();
            GUI.skin.button.fixedHeight = bounds.height;
            if (GUI.Button(bounds, text)) {
                _ClickedButtons.Add(CurrentElementID);
                return true;
            }
            return false;
        }


        public void StartGroup(SGroup group) {
            Vector2 position = group.Position;
            PreparePosition(group, ref position);
            Rect bounds = new Rect(position, group.Size);

            GUI.backgroundColor = _Transparent;
            _RegisterNextElement(group);
            if (group.ScrollDirection == SGroup.EDirection.None) {
                GUI.BeginGroup(bounds);
            } else {
                group.ScrollPosition = GUI.BeginScrollView(
                    bounds, group.ScrollPosition, new Rect(Vector2.zero, group.InnerSize),
                    (group.ScrollDirection & SGroup.EDirection.Horizontal) == SGroup.EDirection.Horizontal,
                    (group.ScrollDirection & SGroup.EDirection.Vertical) == SGroup.EDirection.Vertical
                );
            }
        }
        public void EndGroup(SGroup group) {
            if (group.ScrollDirection != SGroup.EDirection.None) {
                GUI.EndScrollView();
            } else {
                GUI.EndGroup();
            }
        }


        public void Window(SGroup group) {
            Vector2 position = group.Position;
            PreparePosition(group, ref position);
            Rect bounds = new Rect(
                position.x, position.y,
                group.Size.x + group.Border * 2f,
                group.Size.y + group.Border * 2f + group.WindowTitleBar.Size.y * 0.5f
            );

            GUI.backgroundColor = _Transparent;
            _RegisterNextElement(group);
            GUI.BeginGroup(bounds);

            group.RenderGroup(CurrentElementID);

            GUI.EndGroup();
        }
        public void StartWindow(SGroup group) {
            if (group.WindowID == -1) {
                return;
            }

            Rect(new Rect(
                0f, group.WindowTitleBar.Size.y,
                group.Size.x + group.Border * 2f,
                group.Size.y + group.Border * 2f
            ), group.Background);

            Rect bounds = new Rect(
                group.Border,
                group.WindowTitleBar.Size.y + group.Border,
                group.Size.x, group.Size.y
            );
            GUI.backgroundColor = _Transparent;
            _RegisterNextElement(group);
            if (group.ScrollDirection == SGroup.EDirection.None) {
                GUI.BeginClip(
                    bounds,
                    Vector2.zero,
                    new Vector2(0f, group.WindowTitleBar.Size.y),
                    true
                );
            } else {
                group.ScrollPosition = GUI.BeginScrollView(
                    bounds, group.ScrollPosition, new Rect(Vector2.zero, group.InnerSize),
                    (group.ScrollDirection & SGroup.EDirection.Horizontal) == SGroup.EDirection.Horizontal,
                    (group.ScrollDirection & SGroup.EDirection.Vertical) == SGroup.EDirection.Vertical
                );
            }
        }
        public void EndWindow(SGroup group) {
            if (group.WindowID == -1) {
                return;
            }

            if (group.ScrollDirection != SGroup.EDirection.None) {
                GUI.EndScrollView();
            } else {
                GUI.EndClip();
            }

            group.WindowTitleBar.Root = group.Root;
            group.WindowTitleBar.Parent = group;
            group.WindowTitleBar.Render();
        }
        public void WindowTitleBar(SWindowTitleBar bar) {
            Rect bounds = new Rect(Vector2.zero, bar.Size);

            SGroup group = (SGroup) bar.Parent;
            string title = group.WindowTitle;

            Rect(bar, Vector2.zero, bar.Size, group.Background);

            if (!string.IsNullOrEmpty(title)) {
                Vector2 titleSize = MeasureText(title);
                Text(bar, new Rect(group.Border, 0f, titleSize.x, titleSize.y), title);
            }

            // TODO Window header buttons.
        }
        public void UpdateWindows() {
            while (0 < Event.GetEventCount() && (!Event.current.isMouse)) {
                Event.PopEvent(Event.current);
            };

            if (!UpdateWindowsIn(null) && _WindowDragging != null) {
                IList<SElement> children = _WindowDragging.Parent?.Children ?? _WindowDragging.Root.Children;
                children.Remove(_WindowDragging);
                children.Add(_WindowDragging);
            }
        }
        public bool UpdateWindowsIn(SElement elem) {
            IList<SElement> children = elem?.Children ?? CurrentRoot.Children;
            for (int i = children.Count - 1; 0 <= i; i--) {
                if (UpdateWindowsIn(children[i])) return true;
            }

            if (elem is SGroup) {
                if (UpdateWindow((SGroup) elem)) return true;
            }

            return false;
        }
        private Vector2 _WindowDragOffset;
        private SGroup _WindowDragging;
        public bool UpdateWindow(SGroup group) {
            if (!group.IsWindow) {
                return false;
            }
            Event e = Event.current;
            SWindowTitleBar bar = group.WindowTitleBar;

            if (e.type == EventType.MouseDown) {
                Rect screenBounds = new Rect(group.AbsoluteOffset + group.Position, bar.Size);
                if (screenBounds.Contains(e.mousePosition)) {
                    bar.Dragging = true;
                    _WindowDragOffset = group.Position - e.mousePosition;
                    _WindowDragging = group;
                    e.Use();
                    return false; // False because other windows need to be marked as not dragging.
                } else {
                    bar.Dragging = false;
                    return false;
                }

            } else if (bar.Dragging) {
                if (e.type == EventType.MouseDrag) {
                    group.Position = _WindowDragOffset + e.mousePosition;
                } else if (e.type == EventType.MouseUp) {
                    bar.Dragging = false;
                    _WindowDragging = null;
                }
                e.Use();
                return true;
            }

            bar.Dragging = false;
            return false;
        }

        public Vector2 MeasureText(string text, Vector2? size = null) {
            _TextGenerationSettings.richText = true;

            _TextGenerationSettings.font = Font;
            _TextGenerationSettings.fontSize = Font.fontSize;
            _TextGenerationSettings.lineSpacing = LineHeight;

            if (size != null) {
                _TextGenerationSettings.generateOutOfBounds = false;
                _TextGenerationSettings.horizontalOverflow = HorizontalWrapMode.Wrap;
                _TextGenerationSettings.generationExtents = size.Value;
            } else {
                _TextGenerationSettings.generateOutOfBounds = true;
                _TextGenerationSettings.horizontalOverflow = HorizontalWrapMode.Overflow;
                _TextGenerationSettings.generationExtents = Vector2.zero;
            }

            return new Vector2(
                _TextGenerator.GetPreferredWidth(text, _TextGenerationSettings),
                Font.dynamic ? _TextGenerator.GetPreferredHeight(text, _TextGenerationSettings) : (LineHeight * _TextGenerator.lineCount)
            );
        }

        public void Dispose() {
        }

        public void Dispose(SElement elem) {
        }

    }
}
