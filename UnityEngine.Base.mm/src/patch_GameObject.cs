#pragma warning disable 0626
#pragma warning disable 0649

using MonoMod;
using UnityEngineInternal;

namespace UnityEngine {
    internal class patch_GameObject {

        public extern Component orig_AddComponent(System.Type componentType);
        public Component AddComponent(System.Type componentType) {
            Component component = orig_AddComponent(componentType);
            if (ETGModUnityEngineHooks.AddComponent != null) {
                component = ETGModUnityEngineHooks.AddComponent(component);
            }
            return component;
        }

    }
}
