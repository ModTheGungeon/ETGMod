using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//This class is responsible for all mod managment 
class ETGModManager : MonoBehaviour{

    public void Awake() {

    }

    public void Start() {
        ETGMod.Start();
        ModMenu.Create();
    }

    public void Update() {
        ETGMod.Update();
    }

    //This is for GUI stuff.
    public void OnGUI() {
        if(patch_GameManager.GameManager_0)
            if (patch_GameManager.GameManager_0.PlayerController_1!=null)
                GUILayout.Label(patch_GameManager.GameManager_0.PlayerController_1.transform.position.ToString());
    }

}
