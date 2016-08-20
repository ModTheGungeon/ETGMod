#pragma warning disable 0108
#pragma warning disable 0626

using UnityEngine;

internal class patch_HealthHaver : HealthHaver {

    protected extern void orig_ApplyDamage(float damage, Vector2 direction, string sourceName, CoreDamageTypes damageTypes = CoreDamageTypes.None, DamageCategory damageCategory = DamageCategory.Normal, bool ignoreInvulnerabilityFrames = false, PixelCollider hitPixelCollider = null, bool ignoreDamageCaps = false);
    protected void ApplyDamage(float damage, Vector2 direction, string sourceName, CoreDamageTypes damageTypes = CoreDamageTypes.None, DamageCategory damageCategory = DamageCategory.Normal, bool ignoreInvulnerabilityFrames = false, PixelCollider hitPixelCollider = null, bool ignoreDamageCaps = false) {
        orig_ApplyDamage(damage, direction, sourceName, damageTypes, damageCategory, ignoreInvulnerabilityFrames, hitPixelCollider, ignoreDamageCaps);
        if (System.Math.Abs(currentHealth) < 0.01f || damage <= 0f) 
            return;

        if (ETGModGUI.UseDamageIndicators)
            ETGDamageIndicatorGUI.HealthHaverTookDamage(this, damage);
    }

}
