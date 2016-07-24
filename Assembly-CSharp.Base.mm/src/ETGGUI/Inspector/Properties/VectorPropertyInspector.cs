using ETGGUI.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

class VectorPropertyInspector : IBasePropertyInspector {

    static Type v2 = typeof(Vector2);
    static Type v3 = typeof(Vector3);
    static Type v4 = typeof(Vector4);

    public object OnGUI(PropertyInfo info, object input) {
        System.Type inputType = input.GetType();
        if (inputType==v2)
            return drawGUI2((Vector2)input,info.Name);
        else if (inputType==v3)
            return drawGUI3((Vector3)input, info.Name);
        else if (inputType==v4)
            return drawGUI4((Vector4)input, info.Name);
        else
            return input;
    }


    Vector2 drawGUI2(Vector2 input,string name) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name,GUILayout.Width(100));
        for(int i = 0; i < 2; i++) {
            string s = input[i].ToString();

            string edited = GUILayout.TextArea(input[i].ToString());

            if (edited.Length==0)
                edited="0";

            input[i]=float.Parse(edited);
        }
        GUILayout.EndHorizontal();
        return input;
    }
    Vector3 drawGUI3(Vector3 input, string name) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(100));
        for (int i = 0; i<3; i++) {
            string s = input[i].ToString();

            string edited = GUILayout.TextArea(input[i].ToString());

            if (edited.Length==0)
                edited="0";

            input[i]=float.Parse(edited);
        }
        GUILayout.EndHorizontal();
        return input;
    }
    Vector4 drawGUI4(Vector4 input, string name) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(100));
        for (int i = 0; i<4; i++) {
            string s = input[i].ToString();

            string edited = GUILayout.TextArea(input[i].ToString());

            if (edited.Length==0)
                edited="0";

            input[i]=float.Parse(edited);
        }
        GUILayout.EndHorizontal();
        return input;
    }

}

