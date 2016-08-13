using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod database configuration.
    /// </summary>
    public static class Databases {

        public readonly static ItemDB Items = new ItemDB();

    }

}

public sealed class ItemDB {

    public PickupObject this[int id] {
        get {
            return PickupObjectDatabase.Instance.InternalGetById(id);
        }
        set {
            PickupObject old = PickupObjectDatabase.Instance.Objects[id];
            if (old != null) {
                old.PickupObjectId = -1;
            }
            if (value != null) {
                value.PickupObjectId = id;
                value.gameObject.SetActive(true);
            }
            PickupObjectDatabase.Instance.Objects[id] = value;
        }
    }

    public int Add(PickupObject value) {
        int id = PickupObjectDatabase.Instance.Objects.Count;
        if (value != null) {
            value.PickupObjectId = id;
            value.gameObject.SetActive(true);
        }
        PickupObjectDatabase.Instance.Objects.Add(value);
        return id;
    }

    public GameObject NewPrototype(string name = "") {
        GameObject go = new GameObject(name);
        go.SetActive(false);

        tk2dSprite sprite = go.AddComponent<tk2dSprite>();

        tk2dSpriteAnimator spriteAnim = go.AddComponent<tk2dSpriteAnimator>();

        return go;
    }

    public PickupObject NewItemPrototype(string name) {
        GameObject go = NewPrototype(name);

        PickupObject item = go.AddComponent<PickupObject>();
        SetupItem(item, name);

        return item;
    }

    public Gun NewGunPrototype(string gunName, GunClass gunClass) {
        GameObject go = NewPrototype(gunName);

        Gun gun = go.AddComponent<Gun>();
        gun.gunName = gunName;
        gun.gunClass = gunClass;
        SetupItem(gun, gunName);

        return gun;
    }

    public void SetupItem(PickupObject item, string name) {
        string nameKey = "#ITEM_" + name.ToUpperInvariant();
        item.encounterTrackable = new EncounterTrackable();
        item.encounterTrackable.journalData = new JournalEntry();
        item.encounterTrackable.journalData.PrimaryDisplayName = nameKey;
        StringTableManager.StringCollection nameValue = new StringTableManager.SimpleStringCollection();
        nameValue.AddString(name, 0f);
        StringTableManager.ItemTable.Add(nameKey, nameValue);
    }

}
