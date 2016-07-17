using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Cross reflection framework.
/// </summary>
public static class Cross {

    private readonly static object[] _EmptyObjectArray = new object[0];

    public static ICrossConfig Config = new CrossGungeonConfig();

    public static Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

    public static Type XType(this string name_) {
        #pragma warning disable 0618
        return name_.XType(PlatformHelper.Current);
        #pragma warning restore 0618
    }
    [Obsolete("Use CrossSearch to directly find members in Config. Use XType() for types.")]
    public static Type XType(this string name_, Platform from) {
        return name_.XType((int) from, (int) PlatformHelper.Current);
    }
    [Obsolete("Use CrossSearch to directly find members in Config. Use XType() for types.")]
    public static Type XType(this string name_, int from, int to) {
        Type type;
        if (TypeMap.TryGetValue(name_, out type)) {
            return type;
        }
        string name = Config.TypeName(name_, from, to);

        foreach (Assembly assembly in Config.Assemblies) {
            if ((type = assembly.GetType(name, false)) != null) {
                break;
            }
        }

        return TypeMap[name_] = type;
    }

    public static object Xs(this MethodInfo method, params object[] args) {
        return method.X(instance: null, args: args);
    }
    public static object X(this MethodInfo method, object instance, params object[] args) {
        return ReflectionHelper.InvokeMethod(method, instance, args);
    }

    public static T Find<T>(this CrossSearch<T> search) where T : MemberInfo {
        return ((CrossSearch) search).Find() as T;
    }
    public static object Find(this CrossSearch search) {
        return Config.Find(search);
    }


}

/// <summary>
/// Cross reflection configuration interface.
/// </summary>
public interface ICrossConfig {
    IEnumerable<Assembly> Assemblies { get; }
    string TypeName(string name, int from, int to);
    object Find(CrossSearch search);
}
