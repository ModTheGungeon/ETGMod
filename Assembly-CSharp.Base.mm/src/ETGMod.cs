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

        CallInEachModule("Start");
    }

    public static void Exit() {
        // TODO

        CallInEachModule("Exit");
    }

    // A shared object a day keeps the GC away!
    private static object[] _object_0 = new object[0];
    private static Type[] _type_0 = new Type[0];
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

    public static T CallInEachModule<T>(string methodName, T arg) {
        Type[] argsTypes = { typeof(T) };
        object[] args = { arg };
        for (int i = 0; i < AllMods.Count; i++) {
            ETGModule module = AllMods[i];
            //TODO use module method cache
            MethodInfo method = module.GetType().GetMethod(methodName, argsTypes);
            if (method == null) {
                continue;
            }
            arg = (T) ReflectionHelper.InvokeMethod(method, module, args);
        }
        return arg;
    }

}
