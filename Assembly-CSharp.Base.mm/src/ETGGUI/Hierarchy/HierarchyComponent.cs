using System.Collections.Generic;
using UnityEngine;

namespace ETGGUI.Hierarchy {
    public class HierarchyComponent {
        public GameObject Reference;
        public bool ShowChildren;

        public Dictionary<Transform, HierarchyComponent> Children = new Dictionary<Transform, HierarchyComponent>();

        public HierarchyComponent(GameObject _ref, bool show) {
            Reference = _ref;
            ShowChildren = show;
        }

        public static int SpaceAmount = 0;

        public void OnGUI() {
            if (Reference==null)
                return;

            SpaceAmount++;

            GUILayout.BeginVertical();
            if (ShowChildren) {
                foreach (HierarchyComponent c in Children.Values) {
                    if (c.Reference==null)
                        continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(SpaceAmount * 20f);
                    bool isButton = GUILayout.Button(c.Reference.name);
                    c.ShowChildren=isButton ? !c.ShowChildren : c.ShowChildren;
                    if (isButton)
                        ETGModInspector.targetObject=c.Reference;
                    GUILayout.EndHorizontal();
                    c.OnGUI();
                }
            }
            GUILayout.EndVertical();

            SpaceAmount--;
        }
    }
}
