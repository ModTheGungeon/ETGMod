using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGUI {
    public sealed class SGUIIMBackend : ISGUIBackend {

        public static Func<SGUIIMBackend, SGUIRoot, Font> GetFont;

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

        public void StartRender(SGUIRoot root) {
            if (CurrentRoot != null) {
                throw new InvalidOperationException("StartRender already called! Call EndRender first!");
            }
            CurrentRoot = root;

            if (Font == null) {
                if (GetFont != null) {
                    GUI.skin.font = GetFont(this, root);
                }
                Font = GUI.skin.font;

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
            }

            GUI.skin.textField.normal.background = CurrentRoot.TextFieldBackground[0];
            GUI.skin.textField.active.background = CurrentRoot.TextFieldBackground[1];
            GUI.skin.textField.hover.background = CurrentRoot.TextFieldBackground[2];
            GUI.skin.textField.focused.background = CurrentRoot.TextFieldBackground[3];

            GUI.skin.settings.selectionColor = new Color(0.3f, 0.6f, 0.9f, 1f);

            GUI.depth = -0x0ade;

            _ElementSemaphore = 0;
            if (_GlobalElementSemaphore == 0) {
                _Elements.Clear();
            }
            _ClickedButtons.Clear();
        }

        public void EndRender(SGUIRoot root) {
            if (CurrentRoot == null) {
                throw new InvalidOperationException("EndRender already called! Call StartRender first!");
            }
            CurrentRoot = null;

            _GlobalElementSemaphore -= _ElementSemaphore;
        }

        private string _NewElementName(SElement elem) {
            ++_GlobalElementSemaphore;
            ++_ElementSemaphore;
            _Elements.Add(elem);
            return (_Elements.Count - 1).ToString();
        }
        private bool _RegisteredNextElement = false;
        /// <summary>
        /// Registers the next element, mapping it to the element.
        /// </summary>
        /// <param name="elem">Element containing the next element.</param>
        private void _RegisterNextElement(SElement elem) {
            _RegisteredNextElement = true;
            GUI.SetNextControlName(_NewElementName(elem));
        }
        /// <summary>
        /// Registers the next element when not already registered.
        /// </summary>
        private void _RegisterNextElement() {
            if (!_RegisteredNextElement) {
                GUI.SetNextControlName(_NewElementName(null));
            }
            _RegisteredNextElement = false;
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

        public bool IsRelative(SElement elem) {
            if (elem == null) {
                // Root is absolute.
                return true;
            }

            // TODO check if the element would render its children relative to itself
            return false;
        }

        public void PreparePosition(SElement elem, ref Vector2 position) {
            // IMGUI draws relative to the current window.
            // If this is a window or similar, don't add the absolute offset.
            // If not, add the element position to draw relative to *that*.
            if (!IsRelative(elem) && elem != null) {
                position += elem.Position;
            }
        }

        public void Rect(SElement elem, Vector2 position, Vector2 size, Color color) {
            PreparePosition(elem, ref position);
            Rect(new Rect(position, size), color);
        }
        public void Rect(Rect bounds, Color color) {
            Color prevGUIColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(bounds, SGUIRoot.White, ScaleMode.StretchToFill, false);
            GUI.color = prevGUIColor;
        }

        public void Text(SElement elem, Vector2 position, string text) {
            PreparePosition(elem, ref position);

            GUI.skin.label.normal.textColor = elem.Foreground;
            // Direct texts in anything with an unfitting size will have some text bounds issues - use SGUILabel...
            _RegisterNextElement(elem);
            GUI.Label(new Rect(position, elem != null ? elem.Size : Vector2.zero), text);
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
                GUI.skin.button.normal.textColor = elem.Foreground * 0.8f;
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

    }
}
