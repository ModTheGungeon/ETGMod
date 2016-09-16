#pragma warning disable 0626
#pragma warning disable 0649

using System.Collections;
using UnityEngine;

internal abstract class patch_tk2dBaseSprite : tk2dBaseSprite {

    public void Start() {
        this.HandleAuto();
    }

}
