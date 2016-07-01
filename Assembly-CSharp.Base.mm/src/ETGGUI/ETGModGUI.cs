#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ETGGUI;

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

    public void Start() {
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

        // sorry for the if block
        if (ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.None) {
            if (!timeScale.HasValue && (ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.None)) {
                timeScale = Time.timeScale;
                Time.timeScale = 0;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
                ETGModGUI.CurrentMenu=ETGModGUI.MenuOpened.None;
                ETGModGUI.UpdatePlayerState();
            }
        }

        currentMenuScript.OnGUI();

    }

    IEnumerator ListAllItemsAndGuns() {

        yield return new WaitForSeconds(1);

        int count = 0;

        while (PickupObjectDatabase.Instance==null)
            yield return new WaitForEndOfFrame();

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

            if (ETGModConsole.allItems.ContainsKey(name)) {
                name=obj.gameObject.name.Replace(' ', '_').ToLower();
            }
            ETGModConsole.allItems.Add(name, id);
            if (count>=30) {
                yield return new WaitForEndOfFrame();
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

