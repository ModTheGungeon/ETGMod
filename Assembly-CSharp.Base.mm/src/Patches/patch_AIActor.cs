#pragma warning disable 0626
#pragma warning disable 0649

internal class patch_AIActor : AIActor {
    
    public extern void orig_Start();
    public new void Start() {
        ETGMod.AIActor.OnPreStart?.Invoke(this);
        orig_Start();
        ETGMod.AIActor.OnPostStart?.Invoke(this);
    }

    private extern void orig_CheckForBlackPhantomness();
    private void CheckForBlackPhantomness() {
        ETGMod.AIActor.OnBlackPhantomnessCheck?.Invoke(this);
        orig_CheckForBlackPhantomness();
    }

}
