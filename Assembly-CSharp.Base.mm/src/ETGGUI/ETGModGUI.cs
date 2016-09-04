#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ETGGUI;
using System;

public class ETGModGUI : MonoBehaviour {

    public enum MenuOpened {
        None,
        Loader,
        Logger,
        Console,
        Inspector
    };

    private static MenuOpened _CurrentMenu = MenuOpened.None;
    public static MenuOpened CurrentMenu {
        get {
            return _CurrentMenu;
        }
        set {
            bool change = _CurrentMenu != value;
            if (change) {
                CurrentMenuInstance.OnClose();
            }
            _CurrentMenu = value;
            if (change) {
                CurrentMenuInstance.OnOpen();
                UpdateTimeScale();
                UpdatePlayerState();
            }
        }
    }

    public static GameObject MenuObject;
    public readonly static ETGModNullMenu NullMenu = new ETGModNullMenu();
    public static ETGModLoaderMenu LoaderMenu;
    public static ETGModConsole ConsoleMenu;
    public static ETGModDebugLogMenu LoggerMenu;
    public static ETGModInspector InspectorMenu;

    public static float? StoredTimeScale = null;

    public static bool UseDamageIndicators = false;

    public static Texture2D TestTexture;

    public static IETGModMenu CurrentMenuInstance {
        get {
            switch (CurrentMenu) {
                case MenuOpened.Loader:
                    return LoaderMenu;
                case MenuOpened.Console:
                    return ConsoleMenu;
                case MenuOpened.Logger:
                    return LoggerMenu;
                case MenuOpened.Inspector:
                    return InspectorMenu;
            }
            return NullMenu;
        }
    }

    /// <summary>
    /// Creates a new object with this script on it.
    /// </summary>
    public static void Create() {
        if (MenuObject != null) {
            return;
        }
        MenuObject = new GameObject();
        MenuObject.name = "ModLoaderMenu";
        MenuObject.AddComponent<ETGModGUI>();
        DontDestroyOnLoad(MenuObject);
    }

    public void Awake() {
        LoggerMenu = new ETGModDebugLogMenu();
        LoaderMenu = new ETGModLoaderMenu();
        ConsoleMenu = new ETGModConsole();
        InspectorMenu = new ETGModInspector();

        ETGDamageIndicatorGUI.Create();
    }

    public static void Start() {
        TestTexture = Resources.Load<Texture2D>("test/texture");

        LoggerMenu.Start();
        LoaderMenu.Start();
        ConsoleMenu.Start();
        InspectorMenu.Start();
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            if (CurrentMenu == MenuOpened.Loader) {
                CurrentMenuInstance.OnClose();
                CurrentMenu = MenuOpened.None;
            } else {
                CurrentMenu = MenuOpened.Loader;
                CurrentMenuInstance.OnOpen ();
            }

            UpdateTimeScale ();
            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.BackQuote)) {
            if (CurrentMenu == MenuOpened.Console) {
                CurrentMenu = MenuOpened.None;
            } else {
                CurrentMenu = MenuOpened.Console;
            }
        }

        if (Input.GetKeyDown(KeyCode.F3)) {
            if (CurrentMenu == MenuOpened.Logger) {
                CurrentMenu = MenuOpened.None;
            } else {
                CurrentMenu = MenuOpened.Logger;
            }
        }

        if (Input.GetKeyDown(KeyCode.F4)) {
            if (CurrentMenu == MenuOpened.Inspector) {
                CurrentMenuInstance.OnClose();
                CurrentMenu = MenuOpened.None;
            } else {
                CurrentMenu = MenuOpened.Inspector;
                CurrentMenuInstance.OnOpen ();
            }
        }

        if (CurrentMenu != MenuOpened.None && Input.GetKeyDown(KeyCode.Escape)) {
            CurrentMenu = MenuOpened.None;
        }


        CurrentMenuInstance.Update();
    }

    public static void UpdateTimeScale() {
        if (StoredTimeScale.HasValue) {
            Time.timeScale = (float)StoredTimeScale;
            StoredTimeScale = null;
        }
    }

    public static void UpdatePlayerState() {
        if (GameManager.Instance?.PrimaryPlayer != null) {
            bool set = CurrentMenu == MenuOpened.None;
            GameManager.Instance.PrimaryPlayer.enabled = set;
            CameraController cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null) {
                cam.enabled = set;
            }
        }
    }

    // Font f;

    public void OnGUI() {
        if (ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.None) {
            if (!StoredTimeScale.HasValue) {
                StoredTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }
        }

        CurrentMenuInstance.OnGUI();
        //RandomSelector.OnGUI();

    }

    internal static IEnumerator ListAllItemsAndGuns() {

        yield return new WaitForSeconds(1);

        int count = 0;

        while (PickupObjectDatabase.Instance == null)
            yield return new WaitForEndOfFrame();

        for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++) {
            PickupObject obj = PickupObjectDatabase.Instance.Objects[i];

            if (obj==null) 
                continue;
            if (obj.encounterTrackable==null)
                continue;
            if (obj.encounterTrackable.journalData==null)
                continue;

            string name = obj.encounterTrackable.journalData.GetPrimaryDisplayName(true).Replace(' ', '_').ToLower();

            count++;

            // Handle Master Rounds specially because we actually care about the order
            if (name == "master_round") {
                string objectname = obj.gameObject.name;
                int floornumber = 420;
                switch (objectname.Substring("MasteryToken_".Length)) {
                case "Castle": // Keep of the Lead Lord
                    floornumber = 1;
                    break;
                case "Gungeon": // Gungeon Proper
                    floornumber = 2;
                    break;
                case "Mines":
                    floornumber = 3;
                    break;
                case "Catacombs": // Hollow
                    floornumber = 4;
                    break;
                case "Forge":
                    floornumber = 5;
                    break;
                }
                name = name + "_" + floornumber;
            }



            if (ETGModConsole.AllItems.ContainsKey(name)) {
                int appendindex = 2;
                while (ETGModConsole.AllItems.ContainsKey (name + "_" + appendindex.ToString())) {
                    appendindex++;
                }
                name = name + "_" + appendindex.ToString ();
            }
            ETGModConsole.AllItems.Add(name, i);
            if (count >= 30) {
                count = 0;
                yield return null;
            }
        }

        //Add command arguments.
        Debug.Log(ETGModConsole.AllItems.Values.Count + " give command args");

    }

}

