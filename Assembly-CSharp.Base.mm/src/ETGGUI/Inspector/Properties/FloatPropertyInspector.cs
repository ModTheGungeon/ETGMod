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

#pragma warning disable RECS0018
    public static void TextAreaFloat(ref float f) {
        f = TextAreaFloat(f);
    }
    public static float TextAreaFloat(float f) {
        string s = f.ToString();
        if (f == 0f) {
            s = "";
        }
        if (f.IsNegativeZero()) {
            s = "-";
        }
        s = GUILayout.TextArea(s);
        float result;
        if (!float.TryParse(s, out result)) {
            return s.Length != 0 && s[0] == '-' ? -0f : 0f;
        }
        return result;
    }
#pragma warning restore RECS0018
}

