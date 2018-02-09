#pragma warning disable 0626
#pragma warning disable 0649

using System;
using System.Reflection;
using MonoMod;

namespace ETGMod.BasePatches {
    [MonoModPatch("FullInspector.fiSettings")]
    public class fiSettings : FullInspector.fiSettings {
        public extern static void orig_ctor_fiSettings();

        public static void LogDetailed(Exception e, string tag = null) {
            for (Exception e_ = e; e_ != null; e_ = e_.InnerException) {
                Console.WriteLine(e_.GetType().FullName + ": " + e_.Message + "\n" + e_.StackTrace);
                if (e_ is ReflectionTypeLoadException) {
                    ReflectionTypeLoadException rtle = (ReflectionTypeLoadException)e_;
                    for (int i = 0; i < rtle.Types.Length; i++) {
                        Console.WriteLine("ReflectionTypeLoadException.Types[" + i + "]: " + rtle.Types[i]);
                    }
                    for (int i = 0; i < rtle.LoaderExceptions.Length; i++) {
                        LogDetailed(rtle.LoaderExceptions[i], tag + (tag == null ? "" : ", ") + "rtle:" + i);
                    }
                }
                if (e_ is TypeLoadException) {
                    Console.WriteLine("TypeLoadException.TypeName: " + ((TypeLoadException)e_).TypeName);
                }
                if (e_ is BadImageFormatException) {
                    Console.WriteLine("BadImageFormatException.FileName: " + ((BadImageFormatException)e_).FileName);
                }
            }
        }

        [MonoModOriginalName("orig_ctor_fiSettings")]
        [MonoModConstructor]
        public static void ctor_fiSettings() {
            try {
                orig_ctor_fiSettings();
            } catch (Exception e) {
                LogDetailed(e);
            }
        }
    }

    [MonoModPatch("global::MainMenuFoyerController")]
    public class MainMenuFoyerController : global::MainMenuFoyerController {
        public extern void AddLine(string s);

        public void Start() {
            var word = ETGMod.ModLoader.LoadedMods.Count == 1 ? "mod" : "mods";
            AddLine($"{ETGMod.ModLoader.LoadedMods.Count} {word} loaded");
        }

    }
}