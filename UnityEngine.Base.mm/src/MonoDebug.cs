using System.Reflection;
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using System.Reflection.Emit;
using System.Threading;
using Process = System.Diagnostics.Process;

public static class MonoDebug {
    
    // THIS IS AN UGLY HACK. IT'S VERY UGLY.

    // REPLACE THOSE ADDRESSES WITH THOSE IN THE mono.dll SHIPPING WITH YOUR GAME!
    private static long WINDOWS_mono_debug_init =           0x0000000180074cd4;
    private static long WINDOWS_mono_debug_domain_create =  0x0000000180074ac0;
    private static long WINDOWS_mono_debugger_agent_init =  0x0000000180085e50;
    private static long WINDOWS_runtime_initialized =       0x00000001800d4280;
    private static long WINDOWS_appdomain_load =            0x00000001800d4660;
    private static long WINDOWS_thread_startup =            0x00000001800d5510;
    private static long WINDOWS_assembly_load =             0x00000001800d46d4;
    // REPLACE THOSE ADDRESSES WITH THOSE IN THE libmono.so SHIPPING WITH YOUR GAME!
    private static long LINUX_64_mono_debug_init =          0x000000000012eddc;
    private static long LINUX_64_mono_debugger_agent_init = 0x00000000000aee17;
    private static long LINUX_64_runtime_initialized =      0x000000000002ac00;
    private static long LINUX_64_appdomain_load =           0x000000000002bf50;
    private static long LINUX_64_thread_startup =           0x000000000002a8e0;
    private static long LINUX_64_assembly_load =            0x000000000002a130;

    private static FieldInfo f_mono_assembly = typeof(Assembly).GetField("_mono_assembly", BindingFlags.NonPublic | BindingFlags.Instance);
    private static FieldInfo f_mono_app_domain = typeof(AppDomain).GetField("_mono_app_domain", BindingFlags.NonPublic | BindingFlags.Instance);
    private static MethodInfo m_GetProcessData = typeof(Process).GetMethod("GetProcessData", BindingFlags.NonPublic | BindingFlags.Static);

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

