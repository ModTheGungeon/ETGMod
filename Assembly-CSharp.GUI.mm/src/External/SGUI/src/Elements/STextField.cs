using System;
using UnityEngine;

namespace SGUI {
    public class STextField : SElement {

        public string Text;
        public string TextOnSubmit = "";

        public bool OverrideTab;

        public Action<STextField, string> OnTextUpdate;
        public Action<STextField, bool, KeyCode> OnKey;
        public Action<STextField, string> OnSubmit;

        public override bool IsInteractive {
            get {
                return true;
            }
        }

        public int CursorIndex { get; protected set; }
        public int SelectionIndex { get; protected set; }

        protected int? _MoveCursorIndex;
        protected int? _MoveSelectionIndex;

        public STextField()
            : this("") { }
        public STextField(string text) {
            Text = text;
            Background = Color.white;
            Foreground = Color.black;
        }

        public override void UpdateStyle() {
            // This will get called again once this element gets added to the root.
            if (Root == null) return;

            if (UpdateBounds) {
                Size.y = Backend.LineHeight;
            }

            base.UpdateStyle();
        }

        public override void RenderBackground() { }
        public override void Render() {
            // Do not render background - background should be handled by Draw.TextField

            Draw.TextField(this, Vector2.zero, Size, ref Text);

            if (_MoveCursorIndex != null || _MoveSelectionIndex != null) {
                Backend.MoveTextFieldCursor(this, ref _MoveCursorIndex, ref _MoveSelectionIndex);
            }

            // Focusing should happen when the element has got a valid ID (after rendering the element) and after checking for it.
            if (_ScheduleFocus) {
                _ScheduleFocus = false;
                Backend.Focus(this);
            }
        }

        protected bool _ScheduleFocus;
        public override void Focus() {
            _ScheduleFocus = true;
        }

        public void MoveCursor(int cursor, int? selection = null) {
            _MoveCursorIndex = cursor;
            _MoveSelectionIndex = selection;
        }

        public void SetCursorIndex(int secret, int cursor, int select) {
            Backend.VerifySecret(secret);
            CursorIndex = cursor;
            SelectionIndex = select;
        }

   }
}
