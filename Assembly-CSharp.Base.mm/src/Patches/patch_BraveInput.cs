using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using InControl;

class patch_BraveInput : BraveInput {

    [MonoMod.MonoModIgnore]
    public GungeonActions m_activeGungeonActions;

    [MonoMod.MonoModIgnore]
    public int m_playerID;

    public extern void orig_AssignActionsDevice();
    public void AssignActionsDevice() {
        orig_AssignActionsDevice();
    }

    public static extern BraveInput orig_GetInstanceForPlayer(int id);
    public static patch_BraveInput GetInstanceForPlayer(int id) {
        return (patch_BraveInput)orig_GetInstanceForPlayer(id);
    }

    private extern void orig_CheckForActionInitialization();
    public void CheckForActionInitialization() {

        if (!MultiplayerManager.isPlayingMultiplayer) {
            if (m_activeGungeonActions==null) {
                this.m_activeGungeonActions=new GungeonActions();
                this.m_activeGungeonActions.InitializeDefaults();
                this.AssignActionsDevice();
                if (!GameManager.PreventGameManagerExistence&&( !( GameManager.Instance.PrimaryPlayer==null )||this.m_playerID!=0 )) {
                    if (this.m_playerID!=GameManager.Instance.PrimaryPlayer.PlayerIDX) {
                        if (!string.IsNullOrEmpty(GameManager.Options.playerTwoBindingData)) {
                            this.ActiveActions.Load(GameManager.Options.playerTwoBindingData);
                            goto IL_AF;
                        }
                        goto IL_AF;
                    }
                }
                if (!string.IsNullOrEmpty(GameManager.Options.playerOneBindingData)) {
                    this.ActiveActions.Load(GameManager.Options.playerOneBindingData);
                }
                IL_AF:
                if (!GameManager.PreventGameManagerExistence&&GameManager.Instance.CurrentGameType==GameManager.GameType.COOP_2_PLAYER) {

                } else {
                    if (this.m_playerID==0&&GetInstanceForPlayer(GameManager.Instance.SecondaryPlayer.PlayerIDX).m_activeGungeonActions==null) {
                        BraveInput.GetInstanceForPlayer(GameManager.Instance.SecondaryPlayer.PlayerIDX).CheckForActionInitialization();
                    }
                    if (this.m_activeGungeonActions.Device==null) {
                        this.m_activeGungeonActions.IgnoreBindingsOfType(BindingSourceType.DeviceBindingSource);
                    } else if (this.m_playerID==GameManager.Instance.PrimaryPlayer.PlayerIDX) {
                        if (GetInstanceForPlayer(GameManager.Instance.SecondaryPlayer.PlayerIDX).m_activeGungeonActions.Device==null) {
                            this.m_activeGungeonActions.IgnoreBindingsOfType(BindingSourceType.KeyBindingSource);
                            this.m_activeGungeonActions.IgnoreBindingsOfType(BindingSourceType.MouseBindingSource);
                        }
                    } else {
                        this.m_activeGungeonActions.IgnoreBindingsOfType(BindingSourceType.KeyBindingSource);
                        this.m_activeGungeonActions.IgnoreBindingsOfType(BindingSourceType.MouseBindingSource);
                    }
                }

            }

            this.AssignActionsDevice();

        } else {
            //If we're playing multiplayer, we're going to go ahead and replace the secondary player's input with custom input gotten from the network.

            if (GameManager.Instance.SecondaryPlayer) {
                if (this.m_playerID==GameManager.Instance.SecondaryPlayer.PlayerIDX) {

                    if (MultiplayerActions.baseActions==null)
                        new MultiplayerActions();
                    ( (patch_PlayerController)GameManager.Instance.SecondaryPlayer ).m_activeActions=MultiplayerActions.baseActions;

                }
            }
        }
    }
}

