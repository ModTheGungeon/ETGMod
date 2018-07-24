using UnityEngine;

public class GunBehaviour : MonoBehaviour {

    protected Gun gun;

    public void Start() {
        gun = GetComponent<Gun>();
        gun.OnInitializedWithOwner += OnInitializedWithOwner;
        gun.PostProcessProjectile += PostProcessProjectile;
        gun.OnDropped += OnDropped;
        gun.OnAutoReload += OnAutoReload;
        gun.OnReloadPressed += OnReloadPressed;
        gun.OnFinishAttack += OnFinishAttack;
        gun.OnPostFired += OnPostFired;
        gun.OnAmmoChanged += OnAmmoChanged;
        //gun.OnPreFireProjectileModifier += OnPreFireProjectileModifier;
    }

    public virtual void OnInitializedWithOwner(GameActor actor) {
    }

    public virtual void PostProcessProjectile(Projectile projectile) {
    }

    public virtual void OnDropped() {
    }

    public virtual void OnAutoReload(PlayerController player, Gun gun) {
    }

    public virtual void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING) {
    }

    public virtual void OnFinishAttack(PlayerController player, Gun gun) {
    }

    public virtual void OnPostFired(PlayerController player, Gun gun) {
    }

    public virtual void OnAmmoChanged(PlayerController player, Gun gun) {
    }

    public virtual Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile) {
        return projectile.EnabledClonedPrefab();
    }

}
