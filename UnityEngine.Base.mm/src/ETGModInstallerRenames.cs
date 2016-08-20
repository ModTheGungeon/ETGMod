#pragma warning disable 0414

namespace UnityEngine {
    /// <summary>
    /// Hooks in the Unity Engine that must have their original name's first character replaced with O.
    /// </summary>
    internal static class ETGModInstallerRenames {

        private static string[] items = {
            "UnityEngine.Resources:Load",
            "UnityEngine.Resources:LoadAsync",
            "UnityEngine.Resources:LoadAll",
            "UnityEngine.Resources:GetBuiltinResource",
            "UnityEngine.Resources:UnloadAsset",
            "UnityEngine.Resources:UnloadUnusedAssets"

        };

    }
}
