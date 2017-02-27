using System;
using UnityEngine;

namespace Gungeon {
    public static partial class Game {
        public static PlayerController PrimaryPlayer {
            get {
                return GameManager.Instance.PrimaryPlayer;
            }
        }
        public static PlayerController CoopPlayer {
            get {
                return GameManager.Instance.SecondaryPlayer;
            }
        }

        public static bool? InfiniteKeys;
        public static string PrimaryPlayerReplacement;
        public static string CoopPlayerReplacement;

    }
}

public static class PlayerControllerExt {
    public static bool IsPlaying(this PlayerController player) {
        return GameManager.Instance.PrimaryPlayer == player || GameManager.Instance.SecondaryPlayer == player;
    }
    public static bool GiveItem(this PlayerController player, string id) {
        if (!player.IsPlaying()) throw new Exception("Tried to give item to inactive player controller");

        LootEngine.TryGivePrefabToPlayer(Gungeon.Items[id].gameObject, player, false);
        return true;
    }
}