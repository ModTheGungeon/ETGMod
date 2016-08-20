using ETGGUI.Inspector;
using System.Reflection;
using UnityEngine;

public class FloatPropertyInspector : IBasePropertyInspector {
    public object OnGUI(PropertyInfo info, object input) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(info.Name, GUILayout.Width(100f));
        if (input != null) {
            input = TextAreaFloat((float) input);
        }
        GUILayout.EndHorizontal();
        return input;
    }

    public static void TextAreaFloat(ref float f) {
        f = TextAreaFloat(f);
    }
    public static float TextAreaFloat(float f) {
        string s = f.ToString();
        bool zero = false;
#pragma warning disable RECS0018
        if (f == 0f) {
#pragma warning restore RECS0018
            s = "";
            zero = true;
        }
        if (f.IsNegativeZero()) {
            s = "-";
            zero = true;
        }
        s = GUILayout.TextArea(s);
        if (zero) {
            s += "0";
        }
        float result;
        if (!float.TryParse(s, out result)) {
            return f;
        }
        return result;
    }
}

