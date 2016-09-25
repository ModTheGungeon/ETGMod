#pragma warning disable 0626
#pragma warning disable 0649

using System;
using UnityEngine;
public class patch_PlayerController : PlayerController {
  /*  public extern void orig_Awake ();
    public override void Awake () {
        PlayerBehaviour playerBehaviour = gameObject.GetComponent<PlayerBehaviour>();
        if (playerBehaviour) {
            Debug.Log("Found PlayerBehaviour (Awake/PreInitialize)!");
            playerBehaviour.PreInitialize();
        }
        orig_Awake();
    }*/
    public extern void orig_Start ();
    public override void Start () {
        PlayerBehaviour playerBehaviour = gameObject.GetComponent<PlayerBehaviour>();
        if (playerBehaviour) {
            Debug.Log("Found PlayerBehaviour (Awake/PreInitialize)!");
            playerBehaviour.PreInitialize();
        }
        orig_Start();
        if (playerBehaviour) {
            Debug.Log("Found PlayerBehaviour (Start/Initialize)!");
            playerBehaviour.Initialize();
        }
    }
}
