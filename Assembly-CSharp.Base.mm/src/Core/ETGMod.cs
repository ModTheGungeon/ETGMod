﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using System.Runtime.InteropServices;
using System.Collections;
using SGUI;
using ETGGUI;
using ETGMultiplayer;

/// <summary>
/// Main ETGMod class. Most of the "Mod the Gungeon" logic flows through here.
/// </summary>
public static partial class ETGMod {
    
    public readonly static Version BaseVersion = new Version(0, 3, 2);
    // The following line will be replaced by Travis.
    public readonly static int BaseTravisBuild = 0;
    /// <summary>
    /// Base version profile, used separately from BaseVersion.
    /// A higher profile ID means higher instability ("developerness").
    /// </summary>
    public readonly static Profile BaseProfile =
        #if TRAVIS
        new Profile(2, "travis");
        #elif DEBUG
        new Profile(1, "b3-debug");
        #else
        new Profile(0, "b1"); // no tag
        #endif

    public static string BaseUIVersion {
        get {
            string v = BaseVersion.ToString(3);

            if (BaseTravisBuild != 0) {
                v += "-";
                v += BaseTravisBuild;
            }

            if (!string.IsNullOrEmpty(BaseProfile.Name)) {
                v += "-";
                v += BaseProfile.Name;
            }

            return v;
        }
    }

    public static string GameFolder;
    public static string ModsDirectory;
    public static string ModsListFile;
    public static string RelinkCacheDirectory;
    public static string ResourcesDirectory;

    /// <summary>
    /// Used for CallInEachModule to call a method in each type of mod.
    /// </summary>
    public static List<ETGModule> AllMods = new List<ETGModule>();
    private static List<Type> _ModuleTypes = new List<Type>();
    private static List<Dictionary<string, MethodInfo>> _ModuleMethods = new List<Dictionary<string, MethodInfo>>();

    public static List<ETGModule> GameMods = new List<ETGModule>();
    public static List<ETGBackend> Backends = new List<ETGBackend>();

    internal static bool SaidTheMagicWord = false;
    internal static bool KeptSinging = false;

	/*
    public static string[] LaunchArguments;

    private delegate string[] d_mono_runtime_get_main_args(); //ret MonoArray*
    */

    [Obsolete("Use StartGlobalCoroutine instead.")]
    public static Func<IEnumerator, Coroutine> StartCoroutine;
    public static Func<IEnumerator, Coroutine> StartGlobalCoroutine;
    public static Action<Coroutine> StopGlobalCoroutine;

    private static bool _Init = false;
    public static bool Initialized {
        get {
            return _Init;
        }
    }
    public static void Init() {
        if (_Init) {
            return;
        }
        _Init = true;

		/*
        LaunchArguments = PInvokeHelper.Mono.GetDelegate<d_mono_runtime_get_main_args>()();
        for (int i = 1; i < LaunchArguments.Length; i++) {
            string arg = LaunchArguments[i];
            if (arg == "--no-steam" || arg == "-ns") {
                Platform.DisableSteam = true;
            }
        }
        */

		GameFolder = Path.Combine(Application.dataPath, "..");
		Debug.Log($"ETGMOD INIT: GAMEFOLDER = {GameFolder}");
        ModsDirectory = Path.Combine(GameFolder, "Mods");
        ModsListFile = Path.Combine(ModsDirectory, "mods.txt");
        RelinkCacheDirectory = Path.Combine(ModsDirectory, "RelinkCache");
        ResourcesDirectory = Path.Combine(GameFolder, "Resources");

        Application.logMessageReceived += ETGModDebugLogMenu.Logger;

        SGUIIMBackend.GetFont = (SGUIIMBackend backend) => FontConverter.GetFontFromdfFont((dfFont) patch_MainMenuFoyerController.Instance.VersionLabel.Font, 2);
        GameUIRoot.Instance.Manager.ConsumeMouseEvents = false;
        SGUIRoot.Setup();

        Debug.Log("ETGMod " + BaseUIVersion);
        Assets.HookUnity();
        Objects.HookUnity();
        Assembly.GetCallingAssembly().MapAssets();

        ETGModGUI.Create();
		MultiplayerManager.Create();

        _ScanBackends();
        _LoadMods();

        Assets.Crawl(ResourcesDirectory);

        // Blindly check for all objects for the wanted stuff
        tk2dBaseSprite[] sprites = UnityEngine.Object.FindObjectsOfType<tk2dBaseSprite>();
        for (int i = 0; i < sprites.Length; i++) {
            tk2dBaseSprite sprite = sprites[i];
            if (sprite?.Collection == null) continue;
            if (sprite.Collection.spriteCollectionName == "ItemCollection") {
                Databases.Items.ItemCollection = sprite.Collection;
            }
            if (sprite.Collection.spriteCollectionName == "WeaponCollection") {
                Databases.Items.WeaponCollection = sprite.Collection;
            }
            if (sprite.Collection.spriteCollectionName == "WeaponCollection02") {
                Databases.Items.WeaponCollection02 = sprite.Collection;
            }
        }

        _InitializeAPIs();

        CallInEachModule("Init");
    }

