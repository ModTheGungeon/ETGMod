#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ETGGUI;
using System;
using Object = UnityEngine.Object;

public class ETGModGUI : MonoBehaviour {

    public enum MenuOpened {
        None,
        Loader,
        Logger,
        Console,
        Inspector
    };

    public static MenuOpened CurrentMenu;

    public static GameObject MenuObject;
    private readonly static ETGModNullMenu _NullMenu = new ETGModNullMenu();
    private static ETGModLoaderMenu _LoaderMenu;
    private static ETGModConsole _ConsoleMenu;
    private static ETGModDebugLogMenu _LoggerMenu;
    private static ETGModInspector _InspectorMenu;

    public static float? StoredTimeScale = null;

    public static bool UseDamageIndicators = false;

    public static Texture2D BoxTexture;

    public static Texture2D TestTexture;

    private static IETGModMenu _CurrentMenuScript {
        get {
            switch (CurrentMenu) {
                case MenuOpened.Loader:
                    return _LoaderMenu;
                case MenuOpened.Console:
                    return _ConsoleMenu;
                case MenuOpened.Logger:
                    return _LoggerMenu;
                case MenuOpened.Inspector:
                    return _InspectorMenu;

            }
            return _NullMenu;
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
        BoxTexture = new Texture2D(1,1);
        BoxTexture.SetPixel(0,0,Color.white);
        BoxTexture.Apply();

        _LoggerMenu = new ETGModDebugLogMenu();
        _LoaderMenu = new ETGModLoaderMenu();
        _ConsoleMenu = new ETGModConsole();
        _InspectorMenu = new ETGModInspector();

        ETGDamageIndicatorGUI.Create();
        StartCoroutine(ListAllItemsAndGuns());
    }

    public static void Start() {
        TestTexture = Resources.Load<Texture2D>("Test/Texture");

        _LoggerMenu.Start();
        _LoaderMenu.Start();
        _ConsoleMenu.Start();
        _InspectorMenu.Start();

        tk2dButton x = new tk2dButton ();
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            if (CurrentMenu == MenuOpened.Loader)
                CurrentMenu = MenuOpened.None;
            else
                CurrentMenu = MenuOpened.Loader;

            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F2)||Input.GetKeyDown(KeyCode.Slash)||Input.GetKeyDown(KeyCode.BackQuote)) {
            if (CurrentMenu == MenuOpened.Console)
                CurrentMenu = MenuOpened.None;
            else
                CurrentMenu = MenuOpened.Console;

            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F3)) {
            if (CurrentMenu == MenuOpened.Logger)
                CurrentMenu = MenuOpened.None;
            else
                CurrentMenu = MenuOpened.Logger;

            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F4)) {
            if (CurrentMenu == MenuOpened.Inspector)
                CurrentMenu = MenuOpened.None;
            else
                CurrentMenu = MenuOpened.Inspector;

            UpdatePlayerState();
        }


        _CurrentMenuScript.Update();
    }

    public static void UpdatePlayerState() {
        if (GameManager.GameManager_0 != null&&GameManager.GameManager_0.PlayerController_1!=null) {
            bool set = (CurrentMenu == MenuOpened.None);
            GameManager.GameManager_0.PlayerController_1.enabled = set;
            Camera.main.GetComponent<CameraController>().enabled = set;
            if (StoredTimeScale.HasValue) {
                Time.timeScale = (float)StoredTimeScale;
                StoredTimeScale = null;
            }
        }
    }

    // Font f;

    public void OnGUI() {

        //GUI.skin.font=f;


        if (ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.None) {
            if (!StoredTimeScale.HasValue) {
                StoredTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
                ETGModGUI.CurrentMenu=ETGModGUI.MenuOpened.None;
                ETGModGUI.UpdatePlayerState();
            }
        }

        _CurrentMenuScript.OnGUI();
        //RandomSelector.OnGUI();

    }

    IEnumerator ListAllItemsAndGuns() {

        yield return new WaitForSeconds(1);

        int count = 0;

        while (PickupObjectDatabase.Instance==null)
            yield return new WaitForEndOfFrame();

        // TODO: bleh, foreach
        foreach(PickupObject obj in PickupObjectDatabase.Instance.Objects) {

            if (obj==null) 
                continue;
            if (obj.EncounterTrackable_0==null)
                continue;
            if (obj.EncounterTrackable_0.JournalEntry_0==null)
                continue;
            
            string name = obj.EncounterTrackable_0.JournalEntry_0.method_2(true).Replace(' ', '_').ToLower();
            int id = PickupObjectDatabase.Instance.Objects.IndexOf(obj);

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
            ETGModConsole.AllItems.Add(name, id);
            if (count >= 30) {
                count = 0;
                yield return null;
            }
        }

        //Add command arguments.
        string[][] giveCommands = new string[1][];
        giveCommands[0] = new string[ETGModConsole.AllItems.Keys.Count];
        ETGModConsole.AllItems.Keys.CopyTo(giveCommands[0], 0);

        Debug.Log(giveCommands[0].Length + " give command args");

        ETGModConsole.Commands["give"].AcceptedArguments = giveCommands;

    }

}

