using UnityEngine;
using System.Collections;

public class BalloonGunController : GunBehaviour {

    public static void Add() {
        Gun gun = ETGMod.Databases.Items.NewGun("Balloon Gun", "balloon_gun");
        gun.gameObject.AddComponent<BalloonGunController>(); //FIXME NewGun<> causing issues (MonoMod)
        gun.SetShortDescription("Not the real one.");
        gun.SetLongDescription("This bootleg of the true balloon gun was made from the cheapest plastics, causing it to pop 70% more often.");

        gun.SetupSprite(ETGMod.Databases.Items.WeaponCollection02, defaultSprite: "balloon_gun_idle_001", fps: 10);
        gun.SetAnimationFPS(gun.shootAnimation, 20);

        gun.AddProjectileModuleFrom("AK-47");

        // Uh... bootleg.
        gun.DefaultModule.ammoCost = 1;
        gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
        gun.reloadTime = 1f;
        gun.SetBaseMaxAmmo(200);
        gun.DefaultModule.numberOfShotsInClip = 15;

        gun.DefaultModule.projectiles[0].GetAnySprite().SetSprite("air_projectile_001");

        OnGunDamagedModifier damagedModifier = gun.gameObject.AddComponent<OnGunDamagedModifier>();
        damagedModifier.DepleteAmmoOnDamage = true;
        damagedModifier.NondepletedGunGrantsFlight = true;

        gun.quality = PickupObject.ItemQuality.S;
        gun.encounterTrackable.EncounterGuid = "Not The Real Balloon Gun"; // Update this GUID when you need to "refresh" the gun.
        ETGMod.Databases.Items.Add(gun);
    }

}
