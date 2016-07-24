#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_MainMenuFoyerController : MainMenuFoyerController {

    public bool isMatched;

    protected extern void orig_Awake();
    protected void Awake() {
        orig_Awake();

        VersionLabel.Position = new Vector3(
            VersionLabel.Position.x,
            VersionLabel.Position.y + VersionLabel.Height * 2f,
            VersionLabel.Position.z
        );
        VersionLabel.Text = "Enter the Gungeon" + VersionLabel.Text + "\nMod the Gungeon " + ETGMod.BaseUIVersion;
    }

    protected extern void orig_Update();
    protected void Update() {
        orig_Update();
        if (!isMatched) {
            ( (dfTextureSprite)TitleCard ).Texture=Resources.Load<Texture2D>("etgmod/logo");
            ( (dfTextureSprite)TitleCard ).Texture.filterMode=FilterMode.Point;
            if (( (dfTextureSprite)TitleCard ).Texture!=Resources.Load<Texture2D>("etgmod/logo"))
                isMatched=false;
            else
                isMatched=true;
        }
    }

}