    public static void Start() {
        ETGModGUI.Start();

        TestGunController.Add();
        // BalloonGunController.Add();

        dfInputManager manager = GameUIRoot.Instance.Manager.GetComponent<dfInputManager>();
        manager.Adapter = new SGUIDFInput(manager.Adapter);

        CallInEachModule("Start");
        // Needs to happen late as mods can add their own guns.
        StartGlobalCoroutine(ETGModGUI.ListAllItemsAndGuns());
    }

    private static void _InitializeAPIs() {
        Debug.Log("Initializing APIs");
        Gungeon.Game.Initialize();
    }

    private static void _ScanBackends() {
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
        ETGBackend module = (ETGBackend) type.GetConstructor(_EmptyTypeArray).Invoke(_EmptyObjectArray);
        Debug.Log("Initializing backend " + type.FullName);

        // Metadata is pre-set in backends

        Backends.Add(module);
        AllMods.Add(module);
        _ModuleTypes.Add(type);
        _ModuleMethods.Add(new Dictionary<string, MethodInfo>());
        Debug.Log("Backend " + module.Metadata.Name + " initialized.");
    }

	private static void _CreateModsListFile()
	{
		using (StreamWriter writer = File.CreateText(ModsListFile))
		{
			writer.WriteLine("# Lines beginning with # are comment lines and thus ignored.");
			writer.WriteLine("# Each line here should either be the name of a mod .zip or the path to it.");
			writer.WriteLine("# The order in this .txt is the order in which the mods get loaded.");
			writer.WriteLine("# Delete this file and it will be auto-filled.");
			string[] files = Directory.GetFiles(ModsDirectory);
			for (int i = 0; i < files.Length; i++)
			{
				string file = Path.GetFileName(files[i]);
				if (!file.EndsWithInvariant(".zip"))
				{
					continue;
				}
				writer.WriteLine(file);
			}
			files = Directory.GetDirectories(ModsDirectory);
			for (int i = 0; i < files.Length; i++)
			{
				string file = Path.GetFileName(files[i]);
				if (file == "RelinkCache")
				{
					continue;
				}
				writer.WriteLine(file);
			}
		}
	}

    private static void _LoadMods() {
        Debug.Log("Loading game mods...");

        if (!Directory.Exists(ModsDirectory)) {
            Debug.Log("Mods directory not existing, creating...");
            Directory.CreateDirectory(ModsDirectory);
        }

        if (!File.Exists(ModsListFile)) {
			Debug.Log("Mod list file not existing or invalid, creating...");
			_CreateModsListFile();
        }

        // Pre-run all lines to check if something's invalid
        string[] paths = File.ReadAllLines(ModsListFile);
        for (int i = 0; i < paths.Length; i++) {
            string path = paths[i];
            if (string.IsNullOrEmpty(path)) {
                continue;
            }
            if (path[0] == '#') {
                continue;
            }
            path = path.Trim();
            string absolutePath = Path.Combine(ModsDirectory, path);
            if (!File.Exists(path) && !File.Exists(absolutePath) &&
                !Directory.Exists(path) && !Directory.Exists(absolutePath)) {
                File.Delete(ModsListFile);
				_CreateModsListFile();
            }
        }

        for (int i = 0; i < paths.Length; i++) {
            string path = paths[i];
            if (string.IsNullOrEmpty(path)) {
                continue;
            }
            if (path[0] == '#') {
                continue;
            }
            try {
                InitMod(path.Trim());
            } catch (Exception e) {
                Debug.LogError("ETGMOD could not load mod " + path + "! Check your output log / player log.");
                LogDetailed(e);
            }
        }

    }