    private static IntPtr _Mono = NULL;
    public static IntPtr GetMono() {
        if (_Mono != NULL) {
            return _Mono;
        }

        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            return GetModuleHandle("mono.dll");

        } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
            IntPtr e = IntPtr.Zero;
            if (IntPtr.Size == 8) {
                _Mono = dlopen("./EtG_Data/Mono/x86_64/libmono.so", RTLD_NOW);
            } else {
                _Mono = dlopen("./EtG_Data/Mono/x86/libmono.so", RTLD_NOW);
            }
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access libmono.so!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return NULL;
            }
            return _Mono;
        }

        return NULL;
    }

    public static IntPtr GetFunction(string name) {
        return GetFunction(GetMono(), name);
    }
    public static IntPtr GetFunction(IntPtr lib, string name) {
        if (lib == NULL) {
            return NULL;
        }

        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            return GetProcAddress(lib, name);

        } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
            IntPtr s, e;

            s = dlsym(lib, "mono_debug_init");
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("MonoDebug can't access " + name + "!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return NULL;
            }
            return s;
        }

        return NULL;
    }

    public static T GetDelegate<T>() where T : class {
        return GetDelegate<T>(typeof(T).Name.Substring(2));
    }
    public static T GetDelegate<T>(string name) where T : class {
        return GetDelegate<T>(GetMono(), name);
    }
    public static T GetDelegate<T>(IntPtr lib, string name) where T : class {
        if (lib == NULL) {
            return null;
        }

        IntPtr s = GetFunction(lib, name);
        if (s == NULL) {
            return null;
        }

        return GetDelegate<T>(s);
    }
    public static T GetDelegateHacky<T>(long s) where T : class {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            s = s - WINDOWS_mono_debug_init + (GetFunction("mono_debug_init").ToInt64());

        } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
            if (IntPtr.Size == 8) {
                s = s - LINUX_64_mono_debug_init + (GetFunction("mono_debug_init").ToInt64());
            }

        }
        return GetDelegate<T>(new IntPtr(s));
    }
    public static T GetDelegate<T>(IntPtr s) where T : class {
        return Marshal.GetDelegateForFunctionPointer(s, typeof(T)) as T;
    }

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
            if (mono_debug_domain_create == pi_mono_debug_domain_create) mono_debug_domain_create = null;
            if ((mono_debug_domain_create = mono_debug_domain_create ?? GetDelegateHacky<d_mono_debug_domain_create>(WINDOWS_mono_debug_domain_create)) == null) return false;

        } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
            Debug.Log("On Linux, Unity hates any access to libmono.so. Creating delegates from pointers.");
            // Unity doesn't want anyone to open libmono.so as it can't open it... but even checks the correct path!
            if (mono_debug_init == pi_mono_debug_init) mono_debug_init = null;
            if ((mono_debug_init = mono_debug_init ?? GetDelegate<d_mono_debug_init>()) == null) return false;
            if (mono_assembly_get_image == pi_mono_assembly_get_image) mono_assembly_get_image = null;
            if ((mono_assembly_get_image = mono_assembly_get_image ?? GetDelegate<d_mono_assembly_get_image>()) == null) return false;
            if (mono_debug_open_image_from_memory == pi_mono_debug_open_image_from_memory) mono_debug_open_image_from_memory = null;
            if ((mono_debug_open_image_from_memory = mono_debug_open_image_from_memory ?? GetDelegate<d_mono_debug_open_image_from_memory>()) == null) return false;
            if (mono_debug_domain_create == pi_mono_debug_domain_create) mono_debug_domain_create = null;
            if ((mono_debug_domain_create = mono_debug_domain_create ?? GetDelegate<d_mono_debug_domain_create>()) == null) return false;
        }
        // TODO Mac OS X?

        // Prepare everything else required: Assembly / image, domain, ...
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
        // Possible: none, server=y, defer=y
        agent = agent ?? "transport=dt_socket,address=127.0.0.1:10000";
        Debug.Log("Telling Mono to listen to following debugger agent: " + agent);

        // Prepare the functions.
        if (Environment.OSVersion.Platform == PlatformID.Unix) {
            Debug.Log("On Linux, Unity hates any access to libmono.so. Creating delegates from pointers.");
            // Unity doesn't want anyone to open libmono.so as it can't open it... but even checks the correct path!
            if ((mono_jit_parse_options = mono_jit_parse_options ?? GetDelegate<d_mono_jit_parse_options>()) == null) return false;
        }
        // TODO Mac OS X?

        mono_jit_parse_options(1, new string[] { "--debugger-agent=" + agent });

        Debug.Log("Done!");
        return true;
    }

    // Those are hidden everywhere... no need to even have a P/Invoke fallbacks here.
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

    // Windows
    [DllImport("kernel32")]
    private static extern uint GetCurrentThreadId();
    // turns out this code is useless...
    // Linux
    [DllImport("pthread")]
    private static extern ulong pthread_self();
    // TODO probably @zatherz

    public static ulong CurrentThreadId {
        get {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                return GetCurrentThreadId();

            } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
                return pthread_self();

            }

            return 0;
        }
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

        int pid = Process.GetCurrentProcess().Id;

        // Prepare the functions.
        Debug.Log("mono_debugger_agent_init and the other functions are not public. Creating delegates from pointer.");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            if ((mono_debugger_agent_init = mono_debugger_agent_init ?? GetDelegateHacky<d_mono_debugger_agent_init>(WINDOWS_mono_debugger_agent_init)) == null) return false;
            if ((runtime_initialized = runtime_initialized ?? GetDelegateHacky<d_runtime_initialized>(WINDOWS_runtime_initialized)) == null) return false;
            if ((appdomain_load = appdomain_load ?? GetDelegateHacky<d_appdomain_load>(WINDOWS_appdomain_load)) == null) return false;
            if ((thread_startup = thread_startup ?? GetDelegateHacky<d_thread_startup>(WINDOWS_thread_startup)) == null) return false;
            if ((assembly_load = assembly_load ?? GetDelegateHacky<d_assembly_load>(WINDOWS_assembly_load)) == null) return false;

        } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
            if ((mono_debugger_agent_init = mono_debugger_agent_init ?? GetDelegateHacky<d_mono_debugger_agent_init>(LINUX_64_mono_debugger_agent_init)) == null) return false;
            if ((runtime_initialized = runtime_initialized ?? GetDelegateHacky<d_runtime_initialized>(LINUX_64_runtime_initialized)) == null) return false;
            if ((appdomain_load = appdomain_load ?? GetDelegateHacky<d_appdomain_load>(LINUX_64_appdomain_load)) == null) return false;
            if ((thread_startup = thread_startup ?? GetDelegateHacky<d_thread_startup>(LINUX_64_thread_startup)) == null) return false;
            if ((assembly_load = assembly_load ?? GetDelegateHacky<d_assembly_load>(LINUX_64_assembly_load)) == null) return false;
        }

        Debug.Log("Running mono_debugger_agent_init and hoping that Mono won't die...");
        mono_debugger_agent_init();

		appdomain_load(NULL, domain, 0);
		assembly_load(NULL, asmThis, 0);

		Assembly[] asmsManaged = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < asmsManaged.Length; i++) {
			Assembly asmManaged = asmsManaged[i];
			IntPtr asm = (IntPtr) f_mono_assembly.GetValue(asmManaged);
			if (asmManaged == asmThisManaged) {
				continue;
			}
			assembly_load(NULL, asm, 0);
		}

        thread_startup(NULL, CurrentThreadId);

		runtime_initialized(NULL);

        Debug.Log("Done!");
		return true;
	}

}

public enum MonoDebugFormat {
    MONO_DEBUG_FORMAT_NONE,
    MONO_DEBUG_FORMAT_MONO,
    MONO_DEBUG_FORMAT_DEBUGGER
}
