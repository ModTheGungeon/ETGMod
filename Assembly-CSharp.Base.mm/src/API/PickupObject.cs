using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ETGMod.API {
    public class CustomPickupObject {
        internal static GameObject BasePickupGameObject;

        internal static void InitAPI() {
            BasePickupGameObject = FakePrefab.Clone(ETGMod.Items["gungeon:ammo"].gameObject);
            UnityEngine.Object.Destroy(BasePickupGameObject.GetComponent<PickupObject>());
        }

        public static T Create<T>() where T : PickupObject {
            return FakePrefab.Clone(BasePickupGameObject).AddComponent<T>();
        }
    }
}