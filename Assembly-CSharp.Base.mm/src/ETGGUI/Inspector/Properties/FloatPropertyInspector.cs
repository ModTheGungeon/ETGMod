using ETGGUI.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

class FloatPropertyInspector : IBasePropertyInspector {
    public object OnGUI(PropertyInfo info, object input) {
        if (input!=null) {
            string s = input.ToString();

            string edited = GUILayout.TextArea(s);

            if (edited.Length==0)
                edited="0";

            return float.Parse(edited);
        }
        return input;
    }
}

