using System.Collections.Generic;
using UnityEngine;

namespace ETGGUI.Hierarchy {
    public static class ETGHierarchy {

        private static Dictionary<Transform, HierarchyComponent> _FullHierarchy = new Dictionary<Transform, HierarchyComponent>();

        private static Vector2 _ScrollPos;
        private static Rect _WindowRect;

        public static void Start() {
            Debug.Log("Compiling transforms into a hierarchy.");
            _WindowRect = new Rect(0f, 0f, 450f, 900f);
        }

        public static void OnGUI() {
            CompileExistingTransforms();
            _WindowRect = GUILayout.Window(15, _WindowRect, WindowFunction, "Hierarchy");
        }

        private static void WindowFunction(int windowID) {
            _ScrollPos = GUILayout.BeginScrollView(_ScrollPos);
            foreach (HierarchyComponent c in _FullHierarchy.Values) {
                if (c.Reference == null) {
                    continue;
                }
                bool isButton = GUILayout.Button(c.Reference.name);
                c.ShowChildren = isButton ? !c.ShowChildren : c.ShowChildren;
                if (isButton)
                    ETGModInspector.targetObject = c.Reference;
                c.OnGUI();
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        /// <summary>
        /// Compiles all transforms currently in the scene into the dictionary.
        /// </summary>
        public static void CompileExistingTransforms() {
            Transform[] allTransforms = Object.FindObjectsOfType<Transform>();

            foreach (Transform t in allTransforms) {
                // If this object is on the root of the scene, we iterate downward through all it's children and add them all.
                if (t.root == t) {
                    if (t == null)
                        continue;

                    HierarchyComponent component;
                    if (!_FullHierarchy.TryGetValue(t, out component))
                        component = _FullHierarchy[t] = new HierarchyComponent(t.gameObject, false);

                    CompileIntoTransform(component);
                }
            }
        }

        private static void CompileIntoTransform(HierarchyComponent comp) {
            for (int i = 0; i < comp.Reference.transform.childCount; i++) {
                if (!comp.Children.ContainsKey(comp.Reference.transform.GetChild(i)))
                    comp.Children[comp.Reference.transform.GetChild(i)] = new HierarchyComponent(comp.Reference.transform.GetChild(i).gameObject, false);
                CompileIntoTransform(comp.Children[comp.Reference.transform.GetChild(i)]);
            }
        }

    }
}
