using ETGGUI.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

public class VectorPropertyInspector : IBasePropertyInspector {

    private readonly static Type t_v2 = typeof(Vector2);
    private readonly static Type t_v3 = typeof(Vector3);
    private readonly static Type t_v4 = typeof(Vector4);

    public object OnGUI(PropertyInfo info, object input) {
        Type inputType = input.GetType();

        if (inputType == t_v2)
            return DrawVector2((Vector2) input, info.Name);
        
        if (inputType == t_v3)
            return DrawVector3((Vector3) input, info.Name);
        
        if (inputType == t_v4)
            return DrawVector4((Vector4) input, info.Name);
        
        return input;
    }

    public static Vector2 DrawVector2(Vector2 input,string name) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(100f));
        for (int i = 0; i < 2; i++) {
            input[i] = FloatPropertyInspector.TextAreaFloat(input[i]);
        }
        GUILayout.EndHorizontal();
        return input;
    }

    public static Vector3 DrawVector3(Vector3 input, string name) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(100f));
        for (int i = 0; i < 3; i++) {
            input[i] = FloatPropertyInspector.TextAreaFloat(input[i]);
        }
        GUILayout.EndHorizontal();
        return input;
    }

    public static Vector4 DrawVector4(Vector4 input, string name) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, GUILayout.Width(100f));
        for (int i = 0; i < 4; i++) {
            input[i] = FloatPropertyInspector.TextAreaFloat(input[i]);
        }
        GUILayout.EndHorizontal();
        return input;
    }

}

