using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Cross search query.
/// </summary>
public class CrossSearch {

    public readonly static Type TypeVoid = typeof(void);
    public readonly static Type TypeFieldInfo = typeof(FieldInfo);
    public readonly static Type TypeMethodInfo = typeof(MethodInfo);
    public readonly static Type TypePropertyInfo = typeof(PropertyInfo);

    public Type MemberType;

    public string Name;
    public string In;
    public Type InType;
    public bool Private = false;
    public bool Public = true;
    public bool Static = false;
    public Type Returns = TypeVoid;
    public Type[] Args;
    public CrossSearch[] Context;

    public BindingFlags Flags {
        get {
            return
                (Private ? BindingFlags.NonPublic : Public ? BindingFlags.Public : BindingFlags.Default) |
                (Static ? BindingFlags.Static : BindingFlags.Instance);
        }
    }

    public CrossSearch() {
    }
    public CrossSearch(Type type)
        : this() {
        MemberType = type;
    }

}

/// <summary>
/// Cross search query. Generic helper class.
/// </summary>
public class CrossSearch<T> : CrossSearch where T : MemberInfo {
    public CrossSearch()
        : base(typeof(T)) {
    }
}