    public static void LogDetailed(Exception e, string tag = null) {
        for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
            Console.WriteLine(e_.GetType().FullName + ": " + e_.Message + "\n" + e_.StackTrace);
            if (e_ is ReflectionTypeLoadException) {
                ReflectionTypeLoadException rtle = (ReflectionTypeLoadException) e_;
                for (int i = 0; i < rtle.Types.Length; i++) {
                    Console.WriteLine("ReflectionTypeLoadException.Types[" + i + "]: " + rtle.Types[i]);
                }
                for (int i = 0; i < rtle.LoaderExceptions.Length; i++) {
                    LogDetailed(rtle.LoaderExceptions[i], tag + (tag == null ? "" : ", ") + "rtle:" + i);
                }
            }
            if (e_ is TypeLoadException) {
                Console.WriteLine("TypeLoadException.TypeName: " + ((TypeLoadException) e_).TypeName);
            }
            if (e_ is BadImageFormatException) {
                Console.WriteLine("BadImageFormatException.FileName: " + ((BadImageFormatException) e_).FileName);
            }
        }
    }

    public static void InitMod(string path) {
        if (path.EndsWithInvariant(".zip")) {
            InitModZIP(path);
        } else {
            InitModDir(path);
        }
    }

    public static void InitModZIP(string archive) {
        Debug.Log("Initializing mod ZIP " + archive);

        if (!File.Exists(archive)) {
            // Probably a mod in the mod directory
            archive = Path.Combine(ModsDirectory, archive);
        }

        // Fallback metadata in case none is found
        ETGModuleMetadata metadata = new ETGModuleMetadata() {
            Name = Path.GetFileNameWithoutExtension(archive),
            Version = new Version(0, 0),
            DLL = "mod.dll"
        };
        Assembly asm = null;

        using (ZipFile zip = ZipFile.Read(archive)) {
            // First read the metadata, ...
            Texture2D icon = null;
            foreach (ZipEntry entry in zip.Entries) {
                if (entry.FileName == "metadata.txt") {
                    using (MemoryStream ms = new MemoryStream()) {
                        entry.Extract(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        metadata = ETGModuleMetadata.Parse(archive, "", ms);
                    }
                    continue;
                }
                if (entry.FileName == "icon.png") {
                    icon = new Texture2D(2, 2);
                    icon.name = "icon";
                    using (MemoryStream ms = new MemoryStream()) {
                        entry.Extract(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        icon.LoadImage(ms.GetBuffer());
                    }
                    icon.filterMode = FilterMode.Point;
                    continue;
                }
            }
            if (icon != null) {
                metadata.Icon = icon;
            }

            // ... then check if the mod runs on this profile ...
            if (!metadata.Profile.RunsOn(BaseProfile)) {
               // Debug.LogWarning("http://www.windoof.org/sites/default/files/unsupported.gif");
                return;
            }

            // ... then check if the dependencies are loaded ...
            foreach (ETGModuleMetadata dependency in metadata.Dependencies) {
                if (!DependencyLoaded(dependency)) {
                    Debug.LogWarning("DEPENDENCY " + dependency + " OF " + metadata + " NOT LOADED!");
                    return;
                }
            }

            // ... then add an AssemblyResolve handler for all the .zip-ped libraries
            AppDomain.CurrentDomain.AssemblyResolve += metadata._GenerateModAssemblyResolver();

            // ... then everything else
            foreach (ZipEntry entry in zip.Entries) {
                string entryName = entry.FileName.Replace("\\", "/");
                if (entryName == metadata.DLL) {
                    using (MemoryStream ms = new MemoryStream()) {
                        entry.Extract(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        if (metadata.Prelinked) {
                            asm = Assembly.Load(ms.GetBuffer());
                        } else {
                            asm = metadata.GetRelinkedAssembly(ms);
                        }
                    }
                } else {
                    Assets.AddMapping(entryName, new AssetMetadata(archive, entryName) {
                        AssetType = entry.IsDirectory ? Assets.t_AssetDirectory : null
                    });
                }
            }
        }

        if (asm == null) {
            return;
        }

        asm.MapAssets();

        Type[] types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type type = types[i];
            if (!typeof(ETGModule).IsAssignableFrom(type) || type.IsAbstract) {
                continue;
            }

            ETGModule module = (ETGModule) type.GetConstructor(_EmptyTypeArray).Invoke(_EmptyObjectArray);

            module.Metadata = metadata;

            GameMods.Add(module);
            AllMods.Add(module);
            _ModuleTypes.Add(type);
            _ModuleMethods.Add(new Dictionary<string, MethodInfo>());
        }

        Debug.Log("Mod " + metadata.Name + " initialized.");
    }

    public static void InitModDir(string dir) {
        Debug.Log("Initializing mod directory " + dir);

        if (!Directory.Exists(dir)) {
            // Probably a mod in the mod directory
            dir = Path.Combine(ModsDirectory, dir);
        }

        // Fallback metadata in case none is found
        ETGModuleMetadata metadata = new ETGModuleMetadata() {
            Name = Path.GetFileName(dir),
            Version = new Version(0, 0),
            DLL = "mod.dll"
        };
        Assembly asm = null;

        // First read the metadata, ...
        string metadataPath = Path.Combine(dir, "metadata.txt");
        if (File.Exists(metadataPath)) {
            using (FileStream fs = File.OpenRead(metadataPath)) {
                metadata = ETGModuleMetadata.Parse("", dir, fs);
            }
        }

        // ... then check if the dependencies are loaded ...
        foreach (ETGModuleMetadata dependency in metadata.Dependencies) {
            if (!DependencyLoaded(dependency)) {
                Debug.LogWarning("DEPENDENCY " + dependency + " OF " + metadata + " NOT LOADED!");
                return;
            }
        }

        // ... then add an AssemblyResolve handler for all the .zip-ped libraries
        AppDomain.CurrentDomain.AssemblyResolve += metadata._GenerateModAssemblyResolver();

        // ... then everything else
        if (!File.Exists(metadata.DLL)) {
            return;
        }
        if (metadata.Prelinked) {
            asm = Assembly.LoadFrom(metadata.DLL);
        } else {
            using (FileStream fs = File.OpenRead(metadata.DLL)) {
                asm = metadata.GetRelinkedAssembly(fs);
            }
        }

        asm.MapAssets();
        Assets.Crawl(dir);

        Type[] types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type type = types[i];
            if (!typeof(ETGModule).IsAssignableFrom(type) || type.IsAbstract) {
                continue;
            }

            ETGModule module = (ETGModule) type.GetConstructor(_EmptyTypeArray).Invoke(_EmptyObjectArray);

            module.Metadata = metadata;

            GameMods.Add(module);
            AllMods.Add(module);
            _ModuleTypes.Add(type);
            _ModuleMethods.Add(new Dictionary<string, MethodInfo>());
        }

        Debug.Log("Mod " + metadata.Name + " initialized.");
    }

    private static ResolveEventHandler _GenerateModAssemblyResolver(this ETGModuleMetadata metadata) {
        if (!string.IsNullOrEmpty(metadata.Archive)) {
            return delegate (object sender, ResolveEventArgs args) {
                string asmName = new AssemblyName(args.Name).Name + ".dll";
                using (ZipFile zip = ZipFile.Read(metadata.Archive)) {
                    foreach (ZipEntry entry in zip.Entries) {
                        if (entry.FileName != asmName) {
                            continue;
                        }
                        using (MemoryStream ms = new MemoryStream()) {
                            entry.Extract(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            return Assembly.Load(ms.GetBuffer());
                        }
                    }
                }
                return null;
            };
        }
        if (!string.IsNullOrEmpty(metadata.Directory)) {
            return delegate (object sender, ResolveEventArgs args) {
                string asmPath = Path.Combine(metadata.Directory, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(asmPath)) {
                    return null;
                }
                return Assembly.LoadFrom(asmPath);
            };
        }
        return null;
    }

    public static void Exit() {
        // TODO

        CallInEachModule("Exit");
    }

    /// <summary>
    /// Checks if an dependency is loaded.
    /// Can be used by mods manually to f.e. activate / disable functionality if an API's (not) existing.
    /// Currently only checks the backends.
    /// </summary>
    /// <param name="dependency">Dependency to check for. Name and Version will be checked.</param>
    /// <returns></returns>
    public static bool DependencyLoaded(ETGModuleMetadata dependency) {
        string dependencyName = dependency.Name;
        Version dependencyVersion = dependency.Version;

        if (dependencyName == "Base") {
            if (BaseVersion.Major != dependencyVersion.Major) {
                return false;
            }
            if (BaseVersion.Minor < dependencyVersion.Minor) {
                return false;
            }
            return true;
        }

        foreach (ETGBackend backend in Backends) {
            ETGModuleMetadata metadata = backend.Metadata;
            if (metadata.Name != dependencyName) {
                continue;
            }
            Version version = metadata.Version;
            if (version.Major != dependencyVersion.Major) {
                return false;
            }
            if (version.Minor < dependencyVersion.Minor) {
                return false;
            }
            return true;
        }
        return false;
    }

    [Obsolete("Use RunHook instead!")]
    public static T RunHooks<T>(this MulticastDelegate md, T val) {
        if (md == null) {
            return val;
        }

        object[] args = { val };

        Delegate[] ds = md.GetInvocationList();
        for (int i = 0; i < ds.Length; i++) {
            args[0] = ds[i].DynamicInvoke(args);
        }

        return (T) args[0];
    }

    /// <summary>
    /// Invokes all delegates in the invocation list, passing on the result to the next.
    /// </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    /// <param name="md">The multicast delegate.</param>
    /// <param name="val">The initial value.</param>
    /// <param name="args">Any other arguments that may be passed.</param>
    /// <returns>The result of all delegates, or the initial value if md == null.</returns>
    public static T RunHook<T>(this MulticastDelegate md, T val, params object[] args) {
        if (md == null) {
            return val;
        }

        object[] args_ = new object[args.Length + 1];
        args_[0] = val;
        Array.Copy(args, 0, args_, 1, args.Length);

        Delegate[] ds = md.GetInvocationList();
        for (int i = 0; i < ds.Length; i++) {
            args_[0] = ds[i].DynamicInvoke(args_);
        }

        return (T) args_[0];
    }
        
    /// <summary>
    /// Calls a method in every module.
    /// </summary>
    /// <param name="methodName">Method name of the method to call.</param>
    /// <param name="args">Arguments to pass - null for none.</param>
    public static void CallInEachModule(string methodName, object[] args = null) {
        Type[] argsTypes = null;
        if (args == null) {
            args = _EmptyObjectArray;
            args = _EmptyTypeArray;
        }
        for (int i = 0; i < _ModuleTypes.Count; i++) {
            Dictionary<string, MethodInfo> moduleMethods = _ModuleMethods[i];
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
            method = _ModuleTypes[i].GetMethod(methodName, argsTypes);
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

    // A shared object a day keeps the GC away!
    private readonly static Type[] _EmptyTypeArray = new Type[0];
    private readonly static object[] _EmptyObjectArray = new object[0];

    public class Profile {
        public readonly int Id;
        public readonly string Name;

        public Profile(int id, string name) {
            Id = id;
            Name = name;
        }

        public bool RunsOn(Profile p) {
            return Id <= p.Id;
        }
        public bool Runs() {
            return RunsOn(BaseProfile);
        }

        public override bool Equals(object obj) {
            Profile p = obj as Profile;
            if (p == null) {
                return false;
            }
            return p.Id == Id;
        }

        public override int GetHashCode() {
            return Id;
        }

        public static bool operator <(Profile a, Profile b) {
            if ((a == null) || (b == null)) {
                return false;
            }
            return a.Id < b.Id;
        }
        public static bool operator >(Profile a, Profile b) {
            if ((a == null) || (b == null)) {
                return false;
            }
            return a.Id > b.Id;
        }

        public static bool operator <=(Profile a, Profile b) {
            if ((a == null) || (b == null)) {
                return false;
            }
            return a.Id <= b.Id;
        }
        public static bool operator >=(Profile a, Profile b) {
            if ((a == null) || (b == null)) {
                return false;
            }
            return a.Id >= b.Id;
        }

        public static bool operator ==(Profile a, Profile b) {
            if ((a == null) || (b == null)) {
                return false;
            }
            return a.Id == b.Id;
        }
        public static bool operator !=(Profile a, Profile b) {
            return !(a == b);
        }
    }

    public static void SayTheMagicWord() {
        SaidTheMagicWord = true;
    }

    public static void KeepSinging() {
        KeptSinging = true;
    }
}
