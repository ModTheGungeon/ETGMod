using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod player configuration.
    /// </summary>
    public static class Player {
        // Linux: GClass200.smethod_15
        // ret bool, GameObject gameObject_0, PlayerController playerController_0, bool bool_0 = false
        public readonly static MethodInfo _GiveItem = new CrossSearch<MethodInfo>() {
            Name = "smethod_15",
            In = "GClass200",
            Static = true,
            Returns = typeof(bool),
            Args = new [] { typeof(GameObject), typeof(PlayerController), typeof(bool) }
            // No context required.
        }.Find();
        public static bool GiveItemID(int id) {
            if (!GameManager.GameManager_0.PlayerController_1) {
                Debug.Log ("Couldn't access static current PlayerController in GameManager");
                return false;
            }
            PlayerController playercontroller = GameManager.GameManager_0.PlayerController_1;
            GameObject pickupobject = PickupObjectDatabase.GetById (id).gameObject;
            _GiveItem.Xs(pickupobject, playercontroller, false);
            return true;
        }

        public static bool? InfiniteKeys;
        public static string QuickstartReplacement;
        public static string CoopReplacement;
    }

}
