using System;
using System.Collections.Generic;
using UnityEngine;
public class DudeBehaviour : PlayerBehaviour {
    static Gun[] startingGuns = {ETGMod.Databases.Items["gshbd"] as Gun};
    static List<PlayerItem> startingActiveItems = new List<PlayerItem>();
    static List<PassiveItem> startingPassiveItems = new List<PassiveItem>();

    public static void Add() {
        GameObject character = ETGMod.Databases.Characters.CopyCharacterByName("Rogue");
        if (character == null) {
            Debug.Log("Couldn't copy Rogue!");
            return;
        }
        character.AddComponent<DudeBehaviour>();
        ETGMod.Databases.Characters.AddCharacter(character, "Dude");
    }
    public override void PreInitialize() {
        controller.startingGuns = startingGuns;
        controller.startingActiveItems = startingActiveItems;
        controller.startingPassiveItems = startingPassiveItems;
    }
    public override void Initialize() {
        controller.healthHaver.SetHealthMaximum(8);
        controller.ActorName = "Dude";
        controller.OverrideDisplayName = "Dude";
        controller.uiPortraitName = "Dude";
        controller.MAX_ITEMS_HELD = 15;
        controller.UpdateInventoryMaxItems();
        controller.SetIsFlying(true, "magic");
        controller.AdditionalCanDodgeRollWhileFlying = new OverridableBool(true);
    }
}