using System.Reflection;
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using System.Reflection.Emit;

public static class MonoDebug {
    private delegate void d_mono_debug_domain_create(IntPtr domain);

    // THIS IS AN UGLY HACK. IT'S VERY UGLY.
    // REPLACE THOSE ADDRESSES WITH THOSE IN THE mono.dll SHIPPING WITH YOUR GAME!
    private static long WINDOWS_f_mono_debug_init = 0x0000000180074cd4;
    private static long WINDOWS_f_mono_debug_domain_create = 0x0000000180074ac0;

    private static FieldInfo f_mono_assembly = typeof(Assembly).GetField("_mono_assembly", BindingFlags.NonPublic | BindingFlags.Instance);
    private static FieldInfo f_mono_app_domain = typeof(AppDomain).GetField("_mono_app_domain", BindingFlags.NonPublic | BindingFlags.Instance);

    [DllImport("mono")]
    private static extern void mono_debug_init(MonoDebugFormat init);
    
    // mono_debug_add_assembly is static, thus hidden from the outside world
    [DllImport("mono")]
    private static extern IntPtr mono_assembly_get_image(IntPtr assembly); // ret MonoImage*; MonoAssembly* assembly
    
    // same as mono_debug_open_image, without returning the MonoDebugHandle*
    [DllImport("mono")]
    private static extern IntPtr mono_debug_open_image_from_memory(IntPtr image, IntPtr raw_contents, int size); // MonoImage* image, const guint8* raw_contents, int size
    
    // seemingly more official wrapper around create_data_table, not exported for Windows though.
    [DllImport("mono", EntryPoint = "mono_debug_domain_create")]
    private static extern void pi_mono_debug_domain_create(IntPtr domain); // MonoDomain* domain
    private static d_mono_debug_domain_create mono_debug_domain_create = pi_mono_debug_domain_create;
    
    // Windows
    [DllImport("kernel32")]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    public static void Force(MonoDebugFormat format = MonoDebugFormat.MONO_DEBUG_FORMAT_MONO) {
        IntPtr NULL = IntPtr.Zero;

        // At this point only mscorlib and UnityEngine are loaded... if called in ClassLibraryInitializer.
        // TODO: mono_debug_open_image_from_memory all non-mscorlib assemblies? Does Mono even survive that?!

        Debug.Log("Forcing Mono into debug mode: " + format);
        // Prepare everything required: Assembly / image, domain and NULL assembly pointers.
        Assembly asmManaged = Assembly.GetCallingAssembly();
        IntPtr asm = (IntPtr) f_mono_assembly.GetValue(asmManaged);
        IntPtr img = mono_assembly_get_image(asm);

        AppDomain domainManaged = AppDomain.CurrentDomain; // Unity Child Domain; Any other app domains?
        IntPtr domain = (IntPtr) f_mono_app_domain.GetValue(domainManaged);

        Debug.Log("Generating dynamic assembly to prevent mono_debug_open_image_from_memory invocation.");
        AssemblyBuilder asmNULLBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NULL"), AssemblyBuilderAccess.Run);
        IntPtr asmNULL = (IntPtr) f_mono_assembly.GetValue(asmNULLBuilder);
        IntPtr imgNULL = mono_assembly_get_image(asmNULL);

        // Precompile some method calls used after mono_debug_init to prevent Mono from crashing while compiling
        Debug.Log("Precompiling mono_debug_open_image_from_memory.");
        mono_debug_open_image_from_memory(imgNULL, NULL, 0);
        Debug.Log("Getting and precompiling mono_debug_domain_create.");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            // The function is not in the export table on Windows... although it should
            // See https://github.com/Unity-Technologies/mono/blob/unity-staging/msvc/mono.def
            // Compare with https://github.com/mono/mono/blob/master/msvc/mono.def, where mono_debug_domain_create is exported
            IntPtr m_mono = GetModuleHandle("mono.dll");
            IntPtr p_mono_debug_init = GetProcAddress(m_mono, "mono_debug_init");
            IntPtr p_mono_debug_domain_create = new IntPtr(WINDOWS_f_mono_debug_domain_create - WINDOWS_f_mono_debug_init + (p_mono_debug_init.ToInt64()));
            mono_debug_domain_create = (d_mono_debug_domain_create) Marshal.GetDelegateForFunctionPointer(p_mono_debug_domain_create, typeof(d_mono_debug_domain_create));
        }
        mono_debug_domain_create(NULL);

        Debug.Log("Invoking mono_debug_init.");
        mono_debug_init(format);
        Debug.Log("Filling debug data as soon as possible.");
        mono_debug_domain_create(domain);
        mono_debug_open_image_from_memory(img, NULL, 0);
        Debug.Log("Done!");
    }

}

public enum MonoDebugFormat {
    MONO_DEBUG_FORMAT_NONE,
    MONO_DEBUG_FORMAT_MONO,
    MONO_DEBUG_FORMAT_DEBUGGER
}
