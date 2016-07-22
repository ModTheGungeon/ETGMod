using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ETGGUI.Hierarchy;
using ETGGUI.Inspector;
using UnityEngine;

public class ETGModInspector : IETGModMenu {

    public static Dictionary<Type, GenericComponentInspector> ComponentInspectorRegistry = new Dictionary<Type, GenericComponentInspector>() {

    };

    public static Dictionary<Type, IBasePropertyInspector> PropertyInspectorRegistry = new Dictionary<Type, IBasePropertyInspector>() {
        { typeof(string), new StringPropertyInspector() }
    };

    public static GenericComponentInspector baseInspector;
    public static GameObject targetObject;

    static Rect WindowRect;
    static Vector2 ScrollPos;

    public void Start() {
        //Init the hierarchy.
        ETGHierarchy.Start();

        WindowRect=new Rect(500, 0, 450, 900);
        baseInspector=new GenericComponentInspector();
    }

    public void Update() {

    }

    public void OnGUI() {
        ETGHierarchy.OnGUI();
        WindowRect=GUI.Window(14, WindowRect, WindowFunction, "Inspector");
    }

    public void WindowFunction(int windowID) {

        ScrollPos=GUILayout.BeginScrollView(ScrollPos);
        if (targetObject != null) {
            foreach (Component c in targetObject.GetComponents<Component>()) {
                if (c == null) {
                    continue;
                }
                GenericComponentInspector inspector;
                if (ComponentInspectorRegistry.TryGetValue(c.GetType(), out inspector)) {
                    inspector.OnGUI(c);
                } else {
                    baseInspector.OnGUI(c);
                }
            }
        }
        GUILayout.EndScrollView();

        GUI.DragWindow();
    }

    public void OnOpen() { }

    public void OnClose() { }

    public void OnDestroy() { }

    public static object DrawProperty(PropertyInfo inf, object input) {
        if (input == null) {
            return null;
        }
        IBasePropertyInspector inspector;
        if (PropertyInspectorRegistry.TryGetValue(input.GetType(), out inspector)) {
            return inspector.OnGUI(inf, input);
        } else {
            GUILayout.Label(inf.Name + ": " + input.ToStringIfNoString());
            return input;
        }

    }
}
