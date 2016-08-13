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
        public static bool GiveItemID(int id) {
            if (!GameManager.Instance.PrimaryPlayer) {
                Debug.Log ("Couldn't access PlayerController instance in GameManager");
                return false;
            }

            LootEngine.TryGivePrefabToPlayer(PickupObjectDatabase.GetById(id).gameObject, GameManager.Instance.PrimaryPlayer, false);
            return true;
        }

        public static bool? InfiniteKeys;
        public static string QuickstartReplacement;
        public static string CoopReplacement;
    }

}
