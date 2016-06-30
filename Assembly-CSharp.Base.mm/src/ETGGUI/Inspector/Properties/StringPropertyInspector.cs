using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace ETGGUI.Inspector {
    public class StringPropertyInspector : IBasePropertyInspector {

        public object OnGUI(PropertyInfo info, object input) {
            string str = (string) input;

            if (str.Contains("\n")) {
                GUILayout.Label(info.Name);
                str = GUILayout.TextArea(str);
            } else {
                GUILayout.BeginHorizontal();
                GUILayout.Label(info.Name);
                str = GUILayout.TextField(str);
                GUILayout.EndHorizontal();
            }

            return str;
        }

    }
}
