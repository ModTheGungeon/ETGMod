#pragma warning disable 0626
#pragma warning disable 0649

namespace UnityEngine {
    internal class patch_Object {

        public static extern Object orig_Instantiate(Object original, Vector3 position, Quaternion rotation);
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation) {
            Object clone = orig_Instantiate(original, position, rotation);
            clone = ETGModUnityEngineHooks.Instantiate(clone);
            return clone;
        }

        public static extern Object orig_Instantiate(Object original);
        public static Object Instantiate(Object original) {
            Object clone = orig_Instantiate(original);
            clone = ETGModUnityEngineHooks.Instantiate(clone);
            return clone;
        }

        public static extern T orig_Instantiate<T>(Object original, Vector3 position, Quaternion rotation) where T : Object;
        public static T Instantiate<T>(T original) where T : Object {
            Object clone = orig_Instantiate(original);
            clone = ETGModUnityEngineHooks.Instantiate(clone);
            return (T) clone;
        }

    }
}
