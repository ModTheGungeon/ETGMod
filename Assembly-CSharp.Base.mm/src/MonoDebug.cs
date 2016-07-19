using System.Reflection;
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using System.Reflection.Emit;

public static class MonoDebug {
    
    // THIS IS AN UGLY HACK. IT'S VERY UGLY.

    // REPLACE THOSE ADDRESSES WITH THOSE IN THE mono.dll SHIPPING WITH YOUR GAME!
    private static long WINDOWS_mono_debug_init = 0x0000000180074cd4;
    private static long WINDOWS_mono_debug_domain_create = 0x0000000180074ac0;
	private static long WINDOWS_mono_debugger_agent_init = 0x00000001800d4ef4;
	// REPLACE THOSE ADDRESSES WITH THOSE IN THE libmono.so SHIPPING WITH YOUR GAME!
	private static long LINUX_32_mono_debug_init = 0x0000000000000000;
	private static long LINUX_32_mono_debugger_agent_init = 0x0000000000000000;
	private static long LINUX_64_mono_debug_init = 0x0000000000000000;
	private static long LINUX_64_mono_debugger_agent_init = 0x0000000000000000;

    private static FieldInfo f_mono_assembly = typeof(Assembly).GetField("_mono_assembly", BindingFlags.NonPublic | BindingFlags.Instance);
    private static FieldInfo f_mono_app_domain = typeof(AppDomain).GetField("_mono_app_domain", BindingFlags.NonPublic | BindingFlags.Instance);

    private static IntPtr NULL = IntPtr.Zero;

