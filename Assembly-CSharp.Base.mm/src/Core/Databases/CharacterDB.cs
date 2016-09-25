using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;
public class CharacterDB {
    Dictionary<string, GameObject> _ModCharacters = new Dictionary<string, GameObject>();
    public GameObject GetModCharacterByName (string name) {
        if (_ModCharacters.ContainsKey(name)) {
            return _ModCharacters[name];
        }
        return null;
    }

    public GameObject CopyCharacterByName (string name) {
        GameObject prefab = (GameObject)Resources.Load("CHARACTERDB:" + name);
        if (prefab == null) {
            prefab = (GameObject)Resources.Load("Player" + name);
        }
        if (prefab == null) {
            Debug.Log("Uh oh! CopyCharacterByName couldn't find the prefab. This may cause issues.");
            return null;
        }
        prefab.SetActive(false);
        GameObject copy = UnityEngine.Object.Instantiate(prefab);
        prefab.SetActive(true);
        UnityEngine.Object.DontDestroyOnLoad(copy);
        copy.GetComponent<PlayerController>().OverrideDisplayName = "FAKE PREFAB FOR " + name;

        return copy;
    }
    public void AddCharacter (GameObject fakeprefab, string name) {
        _ModCharacters[name] = fakeprefab;
    }
}
