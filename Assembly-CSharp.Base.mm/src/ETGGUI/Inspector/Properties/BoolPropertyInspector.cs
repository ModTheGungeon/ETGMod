using ETGGUI.Inspector;
using System.Reflection;
using UnityEngine;

public class BoolPropertyInspector : IBasePropertyInspector {
    public object OnGUI(PropertyInfo info, object input) {
        bool casted = (bool) input;

        GUILayout.BeginHorizontal();
        GUILayout.Label(info.Name, GUILayout.Width(100f));
        casted = GUILayout.Toggle(casted, "");
        GUILayout.EndHorizontal();

        return casted;
    }
}

