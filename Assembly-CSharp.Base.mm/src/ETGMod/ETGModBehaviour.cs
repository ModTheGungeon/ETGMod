using System;
using UnityEngine;

namespace ETGMod {
    public class ETGModBehaviour : MonoBehaviour {
        public static void Add() {
            new GameObject("ETGMod").AddComponent<ETGModBehaviour>();
        }

        void Awake() { ETGMod.Awake(); }
        void Update() { ETGMod.Update(); }
        void FixedUpdate() { ETGMod.FixedUpdate(); }
    }
}
