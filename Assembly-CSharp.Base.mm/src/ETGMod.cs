using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Main ETGMod class. Most of the "Mod the Gungeon" logic flows through here.
/// </summary>
public static class ETGMod {

    public readonly static Version BaseVersion = new Version(1, 0);

    /// <summary>
    /// Used for CallInEachModule to call a method in each type of mod.
    /// </summary>
    public static List<ETGModule> AllMods = new List<ETGModule>();
    private static Type[] ModuleTypes;
    private static Dictionary<string, MethodInfo>[] ModuleMethods;

    public static List<ETGModule> GameMods = new List<ETGModule>();
    public static List<ETGBackend> Backends = new List<ETGBackend>();

    public static void Start() {
        // TODO Detect all modules and backends

        ModuleTypes = new Type[AllMods.Count];
        ModuleMethods = new Dictionary<string, MethodInfo>[ModuleTypes.Length];
        for (int i = 0; i < ModuleTypes.Length; i++) {
            ETGModule module = AllMods[i];
            ModuleTypes[i] = module.GetType();
            ModuleMethods[i] = new Dictionary<string, MethodInfo>();
        }

        CallInEachModule("Start");
    }

    public static void Update() {
        // TODO

        CallInEachModule("Update");
    }

    public static void Exit() {
        // TODO

        CallInEachModule("Exit");
    }

    /// <summary>
    /// Invokes all delegates in the invocation list, passing on the result to the next.
    /// </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    /// <param name="md">The multicast delegate.</param>
    /// <param name="val">The initial value.</param>
    /// <returns>The result of all delegates, or the initial value if md == null.</returns>
    public static T RunHooks<T>(this MulticastDelegate md, T val) {
        if (md == null) {
            return val;
        }

        Type[] argsTypes = { typeof(T) };
        T[] args = { val };

        Delegate[] ds = md.GetInvocationList();
        for (int i = 0; i < ds.Length; i++) {
            args[0] = (T) ds[i].DynamicInvoke(args);
        }

        return args[0];
    }

    // A shared object a day keeps the GC away!
    private static object[] _object_0 = new object[0];
    private static Type[] _type_0 = new Type[0];
    /// <summary>
    /// Calls a method in every module.
    /// </summary>
    /// <param name="methodName">Method name of the method to call.</param>
    /// <param name="args">Arguments to pass - null for none.</param>
    public static void CallInEachModule(string methodName, object[] args = null) {
        Type[] argsTypes = null;
        if (args == null) {
            args = _object_0;
            args = _type_0;
        }
        for (int i = 0; i < ModuleTypes.Length; i++) {
            Dictionary<string, MethodInfo> moduleMethods = ModuleMethods[i];
            MethodInfo method;
            if (moduleMethods.TryGetValue(methodName, out method)) {
                if (method == null) {
                    continue;
                }
                ReflectionHelper.InvokeMethod(method, AllMods[i], args);
                continue;
            }

            if (argsTypes == null) {
                argsTypes = Type.GetTypeArray(args);
            }
            method = ModuleTypes[i].GetMethod(methodName, argsTypes);
            moduleMethods[methodName] = method;
            if (method == null) {
                continue;
            }
            ReflectionHelper.InvokeMethod(method, AllMods[i], args);
        }
    }

    /// <summary>
    /// Calls a method in every module, passing down the result to the next call.
    /// </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    /// <param name="methodName">Method name of the method to call.</param>
    /// <param name="arg">Argument to pass.</param>
    public static T CallInEachModule<T>(string methodName, T arg) {
        Type[] argsTypes = { typeof(T) };
        T[] args = { arg };
        for (int i = 0; i < AllMods.Count; i++) {
            ETGModule module = AllMods[i];
            //TODO use module method cache
            MethodInfo method = module.GetType().GetMethod(methodName, argsTypes);
            if (method == null) {
                continue;
            }
            args[0] = (T) ReflectionHelper.InvokeMethod(method, module, args);
        }
        return args[0];
    }

}
