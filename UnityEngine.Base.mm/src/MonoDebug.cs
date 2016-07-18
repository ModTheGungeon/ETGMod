﻿using System.Reflection;
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using System.Reflection.Emit;

public static class MonoDebug {
    
    // THIS IS AN UGLY HACK. IT'S VERY UGLY.

    // REPLACE THOSE ADDRESSES WITH THOSE IN THE mono.dll SHIPPING WITH YOUR GAME!
    private static long WINDOWS_f_mono_debug_init = 0x0000000180074cd4;
    private static long WINDOWS_f_mono_debug_domain_create = 0x0000000180074ac0;

    private static FieldInfo f_mono_assembly = typeof(Assembly).GetField("_mono_assembly", BindingFlags.NonPublic | BindingFlags.Instance);
    private static FieldInfo f_mono_app_domain = typeof(AppDomain).GetField("_mono_app_domain", BindingFlags.NonPublic | BindingFlags.Instance);

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

    // same as mono_debug_open_image, without returning the MonoDebugHandle*
    private delegate IntPtr d_mono_debug_open_image_from_memory(IntPtr image, IntPtr raw_contents, int size);
    [DllImport("mono", EntryPoint = "mono_debug_open_image_from_memory")]
    private static extern IntPtr pi_mono_debug_open_image_from_memory(IntPtr image, IntPtr raw_contents, int size); // MonoImage* image, const guint8* raw_contents, int size
    private static d_mono_debug_open_image_from_memory mono_debug_open_image_from_memory = pi_mono_debug_open_image_from_memory;

    // seemingly more official wrapper around create_data_table
    private delegate void d_mono_debug_domain_create(IntPtr domain);
    [DllImport("mono", EntryPoint = "mono_debug_domain_create")]
    private static extern void pi_mono_debug_domain_create(IntPtr domain); // MonoDomain* domain
    private static d_mono_debug_domain_create mono_debug_domain_create = pi_mono_debug_domain_create;


    public static bool/*-if-it-returns-at-all*/ Force(MonoDebugFormat format = MonoDebugFormat.MONO_DEBUG_FORMAT_MONO) {
        if (Environment.OSVersion.Platform == PlatformID.MacOSX) {
            // Give up. TODO Does Mac OSX actually complain?
            return false;
        }

        Action<object> l = Debug.Log;
        if (Environment.OSVersion.Platform == PlatformID.Unix) {
            l = Console.WriteLine;
        }

        IntPtr NULL = IntPtr.Zero;

        // At this point only mscorlib and UnityEngine are loaded... if called in ClassLibraryInitializer.
        // TODO: mono_debug_open_image_from_memory all non-mscorlib assemblies? Does Mono even survive that?!

        l("MonoDebug!");
        l("Forcing Mono into debug mode: " + format);

        // Prepare the functions.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            l("On Windows, mono_debug_domain_create is not public. Creating delegate from pointer.");
            // The function is not in the export table on Windows... although it should
            // See https://github.com/Unity-Technologies/mono/blob/unity-staging/msvc/mono.def
            // Compare with https://github.com/mono/mono/blob/master/msvc/mono.def, where mono_debug_domain_create is exported
            IntPtr m_mono = GetModuleHandle("mono.dll");
            IntPtr p_mono_debug_init = GetProcAddress(m_mono, "mono_debug_init");
            IntPtr p_mono_debug_domain_create = new IntPtr(WINDOWS_f_mono_debug_domain_create - WINDOWS_f_mono_debug_init + (p_mono_debug_init.ToInt64()));
            mono_debug_domain_create = (d_mono_debug_domain_create) Marshal.GetDelegateForFunctionPointer(p_mono_debug_domain_create, typeof(d_mono_debug_domain_create));

        } else if (Environment.OSVersion.Platform == PlatformID.Unix) {
            l("On Linux, Unity hates any access to libmono.so. Creating delegates from pointers.");
            // Unity doesn't want anyone to open libmono.so as it can't open it... but even checks the correct path!
            IntPtr e = IntPtr.Zero;
            IntPtr libmonoso = IntPtr.Zero;
            if (IntPtr.Size == 8) {
                dlopen("./EtG_Data/Mono/x86_64/libmono.so", RTLD_NOW);
            } else {
                dlopen("./EtG_Data/Mono/x86/libmono.so", RTLD_NOW);
            }
            if ((e = dlerror()) != IntPtr.Zero) {
                l("MonoDebug can't access libmono.so!");
                l("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_debug_init = (d_mono_debug_init)
                Marshal.GetDelegateForFunctionPointer(dlsym(libmonoso, "mono_debug_init"), typeof(d_mono_debug_init));
            if ((e = dlerror()) != IntPtr.Zero) {
                l("MonoDebug can't access mono_debug_init!");
                l("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_assembly_get_image = (d_mono_assembly_get_image)
                Marshal.GetDelegateForFunctionPointer(dlsym(libmonoso, "mono_assembly_get_image"), typeof(d_mono_assembly_get_image));
            if ((e = dlerror()) != IntPtr.Zero) {
                l("MonoDebug can't access mono_assembly_get_image!");
                l("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_debug_open_image_from_memory = (d_mono_debug_open_image_from_memory)
                Marshal.GetDelegateForFunctionPointer(dlsym(libmonoso, "mono_debug_open_image_from_memory"), typeof(d_mono_debug_open_image_from_memory));
            if ((e = dlerror()) != IntPtr.Zero) {
                l("MonoDebug can't access mono_debug_open_image_from_memory!");
                l("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
            mono_debug_domain_create = (d_mono_debug_domain_create)
                Marshal.GetDelegateForFunctionPointer(dlsym(libmonoso, "mono_debug_domain_create"), typeof(d_mono_debug_domain_create));
            if ((e = dlerror()) != IntPtr.Zero) {
                l("MonoDebug can't access mono_debug_domain_create!");
                l("dlerror: " + Marshal.PtrToStringAnsi(e));
                return false;
            }
        }

        // Prepare everything else required: Assembly / image, domain and NULL assembly pointers.
        Assembly asmManaged = Assembly.GetCallingAssembly();
        IntPtr asm = (IntPtr) f_mono_assembly.GetValue(asmManaged);
        IntPtr img = mono_assembly_get_image(asm);

        AppDomain domainManaged = AppDomain.CurrentDomain; // Unity Child Domain; Any other app domains?
        IntPtr domain = (IntPtr) f_mono_app_domain.GetValue(domainManaged);

        l("Generating dynamic assembly to prevent mono_debug_open_image_from_memory invocation.");
        AssemblyBuilder asmNULLBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NULL"), AssemblyBuilderAccess.Run);
        IntPtr asmNULL = (IntPtr) f_mono_assembly.GetValue(asmNULLBuilder);
        IntPtr imgNULL = mono_assembly_get_image(asmNULL);

        // Precompile some method calls used after mono_debug_init to prevent Mono from crashing while compiling.
        l("Precompiling mono_debug_open_image_from_memory.");
        mono_debug_open_image_from_memory(imgNULL, NULL, 0);
        l("Precompiling mono_debug_domain_create.");
        mono_debug_domain_create(NULL);

        l("Invoking mono_debug_init.");
        mono_debug_init(format);
        l("Filling debug data as soon as possible.");
        mono_debug_domain_create(domain);
        mono_debug_open_image_from_memory(img, NULL, 0);
        l("Done!");
        return true;
    }

}

public enum MonoDebugFormat {
    MONO_DEBUG_FORMAT_NONE,
    MONO_DEBUG_FORMAT_MONO,
    MONO_DEBUG_FORMAT_DEBUGGER
}