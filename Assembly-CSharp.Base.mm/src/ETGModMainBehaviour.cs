using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// This class is responsible for handling basic Unity events for all mods (Awake, Start, Update, ...).
/// </summary>
public class ETGModMainBehaviour : MonoBehaviour {

    public static ETGModMainBehaviour Instance;

    public void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    public void Start() {
        ETGMod.Start();
        ETGModGUI.Create();
    }

    public void Update() {
        ETGMod.Update();
    }

    public void OnGUI() {

    }

}
