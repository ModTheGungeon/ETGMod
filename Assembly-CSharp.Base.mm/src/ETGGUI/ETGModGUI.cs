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

    public static GameObject menuObj;
    private readonly static ETGModNullMenu nullMenu = new ETGModNullMenu();
    private static ETGModLoaderMenu loaderMenu;
    private static ETGModConsole consoleMenu;
    private static ETGModDebugLogMenu loggerMenu;
    private static ETGModInspector inspectorMenu;

    public static float? timeScale = null;

    public static bool UseDamageIndicators = false;

    public static Texture2D BoxTexture;

    private static IETGModMenu currentMenuScript {
        get {
            switch (CurrentMenu) {
                case MenuOpened.Loader:
                    return loaderMenu;
                case MenuOpened.Console:
                    return consoleMenu;
                case MenuOpened.Logger:
                    return loggerMenu;
                case MenuOpened.Inspector:
                    return inspectorMenu;

            }
            return nullMenu;
        }
    }

    /// <summary>
    /// Creates a new object with this script on it.
    /// </summary>
    public static void Create() {
        if (menuObj!=null) {
            return;
        }
        menuObj=new GameObject();
        menuObj.name="ModLoaderMenu";
        menuObj.AddComponent<ETGModGUI>();
        DontDestroyOnLoad(menuObj);
    }

    public void Awake() {

        BoxTexture=new Texture2D(1,1);
        BoxTexture.SetPixel(0,0,Color.white);
        BoxTexture.Apply();

        loggerMenu=new ETGModDebugLogMenu();
        loggerMenu.Start();

        loaderMenu=new ETGModLoaderMenu();
        loaderMenu.Start();

        consoleMenu=new ETGModConsole();
        consoleMenu.Start();

        inspectorMenu=new ETGModInspector();
        inspectorMenu.Start();

        ETGDamageIndicatorGUI.Create();
        StartCoroutine(ListAllItemsAndGuns());
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            if (CurrentMenu==MenuOpened.Loader)
                CurrentMenu=MenuOpened.None;
            else
                CurrentMenu=MenuOpened.Loader;

            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F2)||Input.GetKeyDown(KeyCode.Slash)||Input.GetKeyDown(KeyCode.BackQuote)) {
            if (CurrentMenu==MenuOpened.Console)
                CurrentMenu=MenuOpened.None;
            else
                CurrentMenu=MenuOpened.Console;

            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F3)) {
            if (CurrentMenu==MenuOpened.Logger)
                CurrentMenu=MenuOpened.None;
            else
                CurrentMenu=MenuOpened.Logger;

            UpdatePlayerState();
        }

        if (Input.GetKeyDown(KeyCode.F4)) {
            if (CurrentMenu==MenuOpened.Inspector)
                CurrentMenu=MenuOpened.None;
            else
                CurrentMenu=MenuOpened.Inspector;

            UpdatePlayerState();
        }


        currentMenuScript.Update();
    }

    public static void UpdatePlayerState() {
        if (GameManager.GameManager_0!=null&&GameManager.GameManager_0.PlayerController_1!=null) {
            bool set = CurrentMenu==MenuOpened.None;
            GameManager.GameManager_0.PlayerController_1.enabled=set;
            Camera.main.GetComponent<CameraController>().enabled=set;
            if (timeScale.HasValue) {
                Time.timeScale = (float)timeScale;
                timeScale = null;
            }
        }
    }

    Font f;

    public void OnGUI() {

        //GUI.skin.font=f;


        if (ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.None) {
            if (!timeScale.HasValue) {
                timeScale = Time.timeScale;
                Time.timeScale = 0;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
                ETGModGUI.CurrentMenu=ETGModGUI.MenuOpened.None;
                ETGModGUI.UpdatePlayerState();
            }
        }

        currentMenuScript.OnGUI();
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



            if (ETGModConsole.allItems.ContainsKey(name)) {
                int appendindex = 2;
                while (ETGModConsole.allItems.ContainsKey (name + "_" + appendindex.ToString())) {
                    appendindex++;
                }
                name = name + "_" + appendindex.ToString ();
            }
            ETGModConsole.allItems.Add(name, id);
            if (count>=30) {
                count=0;
                yield return null;
            }
        }

        //Add command arguments.
        string[][] giveCommands = new string[1][];
        giveCommands[0]=new string[ETGModConsole.allItems.Keys.Count];
        ETGModConsole.allItems.Keys.CopyTo(giveCommands[0], 0);

        Debug.Log(giveCommands[0].Length+" give command args");

        ETGModConsole.Commands["give"].acceptedArguments=giveCommands;

    }

}

