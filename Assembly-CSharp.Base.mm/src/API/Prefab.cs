using System;
using UnityEngine;

namespace ETGMod.API {
    public static class FakePrefab {
        public static GameObject Clone(GameObject obj) {
            var was_active = obj.activeSelf;
            if (was_active) obj.SetActive(false);
            var fakeprefab = UnityEngine.Object.Instantiate(obj);
            if (was_active) obj.SetActive(true);
            return fakeprefab;
        }

        public static GameObject Instantiate(GameObject obj) {
            var inst = UnityEngine.Object.Instantiate(obj);
            inst.SetActive(true);
            return inst;
        }
    }
}
