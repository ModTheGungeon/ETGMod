using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;

namespace SGUI {
    public class SGUIRoot : MonoBehaviour {

        public static SGUIRoot Main;
        public ISGUIBackend Backend;

        public readonly BindingList<SElement> Children = new BindingList<SElement>();
        public readonly List<SElement> AdoptedChildren = new List<SElement>();
        public readonly List<SElement> DisposingChildren = new List<SElement>();

        protected Color _Foreground;
        public Color Foreground {
            get {
                return _Foreground;
            }
            set {
                Color old = _Foreground;
                _Foreground = value;
                if (old != _Foreground && Backend != null) {
                    UpdateStyle();
                }
            }
        }

        protected Color _Background;
        public Color Background {
            get {
                return _Background;
            }
            set {
                Color old = _Background;
                _Background = value;
                if (old != _Background && Backend != null) {
                    UpdateStyle();
                }
            }
        }

        public Vector2 Size {
            get {
                return new Vector2(Screen.width, Screen.height);
            }
        }

        public static Texture2D White;
        /// <summary>
        /// Order of textures depending on backend. For SGUI-IM: Normal, active, hover, focused.
        /// </summary>
        public Texture2D[] TextFieldBackground;

        public static void Setup() {
            Main = new GameObject("WTFGUI Root").AddComponent<SGUIRoot>();
            Main.Backend = new SGUIIMBackend();
        }

        public void Awake() {
            DontDestroyOnLoad(gameObject);

            Children.ListChanged += HandleChange;

            Foreground = new Color(1f, 1f, 1f, 1f);
            Background = new Color(0f, 0f, 0f, 0.65f);

            if (White == null) {
                White = new Texture2D(1, 1);
                White.SetPixel(0, 0, Color.white);
                White.Apply();
            }
            TextFieldBackground = new Texture2D[] { White, White, White, White };
        }

        public void Start() {
            if (!Backend.RenderOnGUI) {
                Backend.Init();
                useGUILayout = false;
            } else {
                _ScheduledBackendInit = true;
                useGUILayout = Backend.RenderOnGUILayout;
            }
        }

        public void HandleChange(object sender, ListChangedEventArgs e) {
            // TODO Also send event to backend.
            if (e.ListChangedType == ListChangedType.ItemAdded) {
                SElement child = Children[e.NewIndex];
                child.Root = this;
                child.Parent = null;
                if (Backend.UpdateStyleOnRender) {
                    // TODO individual scheduled updates
                    _ScheduledUpdateStyle = true;
                } else {
                    child.UpdateStyle();
                }
                int disposeIndex = DisposingChildren.IndexOf(child);
                if (0 <= disposeIndex) {
                    DisposingChildren.RemoveAt(disposeIndex);
                }

            } else if (e.ListChangedType == ListChangedType.ItemDeleted) {
                // TODO Dispose.
                // DisposingChildren.Add(null);
            }
        }

        protected bool _ScheduledUpdateStyle = true;
        public void UpdateStyle() {
            if (Backend.UpdateStyleOnRender && Backend.CurrentRoot == null) {
                _ScheduledUpdateStyle = true;
                return;
            }
            for (int i = 0; i < Children.Count; i++) {
                SElement child = Children[i];
                child.Root = this;
                child.Parent = null;
                child.UpdateStyle();
            }
        }

        public void Update() {
            if (!Backend.Initialized) {
                return;
            }

            for (int i = 0; i < Children.Count; i++) {
                SElement child = Children[i];
                child.Root = this;
                child.Parent = null;
                child.Update();
            }

            if (AdoptedChildren.Count != 0) {
                for (int i = 0; i < AdoptedChildren.Count; i++) {
                    SElement child = AdoptedChildren[i];
                    if (child.Parent == null && !Children.Contains(child)) {
                        // Child had no memory who its parents were, so let's just adopt it.
                        Children.Add(child);
                    } else if (child.Parent != null && !child.Parent.Children.Contains(child)) {
                        // Child remembers about its parents, but parents not about child. Let's force parents to care.
                        child.Parent.Children.Add(child);
                    }
                }
                AdoptedChildren.Clear();
            }

            if (DisposingChildren.Count != 0 && !Backend.RenderOnGUI) {
                for (int i = 0; i < DisposingChildren.Count; i++) {
                    DisposingChildren[i].Dispose();
                }
                DisposingChildren.Clear();
            }
        }

        protected bool _ScheduledBackendInit;
        public void OnGUI() {
            if (!Backend.RenderOnGUI) {
                return;
            }

            if (_ScheduledBackendInit) {
                _ScheduledBackendInit = false;
                Backend.Init();
            }

            CheckForResize();

            Backend.StartRender(this);

            if (_ScheduledUpdateStyle) {
                _ScheduledUpdateStyle = false;
                UpdateStyle();
            }

            Backend.Render();

            if (DisposingChildren.Count != 0) {
                for (int i = 0; i < DisposingChildren.Count; i++) {
                    DisposingChildren[i].Dispose();
                }
                DisposingChildren.Clear();
            }

            Backend.EndRender(this);
        }

        protected int _LastScreenWidth;
        protected int _LastScreenHeight;
        public void CheckForResize() {
            int width = Screen.width;
            int height = Screen.height;
            if (_LastScreenWidth == width && _LastScreenHeight == height) {
                return;
            }
            _LastScreenWidth = width;
            _LastScreenHeight = height;
            UpdateStyle();
        }

    }
}
