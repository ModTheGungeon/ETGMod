using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class UnityExt {
    public static void DestroyComponent<T>(this GameObject go) where T : Component {
        Object.Destroy(go.GetComponent<T>());
    }

    public static void DestroyComponents<T>(this GameObject go) where T : Component {
        var coms = go.GetComponents<T>();
        for (int i = 0; i < coms.Length; i++) {
            Object.Destroy(coms[i]);
        }
    }
}
