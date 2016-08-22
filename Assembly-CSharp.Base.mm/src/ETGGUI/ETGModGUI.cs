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

    public static Texture2D TestTexture;
    public static Texture2D BoxTexture;

    public static GUISkin GuiSkin;

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
    }

    public static void Start() {
        _LoggerMenu.Start();
        _LoaderMenu.Start();
        _ConsoleMenu.Start();
        _InspectorMenu.Start();

        TestTexture = Resources.Load<Texture2D>("test/texture");
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            if (CurrentMenu == MenuOpened.Loader) {
                CurrentMenu = MenuOpened.None;
                _CurrentMenuScript.OnClose ();
            } else {
                CurrentMenu = MenuOpened.Loader;
                _CurrentMenuScript.OnOpen ();
            }

            UpdateTimeScale ();
            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.BackQuote)) {
            if (CurrentMenu == MenuOpened.Console) {
                CurrentMenu = MenuOpened.None;
                _CurrentMenuScript.OnClose ();
            } else {
                CurrentMenu = MenuOpened.Console;
                _CurrentMenuScript.OnOpen ();
            }

            UpdateTimeScale ();
            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F3)) {
            if (CurrentMenu == MenuOpened.Logger) {
                CurrentMenu = MenuOpened.None;
                _CurrentMenuScript.OnClose ();
            } else {
                CurrentMenu = MenuOpened.Logger;
                _CurrentMenuScript.OnOpen ();
            }

            UpdateTimeScale ();
            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F4)) {
            if (CurrentMenu == MenuOpened.Inspector) {
                CurrentMenu = MenuOpened.None;
                _CurrentMenuScript.OnClose ();
            } else {
                CurrentMenu = MenuOpened.Inspector;
                _CurrentMenuScript.OnOpen ();
            }

            UpdateTimeScale ();
            UpdatePlayerState();
        }


        _CurrentMenuScript.Update();
    }

    public static void UpdateTimeScale() {
        if (StoredTimeScale.HasValue) {
            Time.timeScale = (float)StoredTimeScale;
            StoredTimeScale = null;
        }
    }

    public static void UpdatePlayerState() {
        if (GameManager.Instance != null&&GameManager.Instance.PrimaryPlayer!=null) {
            bool set = (CurrentMenu == MenuOpened.None);
            GameManager.Instance.PrimaryPlayer.enabled = set;
            Camera.main.GetComponent<CameraController>().enabled = set;
        }
    }

    // Font f;

    public void OnGUI() {
        if (GuiSkin == null) {
            GuiSkin = GUI.skin;
            // GuiSkin.font = FontConverter.GetFontFromdfFont((dfFont) patch_MainMenuFoyerController.Instance.VersionLabel.Font, 2);
            /*float height = 26f;
            GuiSkin.label.fixedHeight = height;
            GuiSkin.button.fixedHeight = height;
            GuiSkin.toggle.fixedHeight = height;
            GuiSkin.textField.fixedHeight = height;
            GuiSkin.textField.alignment = TextAnchor.MiddleLeft;*/
        }
        GUI.skin = GuiSkin;

        if (ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.None) {
            if (!StoredTimeScale.HasValue) {
                StoredTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
                _CurrentMenuScript.OnClose ();
                ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None;
                ETGModGUI.UpdateTimeScale ();
                ETGModGUI.UpdatePlayerState ();
            }
        }

        _CurrentMenuScript.OnGUI();
        //RandomSelector.OnGUI();

    }

    internal static IEnumerator ListAllItemsAndGuns() {

        yield return new WaitForSeconds(1);

        int count = 0;

        while (PickupObjectDatabase.Instance == null)
            yield return new WaitForEndOfFrame();

        // TODO: bleh, foreach
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

