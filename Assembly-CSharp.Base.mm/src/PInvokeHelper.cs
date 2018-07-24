﻿using System.Reflection;
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using System.Reflection.Emit;

#if UNITYENGINE
internal
#else
public
#endif
    static class PInvokeHelper {

    private readonly static IntPtr NULL = IntPtr.Zero;

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

    private static IntPtr _Unity = NULL;
    public static IntPtr Unity {
        get {
            if (_Unity != NULL) {
                return _Unity;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                return _Unity = GetModuleHandle(null);

            }

            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                IntPtr e = IntPtr.Zero;
                _Unity = dlopen(null, RTLD_NOW);
                if ((e = dlerror()) != IntPtr.Zero) {
                    Debug.Log("PInvokeHelper can't access the main assembly!");
                    Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                    return NULL;
                }
                return _Unity;
            }

            return NULL;
        }
    }

    private static IntPtr _Mono = NULL;
    public static IntPtr Mono {
        get {
            if (_Mono != NULL) {
                return _Mono;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                return _Mono = GetModuleHandle("mono.dll");

            }

            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                IntPtr e = IntPtr.Zero;
                if (PlatformHelper.Is(Platform.MacOS)) {
                    _Mono = dlopen("./EtG_OSX.app/Contents/Frameworks/MonoEmbedRuntime/osx/libmono.0.dylib", RTLD_NOW);
                } else if (IntPtr.Size == 8) {
                    _Mono = dlopen("./EtG_Data/Mono/x86_64/libmono.so", RTLD_NOW);
                } else {
                    _Mono = dlopen("./EtG_Data/Mono/x86/libmono.so", RTLD_NOW);
                }
                if ((e = dlerror()) != IntPtr.Zero) {
                    Debug.Log("PInvokeHelper can't access libmono.so!");
                    Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                    return NULL;
                }
                return _Mono;
            }

            return NULL;
        }
    }

    private static IntPtr _PThread = NULL;
    public static IntPtr PThread {
        get {
            if (_PThread != NULL) {
                return _PThread;
            }

            if (Environment.OSVersion.Platform != PlatformID.Unix) {
                return NULL;
            }

            IntPtr e = IntPtr.Zero;
            _PThread = dlopen(PlatformHelper.Is(Platform.MacOS) ? "libpthread.dylib" : "libpthread.so.0", RTLD_NOW);
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("PInvokeHelper can't access libpthread.so!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return NULL;
            }
            return _PThread;
        }
    }

    public static IntPtr GetFunction(this IntPtr lib, string name) {
        if (lib == NULL) {
            return NULL;
        }

        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            return GetProcAddress(lib, name);
        }

        if (Environment.OSVersion.Platform == PlatformID.Unix) {
            IntPtr s, e;

            s = dlsym(lib, name);
            if ((e = dlerror()) != IntPtr.Zero) {
                Debug.Log("PInvokeHelper can't access " + name + "!");
                Debug.Log("dlerror: " + Marshal.PtrToStringAnsi(e));
                return NULL;
            }
            return s;
        }

        return NULL;
    }

    public static T GetDelegate<T>(this IntPtr lib) where T : class {
        return lib.GetDelegate<T>(typeof(T).Name.Substring(2));
    }
    public static T GetDelegate<T>(this IntPtr lib, string name) where T : class {
        if (lib == NULL) {
            return null;
        }

        IntPtr s = lib.GetFunction(name);
        if (s == NULL) {
            return null;
        }

        return s.AsDelegate<T>();
    }
    public static T GetDelegateAtRVA<T>(this IntPtr basea, long rva) where T : class {
        return new IntPtr(basea.ToInt64() + rva).AsDelegate<T>();
    }
    public static T AsDelegate<T>(this IntPtr s) where T : class {
        return Marshal.GetDelegateForFunctionPointer(s, typeof(T)) as T;
    }

    // Windows
    [DllImport("kernel32")]
    private static extern uint GetCurrentThreadId();
    // Linux
    private delegate ulong d_pthread_self();
    private static d_pthread_self pthread_self;

    public static ulong CurrentThreadId {
        get {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                return GetCurrentThreadId();
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                if ((pthread_self = pthread_self ?? PThread.GetDelegate<d_pthread_self>()) == null) return 0;
                return pthread_self();
            }

            return 0;
        }
    }

}