using UnityEngine;
using System.Collections;

public class TestGun : GunBehaviour {

    public static void Add() {
        Gun gun = ETGMod.Databases.Items.NewGun("Test Gun", "gshbd");
        gun.gameObject.AddComponent<TestGun>(); //FIXME NewGun<> causing issues (MonoMod)
        gun.SetShortDescription("Hello, World!");
        gun.SetLongDescription("Legend tells that this gun has been handcrafted by a team of Gungeoneers that managed to escape into outer reality. A worn engraving on the barrel reads \"JSON\". Hopefully, the previous owner was a better gunsmith than he was a speller.");

        gun.SetupSprite(defaultSprite: "gshbd_fire_002");
        gun.SetAnimationFPS(10);
        gun.SetAnimationFPS(gun.shootAnimation, 40);

        gun.AddProjectileModuleFrom("AK-47");

        gun.DefaultModule.ammoCost = 1;
        gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
        gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
        gun.reloadTime = 2.25f;
        gun.SetBaseMaxAmmo(200);
        gun.DefaultModule.numberOfShotsInClip = 15;

        for (int i = 0; i < ETGMod.Databases.Items.Count; i++) {
            Gun other = ETGMod.Databases.Items[i] as Gun;
            if (other == null) continue;
            if (other.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Beam) continue;
            gun.AddProjectileFrom(other);
        }

        gun.quality = PickupObject.ItemQuality.S;
        gun.encounterTrackable.EncounterGuid = "Test Gun Please Ignore"; // Update this GUID when you need to "refresh" the gun.
        ETGMod.Databases.Items.Add(gun);
    }

    public override void PostProcessProjectile(Projectile projectile) {
        PlayerController player = gun.CurrentOwner as PlayerController;
        if (player == null) return;

        if (0.0005f < UnityEngine.Random.value) return;
        for (int i = 0; i < player.inventory.AllGuns.Count; i++) {
            Gun other = player.inventory.AllGuns[i];
            if (other == gun) continue;
            other.ammo = 0;
        }
        gun.Volley.projectiles.Clear();
        gun.RuntimeModuleData.Clear();
        for (int i = 0; i < ETGMod.Databases.Items.Count; i++) {
            Gun other = ETGMod.Databases.Items[i] as Gun;
            if (other == null) continue;
            ProjectileModule module = gun.AddProjectileModuleFrom(other);
            module.ammoCost = 0;
            module.numberOfShotsInClip = gun.CurrentAmmo;
            ModuleShootData moduleData = new ModuleShootData();
            gun.RuntimeModuleData[module] = moduleData;
        }
        gun.ammo = gun.GetBaseMaxAmmo();
        gun.DefaultModule.ammoCost = 10;

        StartCoroutine(EjectFrom(player));
    }

    public IEnumerator EjectFrom(PlayerController player) {
        yield return new WaitForSeconds(3f);
        player.ForceDropGun(gun);
    }

}
