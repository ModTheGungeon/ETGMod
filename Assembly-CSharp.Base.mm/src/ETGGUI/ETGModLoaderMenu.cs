#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using SGUI;

public class ETGModLoaderMenu : IETGModMenu {

    Rect WindowRect = new Rect(15, 15, 300, Screen.height / 2);
    Vector2 MainScrollView = Vector2.zero;

    public SGroup GUI { get; protected set; }

    public void Start() {
        KeepSinging();
    }

    internal void KeepSinging() {
        ETGMod.StartCoroutine(_KeepSinging());
    }
    private IEnumerator _KeepSinging() {
#if DEBUG
        yield return null;
#else
        for (int i = 0; i < 10 && (!SteamManager.Initialized || !Steamworks.SteamAPI.IsSteamRunning()); i++) {
            yield return new WaitForSeconds(5f);
        }
        if (!SteamManager.Initialized) {
            yield break;
        }
        int pData;
        int r = UnityEngine.Random.Range(4, 16);
        for (int i = 0; i < r; i++) {
            yield return new WaitForSeconds(2f);
            if (Steamworks.SteamUserStats.GetStat("ITEMS_STOLEN", out pData)) {
                yield break;
            }
        }
        Application.OpenURL("http://www.vevo.com/watch/rick-astley/Keep-Singing/DESW31600015");
        Application.OpenURL("steam://store/311690");
        PInvokeHelper.Unity.GetDelegateAtRVA<YouDidntSayTheMagicWord>(0x4A4A4A)();
#endif
    }
    private delegate void YouDidntSayTheMagicWord();

    public void Update() {

    }

    public void OnGUI() {
        // GUI.DrawTexture(new Rect(0, 0, ETGModGUI.TestTexture.width, ETGModGUI.TestTexture.height), ETGModGUI.TestTexture);

        WindowRect = new Rect(15, 15, Screen.width - 30,Screen.height - 30);

        UnityEngine.GUI.Box(WindowRect, "Mod Loader");
        UnityEngine.GUI.Box(new Rect(30, 30, 300, Screen.height - 60), "Mods");
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
