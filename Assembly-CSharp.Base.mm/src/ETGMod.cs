using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;

using Debug = UnityEngine.Debug;

/// <summary>
/// Main ETGMod class. Most of the "Mod the Gungeon" logic flows through here.
/// </summary>
public static class ETGMod {

    private readonly static Type[] a_Type_0 = new Type[0];
    private readonly static object[] a_object_0 = new object[0];

    public readonly static Version BaseVersion = new Version(1, 0);

    public readonly static string GameFolder = ".";
    public readonly static string ModsDirectory = Path.Combine(GameFolder, "Mods");
    public readonly static string ModsListFile = Path.Combine(ModsDirectory, "mods.txt");

    /// <summary>
    /// Used for CallInEachModule to call a method in each type of mod.
    /// </summary>
    public static List<ETGModule> AllMods = new List<ETGModule>();
    private static List<Type> ModuleTypes = new List<Type>();
    private static List<Dictionary<string, MethodInfo>> ModuleMethods = new List<Dictionary<string, MethodInfo>>();

    public static List<ETGModule> GameMods = new List<ETGModule>();
    public static List<ETGBackend> Backends = new List<ETGBackend>();

    private static bool _started;
    public static void Start() {
        _started = true;

        Debug.Log("ETGMod " + BaseVersion);

        ScanBackends();

        LoadMods();

        CallInEachModule("Start");
    }
    
    private static void ScanBackends() {
        Debug.Log("Scanning Assembly-CSharp.dll for backends...");
        Assembly asm = Assembly.GetAssembly(typeof(ETGMod));
        Type[] types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type type = types[i];
            if (typeof(ETGBackend).IsAssignableFrom(type) && !type.IsAbstract) {
                InitBackend(type);
            }
        }
    }
    public static void InitBackend(Type type) {
        ETGBackend module = (ETGBackend) type.GetConstructor(a_Type_0).Invoke(a_object_0);
        Debug.Log("Initializing backend " + type.FullName);

        module.ArchivePath = "";
        // Metadata is pre-set in backends

        Backends.Add(module);
        AllMods.Add(module);
        ModuleTypes.Add(type);
        ModuleMethods.Add(new Dictionary<string, MethodInfo>());
        Debug.Log("Backend " + module.Metadata.Name + " initialized.");
    }

    private static void LoadMods() {
        Debug.Log("Loading game mods...");

        if (!Directory.Exists(ModsDirectory)) {
            Debug.Log("Mods directory not existing, creating...");
            Directory.CreateDirectory(ModsDirectory);
        }

        if (!File.Exists(ModsListFile)) {
            Debug.Log("Mod list file not existing, creating...");
            using (StreamWriter writer = File.CreateText(ModsListFile)) {
                writer.WriteLine("# Lines beginning with # are comment lines and thus ignored.");
                writer.WriteLine("# Each line here should either be the name of a mod .zip or the path to it.");
                writer.WriteLine("# The order in this .txt is the order in which the mods get loaded.");
                writer.WriteLine("# Delete this file and it will be auto-filled.");
                string[] files = Directory.GetFiles(ModsDirectory);
                for (int i = 0; i < files.Length; i++) {
                    string file = Path.GetFileName(files[i]);
                    if (!file.EndsWith(".zip")) {
                        continue;
                    }
                    writer.WriteLine(file);
                }
            }
        }

        string[] archives = File.ReadAllLines(ModsListFile);
        for (int i = 0; i < archives.Length; i++) {
            string archive = archives[i];
            if (string.IsNullOrEmpty(archive)) {
                continue;
            }
            if (archive[0] == '#') {
                continue;
            }
            InitMod(archive.Trim());
        }

    }
    public static void InitMod(string archive) {
        Debug.Log("Initializing mod " + archive);

        if (!File.Exists(archive)) {
            // Probably a mod in the mod directory
            archive = Path.Combine(ModsDirectory, archive);
        }

        // Fallback metadata in case none is found
        ETGModuleMetadata metadata = new ETGModuleMetadata() {
            Name = Path.GetFileNameWithoutExtension(archive),
            Version = new Version(0, 0)
        };
        Assembly asm = null;

        using (ZipFile zip = ZipFile.Read(archive)) {
            foreach (ZipEntry entry in zip.Entries) {
                if (entry.FileName == "metadata.txt") {
                    using (MemoryStream ms = new MemoryStream()) {
                        entry.Extract(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        metadata = ETGModuleMetadata.Parse(ms);
                    }
                } else if (entry.FileName == "mod.dll") {
                    using (MemoryStream ms = new MemoryStream()) {
                        entry.Extract(ms);
                        asm = Assembly.Load(ms.GetBuffer());
                    }
                }
            }
        }

        if (asm == null) {
            return;
        }

        Type[] types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type type = types[i];
            if (!typeof(ETGModule).IsAssignableFrom(type) || type.IsAbstract) {
                continue;
            }

            ETGModule module = (ETGModule) type.GetConstructor(a_Type_0).Invoke(a_object_0);

            module.ArchivePath = archive;
            module.Metadata = metadata;

            GameMods.Add(module);
            AllMods.Add(module);
            ModuleTypes.Add(type);
            ModuleMethods.Add(new Dictionary<string, MethodInfo>());
        }

        Debug.Log("Mod " + metadata.Name + " initialized.");
    }

    public static void Update() {
        // TODO: Find better start point.
        if (!_started) {
            Start();
        }

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
        object[] args = { val };

        Delegate[] ds = md.GetInvocationList();
        for (int i = 0; i < ds.Length; i++) {
            args[0] = ds[i].DynamicInvoke(args);
        }

        return (T) args[0];
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
        for (int i = 0; i < ModuleTypes.Count; i++) {
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
