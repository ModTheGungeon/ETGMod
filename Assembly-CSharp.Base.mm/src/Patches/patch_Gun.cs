#pragma warning disable 0626
#pragma warning disable 0649

internal class patch_Gun : Gun {

    public extern void orig_Initialize(GameActor owner);
    public new void Initialize(GameActor owner) {
        orig_Initialize(owner);
        if (owner is PlayerController) {
            OnPostFired += ETGMod.Gun.OnPostFired;
            OnFinishAttack += ETGMod.Gun.OnFinishAttack;
            ETGMod.Gun.OnInit?.Invoke((PlayerController) owner, this);
        }
    }

}
