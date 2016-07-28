#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_MainMenuFoyerController : MainMenuFoyerController {

    public bool isMatched;

    public static MainMenuFoyerController instance;

    protected extern void orig_Awake();
    protected void Awake() {
        orig_Awake();
        instance=this;

        VersionLabel.Position=new Vector3(
            VersionLabel.Position.x,
            VersionLabel.Position.y+VersionLabel.Height*2f,
            VersionLabel.Position.z
        );
        VersionLabel.Text="Enter the Gungeon"+VersionLabel.Text+"\nMod the Gungeon "+ETGMod.BaseUIVersion;
    }

    Texture2D tex;

    protected extern void orig_Update();
    protected void Update() {
        orig_Update();
        if (!isMatched) {
            if (tex==null)
                tex=Resources.Load<Texture2D>("etgmod/logo");
            if (tex==null)
                return;
            ( (dfTextureSprite)TitleCard ).Texture=tex;
            ( (dfTextureSprite)TitleCard ).Texture.filterMode=FilterMode.Point;
            if (( (dfTextureSprite)TitleCard ).Texture!=tex)
                isMatched=false;
            else
                isMatched=true;
        }
    }

}