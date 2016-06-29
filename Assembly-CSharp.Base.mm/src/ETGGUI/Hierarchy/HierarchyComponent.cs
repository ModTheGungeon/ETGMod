using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace src.ETGGUI.Hierarchy {
    class HierarchyComponent {
        public GameObject reference;
        public bool showChildren;

        public Dictionary<Transform, HierarchyComponent> children = new Dictionary<Transform, HierarchyComponent>();

        public HierarchyComponent(GameObject _ref, bool show) {
            reference=_ref;
            showChildren=show;
        }

        public static int spaceAmount = 0;

        public void OnGUI() {

            if (reference==null)
                return;

            spaceAmount++;

            GUILayout.BeginVertical();
            if (showChildren) {
                foreach (HierarchyComponent c in children.Values) {
                    if (c.reference==null)
                        continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(spaceAmount*20);
                    c.showChildren=GUILayout.Button(c.reference.name) ? !c.showChildren : c.showChildren;
                    GUILayout.EndHorizontal();
                    c.OnGUI();
                }
            }
            GUILayout.EndVertical();

            spaceAmount--;
        }
    }
}
