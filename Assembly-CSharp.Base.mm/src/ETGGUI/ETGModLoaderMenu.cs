#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;

public class ETGModLoaderMenu : IETGModMenu {

    Rect WindowRect = new Rect(15, 15, 300, Screen.height / 2);
    Vector2 MainScrollView = Vector2.zero;

    public void Start() {

    }

    public void Update() {

    }

    public void OnGUI() {
        // GUI.DrawTexture(new Rect(0, 0, ETGModGUI.TestTexture.width, ETGModGUI.TestTexture.height), ETGModGUI.TestTexture);

        WindowRect = new Rect(15, 15, Screen.width - 30,Screen.height - 30);

        GUI.Box(WindowRect, "Mod Loader");
        GUI.Box(new Rect(30, 30, 300, Screen.height - 60), "Mods");
        GUILayout.BeginArea(WindowRect);
        GUISelector();
        GUILayout.EndArea();
    }

    public void OnOpen() { }

    public void OnClose() { }

    public void OnDestroy() { }

    //Mod selector
    public void GUISelector() {
        GUILayout.BeginArea(new Rect(15, 15, WindowRect.width - 30, WindowRect.height - 30));
        MainScrollView=GUILayout.BeginScrollView(MainScrollView);

        foreach (ETGModule asset in ETGMod.GameMods) {

            GUILayout.Label(asset.Metadata.Name);

        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

}
