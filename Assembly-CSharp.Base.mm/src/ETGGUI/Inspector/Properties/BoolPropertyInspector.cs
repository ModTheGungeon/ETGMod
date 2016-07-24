using ETGGUI.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

class BoolPropertyInspector : IBasePropertyInspector {
    public object OnGUI(PropertyInfo info, object input) {

        bool casted = (bool)input;

        GUILayout.BeginHorizontal();
        GUILayout.Label(info.Name,GUILayout.Width(100));
        casted=GUILayout.Toggle(casted, "");
        GUILayout.EndHorizontal();

        return casted;
    }
}

