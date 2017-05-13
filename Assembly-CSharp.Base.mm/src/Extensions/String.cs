using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

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

    public static string RemovePrefix(this string s, string prefix) {
        return s.EndsWithInvariant(prefix) ? s.Substring(prefix.Length) : s;
    }

    public static string RemoveSuffix(this string s, string suffix) {
        return s.EndsWithInvariant(suffix) ? s.Substring(0, s.Length - suffix.Length) : s;
    }

    public static string ToTitleCaseInvariant(this string s) {
        var text_info = CultureInfo.InvariantCulture.TextInfo;
        return text_info.ToTitleCase(s);
    }

    private static char[] _SeparatorSplitArray = { Path.DirectorySeparatorChar };
    public static string NormalizePath(this string path) {
        var split = path.Split(_SeparatorSplitArray, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<string>();

        for (int i = 0; i < split.Length; i++) {
            var el = split[i];

            if (el == "..") {
                if (list.Count > 0) list.RemoveAt(list.Count - 1);
            } else if (el != ".") {
                list.Add(el);
            }
        }

        return string.Join("/", list.ToArray());
    }

    //used in IDPool.cs/VerifyID
    public static int Count(this string @in, char c)
    {
        int count = 0;
        for (int i = 0; i < @in.Length; i++)
        {
            if (@in[i] == c) count++;
        }
        return count;
    }
}