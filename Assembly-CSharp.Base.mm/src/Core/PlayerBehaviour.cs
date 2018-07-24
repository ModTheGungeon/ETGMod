using System;
using UnityEngine;
public class PlayerBehaviour : MonoBehaviour {
    protected PlayerController controller;
    public void Awake() {
        controller = gameObject.GetComponent<PlayerController>();
        if (controller == null) {
            Debug.Log("Couldn't find the PlayerController component... this is gonna end badly.");
            return;
        }
    }
    public virtual void PreInitialize() {}
    public virtual void Initialize() {}
}
