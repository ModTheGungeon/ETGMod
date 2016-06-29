using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using src.ETGGUI.Hierarchy;
using src.ETGGUI.Inspector;
using UnityEngine;

public class ETGModInspector : IETGModMenu {

    public static Dictionary<System.Type, GenericComponentInspector> ComponentInspectorRegistry = new Dictionary<Type, GenericComponentInspector>();
    public static Dictionary<System.Type, IBasePropertyInspector> PropertyInspectorRegistry = new Dictionary<Type, IBasePropertyInspector>();
    public static GenericComponentInspector baseInspector;
    public static GameObject targetObject;

    static Rect WindowRect;
    static Vector2 scrollPos;

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

        scrollPos=GUILayout.BeginScrollView(scrollPos);
        if (targetObject) {
            foreach (Component c in targetObject.GetComponents<Component>()) {
                if (ComponentInspectorRegistry.ContainsKey(c.GetType())) {
                    ComponentInspectorRegistry[c.GetType()].OnGUI(c);
                } else {
                    baseInspector.OnGUI(c);
                }
            }
        }
        GUILayout.EndScrollView();

        GUI.DragWindow();
    }

    public void OnDestroy() {

    }

    public static object DrawProperty(PropertyInfo inf, object input) {

        if (PropertyInspectorRegistry.ContainsKey(input.GetType())) {
            return PropertyInspectorRegistry[input.GetType()].OnGUI(inf, input);
        } else {
            return input;
        }

    }
}
