#pragma warning disable 0108
#pragma warning disable 0626

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod;

internal class patch_HealthHaver : HealthHaver {

    protected extern void orig_ApplyDamage(float damage, Vector2 direction, string sourceName, CoreDamageTypes damageTypes = CoreDamageTypes.None, DamageCategory damageCategory = DamageCategory.Normal, bool ignoreInvulnerabilityFrames = false, PixelCollider hitPixelCollider = null, bool ignoreDamageCaps = false)
    protected void ApplyDamage(float damage, Vector2 direction, string sourceName, CoreDamageTypes damageTypes = CoreDamageTypes.None, DamageCategory damageCategory = DamageCategory.Normal, bool ignoreInvulnerabilityFrames = false, PixelCollider hitPixelCollider = null, bool ignoreDamageCaps = false) {
        orig_ApplyDamage(damage, direction, sourceName, damageTypes, damageCategory, ignoreInvulnerabilityFrames, hitPixelCollider, ignoreDamageCaps);
        if (currentHealth == 0f) {
            return;
        }

        Vector3 centerPos = specRigidbody.UnitTopCenter;

        if (ETGModGUI.UseDamageIndicators) {
            ETGDamageIndicatorGUI.CreateIndicator(centerPos, damage);
            ETGDamageIndicatorGUI.CreateBar(this);
        }
        ETGDamageIndicatorGUI.MaxHP[this] = maximumHealth;
        ETGDamageIndicatorGUI.CurrentHP[this] = currentHealth;

        ETGDamageIndicatorGUI.UpdateHealthBar(this, damage);

        if (currentHealth == 0) {
            ETGDamageIndicatorGUI.ToRemoveBars.Add(this);
            ETGDamageIndicatorGUI.MaxHP.Remove(this);
            ETGDamageIndicatorGUI.CurrentHP.Remove(this);
        }
    }

}
