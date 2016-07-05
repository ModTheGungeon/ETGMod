#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;

public class ETGModLoaderMenu : IETGModMenu {

    Rect windowRect = new Rect(15,15,300,Screen.height/2);
    Vector2 mainScrollView=Vector2.zero;

    public void Start() {

    }

    public void Update() {

    }

    public void OnGUI() {

        windowRect=new Rect(15,15,Screen.width-30,Screen.height-30);

        GUI.Box(windowRect,"Mod Loader");
        GUI.Box(new Rect(30, 30, 300, Screen.height-60), "Mods");
        GUILayout.BeginArea(windowRect);
        GUISelector();
        GUILayout.EndArea();
    }

    public void OnDestroy() {

    }

    //Mod selector
    public void GUISelector() {
        GUILayout.BeginArea(new Rect(15,15,windowRect.width-30,windowRect.height-30));
        mainScrollView=GUILayout.BeginScrollView(mainScrollView);

        foreach (ETGModule asset in ETGMod.GameMods) {

            GUILayout.Label(asset.Metadata.Name);

        }

        GUILayout.EndScrollView();
    }

}
