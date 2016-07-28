using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;
using InControl;
using UnityEngine;

class NetworkInput {

    public static float[] directions = new float[4];

    public static string displayBytesSent,displayBytesRecieved;

    public static void SendUpdatePacket(BraveInput toSend) {

        byte[] totalArray = new byte[sizeof(float)*4];

        BitConverter.GetBytes(toSend.ActiveActions.Up.Value)   .CopyTo(totalArray, 0);
        BitConverter.GetBytes(toSend.ActiveActions.Right.Value).CopyTo(totalArray, 4);
        BitConverter.GetBytes(toSend.ActiveActions.Down.Value) .CopyTo(totalArray, 8);
        BitConverter.GetBytes(toSend.ActiveActions.Left.Value) .CopyTo(totalArray, 12);

        displayBytesSent = "";
        foreach (byte b in totalArray)
            displayBytesSent+=b;

        PacketHelper.SendPacketToPlayer(Steamworks.SteamUser.GetSteamID(),"NetInput",totalArray);
    }

    public static void RecieveUpdatePacket(byte[] data) {

        Debug.Log("Player input packet gotten");

        directions[0]=BitConverter.ToSingle(data, 0);
        directions[1]=BitConverter.ToSingle(data, 4);
        directions[2]=BitConverter.ToSingle(data, 8);
        directions[3]=BitConverter.ToSingle(data, 12);

        displayBytesRecieved="";
        foreach (byte b in data)
            displayBytesRecieved+=b;
    }

}

