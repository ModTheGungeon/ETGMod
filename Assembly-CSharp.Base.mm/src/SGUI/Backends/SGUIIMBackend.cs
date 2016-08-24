using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGUI {
    public sealed class SGUIIMBackend : ISGUIBackend {

        public readonly static Rect NULLRECT = new Rect(-1f, -1f, 0f, 0f);

        public static int DefaultDepth = 0x0ade;
        public int Depth = DefaultDepth;

        public static Func<SGUIIMBackend, Font> GetFont;

        private readonly static Color _Transparent = new Color(0f, 0f, 0f, 0f);

        // IDisposable, yet cannot be disposed. Let's keep one reference for the lifetime of all backend instances.
        private readonly static TextGenerator _TextGenerator = new TextGenerator();
        private TextGenerationSettings _TextGenerationSettings = new TextGenerationSettings();

        private readonly static List<SElement> _ComponentElements = new List<SElement>();
        private static int _GlobalComponentSemaphore;
        private int _ComponentSemaphore;

        private readonly List<SElement> _Elements = new List<SElement>();
        private int _ElementSemaphore;

        private readonly List<int> _ClickedButtons = new List<int>();

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

        private Event _Reason;
        public bool Repainting {
            get {
                return _Reason.type == EventType.Repaint;
            }
        }

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

        public bool RenderOnGUILayout {
            get {
                return false;
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
            _Reason = Event.current;

            GUI.skin.textField.normal.background = CurrentRoot.TextFieldBackground[0];
            GUI.skin.textField.active.background = CurrentRoot.TextFieldBackground[1];
            GUI.skin.textField.hover.background = CurrentRoot.TextFieldBackground[2];
            GUI.skin.textField.focused.background = CurrentRoot.TextFieldBackground[3];

            GUI.skin.settings.selectionColor = new Color(0.3f, 0.6f, 0.9f, 1f);

            GUI.depth = Depth;

            if (Repainting) {
                _ElementSemaphore = 0;
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
        public void EndRender(SGUIRoot root) {
            if (CurrentRoot == null) {
                throw new InvalidOperationException("EndRender already called! Call StartRender first!");
            }
            // Console.WriteLine("SGUI-IM: Ending render with " + _GlobalComponentSemaphore + " components, of which " + _ComponentSemaphore + " are local.");

            _GlobalComponentSemaphore -= _ComponentSemaphore;
            _ComponentSemaphore = 0;

            if (_GlobalComponentSemaphore == 0) {
                _ComponentElements.Clear();
            }

            if (Repainting) {
                _ClickedButtons.Clear();
            }

            CurrentRoot = null;
        }
        public void Render() {
            SGUIRoot root = CurrentRoot;
            // TODO or use GUI.tooltip..?! WHY THE FUCK DID NOBODY TELL ME ABOUT THIS?!
            if (_Reason.isMouse) {
                HandleMouseEvent();
                return;
            }

            // Your normal rendering run.
            for (int i = 0; i < root.Children.Count; i++) {
                SElement child = root.Children[i];
                child.Root = root;
                child.Parent = null;
                child.Render();
            }
        }

        public bool HandleMouseEvent() {
            // Console.WriteLine();
            // Console.WriteLine("SGUI-IM: Handling mouse event " + Event.current);
            _ElementSemaphore = _Elements.Count;
            SGroup lastWindowDragging = _WindowDragging;

            bool handled = HandleMouseEventIn(null);

            if (handled) {
                _RegisteredNextElement = false;
                for (int i = 0; i < _OPs.Count; i++) {
                    _RecreateOperation(i);
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
        public bool HandleMouseEventIn(SElement elem) {
            if (elem is SGroup && UpdateWindow((SGroup) elem)) {
                return true;
            }

            IList<SElement> children = elem?.Children ?? CurrentRoot.Children;
            for (int i = children.Count - 1; 0 <= i; i--) {
                if (HandleMouseEventIn(children[i])) return true;
            }

            if (!(elem is SGroup) && elem != null) {
                // Simple bounding box check.
                if (new Rect(elem.AbsoluteOffset + elem.Position, elem.Size).Contains(Event.current.mousePosition)) {
                    // Console.WriteLine("SGUI-IM: Bounding box check passed for " + elem);
                    return true;
                }

            }

            _ElementSemaphore--;
            return false;
        }
        private SGroup _WindowDragging;
        public bool UpdateWindow(SGroup group) {
            if (!group.IsWindow) {
                return false;
            }
            Event e = Event.current;
            SWindowTitleBar bar = group.WindowTitleBar;

            if (e.type == EventType.MouseDown) {
                Rect bounds = (_ClipScopeRects.Count == 0 || !Repainting) ?
                    new Rect(group.AbsoluteOffset + group.Position, bar.Size) :
                    new Rect(Vector2.zero, bar.Size);
                if (bounds.Contains(e.mousePosition)) {
                    bar.Dragging = true;
                    _WindowDragging = group;
                    e.Use();
                    return true;
                }

                bar.Dragging = false;
                return false;
            }

            if (bar.Dragging) {
                if (e.type == EventType.MouseDrag) {
                    group.Position += e.delta;
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
                ++_ElementSemaphore;
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
        private bool _RecreateOperation(int opID = -1) {
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

            if (e.type == EventType.Used && op == EGUIOperation.Draw) {
                // Console.WriteLine("SGUI-IM: Current event used - recreating operation @ NULLRECT");
                bounds = NULLRECT;
            }

            switch (op) {
                case EGUIOperation.Draw:
                    switch (elemGUI) {
                        /*case EGUIComponent.Rect:
                            // Don't draw the rect. Only check for input.
                            if (e.isMouse && bounds.Contains(e.mousePosition)) {
                                e.Use();
                                return true;
                            }
                            break;*/

                        case EGUIComponent.Label:
                            RegisterNextComponent();
                            // Label isn't solid by default.
                            GUI.Label(bounds, (string) data[0]);
                            if (e.isMouse && bounds.Contains(e.mousePosition)) {
                                e.Use();
                                return true;
                            }
                            break;

                        case EGUIComponent.TextField:
                        case EGUIComponent.Button:
                            RegisterNextComponent();
                            // TextField and Button use mouse input by themselves.
                            if (elemGUI == EGUIComponent.TextField) {
                                GUI.TextField(bounds, (string) data[0]);

                            } else {
                                if (GUI.Button(bounds, (string) data[0])) {
                                    _ClickedButtons.Add(CurrentComponentID);
                                    (elem as SButton)?.HandleStatus(true);
                                } else {
                                    _ClickedButtons.Remove(CurrentComponentID);
                                    (elem as SButton)?.HandleStatus(false);
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
                return _ComponentElements.Count - 1;
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

        public bool IsClicked(SElement elem) {
            return IsClicked(GetFirstComponentID(elem));
        }
        public bool IsClicked(int id) {
            return _ClickedButtons.Contains(id);
        }

        public bool IsRelativeContext(SElement elem) {
            if (!Repainting) {
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
            position += elem.Position;

            PreparePosition(elem.Parent, ref position);
        }


        public void Rect(SElement elem, Vector2 position, Vector2 size, Color color) {
            PreparePosition(elem, ref position);
            Rect(new Rect(position, size), color);
        }
        public void Rect(Rect bounds, Color color) {
            if (!Repainting) return;
            Color prevGUIColor = GUI.color;
            GUI.color = color;
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


        public void Text(SElement elem, Vector2 position, string text) {
            PreparePosition(elem, ref position);
            Rect bounds = new Rect(position, elem != null ? elem.Size : Vector2.zero);

            GUI.skin.label.normal.textColor = elem.Foreground;
            // Direct texts in anything with an unfitting size will have some text bounds issues - use SGUILabel or the Rect variant.
            RegisterNextComponentIn(elem);
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.Label, bounds, text);
            GUI.Label(bounds, text);
        }
        public void Text(SElement elem, Rect bounds, string text) {
            Vector2 position = Vector2.zero;
            PreparePosition(elem, ref position);
            bounds.position += position;

            GUI.skin.label.normal.textColor = elem.Foreground;
            RegisterNextComponentIn(elem);
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.Label, bounds, text);
            GUI.Label(bounds, text);
        }

        public bool TextField(SElement elem, Vector2 position, Vector2 size, ref string text) {
            PreparePosition(elem, ref position);

            if (elem != null && Repainting) {
                GUI.skin.textField.normal.textColor = elem.Foreground * 0.8f;
                GUI.skin.textField.active.textColor = elem.Foreground;
                GUI.skin.textField.hover.textColor = elem.Foreground;
                GUI.skin.textField.focused.textColor = elem.Foreground;
                GUI.skin.settings.cursorColor = elem.Foreground;
                GUI.backgroundColor = elem.Background;
            }

            RegisterNextComponentIn(elem);
            return TextField(new Rect(position, size), ref text);
        }
        public bool TextField(Rect bounds, ref string text) {
            RegisterNextComponent();
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.TextField, bounds, text);
            text = GUI.TextField(bounds, text);
            bool focused = IsFocused(CurrentComponentID);

            if (!Font.dynamic && focused && Repainting) {
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

            if (elem != null && Repainting) {
                GUI.skin.button.normal.textColor = elem.Foreground * 0.9f;
                GUI.skin.button.active.textColor = elem.Foreground;
                GUI.skin.button.hover.textColor = elem.Foreground;
                GUI.skin.button.focused.textColor = elem.Foreground;
                GUI.backgroundColor = elem.Background;
            }

            RegisterNextComponentIn(elem);
            return Button(new Rect(position, size), text);
        }
        public bool Button(Rect bounds, string text) {
            RegisterNextComponent();
            GUI.skin.button.fixedHeight = bounds.height;
            RegisterOperation(EGUIOperation.Draw, EGUIComponent.Button, bounds, text);
            GUI.Button(bounds, text); // Input handled elsewhere.
            return _ClickedButtons.Contains(CurrentComponentID);
        }


        public void StartGroup(SGroup group) {
            Vector2 position = group.Position;
            PreparePosition(group, ref position);
            Rect bounds = new Rect(position, group.Size);

            GUI.backgroundColor = _Transparent;
            RegisterNextComponentIn(group);
            if (group.ScrollDirection == SGroup.EDirection.None) {
                RegisterOperation(EGUIOperation.Start, EGUIComponent.Group, bounds);
                GUI.BeginGroup(bounds);
                _ClipScopeTypes.Push(EClipScopeType.Group);

            } else {
                Rect viewBounds = new Rect(Vector2.zero, group.InnerSize);
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

            GUI.EndGroup();
            _ClipScopeTypes.Pop();
            _ClipScopeRects.Pop();
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
                Rect viewBounds = new Rect(Vector2.zero, group.InnerSize);
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
                Vector2 titleSize = MeasureText(title);
                Text(bar, new Rect(group.Border, 0f, titleSize.x, titleSize.y), title);
            }

            // TODO Window header buttons.
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
