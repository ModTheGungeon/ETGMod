using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SGUI {
    public sealed class SGUIIMBackend : ISGUIBackend {

        public readonly static Rect NULLRECT = new Rect(-1f, -1f, 0f, 0f);
        public readonly static Vector2 MAXVEC2 = new Vector2(float.MaxValue, float.MaxValue);

        public static int DefaultDepth = 0x0ade;
        public int Depth = DefaultDepth;

        public static Func<SGUIIMBackend, Font> GetFont;

        private readonly static Color _Transparent = new Color(0f, 0f, 0f, 0f);

        private readonly static List<SElement> _ComponentElements = new List<SElement>();
        private static int _GlobalComponentSemaphore;
        private int _ComponentSemaphore;

        private readonly List<SElement> _Elements = new List<SElement>();

        // Required for UpdateWindow magic
        private readonly Stack<EClipScopeType> _ClipScopeTypes = new Stack<EClipScopeType>();
        private readonly Stack<Rect> _ClipScopeRects = new Stack<Rect>();

        // Required for near-to-far mouse event handling to mimic far-to-near drawing of everything before.
        private readonly List<EGUIOperation> _OPs = new List<EGUIOperation>();
        private readonly List<int> _OPComponentIDs = new List<int>();
        private readonly List<int> _OPElementIDs = new List<int>();
        private readonly List<EGUIComponent> _OPComponents = new List<EGUIComponent>();
        private readonly List<Rect> _OPBounds = new List<Rect>();
        private readonly List<object[]> _OPData = new List<object[]>();

        public SGUIRoot CurrentRoot { get; private set; }

        public bool UpdateStyleOnGUI {
            get {
                return true;
            }
        }

        public bool RenderOnGUI {
            get {
                return true;
            }
        }

        public bool RenderOnGUILayout {
            get {
                return false;
            }
        }

        private Event _Reason;
        public bool IsOnGUIRepainting {
            get {
                return _Reason.type == EventType.Repaint;
            }
        }

        public float LineHeight { get; private set; }
        public float IconPadding {
            get {
                return LineHeight * 0.333f + 1f; // Space is often 1/4 em; this should do the job well enough.
            }
        }

        private Font _Font;
        public object Font {
            get {
                return _Font;
            }
            set {
                Font valueFont = (Font) value;
                if (_Font != valueFont) {
                    LineHeight = valueFont.dynamic ? valueFont.lineHeight * 2f : (valueFont.characterInfo[0].glyphHeight + 4f);
                }
                _Font = valueFont;
            }
        }

        public bool LastMouseEventConsumed { get; private set; }
        private Vector2 _LastMousePosition = new Vector2(-1f, -1f);

        private System.Random _SecretPRNG = new System.Random();
        private int _CurrentSecret;
        private int _Secret {
            get {
                return _CurrentSecret = _SecretPRNG.Next();
            }
        }
        public bool IsValidSecret(long secret) {
            return _CurrentSecret == secret;
        }
        public void VerifySecret(long secret) {
            if (!IsValidSecret(secret)) {
                throw new InvalidOperationException("Don't leak secrets!");
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

        public void StartOnGUI(SGUIRoot root) {
            if (CurrentRoot != null) {
                throw new InvalidOperationException("StartOnGUI already called! Call EndOnGUI first!");
            }
            CurrentRoot = root;
            _Reason = Event.current;

            GUI.skin.textField.normal.background = CurrentRoot.TextFieldBackground[0];
            GUI.skin.textField.active.background = CurrentRoot.TextFieldBackground[1];
            GUI.skin.textField.hover.background = CurrentRoot.TextFieldBackground[2];
            GUI.skin.textField.focused.background = CurrentRoot.TextFieldBackground[3];

            GUI.skin.settings.selectionColor = new Color(0.3f, 0.6f, 0.9f, 1f);

            GUI.depth = Depth;

            if (IsOnGUIRepainting) {
                if (_GlobalComponentSemaphore == 0) {
                    _ComponentElements.Clear();
                }

                _Elements.Clear();

                _OPs.Clear();
                _OPComponentIDs.Clear();
                _OPElementIDs.Clear();
                _OPComponents.Clear();
                _OPBounds.Clear();
                _OPData.Clear();
            }

            // Console.WriteLine("SGUI-IM: Starting new render with following reason: " + _Reason);
        }
        public void EndOnGUI(SGUIRoot root) {
            if (CurrentRoot == null) {
                throw new InvalidOperationException("EndOnGUI already called! Call StartOnGUI first!");
            }
            // Console.WriteLine("SGUI-IM: Ending render with " + _GlobalComponentSemaphore + " components, of which " + _ComponentSemaphore + " are local.");

            _GlobalComponentSemaphore -= _ComponentSemaphore;
            _ComponentSemaphore = 0;

            CurrentRoot = null;
        }
        public void OnGUI() {
            SGUIRoot root = CurrentRoot;
            if (_Reason.isMouse || _Reason.type == EventType.ScrollWheel) {
                LastMouseEventConsumed = HandleMouseEvent(Event.current) != -1;
                return;
            }

            // Your normal rendering run.
            for (int i = 0; i < root.Children.Count; i++) {
                SElement child = root.Children[i];
                child.Root = root;
                child.Parent = null;
                if (!child.Visible) continue;
                child.Render();
            }

            // After the normal (possibly) repaint run, check for any mouse movement.
            if (IsOnGUIRepainting) {
                Vector2 mousePosition = Input.mousePosition;
                mousePosition = new Vector2(
                    mousePosition.x,
                    root.Size.y - mousePosition.y - 1f
                );
                if (_LastMousePosition.x <= -1f && _LastMousePosition.y <= -1f) {
                    _LastMousePosition = mousePosition;
                } else {
                    HandleMouseEvent(new Event(Event.current.displayIndex) {
                        type = EventType.MouseMove,
                        mousePosition = mousePosition,
                        delta = mousePosition - _LastMousePosition
                    });
                    _LastMousePosition = mousePosition;
                }
            }
        }

        public int HandleMouseEvent(Event e) {
            // Console.WriteLine();
            // Console.WriteLine("SGUI-IM: Handling mouse event " + e);
            SGroup lastWindowDragging = _WindowDragging;

            EventType type = e.type;
            EMouseStatus status = EMouseStatus.Outside;
            switch (type) {
                case EventType.MouseUp  : status = EMouseStatus.Up  ; break;
                case EventType.MouseDown: status = EMouseStatus.Down; break;
                case EventType.MouseDrag: status = EMouseStatus.Drag; break;
            }
            Vector2 pos = e.mousePosition;

            int handled = HandleMouseEventIn(e, null);

            if (handled != -1) {
                if (type != EventType.MouseMove) {
                    _RegisteredNextElement = false;
                    for (int i = 0; i < _OPs.Count; i++) {
                        _RecreateOperation(i, handled);
                    }

                    _ComponentElements[handled]?.SetMouseStatus(_Secret, status, pos);

                } else {
                    for (int i = 0; i < _Elements.Count; i++) {
                        SElement elem = _Elements[i];
                        if (elem == null) continue;
                        // TODO replace GetFirstComponentID with possible future HasComponentID
                        elem.SetMouseStatus(_Secret, GetFirstComponentID(elem) == handled ? EMouseStatus.Inside : EMouseStatus.Outside, pos);
                    }
                }
            }

            if (lastWindowDragging == null && _WindowDragging != null) {
                IList<SElement> children = _WindowDragging.Parent?.Children ?? _WindowDragging.Root.Children;
                children.Remove(_WindowDragging);
                children.Insert(children.Count, _WindowDragging);
            }

            // Console.WriteLine("SGUI-IM: Mouse event handled: " + handled);
            // Console.WriteLine();
            return handled;
        }
        public int HandleMouseEventIn(Event e, SElement elem) {
            int handled;
            if (elem != null) {
                if (!elem.Visible) return -1;
                if (elem is SGroup) return HandleMouseEventInGroup(e, (SGroup) elem);

                if (!new Rect(elem.AbsoluteOffset + elem.Position, elem.Size).Contains(e.mousePosition)) {
                    return -1;
                }
                if (!elem.Enabled) {
                    return GetFirstComponentID(elem);
                }
            }

            IList<SElement> children = elem?.Children ?? CurrentRoot.Children;
            for (int i = children.Count - 1; 0 <= i; i--) {
                if ((handled = HandleMouseEventIn(e, children[i])) != -1) return handled;
            }

            if (elem == null) {
                return -1;
            }

            if (e.type == EventType.ScrollWheel) {
                return -1;
            }

            return GetFirstComponentID(elem);
        }
        private SGroup _WindowDragging;
        public int HandleMouseEventInGroup(Event e, SGroup group) {
            int handled;
            int groupFirstComponent = GetFirstComponentID(group);
            bool containsMouse = new Rect(
                group.AbsoluteOffset.x + group.Position.x,
                group.AbsoluteOffset.y + group.Position.y,
                group.Size.x + group.Border * 2f,
                group.Size.y + (group.IsWindow ? group.WindowTitleBar.Size.y : 0f) + group.Border * 2f
            ).Contains(e.mousePosition);

            if (group.IsWindow) {
                SWindowTitleBar bar = group.WindowTitleBar;

                Rect bounds = (_ClipScopeRects.Count == 0 || !IsOnGUIRepainting) ?
                        new Rect(group.AbsoluteOffset + group.Position, bar.Size) :
                        new Rect(Vector2.zero, bar.Size);
                if (e.type == EventType.MouseDown && bounds.Contains(e.mousePosition)) {
                    bar.Dragging = true;
                    _WindowDragging = group;
                    e.Use();
                    return groupFirstComponent;
                }

                if (bar.Dragging) {
                    if (e.type == EventType.MouseDrag) {
                        group.Position += e.delta;
                    } else if (e.type == EventType.MouseUp) {
                        bar.Dragging = false;
                        _WindowDragging = null;
                    }
                    e.Use();
                    return groupFirstComponent;
                }

                bar.Dragging = false;
            }

            if (!containsMouse) {
                return -1;
            }

            if (e.type == EventType.ScrollWheel && group.ScrollDirection != SGroup.EDirection.None) {
                group.ScrollMomentum = e.delta * group.ScrollInitialMomentum;
                e.Use();
                return groupFirstComponent;
            }

            IList<SElement> children = group.Children;
            for (int i = children.Count - 1; 0 <= i; i--) {
                if ((handled = HandleMouseEventIn(e, children[i])) != -1) return handled;
            }

            e.Use(); // Window background would be click-through otherwise.
            return groupFirstComponent;
        }

        public string NextComponentName(SElement elem) {
            ++_GlobalComponentSemaphore;
            ++_ComponentSemaphore;
            _ComponentElements.Add(elem);
            return (_ComponentElements.Count - 1).ToString();
        }
        private bool _RegisteredNextElement;
        public int RegisterNextComponentIn(SElement elem) {
            _RegisteredNextElement = true;
            GUI.SetNextControlName(NextComponentName(elem));
            if (!_Elements.Contains(elem)) {
                _Elements.Add(elem);
            }
            // Console.WriteLine("SGUI-IM: Registered E" + CurrentElementID + "C" + CurrentComponentID + " (" + elem + ")");
            return CurrentComponentID;
        }
        public int RegisterNextComponent() {
            if (!_RegisteredNextElement) {
                GUI.SetNextControlName(NextComponentName(null));
            }
            _RegisteredNextElement = false;
            return CurrentComponentID;
        }

        public void RegisterOperation(EGUIOperation op, EGUIComponent elem, Rect bounds, params object[] args) {
            _OPs.Add(op);
            _OPComponentIDs.Add(CurrentComponentID);
            _OPElementIDs.Add(CurrentElementID);
            _OPComponents.Add(elem);
            _OPBounds.Add(bounds);
            _OPData.Add(args);
            // Console.WriteLine("SGUI-IM: Registered OP " + op + " " + elem + " @ " + bounds + " for E" + CurrentElementID + "C" + CurrentComponentID);
        }
        private bool _RecreateOperation(int opID, int handledComponentID = -1) {
            if (opID < 0) {
                opID = _OPs.Count + opID;
            }
            Event e = Event.current;

            EGUIOperation op = _OPs[opID];
            int elemID = _OPElementIDs[opID];
            int compID = _OPComponentIDs[opID];
            EGUIComponent elemGUI = _OPComponents[opID];
            Rect bounds = _OPBounds[opID];
            object[] data = _OPData[opID];

            // Console.WriteLine("SGUI-IM: Recreating operation for E" + elemID + "C" + compID + ": " + op + " " + elemGUI);

            SElement elem = _Elements[elemID];

            if (op == EGUIOperation.Draw) {
                if (e.type == EventType.Used) {
                    // Console.WriteLine("SGUI-IM: Current event used - recreating operation @ NULLRECT");
                    bounds = NULLRECT;
                }

                if (compID < handledComponentID) {
                    // Console.WriteLine("SGUI-IM: Current component before handled component (" + handledComponentID + ") - recreating operation @ NULLRECT");
                    bounds = NULLRECT;
                }

                if (elem == null || !elem.Visible || !elem.Enabled) {
                    // Console.WriteLine("SGUI-IM: Current component element not interactable - recreating operation @ NULLRECT");
                    bounds = NULLRECT;
                }
            }

            switch (op) {
                case EGUIOperation.Draw:
                    switch (elemGUI) {
                        case EGUIComponent.Label:
                            RegisterNextComponent();
                            GUI.Label(bounds, (string) data[0]);
                            break;

                        case EGUIComponent.TextField:
                        case EGUIComponent.Button:
                            RegisterNextComponent();
                            // TextField and Button use mouse input by themselves.
                            if (elemGUI == EGUIComponent.TextField) {
                                if (e.isMouse && bounds.Contains(e.mousePosition)) {
                                    // ... although the TextField mouse input requires some help with the cursor placement.
                                    string text = (string) data[0];
                                    Vector2 mousePosition = e.mousePosition;
                                    EventType type = e.type;
                                    Event.current.Use();
                                    GUI.TextField(bounds, text);
                                    // Focus(compID); // Actually kills focus.
                                    TextEditor editor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
#pragma warning disable CS0618
                                    // TextEditor.content is obsolete, yet it must be accessed.
                                    // Alternatively access TextEditor.m_Content via reflection.
                                    GUIContent content = editor.content;
#pragma warning restore CS0618

                                    // editor.style.GetCursorStringIndex(this.position, this.m_Content, cursorPosition + this.scrollOffset);
                                    // GetCursorStringIndex seems broken.
                                    int index = 0;
                                    Vector2 position = -editor.scrollOffset;
                                    PreparePosition(elem, ref position);
                                    Rect boundsScrolled = new Rect(position.x, position.y, bounds.size.x, bounds.size.y);
                                    for (; index < text.Length; index++) {
                                        if (mousePosition.x < editor.style.GetCursorPixelPosition(boundsScrolled, content, index).x - LineHeight * 0.5f) {
                                            break;
                                        }
                                    }

                                    if (type == EventType.MouseDown) {
                                        editor.cursorIndex = index;
                                        editor.selectIndex = index;
                                    } else if (type == EventType.MouseDrag) {
                                        editor.cursorIndex = index;
                                    }

                                    editor.UpdateScrollOffsetIfNeeded();
                                    return true;

                                } else {
                                    GUI.TextField(bounds, (string) data[0]);
                                }

                            }

                            if (e.isMouse && bounds.Contains(e.mousePosition)) {
                                // Still need to know whether mouse is in it or not, though
                                return true;
                            }
                            break;
                    }
                    break;

                case EGUIOperation.Start:
                    RegisterNextComponent();
                    switch (elemGUI) {
                        case EGUIComponent.Clip:
                            GUI.BeginClip(bounds, Vector2.zero, Vector2.zero, true);
                            _ClipScopeTypes.Push(EClipScopeType.Clip);
                            _ClipScopeRects.Push(bounds);
                            break;

                        case EGUIComponent.Group:
                            GUI.BeginGroup(bounds);
                            _ClipScopeTypes.Push(EClipScopeType.Group);
                            _ClipScopeRects.Push(bounds);
                            break;

                        case EGUIComponent.Scroll:
                            GUI.BeginScrollView(bounds, (Vector2) data[0], (Rect) data[1]);
                            _ClipScopeTypes.Push(EClipScopeType.Scroll);
                            _ClipScopeRects.Push(bounds);
                            break;
                    }
                    break;

                case EGUIOperation.End:
                    switch (elemGUI) {
                        case EGUIComponent.Clip:
                            GUI.EndClip();
                            break;

                        case EGUIComponent.Group:
                            GUI.EndGroup();
                            break;

                        case EGUIComponent.Scroll:
                            GUI.EndScrollView();
                            break;
                    }
                    _ClipScopeTypes.Pop();
                    _ClipScopeRects.Pop();
                    break;
            }

            return false;
        }

        public int CurrentComponentID {
            get {
                return _GlobalComponentSemaphore - 1;
            }
        }
        public int GetFirstComponentID(SElement elem) {
            return _ComponentElements.IndexOf(elem);
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
            string id = GetFirstComponentID(elem).ToString();
            // if window, use GUI.FocusWindow
            GUI.FocusControl(id);
        }
        public void Focus(int id) {
            GUI.FocusControl(id.ToString());
        }
        public bool IsFocused(SElement elem) {
            return GUI.GetNameOfFocusedControl() == GetFirstComponentID(elem).ToString();
        }
        public bool IsFocused(int id) {
            return GUI.GetNameOfFocusedControl() == id.ToString();
        }

        public bool IsRelativeContext(SElement elem) {
            if (!IsOnGUIRepainting) {
                // Everything's absolute in event-handling mode.
                return false;
            }
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
            position += elem.InnerOrigin;

            PreparePosition(elem.Parent, ref position);
        }

        public void Texture(SElement elem, Vector2 position, Vector2 size, Texture texture, Color? color = null) {
            PreparePosition(elem, ref position);
            RegisterNextComponentIn(elem);
            Texture(position, size, texture, color);
        }
        public void Texture(Vector2 position, Vector2 size, Texture texture, Color? color = null) {
            if (!IsOnGUIRepainting) return;
            if (color != null && color.Value.a < 0.01f) return;
            Rect bounds = new Rect(position, size);
            Color prevGUIColor = GUI.color;
            GUI.color = color ?? Color.white;
            RegisterNextComponent();
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.Rect, bounds);
            GUI.DrawTexture(bounds, texture, ScaleMode.StretchToFill);
            GUI.color = prevGUIColor;
        }

        public void Rect(SElement elem, Vector2 position, Vector2 size, Color color) {
            PreparePosition(elem, ref position);
            RegisterNextComponentIn(elem);
            Rect(position, size, color);
        }
        public void Rect(Vector2 position, Vector2 size, Color color) {
            if (!IsOnGUIRepainting) return;
            Rect bounds = new Rect(position, size);
            Color prevGUIColor = GUI.color;
            GUI.color = color;
            RegisterNextComponent();
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.Rect, bounds);
            GUI.DrawTexture(bounds, SGUIRoot.White, ScaleMode.StretchToFill, color.a < 1f);
            GUI.color = prevGUIColor;
        }


        public void StartClip(SElement elem) {
            Vector2 position = elem.Position;
            PreparePosition(elem, ref position);
            RegisterNextComponentIn(elem);
            StartClip(new Rect(position, elem.Size));
        }
        public void StartClip(SElement elem, Rect bounds) {
            Vector2 position = Vector2.zero;
            PreparePosition(elem, ref position);
            bounds.position += position;
            RegisterNextComponentIn(elem);
            StartClip(bounds);
        }
        public void StartClip(Rect bounds) {
            RegisterNextComponent();
            RegisterOperation(EGUIOperation.Start, EGUIComponent.Clip, bounds);
            GUI.BeginClip(bounds, Vector2.zero, Vector2.zero, true);
            _ClipScopeTypes.Push(EClipScopeType.Clip);
            _ClipScopeRects.Push(bounds);
        }
        public void EndClip() {
            RegisterOperation(EGUIOperation.End, EGUIComponent.Clip, NULLRECT);
            GUI.EndClip();
            _ClipScopeTypes.Pop();
            _ClipScopeRects.Pop();
        }


        public void Text(SElement elem, Vector2 position, Vector2 size, string text, TextAnchor alignment = TextAnchor.MiddleCenter, Texture icon = null) {
            Text_(elem, position, size, text, alignment, icon);
        }
        private void Text_(SElement elem, Vector2 position, Vector2 size, string text, TextAnchor alignment = TextAnchor.MiddleCenter, Texture icon = null, bool registerProperly = true) {
            GUI.skin.font = (Font) elem?.Font ?? _Font;

            if (text.Contains("\n")) {
                string[] lines = text.Split('\n');
                float y = 0f;
                Vector2 lineSize = new Vector2(size.x, LineHeight);
                for (int i = 0; i < lines.Length; i++) {
                    string line = lines[i];
                    bool iconMissing = icon != null && i > 0;
                    Text_(
                        elem,
                        position + new Vector2(iconMissing ? (icon.width + 1f) : 0f, y),
                        lineSize.WithX(size.x - (iconMissing ? icon.width : 0f)),
                        iconMissing ? $" {line}" : line,
                        alignment,
                        i == 0 ? icon : null,
                        registerProperly
                    );
                    y += lineSize.y;
                }
                return;
            }

            PreparePosition(elem, ref position);
            Rect bounds = new Rect(position, size);

            if (elem != null) {
                GUI.skin.label.normal.textColor = elem.Foreground;
            }
            GUI.skin.label.alignment = alignment;
            RegisterNextComponentIn(elem);
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.Label, registerProperly ? bounds : NULLRECT, text);
            if (icon != null) text = $" {text}";
            GUI.Label(bounds, new GUIContent(text, icon));
        }

        public void TextField(SElement elem, Vector2 position, Vector2 size, ref string text) {
            GUI.skin.font = (Font) elem?.Font ?? _Font;
            PreparePosition(elem, ref position);

            if (elem != null && IsOnGUIRepainting) {
                GUI.skin.textField.normal.textColor = elem.Foreground * 0.8f;
                GUI.skin.textField.active.textColor = elem.Foreground;
                GUI.skin.textField.hover.textColor = elem.Foreground;
                GUI.skin.textField.focused.textColor = elem.Foreground;
                GUI.skin.settings.cursorColor = elem.Foreground;
                GUI.backgroundColor = elem.Background;
            }

            Event e = Event.current;
            bool keyEvent = e.isKey;
            bool keyDown = e.type == EventType.KeyDown;
            KeyCode keyCode = e.keyCode;
            bool submit = keyDown && keyCode == KeyCode.Return;

            STextField field = elem as STextField;

            if (field != null && field.OverrideTab && keyEvent && (e.keyCode == KeyCode.Tab || e.character == '\t')) {
                e.Use();
            }

            RegisterNextComponentIn(elem);
            string prevText = text;
            TextField(new Rect(position, elem.Size), ref text);

            if (IsOnGUIRepainting) {
                elem.SetFocused(_Secret, IsFocused(CurrentComponentID));
            } else if (field != null && elem.IsFocused) {
                TextEditor editor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

                field.SetCursorIndex(_Secret, editor.cursorIndex, editor.selectIndex);
                if (submit) text = field.TextOnSubmit ?? prevText;

                if (prevText != text) field.OnTextUpdate?.Invoke(field, prevText);
                if (keyEvent) field.OnKey?.Invoke(field, keyDown, keyCode);
                if (submit) field.OnSubmit?.Invoke(field, prevText);
            }
        }
        public void TextField(Rect bounds, ref string text) {
            RegisterNextComponent();
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.TextField, bounds, text);
            text = GUI.TextField(bounds, text);

            if (!GUI.skin.font.dynamic && IsFocused(CurrentComponentID) && IsOnGUIRepainting) {
                TextEditor editor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
#pragma warning disable CS0618
                // TextEditor.content is obsolete, yet it must be accessed.
                // Alternatively access TextEditor.m_Content via reflection.
                GUIContent content = editor.content;
#pragma warning restore CS0618
                Color prevGUIColor = GUI.color;

                if (editor.cursorIndex != editor.selectIndex) {
                    Rect boundsRelative = new Rect(-editor.scrollOffset, bounds.size);
                    Vector2 selectA = editor.style.GetCursorPixelPosition(boundsRelative, content, editor.cursorIndex);
                    Vector2 selectB = editor.style.GetCursorPixelPosition(boundsRelative, content, editor.selectIndex);
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
        }
        public void MoveTextFieldCursor(SElement elem, ref int? cursor, ref int? selection) {
            if (!IsFocused(GetFirstComponentID(elem))) {
                return;
            }

            TextEditor editor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (elem is STextField) editor.text = ((STextField) elem).Text;

            if (cursor != null)     editor.cursorIndex = cursor.Value;

            if (selection != null)  editor.selectIndex = selection.Value;
            else                    editor.selectIndex = cursor.Value;

            cursor = null;
            selection = null;
        }


        public void Button(SElement elem, Vector2 position, Vector2 size, string text, TextAnchor alignment = TextAnchor.MiddleCenter, Texture icon = null) {
            GUI.skin.font = (Font) elem?.Font ?? _Font;
            PreparePosition(elem, ref position);

            if (elem != null && IsOnGUIRepainting) {
                GUI.skin.label.normal.textColor = elem.Foreground * (elem.IsHovered ? 1f : 0.9f);
                GUI.backgroundColor = elem.Background;
            }

            RegisterNextComponentIn(elem);
            Button(position, size, text, (elem as SButton)?.Border, alignment, icon);
        }
        public void Button(Vector2 position, Vector2 size, string text, Vector2? border = null, TextAnchor alignment = TextAnchor.MiddleCenter, Texture icon = null) {
            Vector2 border_ = border ?? new Vector2(4f, 4f);
            if (border == null) size += border_;
            RegisterNextComponent();
            Rect(null, position, size, GUI.backgroundColor);

            Text_(
                null,
                position + border_,
                size - border_ * 2f,
                text, alignment, icon, false);
        }


        public void StartGroup(SGroup group) {
            Vector2 position = group.InnerOrigin;
            PreparePosition(group, ref position);
            Rect bounds = new Rect(position, group.InnerSize);

            GUI.backgroundColor = _Transparent;
            RegisterNextComponentIn(group);
            if (group.ScrollDirection == SGroup.EDirection.None) {
                RegisterOperation(EGUIOperation.Start, EGUIComponent.Group, bounds);
                GUI.BeginGroup(bounds);
                _ClipScopeTypes.Push(EClipScopeType.Group);

            } else {
                Rect viewBounds = new Rect(Vector2.zero, group.ContentSize);
                RegisterOperation(EGUIOperation.Start, EGUIComponent.Scroll, bounds, group.ScrollPosition, viewBounds);
                group.ScrollPosition = GUI.BeginScrollView(
                    bounds, group.ScrollPosition, viewBounds,
                    (group.ScrollDirection & SGroup.EDirection.Horizontal) == SGroup.EDirection.Horizontal,
                    (group.ScrollDirection & SGroup.EDirection.Vertical) == SGroup.EDirection.Vertical
                );
                _ClipScopeTypes.Push(EClipScopeType.Scroll);
            }

            _ClipScopeRects.Push(bounds);
        }
        public void EndGroup(SGroup group) {
            if (group.ScrollDirection != SGroup.EDirection.None) {
                RegisterOperation(EGUIOperation.End, EGUIComponent.Scroll, NULLRECT);
                GUI.EndScrollView();
                _ScrollBar(group);
            } else {
                RegisterOperation(EGUIOperation.End, EGUIComponent.Group, NULLRECT);
                GUI.EndGroup();
            }

            _ClipScopeTypes.Pop();
            _ClipScopeRects.Pop();
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
            RegisterNextComponentIn(group);
            RegisterOperation(EGUIOperation.Start, EGUIComponent.Group, bounds);
            GUI.BeginGroup(bounds);
            _ClipScopeTypes.Push(EClipScopeType.CustomWindow);
            _ClipScopeRects.Push(bounds);

            group.RenderGroup(CurrentComponentID);

            RegisterOperation(EGUIOperation.End, EGUIComponent.Group, NULLRECT);
            GUI.EndGroup();
            _ClipScopeTypes.Pop();
            _ClipScopeRects.Pop();
        }
        public void StartWindow(SGroup group) {
            if (group.WindowID == -1) {
                return;
            }

            Rect(
                new Vector2(0f, group.WindowTitleBar.Size.y),
                new Vector2(
                    group.Size.x + group.Border * 2f,
                    group.Size.y + group.Border * 2f
                ), group.Background
            );

            Rect bounds = new Rect(
                group.Border,
                group.WindowTitleBar.Size.y + group.Border,
                group.Size.x, group.Size.y
            );
            GUI.backgroundColor = _Transparent;
            RegisterNextComponentIn(group);
            if (group.ScrollDirection == SGroup.EDirection.None) {
                RegisterOperation(EGUIOperation.Start, EGUIComponent.Clip, bounds);
                GUI.BeginClip(
                    bounds,
                    Vector2.zero,
                    new Vector2(0f, group.WindowTitleBar.Size.y),
                    true
                );
                _ClipScopeTypes.Push(EClipScopeType.Clip);
            } else {
                Rect viewBounds = new Rect(Vector2.zero, group.ContentSize);
                RegisterOperation(EGUIOperation.Start, EGUIComponent.Scroll, bounds, group.ScrollPosition, viewBounds);
                group.ScrollPosition = GUI.BeginScrollView(
                    bounds, group.ScrollPosition, viewBounds,
                    (group.ScrollDirection & SGroup.EDirection.Horizontal) == SGroup.EDirection.Horizontal,
                    (group.ScrollDirection & SGroup.EDirection.Vertical) == SGroup.EDirection.Vertical
                );
                _ClipScopeTypes.Push(EClipScopeType.Scroll);
            }

            _ClipScopeRects.Push(bounds);
        }
        public void EndWindow(SGroup group) {
            if (group.WindowID == -1) {
                return;
            }

            if (group.ScrollDirection != SGroup.EDirection.None) {
                RegisterOperation(EGUIOperation.End, EGUIComponent.Clip, NULLRECT);
                GUI.EndScrollView();
                _ScrollBar(group);
            } else {
                RegisterOperation(EGUIOperation.End, EGUIComponent.Clip, NULLRECT);
                GUI.EndClip();
            }

            _ClipScopeTypes.Pop();
            _ClipScopeRects.Pop();

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
                Text(bar, new Vector2(group.Border, 0f), bar.InnerSize, title);
            }

            // TODO Window header buttons.
        }

        private void _ScrollBar(SGroup group) {
            Rect bounds = !group.IsWindow ? new Rect(group.InnerOrigin, group.InnerSize) : new Rect(
                group.Border,
                group.WindowTitleBar.Size.y + group.Border,
                group.Size.x, group.Size.y
            );

            if (bounds.height <= group.ContentSize.y) {
                float width = 2f; // TODO Grow on mouse-over.
                Vector2 position = new Vector2(
                    bounds.x + bounds.width - width,
                    bounds.y + bounds.height * group.ScrollPosition.y / group.ContentSize.y
                );
                Vector2 size = new Vector2(
                    width,
                    Mathf.Max(0f, Mathf.Min(bounds.height, bounds.height * bounds.height / group.ContentSize.y + position.y) - position.y)
                );

                Rect(group, position, size, group.Foreground * 0.8f);

                // TODO Mouse input.
            }

            // TODO Horizontal scroll.

        }

        public Vector2 MeasureText(string text, Vector2? size = null, object font = null) {
            return MeasureText(ref text, size, font);
        }
        public Vector2 MeasureText(ref string text, Vector2? size = null, object font = null) {
            Font font_ = (Font) font ?? _Font;

            Vector2 bounds = size ?? MAXVEC2;
            float x = 0f;
            float maxX = 0f;
            float y = 0f;

            StringBuilder rebuilt = new StringBuilder();
            int lastSpace = -1;
            int offset = 0;
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                CharacterInfo ci;
                bool ciGot = font_.GetCharacterInfo(c, out ci);
                if (!ciGot) {
                    if (c != '\n') {
                        offset--;
                        continue;
                    }
                }

                if (ciGot) x += ci.advance;
                if (x > maxX) maxX = x;
                if (x > bounds.x || c == '\n') {
                    if (lastSpace == -1 || c == '\n') {
                        rebuilt.Append('\n');
                        if (c != '\n') ++offset;
                    } else {
                        rebuilt[lastSpace + offset] = '\n';
                    }

                    lastSpace = -1;
                    x = 0f;
                    y += LineHeight;
                }
                if (c == '\n') {
                    continue;
                }
                if (c == ' ') lastSpace = i;
                rebuilt.Append(c);
            }

            text = rebuilt.ToString().TrimEnd();
            return new Vector2(maxX, y + LineHeight);
        }

        public void Dispose() {
        }

        public void Dispose(SElement elem) {
        }

        public enum EClipScopeType {
            Clip,
            Group,
            Scroll,
            Window,
            CustomWindow,
        }

        public enum EGUIOperation {
            Draw,
            Start,
            End,
        }
        public enum EGUIComponent {
            Ignore,
            Rect,
            Label,
            TextField,
            Button,
            Clip,
            Group,
            Scroll,
        }

    }
}
