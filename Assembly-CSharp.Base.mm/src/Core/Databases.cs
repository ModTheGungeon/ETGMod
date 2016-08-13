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

    public int Count {
        get {
            return PickupObjectDatabase.Instance.Objects.Count;
        }
    }

    public int Add(PickupObject value) {
        int id = PickupObjectDatabase.Instance.Objects.Count;
        if (value != null) {
            value.PickupObjectId = id;
        }
        PickupObjectDatabase.Instance.Objects.Add(value);
        return id;
    }

    private Gun _GunGivenPrototype;
    public Gun NewGun(string gunName) {
        if (_GunGivenPrototype == null) {
            _GunGivenPrototype = (Gun) PickupObjectDatabase.GetByName("Pea_Shooter");
        }

        return NewGun(gunName, _GunGivenPrototype);
    }
    public Gun NewGun(string gunName, string baseGun) {
        return NewGun(gunName, (Gun) PickupObjectDatabase.GetByName(baseGun));
    }
    public Gun NewGun(string gunName, Gun baseGun) {
        GameObject go = UnityEngine.Object.Instantiate(baseGun.gameObject);
        go.name = gunName.Replace(" ", "_");

        Gun gun = go.GetComponent<Gun>();
        SetupItem(gun, gunName);
        gun.gunName = gunName;
        gun.gunSwitchGroup = go.name;

        return gun;
    }

    public void SetupItem(PickupObject item, string name) {
        string nameKey = "#" + item.name.ToUpperInvariant();
        item.encounterTrackable.journalData.PrimaryDisplayName = nameKey;
        StringTableManager.StringCollection nameValue = new StringTableManager.SimpleStringCollection();
        nameValue.AddString(name, 0f);
        StringTableManager.ItemTable[nameKey] = nameValue;
    }

}
