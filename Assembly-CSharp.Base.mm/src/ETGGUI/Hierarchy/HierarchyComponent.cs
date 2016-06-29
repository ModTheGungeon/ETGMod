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

        public void OnGUI() {
            if (!showChildren)
                return;
            GUILayout.BeginArea(new Rect(15,15,15,15));
            GUILayout.Label(reference.name);
            foreach (HierarchyComponent c in children.Values)
                c.OnGUI();
            GUILayout.EndArea();
        }
    }
}
