using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using src.ETGGUI.Hierarchy;
using UnityEngine;

class ETGModInspector: IETGModMenu {

    public static Dictionary<System.Type, GenericInspector> InspectorRegistry = new Dictionary<Type, GenericInspector>();
    public static GameObject targetOjbect;

    static Rect WindowRect;

    public void Start() {

        InspectorRegistry.Add(typeof(Component),new GenericInspector());

        //Init the hierarchy.
        ETGHierarchy.Start();
    }

    public void Update() {

    }

    public void OnGUI() {
        ETGHierarchy.OnGUI();
        WindowRect=GUILayout.Window(14, WindowRect, WindowFunction, "Inspector");
    }

    public void WindowFunction(int windowID) {

        if (targetOjbect) {
            foreach (Component c in targetOjbect.GetComponents<Component>()) {
                if (InspectorRegistry.ContainsKey(c.GetType())) {
                    InspectorRegistry[c.GetType()].OnGUI(c);
                } else {
                    InspectorRegistry[typeof(Component)].OnGUI(c);
                }
            }
        }

        GUI.DragWindow();
    }

    public void OnDestroy() {

    }
}