    // Windows
    [DllImport("kernel32")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    // Linux
    private const int RTLD_NOW = 2;
    [DllImport("dl")]
    private static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPTStr)] string filename, int flags);
    [DllImport("dl")]
    private static extern IntPtr dlsym(IntPtr handle, [MarshalAs(UnmanagedType.LPTStr)] string symbol);
    [DllImport("dl")]
    private static extern IntPtr dlerror();

    private delegate void d_mono_debug_init(MonoDebugFormat init);
    [DllImport("mono", EntryPoint = "mono_debug_init")]
    private static extern void pi_mono_debug_init(MonoDebugFormat init);
    private static d_mono_debug_init mono_debug_init = pi_mono_debug_init;

    // mono_debug_add_assembly is static, thus hidden from the outside world
    private delegate IntPtr d_mono_assembly_get_image(IntPtr assembly);
    [DllImport("mono", EntryPoint = "mono_assembly_get_image")]
    private static extern IntPtr pi_mono_assembly_get_image(IntPtr assembly); // ret MonoImage*; MonoAssembly* assembly
    private static d_mono_assembly_get_image mono_assembly_get_image = pi_mono_assembly_get_image;

    // Same as mono_debug_open_image, without returning the MonoDebugHandle*
    private delegate IntPtr d_mono_debug_open_image_from_memory(IntPtr image, IntPtr raw_contents, int size);
    [DllImport("mono", EntryPoint = "mono_debug_open_image_from_memory")]
    private static extern IntPtr pi_mono_debug_open_image_from_memory(IntPtr image, IntPtr raw_contents, int size); // MonoImage* image, const guint8* raw_contents, int size
    private static d_mono_debug_open_image_from_memory mono_debug_open_image_from_memory = pi_mono_debug_open_image_from_memory;

    // Seemingly more official wrapper around create_data_table
    private delegate void d_mono_debug_domain_create(IntPtr domain);
    [DllImport("mono", EntryPoint = "mono_debug_domain_create")]
    private static extern void pi_mono_debug_domain_create(IntPtr domain); // MonoDomain* domain
    private static d_mono_debug_domain_create mono_debug_domain_create = pi_mono_debug_domain_create;

    public static bool/*-if-it-returns-at-all*/ Init(MonoDebugFormat format = MonoDebugFormat.MONO_DEBUG_FORMAT_MONO) {
        Debug.Log("MonoDebug.Init!");
        if (Application.isEditor || Type.GetType("Mono.Runtime") == null) {
            return false;
        }
        // At this point only mscorlib and UnityEngine are loaded... if called in ClassLibraryInitializer.
        Debug.Log("Forcing Mono into debug mode: " + format);

        // Prepare the functions.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            Debug.Log("On Windows, mono_debug_domain_create is not public. Creating delegate from pointer.");
            // The function is not in the export table on Windows... although it should
            // See https://github.com/Unity-Technologies/mono/blob/unity-staging/msvc/mono.def
            // Compare with https://github.com/mono/mono/blob/master/msvc/mono.def, where mono_debug_domain_create is exported
            IntPtr m_mono = GetModuleHandle("mono.dll");
            IntPtr p_mono_debug_init = GetProcAddress(m_mono, "mono_debug_init");
            IntPtr p_mono_debug_domain_create = new IntPtr(WINDOWS_mono_debug_domain_create - WINDOWS_mono_debug_init + (p_mono_debug_init.ToInt64()));
            mono_debug_domain_create = (d_mono_debug_domain_create) Marshal.GetDelegateForFunctionPointer(p_mono_debug_domain_create, typeof(d_mono_debug_domain_create));

        } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
            Debug.Log("On Linux, Unity hates any access to libmono.so. Creating delegates from pointers.");
            // Unity doesn't want anyone to open libmono.so as it can't open it... but even checks the correct path!
            IntPtr e = IntPtr.Zero;
            IntPtr libmonoso = IntPtr.Zero;
            if (IntPtr.Size == 8) {
                libmonoso = dlopen("./EtG_Data/Mono/x86_64/libmono.so", RTLD_NOW);
            } else {
                libmonoso = dlopen("./EtG_Data/Mono/x86/libmono.so", RTLD_NOW);
            }
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access libmono.so!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            IntPtr s;

            s = dlsym(libmonoso, "mono_debug_init");
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access mono_debug_init!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_debug_init = (d_mono_debug_init)
                Marshal.GetDelegateForFunctionPointer(s, typeof(d_mono_debug_init));

            s = dlsym(libmonoso, "mono_assembly_get_image");
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access mono_assembly_get_image!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_assembly_get_image = (d_mono_assembly_get_image)
                Marshal.GetDelegateForFunctionPointer(s, typeof(d_mono_assembly_get_image));

            s = dlsym(libmonoso, "mono_debug_open_image_from_memory");
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access mono_debug_open_image_from_memory!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_debug_open_image_from_memory = (d_mono_debug_open_image_from_memory)
                Marshal.GetDelegateForFunctionPointer(s, typeof(d_mono_debug_open_image_from_memory));

            s = dlsym(libmonoso, "mono_debug_domain_create");
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access mono_debug_domain_create!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_debug_domain_create = (d_mono_debug_domain_create)
                Marshal.GetDelegateForFunctionPointer(s, typeof(d_mono_debug_domain_create));
        }
        // TODO Mac OS X?

        // Prepare everything else required: Assembly / image, domain and NULL assembly pointers.
        Assembly asmThisManaged = Assembly.GetCallingAssembly();
        IntPtr asmThis = (IntPtr) f_mono_assembly.GetValue(asmThisManaged);
        IntPtr imgThis = mono_assembly_get_image(asmThis);

        AppDomain domainManaged = AppDomain.CurrentDomain; // Unity Child Domain; Any other app domains?
        IntPtr domain = (IntPtr) f_mono_app_domain.GetValue(domainManaged);

        Debug.Log("Generating dynamic assembly to prevent mono_debug_open_image_from_memory invocation.");
        AssemblyBuilder asmNULLBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NULL"), AssemblyBuilderAccess.Run);
        IntPtr asmNULL = (IntPtr) f_mono_assembly.GetValue(asmNULLBuilder);
        IntPtr imgNULL = mono_assembly_get_image(asmNULL);

        // Precompile some method calls used after mono_debug_init to prevent Mono from crashing while compiling.
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
            Debug.Log(i + ": " + asmManaged.FullName + "; ASM: 0x" + asm.ToString("X8") + "; IMG: " + img.ToString("X8"));
            if (asmManaged == asmThisManaged) {
                Debug.Log("Skipping because already filled.");
            }
            if (asmManaged is AssemblyBuilder) {
                Debug.Log("Skipping because dynamic.");
            }
            if (asmManaged.GetName().Name == "mscorlib") {
                Debug.Log("Skipping because mscorlib.");
            }
            mono_debug_open_image_from_memory(imgThis, NULL, 0);
        }

        Debug.Log("Done!");
        return true;
    }

    // Unity ships with its own implementation where only --debugger-agent= gets parsed. --soft-breakpoints cannot be set.
    private delegate void d_mono_jit_parse_options(int argc, string[] argv);
    [DllImport("mono", EntryPoint = "mono_jit_parse_options")]
    private static extern void pi_mono_jit_parse_options(int argc, string[] argv); // int argc, char* argv[]
    private static d_mono_jit_parse_options mono_jit_parse_options = pi_mono_jit_parse_options;

    public static bool SetupDebuggerAgent(string agent = null) {
        Debug.Log("MonoDebug.SetupDebuggerAgent!");
        if (Application.isEditor || Type.GetType("Mono.Runtime") == null) {
            return false;
        }
        agent = agent ?? "transport=dt_socket,address=127.0.0.1:55555,server=y";
        Debug.Log("Telling Mono to listen to following debugger agent: " + agent);

        // Prepare the functions.
        if (Environment.OSVersion.Platform == PlatformID.Unix) {
            Debug.Log("On Linux, Unity hates any access to libmono.so. Creating delegates from pointers.");
            // Unity doesn't want anyone to open libmono.so as it can't open it... but even checks the correct path!
            IntPtr e = IntPtr.Zero;
            IntPtr libmonoso = IntPtr.Zero;
            if (IntPtr.Size == 8) {
                libmonoso = dlopen("./EtG_Data/Mono/x86_64/libmono.so", RTLD_NOW);
            } else {
                libmonoso = dlopen("./EtG_Data/Mono/x86/libmono.so", RTLD_NOW);
            }
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access libmono.so!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            IntPtr s;

            s = dlsym(libmonoso, "mono_jit_parse_options");
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access mono_jit_parse_options!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_jit_parse_options = (d_mono_jit_parse_options)
                Marshal.GetDelegateForFunctionPointer(s, typeof(d_mono_jit_parse_options));
        }
        // TODO Mac OS X?

        mono_jit_parse_options(1, new string[] { "--debugger-agent=" + agent });

        Debug.Log("Done!");
        return true;
    }

    // It's hidden everywhere... no need to even have a P/Invoke fallback here.
    private delegate void d_mono_debugger_agent_init();
	private static d_mono_debugger_agent_init mono_debugger_agent_init;

	public static bool InitDebuggerAgent() {
		Debug.Log("MonoDebug.InitDebuggerAgent!");
		if (Application.isEditor || Type.GetType("Mono.Runtime") == null) {
			return false;
		}
		if (Environment.OSVersion.Platform == PlatformID.Unix && LINUX_64_mono_debugger_agent_init == 0L) {
            Debug.Log("Linux / Unix currently not supported!");
			return false;
		}
        if (Environment.OSVersion.Platform == PlatformID.MacOSX) {
            Debug.Log("Mac OS X currently not supported!");
            return false;
        }
        Debug.Log("Kick-starting Mono's debugger agent.");

        // Prepare the functions.
        Debug.Log("mono_debugger_agent_init is not public. Creating delegate from pointer.");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            IntPtr m_mono = GetModuleHandle("mono.dll");
            IntPtr p_mono_debug_init = GetProcAddress(m_mono, "mono_debug_init");
            IntPtr p_mono_debugger_agent_init = new IntPtr(WINDOWS_mono_debugger_agent_init - WINDOWS_mono_debug_init + (p_mono_debug_init.ToInt64()));
            mono_debugger_agent_init = (d_mono_debugger_agent_init) Marshal.GetDelegateForFunctionPointer(p_mono_debugger_agent_init, typeof(d_mono_debugger_agent_init));
        }

        //mono_debugger_agent_init(); // UNCOMMENT IF YOU WANT HANG

        // Manually call:

        // debugger_profiler?
        // runtime_initialized
        // appdomain_load
        // thread_startup
        // assembly_load

        Debug.Log("Done!");
		return true;
	}

}

public enum MonoDebugFormat {
    MONO_DEBUG_FORMAT_NONE,
    MONO_DEBUG_FORMAT_MONO,
    MONO_DEBUG_FORMAT_DEBUGGER
}
