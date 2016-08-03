using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;

public static partial class ETGMod {
    // ETGMod helper extension methods.



    public static string ToStringIfNoString(this object o) {
        return o == null ? null : o is string ? (string) o : o.ToString();
    }

    public static T GetFirst<T>(this IEnumerable<T> e) {
        foreach (T t in e) {
            return t;
        }
        return default(T);
    }

    public static int IndexOfInvariant(this string s, string a) {
        return s.IndexOf(a, StringComparison.InvariantCulture);
    }
    public static bool StartsWithInvariant(this string s, string a) {
        return s.StartsWith(a, StringComparison.InvariantCulture);
    }
    public static bool EndsWithInvariant(this string s, string a) {
        return s.EndsWith(a, StringComparison.InvariantCulture);
    }

}
