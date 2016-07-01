#pragma warning disable 0108
#pragma warning disable 0626

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class patch_HealthHaver : HealthHaver {

    protected extern void orig_method_16(float float_14, Vector2 vector2_1, string string_6, GEnum1 genum1_0, GEnum165 genum165_0 = GEnum165.const_0, bool bool_18 = false, PixelCollider pixelCollider_0 = null, bool bool_19 = false);
    protected void method_16(float float_14, Vector2 vector2_1, string string_6, GEnum1 genum1_0, GEnum165 genum165_0 = GEnum165.const_0, bool bool_18 = false, PixelCollider pixelCollider_0 = null, bool bool_19 = false) {

        if (currentHealth==0) {
            orig_method_16(float_14, vector2_1, string_6, genum1_0, genum165_0, bool_18, pixelCollider_0, bool_19);
            return;
        }

        if (!ETGDamageIndicatorGUI.allHealthHavers.Contains(this)) {
            ETGDamageIndicatorGUI.allHealthHavers.Add(this);
            ETGDamageIndicatorGUI.maxHP[this]=maximumHealth;
        }

        float currHP = this.currentHealth;

        orig_method_16(float_14, vector2_1, string_6, genum1_0, genum165_0, bool_18, pixelCollider_0, bool_19);

        float newHP = this.currentHealth;

        float deltaHP = currHP-newHP;

        Vector3 centerPos = this.SpeculativeRigidbody_0.Vector2_4;
        centerPos+=this.transform.up;

        if (ETGModGUI.UseDamageIndicators)
            ETGDamageIndicatorGUI.CreateIndicator(centerPos, deltaHP);
        ETGDamageIndicatorGUI.currentHP[this]=currentHealth;

        if (currentHealth==0) {
            if (ETGDamageIndicatorGUI.allHealthHavers.Contains(this)) {
                ETGDamageIndicatorGUI.allHealthHavers.Remove(this);
            }
            ETGDamageIndicatorGUI.maxHP.Remove(this);
            ETGDamageIndicatorGUI.currentHP.Remove(this);
        }
    }

}

