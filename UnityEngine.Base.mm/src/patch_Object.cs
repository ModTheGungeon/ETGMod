#pragma warning disable 0626
#pragma warning disable 0649

using MonoMod;

namespace UnityEngine {
    internal class patch_Object {

        [MonoModConstructor]
        public patch_Object() {
            ETGModUnityEngineHooks.Construct?.Invoke(this as object as Object);
        }

        public static extern Object orig_Instantiate(Object original, Vector3 position, Quaternion rotation);
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation) {
            Object clone = orig_Instantiate(original, position, rotation);
            if (ETGModUnityEngineHooks.Instantiate != null) {
                clone = ETGModUnityEngineHooks.Instantiate(clone);
            }
            return clone;
        }

        public static extern Object orig_Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent);
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent) {
            Object clone = orig_Instantiate(original, position, rotation, parent);
            if (ETGModUnityEngineHooks.Instantiate != null) {
                clone = ETGModUnityEngineHooks.Instantiate(clone);
            }
            return clone;
        }

        public static extern Object orig_Instantiate(Object original, Transform parent, bool worldPositionStays);
        public static Object Instantiate(Object original, Transform parent, bool worldPositionStays) {
            Object clone = orig_Instantiate(original, parent, worldPositionStays);
            if (ETGModUnityEngineHooks.Instantiate != null) {
                clone = ETGModUnityEngineHooks.Instantiate(clone);
            }
            return clone;
        }

        public static extern Object orig_Instantiate(Object original);
        public static Object Instantiate(Object original) {
            Object clone = orig_Instantiate(original);
            if (ETGModUnityEngineHooks.Instantiate != null) {
                clone = ETGModUnityEngineHooks.Instantiate(clone);
            }
            return clone;
        }

        public static extern T orig_Instantiate<T>(Object original, Vector3 position, Quaternion rotation) where T : Object;
        public static T Instantiate<T>(T original) where T : Object {
            Object clone = orig_Instantiate(original);
            if (ETGModUnityEngineHooks.Instantiate != null) {
                clone = ETGModUnityEngineHooks.Instantiate(clone);
            }
            return (T) clone;
        }

    }
}
