using System;
using UnityEngine;

namespace SGUI {
    public class SGUIDFInput : IInputAdapter {

        protected readonly static Vector2 NULLV2 = new Vector2(-1, -1);

        public readonly IInputAdapter Base;
        private ISGUIBackend _Backend;
        public ISGUIBackend Backend {
            get {
                return _Backend ?? SGUIRoot.Main?.Backend;
            }
            set {
                _Backend = value;
            }
        }

        public SGUIDFInput(IInputAdapter @base) {
            Base = @base;
            if (@base == null) {
                throw new NullReferenceException("SDGUIFInput cannot be instantiated without base input!");
            }
        }

        public bool GetKeyDown(KeyCode key) {
            return Base.GetKeyDown(key);
        }

        public bool GetKeyUp(KeyCode key) {
            return Base.GetKeyUp(key);
        }

        public float GetAxis(string axisName) {
            return Base.GetAxis(axisName);
        }

        public Vector2 GetMousePosition() {
            if (Backend.LastMouseEventConsumed) {
                return NULLV2;
            }
            return Base.GetMousePosition();
        }

        public bool GetMouseButton(int button) {
            if (Backend.LastMouseEventConsumed) {
                return false;
            }
            return Base.GetMouseButton(button);
        }

        public bool GetMouseButtonDown(int button) {
            if (Backend.LastMouseEventConsumed) {
                return false;
            }
            return Base.GetMouseButtonDown(button);
        }

        public bool GetMouseButtonUp(int button) {
            if (Backend.LastMouseEventConsumed) {
                return true;
            }
            return Base.GetMouseButtonUp(button);
        }

    }
}
