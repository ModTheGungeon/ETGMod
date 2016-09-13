using System.Reflection;
using System;
using UnityEngine;
using System.Reflection.Emit;

public static class MonoDebug {
    
    private readonly static IntPtr NULL = IntPtr.Zero;

    private static Assembly _NULLASM;
    private static Assembly NULLASM {
        get {
            if (_NULLASM == null) {
                _NULLASM = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NULL"), AssemblyBuilderAccess.Run);
            }
            return _NULLASM;
        }
    }

    #region MonoDebug reflection
    private static readonly FieldInfo f_mono_assembly = typeof(Assembly).GetField("_mono_assembly", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo f_mono_app_domain = typeof(AppDomain).GetField("_mono_app_domain", BindingFlags.NonPublic | BindingFlags.Instance);
    #endregion

    #region MonoDebug Init
    #region MonoDebug Init RVAs
    private const long WINDOWS_mono_debug_domain_create = 0x74B08;
    #endregion

    #region MonoDebug Init definitions
    private delegate void d_mono_debug_init(MonoDebugFormat init);
    private static d_mono_debug_init mono_debug_init;

    // mono_debug_add_assembly is static, thus hidden from the outside world
    private delegate IntPtr d_mono_assembly_get_image(IntPtr assembly); // ret MonoImage*; MonoAssembly* assembly
    private static d_mono_assembly_get_image mono_assembly_get_image;

    // Same as mono_debug_open_image, without returning the MonoDebugHandle*
    private delegate IntPtr d_mono_debug_open_image_from_memory(IntPtr image, IntPtr raw_contents, int size); // MonoImage* image, const guint8* raw_contents, int size
    private static d_mono_debug_open_image_from_memory mono_debug_open_image_from_memory;

    // Seemingly more official wrapper around create_data_table
    private delegate void d_mono_debug_domain_create(IntPtr domain); // MonoDomain* domain
    private static d_mono_debug_domain_create mono_debug_domain_create;
    #endregion

    public static bool/*-if-it-returns-at-all*/ Init(MonoDebugFormat format = MonoDebugFormat.MONO_DEBUG_FORMAT_MONO) {
        Debug.Log("MonoDebug.Init!");
        if (Application.isEditor || Type.GetType("Mono.Runtime") == null) {
            return false;
        }
        // At this point only mscorlib and UnityEngine are loaded... if called in ClassLibraryInitializer.
        Debug.Log("Forcing Mono into debug mode: " + format);


        // Prepare native calls.
        mono_debug_init                     = mono_debug_init                   ?? PInvokeHelper.Mono.GetDelegate<d_mono_debug_init>();
        mono_assembly_get_image             = mono_assembly_get_image           ?? PInvokeHelper.Mono.GetDelegate<d_mono_assembly_get_image>();
        mono_debug_open_image_from_memory   = mono_debug_open_image_from_memory ?? PInvokeHelper.Mono.GetDelegate<d_mono_debug_open_image_from_memory>();
        mono_debug_domain_create            = mono_debug_domain_create          ?? PInvokeHelper.Mono.GetDelegate<d_mono_debug_domain_create>()
                                                                                ?? PInvokeHelper.Mono.GetDelegateAtRVA<d_mono_debug_domain_create>(WINDOWS_mono_debug_domain_create);


        // Prepare everything else required: Assembly / image, domain, ...
        Assembly asmThisManaged = Assembly.GetCallingAssembly();
        IntPtr asmThis = (IntPtr) f_mono_assembly.GetValue(asmThisManaged);
        IntPtr imgThis = mono_assembly_get_image(asmThis);

        AppDomain domainManaged = AppDomain.CurrentDomain; // Unity Child Domain; Any other app domains?
        IntPtr domain = (IntPtr) f_mono_app_domain.GetValue(domainManaged);

        Debug.Log("Generating dynamic assembly to prevent mono_debug_open_image_from_memory invocation.");
        IntPtr asmNULL = (IntPtr) f_mono_assembly.GetValue(NULLASM);
        IntPtr imgNULL = mono_assembly_get_image(asmNULL);


        // Precompile some calls used after mono_debug_init to prevent Mono from crashing while compiling.
        Debug.Log("Precompiling mono_debug_open_image_from_memory.");
        mono_debug_open_image_from_memory(imgNULL, NULL, 0);
        Debug.Log("Precompiling mono_debug_domain_create.");
        mono_debug_domain_create(NULL);


        // Entering highly critical part
        Debug.Log("Invoking mono_debug_init.");
        mono_debug_init(format);
        Debug.Log("Filling debug data for main domain and main assembly.");
        mono_debug_domain_create(domain);
        mono_debug_open_image_from_memory(imgThis, NULL, 0);
        // Leaving highly critical part


        Debug.Log("Filling debug data for other assemblies.");
        Assembly[] asmsManaged = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asmsManaged.Length; i++) {
            Assembly asmManaged = asmsManaged[i];
            IntPtr asm = (IntPtr) f_mono_assembly.GetValue(asmManaged);
            IntPtr img = mono_assembly_get_image(asm);
            Debug.Log(i + ": " + asmManaged.FullName + "; ASM: 0x" + asm.ToString("X8") + "; IMG: 0x" + img.ToString("X8"));
            if (asmManaged == asmThisManaged) {
                Debug.Log("Skipping because already filled.");
                continue;
            }
            if (asmManaged is AssemblyBuilder) {
                Debug.Log("Skipping because dynamic.");
                continue;
            }
            if (asmManaged.GetName().Name == "mscorlib") {
                Debug.Log("Skipping because mscorlib.");
                continue;
            }
            mono_debug_open_image_from_memory(imgThis, NULL, 0);
        }


        Debug.Log("Done!");
        return true;
    }
    #endregion

