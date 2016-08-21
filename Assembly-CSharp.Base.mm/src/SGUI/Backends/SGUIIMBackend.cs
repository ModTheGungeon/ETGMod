using System;
using UnityEngine;

namespace SGUI {
public sealed class SGUIIMBackend : ISGUIBackend {

        // IDisposable, yet cannot be disposed. Let's keep one reference for the lifetime of all backend instances.
        private readonly static TextGenerator _TextGenerator = new TextGenerator();
        private TextGenerationSettings _TextGenerationSettings = new TextGenerationSettings();

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
                    LineHeight = value.dynamic ? value.lineHeight : value.characterInfo[0].glyphHeight;
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
                Font = GUI.skin.font;
            }

            GUI.skin.textField.normal.background = CurrentRoot.TextFieldBackground[0];
            GUI.skin.textField.active.background = CurrentRoot.TextFieldBackground[1];
            GUI.skin.textField.hover.background = CurrentRoot.TextFieldBackground[2];
            GUI.skin.textField.focused.background = CurrentRoot.TextFieldBackground[3];
        }

        public void EndRender(SGUIRoot root) {
            if (CurrentRoot == null) {
                throw new InvalidOperationException("EndRender already called! Call StartRender first!");
            }
            CurrentRoot = null;
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

        public void Text(SElement elem, Vector2 position, string text) {
            PreparePosition(elem, ref position);

            GUI.skin.label.normal.textColor = elem.Foreground;
            // Direct texts in anything with an unfitting size will have some text bounds issues - use SGUILabel...
            GUI.Label(new Rect(position, elem != null ? elem.Size : Vector2.zero), text);
        }

        public void TextField(SElement elem, Vector2 position, ref string text) {
            if (!IsRelative(elem) && elem != null) {
                position += elem.Position;
            }

            GUI.skin.textField.normal.textColor = elem.Foreground;
            GUI.skin.textField.active.textColor = elem.Foreground;
            GUI.skin.textField.hover.textColor = elem.Foreground;
            GUI.skin.textField.focused.textColor = elem.Foreground;
            GUI.backgroundColor = elem.Background;
            text = GUI.TextField(new Rect(position, elem.Size), text);
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
                _TextGenerator.GetPreferredHeight(text, _TextGenerationSettings)
            );
        }

        public void Dispose() {
        }

    }
}
