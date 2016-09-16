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
        ETGMod.StartCoroutine = StartCoroutine; // Set this here so ETGMod can access it statically.
        ETGMod.Init();
    }

    public void Start() {
        ETGMod.Start();

        // StartCoroutine(ListTextures());
    }

    public IEnumerator ListTextures() {
        yield return new WaitForSeconds(1f);
        while (isActiveAndEnabled) {
            tk2dSpriteCollectionData[] atlases = Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>();
            Console.WriteLine("Found " + atlases.Length + " atlases:");
            for (int i = 0; i < atlases.Length; i++) {
                tk2dSpriteCollectionData atlas = atlases[i];
                Console.WriteLine(i + ": " + atlas.spriteCollectionName + " (" + atlas.transform.GetPath() + "): " + (atlas.materials[0]?.mainTexture?.name ?? "NULL"));
                atlas.Handle();
            }
            yield return new WaitForSeconds(10f);
        }
    }

    public void Update() {
        ETGMod.Assets.Packer.Apply();
    }

}
