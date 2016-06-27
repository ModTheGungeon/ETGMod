using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ModMenu: MonoBehaviour {

    public enum MenuOpened {
        None,
        Logger,
        Console,
        Loader
    };

    public static MenuOpened currentMenu;

    private static ModDebugLogMenu mainLogger;
    private static ModConsole mainConsole;
    private static ModLoaderMenu mainLoaderMenu;

    //Creates a new object with this script on it.
    public static void Create() {
        GameObject newObject = new GameObject();

        newObject.name="ModLoaderMenu";
        newObject.AddComponent<ModMenu>();
        DontDestroyOnLoad(newObject);
    }

    public void Start() {

        mainLogger=new ModDebugLogMenu();
        mainLogger.Start();

        mainConsole=new ModConsole();
        mainConsole.Start();

        mainLoaderMenu=new ModLoaderMenu();
        mainLoaderMenu.Start();


    }

    public void Update() {

        if (Input.GetKeyDown(KeyCode.F1)) {
            currentMenu=MenuOpened.Logger;

            if (GameManager.GameManager_0) {
                if (GameManager.GameManager_0.PlayerController_1) {
                    GameManager.GameManager_0.PlayerController_1.enabled=false;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.F2)) {
            currentMenu=MenuOpened.Console;

            if (GameManager.GameManager_0) {
                if (GameManager.GameManager_0.PlayerController_1) {
                    GameManager.GameManager_0.PlayerController_1.enabled=false;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.F3)) {
            currentMenu=MenuOpened.Loader;

            if (GameManager.GameManager_0) {
                if (GameManager.GameManager_0.PlayerController_1) {
                    GameManager.GameManager_0.PlayerController_1.enabled=false;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.F4)) {
            currentMenu=MenuOpened.None;

            if (GameManager.GameManager_0) {
                if (GameManager.GameManager_0.PlayerController_1) {
                    GameManager.GameManager_0.PlayerController_1.enabled=true;
                }
            }
        }


        if (currentMenu==MenuOpened.Logger)
            mainLogger.Update();
        else if (currentMenu==MenuOpened.Console)
            mainConsole.Update();
        else if (currentMenu==MenuOpened.Loader)
            mainLoaderMenu.Update();

    }

    public void OnGUI() {

        if (currentMenu==MenuOpened.Logger)
            mainLogger.OnGUI();
        else if (currentMenu==MenuOpened.Console)
            mainConsole.OnGUI();
        else if (currentMenu==MenuOpened.Loader)
            mainLoaderMenu.OnGUI();

    }





}

