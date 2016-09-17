#pragma warning disable 0414

namespace UnityEngine {
    /// <summary>
    /// Hooks in the Unity Engine that must have their original name's first character replaced with O.
    /// </summary>
    internal static class ETGModInstallerRenames {

        private static string[] items = {
            "UnityEngine.Resources:Load", "Ooad",
            "UnityEngine.Resources:LoadAsync", "OoadAsync",
            "UnityEngine.Resources:LoadAll", "OoadAll",
            "UnityEngine.Resources:GetBuiltinResource", "OetBuiltinResource",
            "UnityEngine.Resources:UnloadAsset", "OnloadAsset",
            "UnityEngine.Resources:UnloadUnusedAssets", "OnloadUnusedAssets",

        };

    }
}
