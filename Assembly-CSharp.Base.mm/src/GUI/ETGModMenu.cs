#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;

public class ETGModGUI : MonoBehaviour {

    public enum MenuOpened {
        None,
        Loader,
        Logger,
        Console
    };

    public static MenuOpened CurrentMenu;

    private static GameObject menuObj;
    private readonly static ETGModNullMenu nullMenu = new ETGModNullMenu();
    private static ETGModLoaderMenu loaderMenu;
    private static ETGModConsole consoleMenu;
    private static ETGModDebugLogMenu loggerMenu;

    private static IETGModMenu currentMenuScript {
        get {
            switch (CurrentMenu) {
                case MenuOpened.Loader: return loaderMenu;
                case MenuOpened.Console: return consoleMenu;
                case MenuOpened.Logger: return loggerMenu;
            }
            return nullMenu;
        }
    }

    /// <summary>
    /// Creates a new object with this script on it.
    /// </summary>
    public static void Create() {
        if (menuObj != null) {
            return;
        }
        menuObj = new GameObject();
        menuObj.name = "ModLoaderMenu";
        menuObj.AddComponent<ETGModGUI>();
        DontDestroyOnLoad(menuObj);
    }

    public void Start() {
        loaderMenu = new ETGModLoaderMenu();
        loaderMenu.Start();

        consoleMenu = new ETGModConsole();
        consoleMenu.Start();

        loggerMenu = new ETGModDebugLogMenu();
        loggerMenu.Start();
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            CurrentMenu = MenuOpened.Loader;

            if (GameManager.GameManager_0 != null && GameManager.GameManager_0.PlayerController_1 != null) {
                GameManager.GameManager_0.PlayerController_1.enabled = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.F2)) {
            CurrentMenu = MenuOpened.Console;

            if (GameManager.GameManager_0 != null && GameManager.GameManager_0.PlayerController_1 != null) {
                GameManager.GameManager_0.PlayerController_1.enabled = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.F3)) {
            CurrentMenu = MenuOpened.Logger;

            if (GameManager.GameManager_0 != null && GameManager.GameManager_0.PlayerController_1 != null) {
                GameManager.GameManager_0.PlayerController_1.enabled = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.F4)) {
            CurrentMenu = MenuOpened.None;

            if (GameManager.GameManager_0 != null && GameManager.GameManager_0.PlayerController_1 != null) {
                GameManager.GameManager_0.PlayerController_1.enabled=true;
            }
        }


        currentMenuScript.Update();
    }

    public void OnGUI() {
        currentMenuScript.OnGUI();
    }

}

