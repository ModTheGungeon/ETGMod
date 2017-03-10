using System;
using UnityEngine;

public static class StringExt {
    public static bool StartsWithInvariant(this string s, string v, bool ignore_case = false) {
        return s.StartsWith(v, StringComparison.InvariantCulture);
    }

    public static bool EndsWithInvariant(this string s, string v, bool ignore_case = false) {
        return s.EndsWith(v, StringComparison.InvariantCulture);
    }

    public static string ToPlatformPath(this string s) {
        return Application.platform == RuntimePlatform.WindowsPlayer ? s : s.Replace('\\', '/');
    }
}