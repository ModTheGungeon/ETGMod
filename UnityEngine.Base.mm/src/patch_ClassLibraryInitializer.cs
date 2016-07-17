#pragma warning disable 0626
#pragma warning disable 0649

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngineInternal;
using MonoMod;
using System.Reflection;
using System.Runtime.InteropServices;
using System;
using System.Reflection.Emit;

namespace UnityEngine {
    internal delegate void d_mono_debug_domain_create(IntPtr domain);
    internal static class patch_ClassLibraryInitializer {

        internal static FieldInfo f_mono_assembly = typeof(Assembly).GetField("_mono_assembly", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo f_mono_app_domain = typeof(AppDomain).GetField("_mono_app_domain", BindingFlags.NonPublic | BindingFlags.Instance);

        [DllImport("mono")]
        private static extern void mono_debug_init(MonoDebugFormat init);

        // static, thus hidden from the outside world
        /*
        [DllImport("mono")]
        private static extern void mono_debug_add_assembly(Assembly assembly, IntPtr user_data); // MonoAssembly* assembly, gpointer user_data
        */

        [DllImport("mono")]
        private static extern IntPtr mono_assembly_get_image(IntPtr assembly); // ret MonoImage*; MonoAssembly* assembly

        // same as mono_debug_open_image, without returning the MonoDebugHandle*
        [DllImport("mono")]
        private static extern IntPtr mono_debug_open_image_from_memory(IntPtr image, IntPtr raw_contents, int size); // MonoImage* image, const guint8* raw_contents, int size

        // more official wrapper around create_data_table, symbol hidden in EtG's mono... on Windows only.
        [DllImport("mono")]
        private static extern void mono_debug_domain_create(IntPtr domain); // MonoDomain* domain
        private static d_mono_debug_domain_create d_mono_debug_domain_create;

        // Windows
        [DllImport("kernel32")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        private static long WINDOWS_f_mono_debug_init = 0x0000000180074cd4;
        private static long WINDOWS_f_mono_debug_domain_create = 0x0000000180074ac0;

        private static extern void orig_Init();
        // Seemingly first piece of managed code running
        private static void Init() {
            IntPtr NULL = IntPtr.Zero;

            Debug.Log("Forcing Mono into debug mode.");
            Assembly asmUEManaged = Assembly.GetCallingAssembly();
            IntPtr asmUE = (IntPtr) f_mono_assembly.GetValue(asmUEManaged);
            IntPtr imgUE = mono_assembly_get_image(asmUE);
            AppDomain domainUCDManaged = AppDomain.CurrentDomain; // Unity Child Domain; Any other app domains?
            IntPtr domainUCD = (IntPtr) f_mono_app_domain.GetValue(domainUCDManaged);


            Debug.Log("Generating dynamic assembly to prevent mono_debug_open_image_from_memory invocation.");
            AssemblyBuilder asmNULLBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NULL"), AssemblyBuilderAccess.Run);
            IntPtr asmNULL = (IntPtr) f_mono_assembly.GetValue(asmNULLBuilder);
            IntPtr imgNULL = mono_assembly_get_image(asmNULL);


            Debug.Log("Precompiling mono_debug_open_image_from_memory.");
            mono_debug_open_image_from_memory(imgNULL, NULL, 0);


            Debug.Log("Getting and precompiling mono_debug_domain_create.");

            if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
                d_mono_debug_domain_create = mono_debug_domain_create;
            } else {
                // WINDOWS SUUUUUUUUUUUUUCKS
                IntPtr m_mono = GetModuleHandle("mono.dll");
                IntPtr p_mono_debug_init = GetProcAddress(m_mono, "mono_debug_init");
                IntPtr p_mono_debug_domain_create = new IntPtr(WINDOWS_f_mono_debug_domain_create - WINDOWS_f_mono_debug_init + (p_mono_debug_init.ToInt64()));
                d_mono_debug_domain_create = (d_mono_debug_domain_create) Marshal.GetDelegateForFunctionPointer(p_mono_debug_domain_create, typeof(d_mono_debug_domain_create));
            }

            d_mono_debug_domain_create(NULL);


            Assembly[] asmsManaged = AppDomain.CurrentDomain.GetAssemblies();
            IntPtr[] asms = new IntPtr[asmsManaged.Length];
            IntPtr[] imgs = new IntPtr[asms.Length];
            for (int i = 0; i < asmsManaged.Length; i++) {
                IntPtr asm = asms[i] = (IntPtr) f_mono_assembly.GetValue(asmsManaged[i]);
                imgs[i] = mono_assembly_get_image(asm);
                Debug.Log(i + ": " + asmsManaged[i].FullName + ": " + asms[i] + ", " + imgs[i]);
            }


            Debug.Log("Invoking mono_debug_init.");
            mono_debug_init(MonoDebugFormat.MONO_DEBUG_FORMAT_MONO);


            Debug.Log("Filling debug data as soon as possible.");
            d_mono_debug_domain_create(domainUCD);
            mono_debug_open_image_from_memory(imgUE, NULL, 0);


            Debug.Log("Done!");

            orig_Init();
        }
    }
}

internal enum MonoDebugFormat {
    MONO_DEBUG_FORMAT_NONE,
    MONO_DEBUG_FORMAT_MONO,
    MONO_DEBUG_FORMAT_DEBUGGER
}
