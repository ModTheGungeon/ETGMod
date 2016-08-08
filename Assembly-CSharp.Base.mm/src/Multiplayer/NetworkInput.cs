using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;
using InControl;
using UnityEngine;
using ETGMultiplayer;

class NetworkInput {

    public static float[] directions = new float[4];

    public static Position SyncPos;

    public static string displayBytesSent, displayBytesRecieved;

    public static void SendUpdatePacket(BraveInput toSend) {
        if (!SteamManager.Initialized)
            return;

        if (!GameManager.Instance.PrimaryPlayer)
            return;

        displayBytesSent="";
        displayBytesSent+=toSend.ActiveActions.Right.Value+"\n";
        displayBytesSent+=toSend.ActiveActions.Left.Value+"\n";
        displayBytesSent+=toSend.ActiveActions.Up.Value+"\n";
        displayBytesSent+=toSend.ActiveActions.Down.Value+"\n";

        PacketHelper.SendRPCToPlayersInGame("NetInput", new Vector4(toSend.ActiveActions.Right.Value, toSend.ActiveActions.Left.Value, toSend.ActiveActions.Up.Value, toSend.ActiveActions.Down.Value), GameManager.Instance.PrimaryPlayer.specRigidbody.Position.PixelPosition.X, GameManager.Instance.PrimaryPlayer.specRigidbody.Position.PixelPosition.Y );
    }

    [CustomRPC("NetInput")]
    public static void RecieveUpdatePacket(Vector4 dir, int x, int y) {

        displayBytesRecieved="";

        for (int i = 0; i<4; i++) {
            directions[i]=dir[i];

            displayBytesRecieved+=dir[i]+"\n";
        }

        displayBytesRecieved+=x+"\n";
        displayBytesRecieved+=y+"\n";

        if (GameManager.Instance.SecondaryPlayer) {
            GameManager.Instance.SecondaryPlayer.specRigidbody.Position = new Position(x,y);
            displayBytesRecieved+=GameManager.Instance.SecondaryPlayer.specRigidbody.Position.X+"\n";
            displayBytesRecieved+=GameManager.Instance.SecondaryPlayer.specRigidbody.Position.Y+"\n";
        }
    }

    [CustomRPC("SelectPlayer")]
    public static void NetSelectPlayer(string prefabName) {
        patch_Foyer.IsNetSelect=true;
        ETGMod.Player.CoopReplacement=prefabName;
        PlayerController newPlayer = GeneratePlayer(GetCoopSelectFlag());

        GameManager.Instance.CurrentGameType=GameManager.GameType.COOP_2_PLAYER;
        if (GameManager.Instance.PrimaryPlayer) {
            GameManager.Instance.PrimaryPlayer.ReinitializeMovementRestrictors();
        }

        GameUIRoot.Instance.ConvertCoreUIToCoopMode();
        PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(newPlayer.specRigidbody, 2147483647);
        GameManager.Instance.MainCameraController.ClearPlayerCache();
        Foyer.Instance.ProcessPlayerEnteredFoyer(newPlayer);

        BraveInput.ReassignAllControllers(InputDevice.Null);
        if (Foyer.Instance.OnCoopModeChanged!=null) {
            Foyer.Instance.OnCoopModeChanged();
        }
    }

    public static FoyerCharacterSelectFlag GetCoopSelectFlag() {
        foreach (FoyerCharacterSelectFlag f in GameObject.FindObjectsOfType<FoyerCharacterSelectFlag>())
            if (f.IsCoopCharacter)
                return f;
        return GameObject.FindObjectsOfType<FoyerCharacterSelectFlag>().Last();
    }

    private static PlayerController GeneratePlayer(FoyerCharacterSelectFlag Owner) {
        if (GameManager.Instance.SecondaryPlayer!=null) {
            return GameManager.Instance.SecondaryPlayer;
        }
        GameManager.Instance.ClearSecondaryPlayer();
        GameManager.LastUsedCoopPlayerPrefab=(GameObject)Resources.Load("PlayerCoopCultist");
        PlayerController playerController = null;
        if (playerController==null) {
            GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(GameManager.LastUsedCoopPlayerPrefab, new Vector3(25,25), Quaternion.identity);
            gameObject.SetActive(true);
            playerController=gameObject.GetComponent<PlayerController>();
        }
        FoyerCharacterSelectFlag component = Owner.GetComponent<FoyerCharacterSelectFlag>();
        if (component&&component.IsAlternateCostume) {
            playerController.SwapToAlternateCostume();
        }
        GameManager.Instance.SecondaryPlayer=playerController;
        playerController.PlayerIDX=1;
        return playerController;
    }

}

