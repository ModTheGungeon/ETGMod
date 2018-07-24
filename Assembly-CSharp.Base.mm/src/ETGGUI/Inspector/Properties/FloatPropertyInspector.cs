using ETGGUI.Inspector;
using System.Reflection;
using UnityEngine;

public class FloatPropertyInspector : IBasePropertyInspector {
    public object OnGUI(PropertyInfo info, object input) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(info.Name, GUILayout.Width(100f));
        if (input != null) {
            input = TextFieldFloat((float) input);
        }
        GUILayout.EndHorizontal();
        return input;
    }

#pragma warning disable RECS0018
    public static void TextFieldFloat(ref float f) {
        f = TextFieldFloat(f);
    }
    public static float TextFieldFloat(float f) {
        string s = f.ToString();
        if (f == 0f) {
            s = "";
        }
        if (f.IsNegativeZero()) {
            s = "-";
        }
        s = GUILayout.TextField(s);
        float result;
        if (!float.TryParse(s, out result)) {
            return s.Length != 0 && s[0] == '-' ? -0f : 0f;
        }
        return result;
    }
#pragma warning restore RECS0018
}