    #region MonoDebug Agent
    #region MonoDebug Agent RVAs
    private const long WINDOWS_mono_debugger_agent_init = 0xD5E50;
    private const long WINDOWS_runtime_initialized = 0xD4280;
    private const long WINDOWS_appdomain_load = 0xD4660;
    private const long WINDOWS_thread_startup = 0xD42C8;
    private const long WINDOWS_assembly_load = 0xD46D4;
    #endregion

    #region MonoDebug Agent Setup definitions
    // Unity ships with its own implementation where only --debugger-agent= gets parsed. --soft-breakpoints cannot be set.
    private delegate void d_mono_jit_parse_options(int argc, string[] argv); // int argc, char* argv[]
    private static d_mono_jit_parse_options mono_jit_parse_options;
    #endregion
    #region MonoDebug Agent Init definitions
    private delegate void d_mono_debugger_agent_init();
    private static d_mono_debugger_agent_init mono_debugger_agent_init;
    private delegate void d_runtime_initialized(IntPtr prof); // MonoProfiler* prof
    private static d_runtime_initialized runtime_initialized;
    private delegate void d_appdomain_load(IntPtr prof, IntPtr domain, int result); // MonoProfiler* prof, MonoDomain* domain, int result
    private static d_appdomain_load appdomain_load;
    private delegate void d_thread_startup(IntPtr prof, ulong tid); // MonoProfiler* prof, gsize tid
    private static d_thread_startup thread_startup;
    private delegate void d_assembly_load(IntPtr prof, IntPtr assembly, int result); //MonoProfiler* prof, MonoAssembly* assembly, int result
    private static d_assembly_load assembly_load;
    #endregion

    public static bool SetupDebuggerAgent(string agent = null) {
        Debug.Log("MonoDebug.SetupDebuggerAgent!");
        if (Application.isEditor || Type.GetType("Mono.Runtime") == null) {
            return false;
        }
        // Possible: none, server=y, defer=y
        agent = agent ?? "transport=dt_socket,address=127.0.0.1:10000";
        Debug.Log("Telling Mono to listen to following debugger agent: " + agent);

        // Prepare the functions.
        mono_jit_parse_options = mono_jit_parse_options ?? PInvokeHelper.Mono.GetDelegate<d_mono_jit_parse_options>();
        mono_jit_parse_options(1, new string[] { "--debugger-agent=" + agent });

        Debug.Log("Done!");
        return true;
    }

    public static bool InitDebuggerAgent() {
        Debug.Log("MonoDebug.InitDebuggerAgent!");
        if (Application.isEditor || Type.GetType("Mono.Runtime") == null) {
            return false;
        }

        if (IntPtr.Size == 4) {
            Debug.Log("x86 not supported!");
            return false;
        }
        if (Environment.OSVersion.Platform == PlatformID.MacOSX) {
            Debug.Log("Mac OS X not supported!");
            return false;
        }
        Debug.Log("Kick-starting Mono's debugger agent.");

        // Prepare everything else required: Assembly, domain, ...
        Assembly asmThisManaged = Assembly.GetCallingAssembly();
        IntPtr asmThis = (IntPtr) f_mono_assembly.GetValue(asmThisManaged);

        AppDomain domainManaged = AppDomain.CurrentDomain; // Unity Child Domain; Any other app domains?
        IntPtr domain = (IntPtr) f_mono_app_domain.GetValue(domainManaged);

        // Prepare the functions.
        Debug.Log("mono_debugger_agent_init and the other functions are not public. Creating delegates from pointer.");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            mono_debugger_agent_init    = mono_debugger_agent_init  ?? PInvokeHelper.Mono.GetDelegateAtRVA<d_mono_debugger_agent_init>(WINDOWS_mono_debugger_agent_init);
            runtime_initialized         = runtime_initialized       ?? PInvokeHelper.Mono.GetDelegateAtRVA<d_runtime_initialized>(WINDOWS_runtime_initialized);
            appdomain_load              = appdomain_load            ?? PInvokeHelper.Mono.GetDelegateAtRVA<d_appdomain_load>(WINDOWS_appdomain_load);
            thread_startup              = thread_startup            ?? PInvokeHelper.Mono.GetDelegateAtRVA<d_thread_startup>(WINDOWS_thread_startup);
            assembly_load               = assembly_load             ?? PInvokeHelper.Mono.GetDelegateAtRVA<d_assembly_load>(WINDOWS_assembly_load);
        }


        Debug.Log("Running mono_debugger_agent_init and hoping that Mono won't die...");
        mono_debugger_agent_init();

        appdomain_load(NULL, domain, 0);
        assembly_load(NULL, asmThis, 0);

        Assembly[] asmsManaged = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asmsManaged.Length; i++) {
            Assembly asmManaged = asmsManaged[i];
            IntPtr asm = (IntPtr) f_mono_assembly.GetValue(asmManaged);
            if (asmManaged == asmThisManaged ||
                asmManaged is AssemblyBuilder ||
                asmManaged.GetName().Name == "mscorlib") {
                continue;
            }
            assembly_load(NULL, asm, 0);
        }

        Debug.Log("thread_startup " + PInvokeHelper.CurrentThreadId);
        thread_startup(NULL, PInvokeHelper.CurrentThreadId);
        Debug.Log("aaand...");
        runtime_initialized(NULL);

        Debug.Log("Done!");
        return true;
    }

    #endregion

}

public enum MonoDebugFormat {
    MONO_DEBUG_FORMAT_NONE,
    MONO_DEBUG_FORMAT_MONO,
    MONO_DEBUG_FORMAT_DEBUGGER
}
