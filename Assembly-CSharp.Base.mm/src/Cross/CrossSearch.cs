using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Cross search query.
/// </summary>
public class CrossSearch {

    public readonly static Type t_void = typeof(void);
    public readonly static Type t_FieldInfo = typeof(FieldInfo);
    public readonly static Type t_MethodInfo = typeof(MethodInfo);
    public readonly static Type t_PropertyInfo = typeof(PropertyInfo);

    public Type Type;

    public string Name;
    public string In;
    public bool Private;
    public bool Public = true;
    public bool Static;
    public Type Returns;
    public Type[] Args;
    public CrossSearch[] Context;

    public CrossSearch() {
    }
    public CrossSearch(Type type)
        : this() {
        Type = type;
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
