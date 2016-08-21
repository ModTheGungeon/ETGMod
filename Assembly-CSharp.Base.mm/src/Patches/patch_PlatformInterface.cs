#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

public class patch_PlatformInterface {
    extern public void orig_AchievementUnlock(Achievement achievement, int playerIndex = 0);
    public void AchievementUnlock(Achievement achievement, int playerIndex = 0) {
        if (ETGMod.Platform.EnableAchievements) {
            orig_AchievementUnlock (achievement, playerIndex);
        } else {
            Debug.Log ("Refusing to give achievement because of ETGMod.Platform.EnableAchievements!");
        }
    }
}