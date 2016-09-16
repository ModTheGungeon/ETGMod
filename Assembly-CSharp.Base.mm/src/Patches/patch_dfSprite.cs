#pragma warning disable 0626
#pragma warning disable 0649

using System.Collections;
using UnityEngine;

internal class patch_dfSprite : dfSprite {

    public override void Start() {
        base.Start();

        Atlas.HandleAuto();
    }

}
