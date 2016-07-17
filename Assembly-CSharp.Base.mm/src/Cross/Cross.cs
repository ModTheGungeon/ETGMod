using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Cross reflection framework.
/// </summary>
public static class Cross {

    private readonly static object[] EmptyObjectArray = new object[0];

    static Cross() {
        //for mono, get from
        //static extern PlatformID Platform
        PropertyInfo property_platform = typeof(Environment).GetProperty("Platform", BindingFlags.NonPublic | BindingFlags.Static);
        string platID;
        if (property_platform != null) {
            platID = property_platform.GetValue(null, EmptyObjectArray).ToString();
        } else {
            //for .net, use default value
            platID = Environment.OSVersion.Platform.ToString();
        }
        platID = platID.ToLowerInvariant();

        CurrentPlatform = Platform.Unknown;
        if (platID.Contains("win")) {
            CurrentPlatform = Platform.Windows;
        } else if (platID.Contains("mac") || platID.Contains("osx")) {
            CurrentPlatform = Platform.MacOS;
        } else if (platID.Contains("lin") || platID.Contains("unix")) {
            CurrentPlatform = Platform.Linux;
        }
        CurrentPlatform |= (IntPtr.Size == 4 ? Platform.X86 : Platform.X64);

    }

    public static Platform CurrentPlatform { get; private set; } 

    public static ICrossConfig Config = new CrossGungeonConfig();

    public static Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

    public static Type XType(this string name_) {
        #pragma warning disable 0618
        return name_.XType(CurrentPlatform);
        #pragma warning restore 0618
    }
    [Obsolete("Use CrossSearch to directly find members in Config. Use XType() for types.")]
    public static Type XType(this string name_, Platform from) {
        return name_.XType((int) from, (int) CurrentPlatform);
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

/// <summary>
/// Cross reflection platform enum that can be used as "from" and "to".
/// </summary>
public enum Platform : int {
    None = 0,

    // Underlying platform categories
    OS = 1,

    X86 = 0,
    X64 = 2,

    NT = 4,
    Unix = 8,

    // Operating systems (OSes are always "and-equal" to OS)
    Unknown = OS | 16,
    Windows = OS | NT | 32,
    MacOS = OS | Unix | 64,
    Linux = OS | Unix | 128,

    // AMD64 (64bit) variants (always "and-equal" to X64)
    Unknown64 = Unknown | X64,
    Windows64 = Windows | X64,
    MacOS64 = MacOS | X64,
    Linux64 = Linux | X64,
}
