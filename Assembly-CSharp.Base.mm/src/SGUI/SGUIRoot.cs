using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;

namespace SGUI {
    public class SGUIRoot : MonoBehaviour {

        public static SGUIRoot Main;
        public ISGUIBackend Backend;

        public readonly BindingList<SGUIElement> Children = new BindingList<SGUIElement>();
        public readonly List<SGUIElement> AdoptedChildren = new List<SGUIElement>();

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

        public static void Setup() {
            Main = new GameObject("WTFGUI Root").AddComponent<SGUIRoot>();
            Main.Backend = new SGUIIMBackend();
        }

        public void Awake() {
            DontDestroyOnLoad(gameObject);

            Children.ListChanged += HandleChange;

            Foreground = new Color(1f, 1f, 1f, 1f);
            Background = new Color(0f, 0f, 0f, 0.8f);
        }

        public void Start() {
        }

        public void HandleChange(object sender, ListChangedEventArgs e) {
            // TODO Also send event to backend.
            if (e.ListChangedType == ListChangedType.ItemAdded) {
                SGUIElement child = Children[e.NewIndex];
                child.Root = this;
                child.Parent = null;
                if (Backend.UpdateStyleOnRender) {
                    // TODO individual scheduled updates
                    _ScheduledUpdateStyle = true;
                } else {
                    child.UpdateStyle();
                }
            }
            if (e.ListChangedType == ListChangedType.ItemDeleted) {
                // TODO Dispose.
            }
        }

        protected bool _ScheduledUpdateStyle = true;
        public void UpdateStyle() {
            if (Backend.UpdateStyleOnRender && Backend.CurrentRoot == null) {
                _ScheduledUpdateStyle = true;
                return;
            }
            for (int i = 0; i < Children.Count; i++) {
                SGUIElement child = Children[i];
                child.Root = this;
                child.Parent = null;
                child.UpdateStyle();
            }
        }

        public void Update() {
            for (int i = 0; i < Children.Count; i++) {
                SGUIElement child = Children[i];
                child.Root = this;
                child.Parent = null;
                child.Update();
            }

            if (AdoptedChildren.Count != 0) {
                for (int i = 0; i < AdoptedChildren.Count; i++) {
                    SGUIElement child = AdoptedChildren[i];
                    if (child.Parent == null && !Children.Contains(child)) {
                        Children.Add(child);
                    }
                }
                AdoptedChildren.Clear();
            }
        }

        public void OnGUI() {
            if (!Backend.RenderOnGUI) {
                return;
            }

            CheckForResize();

            Backend.StartRender(this);

            if (_ScheduledUpdateStyle) {
                _ScheduledUpdateStyle = false;
                UpdateStyle();
            }

            for (int i = 0; i < Children.Count; i++) {
                SGUIElement child = Children[i];
                child.Root = this;
                child.Parent = null;
                child.Render();
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
