using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is responsible for handling basic Unity events for all mods (Awake, Start, Update, ...).
/// </summary>
public class ETGModMainBehaviour : MonoBehaviour {

    public static ETGModMainBehaviour Instance;

    public void Awake() {
        DontDestroyOnLoad(gameObject);
        ETGMod.StartGlobalCoroutine = StartCoroutine;
        ETGMod.StopGlobalCoroutine = StopCoroutine;
        ETGMod.Init();
    }

    public void Start() {
        ETGMod.Start();
    }

    public void Update() {
        ETGMod.Assets.Packer.Apply();
    }

}
