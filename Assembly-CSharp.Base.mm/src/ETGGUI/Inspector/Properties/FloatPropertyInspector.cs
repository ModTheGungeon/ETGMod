using ETGGUI.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

class FloatPropertyInspector : IBasePropertyInspector {
    public object OnGUI(PropertyInfo info, object input) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(info.Name,GUILayout.Width(100));
        if (input!=null) {
            string s = input.ToString();

            string edited = GUILayout.TextArea(s);

            if (edited.Length==0)
                edited="0";

            GUILayout.EndHorizontal();
            return float.Parse(edited);
        }
        GUILayout.EndHorizontal();
        return input;
    }
}

