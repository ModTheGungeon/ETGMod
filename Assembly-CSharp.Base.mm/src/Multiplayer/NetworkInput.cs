using System.Linq;
using System.Text;
using InControl;
using UnityEngine;
using ETGMultiplayer;

public class NetworkInput {

    public static float[] Directions = new float[4];
    public static Position SyncPos;
    public static string DisplayBytesSent, DisplayBytesRecieved;

    public static void SendUpdatePacket(BraveInput toSend) {
        if (!SteamManager.Initialized || !GameManager.Instance.PrimaryPlayer) return;

        StringBuilder builder = new StringBuilder();
        builder.Append(toSend.ActiveActions.Right.Value).AppendLine();
        builder.Append(toSend.ActiveActions.Left.Value).AppendLine();
        builder.Append(toSend.ActiveActions.Up.Value).AppendLine();
        builder.Append(toSend.ActiveActions.Down.Value).AppendLine();
        DisplayBytesSent = builder.ToString();

        PacketHelper.SendRPCToPlayersInGame(
            "NetInput",
            new Vector4(
                toSend.ActiveActions.Right.Value,
                toSend.ActiveActions.Left.Value,
                toSend.ActiveActions.Up.Value,
                toSend.ActiveActions.Down.Value
            ),
            GameManager.Instance.PrimaryPlayer.specRigidbody.Position.PixelPosition.X,
            GameManager.Instance.PrimaryPlayer.specRigidbody.Position.PixelPosition.Y
        );
    }

    [CustomRPC("NetInput")]
    public static void RecieveUpdatePacket(Vector4 dir, int x, int y) {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < 4; i++) {
            float dirValue = dir[i];
            Directions[i] = dirValue;
            builder.Append(dirValue).AppendLine();
        }

        builder.Append(x).AppendLine();
        builder.Append(y).AppendLine();

        if (GameManager.Instance.SecondaryPlayer) {
            GameManager.Instance.SecondaryPlayer.specRigidbody.Position = new Position(x, y);
            builder.Append(GameManager.Instance.SecondaryPlayer.specRigidbody.Position.X).AppendLine();
            builder.Append(GameManager.Instance.SecondaryPlayer.specRigidbody.Position.Y).AppendLine();
        }

        DisplayBytesRecieved = builder.ToString();
    }

    [CustomRPC("SelectPlayer")]
    public static void NetSelectPlayer(string prefabName) {
        patch_Foyer.IsNetSelect = true;
        ETGMod.Player.CoopReplacement = prefabName;
        PlayerController newPlayer = GeneratePlayer(GetCoopSelectFlag());

        GameManager.Instance.CurrentGameType = GameManager.GameType.COOP_2_PLAYER;
        if (GameManager.Instance.PrimaryPlayer) {
            GameManager.Instance.PrimaryPlayer.ReinitializeMovementRestrictors();
        }

        GameUIRoot.Instance.ConvertCoreUIToCoopMode();
        PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(newPlayer.specRigidbody);
        GameManager.Instance.MainCameraController.ClearPlayerCache();
        Foyer.Instance.ProcessPlayerEnteredFoyer(newPlayer);

        BraveInput.ReassignAllControllers(InputDevice.Null);
        if (Foyer.Instance.OnCoopModeChanged != null) {
            Foyer.Instance.OnCoopModeChanged();
        }
    }

    public static FoyerCharacterSelectFlag GetCoopSelectFlag() {
        foreach (FoyerCharacterSelectFlag f in Object.FindObjectsOfType<FoyerCharacterSelectFlag>())
            if (f.IsCoopCharacter) return f;
        return Object.FindObjectsOfType<FoyerCharacterSelectFlag>().Last();
    }

    private static PlayerController GeneratePlayer(FoyerCharacterSelectFlag Owner) {
        if (GameManager.Instance.SecondaryPlayer != null) {
            return GameManager.Instance.SecondaryPlayer;
        }
        GameManager.Instance.ClearSecondaryPlayer();
        GameManager.LastUsedCoopPlayerPrefab = (GameObject) Resources.Load("PlayerCoopCultist");
        PlayerController playerController = null;
        if (playerController==null) {
            GameObject gameObject = (GameObject) Object.Instantiate(GameManager.LastUsedCoopPlayerPrefab, new Vector3(25f, 25f), Quaternion.identity);
            gameObject.SetActive(true);
            playerController = gameObject.GetComponent<PlayerController>();
        }
        FoyerCharacterSelectFlag component = Owner.GetComponent<FoyerCharacterSelectFlag>();
        if (component&&component.IsAlternateCostume) {
            playerController.SwapToAlternateCostume();
        }
        GameManager.Instance.SecondaryPlayer = playerController;
        playerController.PlayerIDX = 1;
        return playerController;
    }

}

