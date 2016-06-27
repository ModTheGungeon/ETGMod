using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//This class is responsible for all mod managment 
class ETGModManager: MonoBehaviour {

    public void Awake() {

    }

    public void Start() {
        ETGMod.Start();
        ModMenu.Create();
        DontDestroyOnLoad(gameObject);
    }

    GameObject MoneyPickup;

    public void Update() {
        ETGMod.Update();
    }

    //This is for GUI stuff.
    public void OnGUI() {

    }

}
