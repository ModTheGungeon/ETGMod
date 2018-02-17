#pragma warning disable 0626
#pragma warning disable 0649
#pragma warning disable 0436

using System.Diagnostics;
using MonoMod;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEnginePatches {
    public static class ObjectInstantiateHijacker {
        public static Action<Object, Object> HijackDelegate = (original_obj, new_obj) => {};

        internal static Object Filter(Object source, Object instance) {
            HijackDelegate.Invoke(source, instance);
            return instance;
        }

        public static void Add(Action<Object, Object> deleg) {
            HijackDelegate += deleg;
        }

        public static void Add<T>(Action<T, T> deleg) where T : Object {
            HijackDelegate += (o1, o2) => {
                if (o1 is GameObject && o2 is GameObject) {
                    var comp1 = ((GameObject)o1).GetComponent<T>();
                    var comp2 = ((GameObject)o2).GetComponent<T>();

                    if (comp1 != null && comp2 != null) {
                        deleg.Invoke(comp1, comp2);
                    }
                }
            };
        }
    }

    [MonoModPatch("UnityEngine.Object")]
    public class ObjectPatch {
        // hijacking object.instantiate

        [MonoModOriginal]
        private static extern Object ONTERNAL_CALL_Internal_InstantiateSingle(Object data, ref Vector3 pos, ref Quaternion rot);

        [MonoModOriginalName("ONTERNAL_CALL_Internal_InstantiateSingle")]
        private static Object INTERNAL_CALL_Internal_InstantiateSingle(Object data, ref Vector3 pos, ref Quaternion rot) {
            return ObjectInstantiateHijacker.Filter(data, ONTERNAL_CALL_Internal_InstantiateSingle(data, ref pos, ref rot));
        }

        [MonoModOriginal]
        private static extern Object ONTERNAL_CALL_Internal_InstantiateSingleWithParent(Object data, Transform parent, ref Vector3 pos, ref Quaternion rot);

        [MonoModOriginalName("ONTERNAL_CALL_Internal_InstantiateSingleWithParent")]
        private static Object INTERNAL_CALL_Internal_InstantiateSingleWithParent(Object data, Transform parent, ref Vector3 pos, ref Quaternion rot) {
            return ObjectInstantiateHijacker.Filter(data, ONTERNAL_CALL_Internal_InstantiateSingleWithParent(data, parent, ref pos, ref rot));
        }

        [MonoModOriginal]
        private static extern Object Onternal_CloneSingle(Object data);

        [MonoModOriginalName("Onternal_CloneSingle")]
        private static Object Internal_CloneSingle(Object data) {
            return ObjectInstantiateHijacker.Filter(data, Onternal_CloneSingle(data));
        }

        [MonoModOriginal]
        private static extern Object Onternal_CloneSingleWithParent(Object data, Transform parent, bool worldPositionStays);

        [MonoModOriginalName("Onternal_CloneSingleWithParent")]
        private static Object Internal_CloneSingleWithParent(Object data, Transform parent, bool worldPositionStays) {
            return ObjectInstantiateHijacker.Filter(data, Onternal_CloneSingleWithParent(data, parent, worldPositionStays));
        }
    }
}
