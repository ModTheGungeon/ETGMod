using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;

public static partial class ETGMod {
    // ETGMod helper extension methods.

    public static string ToStringIfNoString(this object o) {
        return o==null ? null : o is string ? (string)o : o.ToString();
    }

}
