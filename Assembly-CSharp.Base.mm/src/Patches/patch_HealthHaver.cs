#pragma warning disable 0108
#pragma warning disable 0626

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod;

internal class patch_HealthHaver : HealthHaver {

    protected extern void orig_method_16(float float_14, Vector2 vector2_1, string string_6, GEnum1 genum1_0, global::GEnum165 genum165_0 = global::GEnum165.const_0, bool bool_18 = false, PixelCollider pixelCollider_0 = null, bool bool_19 = false);
    [MonoModOnPlatform(Platform.Windows)]
    protected void method_16(float float_14, Vector2 vector2_1, string string_6, GEnum1 genum1_0, global::GEnum165 genum165_0 = global::GEnum165.const_0, bool bool_18 = false, PixelCollider pixelCollider_0 = null, bool bool_19 = false) {
        if (currentHealth == 0f) {
            orig_method_16(float_14, vector2_1, string_6, genum1_0, genum165_0, bool_18, pixelCollider_0, bool_19);
            return;
        }
        float currHP = currentHealth;
        orig_method_16(float_14, vector2_1, string_6, genum1_0, genum165_0, bool_18, pixelCollider_0, bool_19);
        float newHP = currentHealth;
        IndicatorShared(currHP - newHP);
    }

    protected extern void orig_method_16(float float_14, Vector2 vector2_1, string string_6, GEnum1 genum1_0, global::GEnum164 genum164_0 = global::GEnum164.const_0, bool bool_18 = false, PixelCollider pixelCollider_0 = null, bool bool_19 = false);
    [MonoModOnPlatform(Platform.Unix)]
    protected void method_16(float float_14, Vector2 vector2_1, string string_6, GEnum1 genum1_0, global::GEnum164 genum164_0 = global::GEnum164.const_0, bool bool_18 = false, PixelCollider pixelCollider_0 = null, bool bool_19 = false) {
        if (currentHealth == 0f) {
            orig_method_16(float_14, vector2_1, string_6, genum1_0, genum164_0, bool_18, pixelCollider_0, bool_19);
            return;
        }
        float currHP = currentHealth;
        orig_method_16(float_14, vector2_1, string_6, genum1_0, genum164_0, bool_18, pixelCollider_0, bool_19);
        float newHP = currentHealth;
        IndicatorShared(currHP - newHP);
    }

    protected void IndicatorShared(float deltaHP) {
        Vector3 centerPos = SpeculativeRigidbody_0.Vector2_4;
        centerPos += transform.up;

        if (ETGModGUI.UseDamageIndicators) {
            ETGDamageIndicatorGUI.CreateIndicator(centerPos, deltaHP);
            ETGDamageIndicatorGUI.CreateBar(this);
        }
        ETGDamageIndicatorGUI.MaxHP[this] = maximumHealth;
        ETGDamageIndicatorGUI.CurrentHP[this] = currentHealth;

        ETGDamageIndicatorGUI.UpdateHealthBar(this, deltaHP);

        if (currentHealth == 0) {
            ETGDamageIndicatorGUI.ToRemoveBars.Add(this);
            ETGDamageIndicatorGUI.MaxHP.Remove(this);
            ETGDamageIndicatorGUI.CurrentHP.Remove(this);
        }
    }

}

[MonoModIgnore]
internal enum GEnum164 {
    const_0,
    const_1,
    const_2,
    const_3,
    const_4
}
[MonoModIgnore]
internal enum GEnum165 {
    const_0,
    const_1,
    const_2,
    const_3,
    const_4
}
