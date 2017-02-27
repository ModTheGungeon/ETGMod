using UnityEngine;
using System.Collections;
using Gungeon;

public class TestGun : CustomGun {
    public TestGun() : base("etgmod:test_gun") {
        // Here we tell Gungeon about the description of the gun.
        // If you want to use per-language descriptions, use the ETGMod.Databases.Strings.OnLanguageChanged
        // field, += your own delegate there and update the descriptions.
        // (You can also gun.SetName in that hook.)
        ShortDescription = "Hello, World!";
        LongDescription = "Legend tells that this gun has been handcrafted by a team of Gungeoneers that managed to escape into outer reality. A worn engraving on the barrel reads \"JSON\". Hopefully, the previous owner was a better gunsmith than he was a speller.";

        // This is required, unless you want to use the sprites of the base gun.
        // That, by default, is the pea shooter.
        // SetupSprite sets up the default gun sprite for the ammonomicon and the "gun get" popup.
        // WARNING: Add a copy of your default sprite to Ammonomicon Encounter Icon Collection!
        // That means, "sprites/Ammonomicon Encounter Icon Collection/defaultsprite.png" in your mod .zip
        SetupSprite(defaultSprite: "gshbd_fire_002", fps: 10);
        // ETGMod automatically checks which animations are available.
        // If you want to, you can set the FPS of those animations individually.
        SetAnimationFPS(shootAnimation, 40);

        // This is how you add "projectile modules". Each shoot, all the projectile modules
        // fire at once. The DefaultModule is the first added projectile module.
        // If you know you won't modify the projectiles at all, feel free to add "clonedProjectiles: false" (done here).
        // WARNING: The gun names are the names from the JSON dump! If you are not sure, ask one of the other modders
        // who have got the JSON dump for either a copy of it, or the name of the gun.
        // (You can also pass a gun here.)
        // WARNING: At this point, both the other gun needs a DefaultModule and your mod gun needs a Volley!
        // If you touched your Volley in any way, please don't blame us for this erroring.
        AddProjectileModuleFrom("AK-47", clonedProjectiles: false);

        // Here we just take the default projectile module and change its settings how we want it to be.
        DefaultModule.ammoCost = 1;
        DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
        DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
        reloadTime = 2.25f;
        SetBaseMaxAmmo(200);
        DefaultModule.numberOfShotsInClip = 15;

        // This is the test gun. If you don't know about the test gun, it shoots *every possible projectile, except beams*.
        // While all projectile modules shoot at once, projectiles themselves get picked either sequentially or randomly.
        // This way you can make your gun shoot random projectiles.
        foreach (var item in Game.Items.Entries) {
            Gun other = item as Gun;
            if (other == null || other.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Beam) continue;
            // Same applies as above: Your mod gun needs a volley, the other gun needs a projectile module with projectile,
            // and cloned: false just means you won't modify the projectile. Set it to true / remove it if you modify
            // the projectiles to not affect other guns.
            AddProjectileFrom(other, cloned: false);

        }

        // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
        quality = PickupObject.ItemQuality.S;
    }

    // If you want to do magic fancy stuff or calculations when your projectile leaves the gun,
    // just do it here.
    public override void PostProcessProjectile(Projectile projectile) {
        PlayerController player = CurrentOwner as PlayerController;
        if (player == null) return;

        // Random chance for the gun to "shoot everything at once".
        if (Mathf.Lerp(0.005f, 0.002f, ammo / 300f) < Random.value) return;
        // We clear all projectile modules. As we want to shoot everything at once,
        // not in a random sequence, we re-add all gun projectile MODULES.
        Volley.projectiles.Clear();
        RuntimeModuleData.Clear();
        for (int i = 0; i < ETGMod.Databases.Items.Count; i++) {
            Gun other = ETGMod.Databases.Items[i] as Gun;
            if (other == null) continue; // Watch out for null entries in the database! Those are WIP / removed guns.
            ProjectileModule module = AddProjectileModuleFrom(other, clonedProjectiles: false);
            // We don't want to eat more ammo than required.
            module.ammoCost = 0;
            module.numberOfShotsInClip = CurrentAmmo;
            ModuleShootData moduleData = new ModuleShootData();
            RuntimeModuleData[module] = moduleData;
        }
        // We refill the gun and make it only deplete ammo on the first module.
        ammo = GetBaseMaxAmmo();
        DefaultModule.ammoCost = 1;

        // Finally, the player looses the gun with 0 ammo after 3 seconds.
        StartCoroutine(EjectFrom(player));
    }

    // This is just a helper method for the test gun.
    public IEnumerator EjectFrom(PlayerController player) {
        yield return new WaitForSeconds(3f);
        ammo = 0;
        player.ForceDropGun(this);
    }
}