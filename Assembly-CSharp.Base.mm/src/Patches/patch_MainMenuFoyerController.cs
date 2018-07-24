#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_MainMenuFoyerController : MainMenuFoyerController {

    public static MainMenuFoyerController Instance;

    protected extern void orig_Awake();
    protected void Awake() {
        orig_Awake();
        Instance = this;

        VersionLabel.Text = VersionLabel.Text + " | " + ETGMod.BaseUIVersion;
    }

    private Texture2D logo;
    private bool logoReplaced;

    protected extern void orig_Update();
    protected void Update() {
        orig_Update();
        if (!logoReplaced) {
            if (logo == null) {
                logo = Resources.Load<Texture2D>("etgmod/logo");
            }
            if (logo == null) {
                return;
            }
            logo.filterMode = FilterMode.Point;
            ((dfTextureSprite) TitleCard).Texture = logo;
            logoReplaced = true;
        }
    }

}