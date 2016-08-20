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
        ETGMod.StartCoroutine = StartCoroutine; // Set this here so ETGMod can access it statically.
        ETGMod.Init();
    }

    public void Start() {
        ETGMod.Start();
    }

}
