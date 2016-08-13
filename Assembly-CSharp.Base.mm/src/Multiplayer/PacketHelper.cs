using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;
using System.Threading;
using System.Reflection;

namespace ETGMultiplayer {
    class PacketHelper {

        public static Dictionary<string, MethodInfo> allRPCs = new Dictionary<string, MethodInfo>();

        public static List<CustomPacket> packetsReady = new List<CustomPacket>();

        static Thread collectThread;

        public static ulong GlobalPacketID;

        public static void SendPacketToPlayersInGame(string RPCName, byte[] data, bool isTCP = false) {

            return;

            if (!SteamManager.Initialized)
                return;

            if (SteamHelper.playersInLobbyWithoutMe.Count==0)
                return;

            foreach (CSteamID pID in SteamHelper.playersInLobbyWithoutMe) {
                byte[] stringBytes = Encoding.ASCII.GetBytes(RPCName);

                byte[] allData = new byte[stringBytes.Length+1+8+data.Length];

                if (allData.Length>=1200) {
                    Debug.LogError("Packet's over max size!");
                }

                BitConverter.GetBytes(GlobalPacketID).CopyTo(allData, 0);

                stringBytes.CopyTo(allData, 8);

                allData[stringBytes.Length+8]=0;

                data.CopyTo(allData, stringBytes.Length+9);

                SteamNetworking.SendP2PPacket(pID, allData, (uint)allData.Length, isTCP ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliable);
            }

        }

        public static void SendPacketToPlayer(CSteamID player, string RPCName, byte[] data, bool isTCP = false) {

            return;

            if (!SteamManager.Initialized)
                return;

            //if (SteamHelper.playersInLobbyWithoutMe.Count==0)
            //return;

            byte[] stringBytes = Encoding.ASCII.GetBytes(RPCName);

            byte[] allData = new byte[stringBytes.Length+1+8+data.Length];

            if (allData.Length>=1200) {
                Debug.LogError("Packet's over max size!");
            }

            BitConverter.GetBytes(GlobalPacketID).CopyTo(allData, 0);

            stringBytes.CopyTo(allData, 8);

            allData[stringBytes.Length+8]=0;

            data.CopyTo(allData, stringBytes.Length+9);

            SteamNetworking.SendP2PPacket(player, allData, (uint)allData.Length, isTCP ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliable);

        }

        public static void SendRPCToPlayersInGame(string rpcName, bool isTCP = false, params object[] args) {
            return;
            SendPacketToPlayersInGame(rpcName,RPCSerializer.GetCompleteSerializedData(args),isTCP);
        }

        public static void SendRPCToPlayersInGame(string rpcName, params object[] args) {
            return;
            SendPacketToPlayersInGame(rpcName, RPCSerializer.GetCompleteSerializedData(args));
        }


        /// <summary>
        /// ONLY FOR DEBUG USE, DO NOT, I REPEAT, DO NOT USE THIS FOR ANYTHING OTHER THAN TESTING PACKET SENDING LOCALLY!!!!!
        /// </summary>
        /// <param name="rpcName"></param>
        /// <param name="args"></param>
        public static void SendRPCToSelf(string rpcName, params object[] args) {
            return;
            SendPacketToPlayer(SteamUser.GetSteamID(),rpcName, RPCSerializer.GetCompleteSerializedData(args));
        }

        public static void Init() {
            collectThread=new Thread(new ThreadStart(PacketCollectionThread));
            collectThread.Start();

            ETGMultiplayer.RPCSerializer.Init();

            CustomRPC.DocAllRPCAttributes();
        }

        public static void StopThread() {
            collectThread.Abort();
        }

        public static void PacketCollectionThread() {
            //while (true) {
            //Thread.Sleep(15);

            uint packetSize;
            uint readBytes;
            CSteamID remoteID;
            byte[] data;

            while (SteamNetworking.IsP2PPacketAvailable(out packetSize)) {
                data=new byte[packetSize];
                if (SteamNetworking.ReadP2PPacket(data, packetSize, out readBytes, out remoteID)) {
                    CustomPacket newPacket = ParseCustomPacket(data);

                    if (newPacket!=null) {
                        if (newPacket.PACKETID>=GlobalPacketID) {
                            packetsReady.Add(newPacket);
                            GlobalPacketID=newPacket.PACKETID+1;
                        } else {
                            //Junk packet
                        }
                    }
                }
            }

            //}
        }

        public static CustomPacket ParseCustomPacket(byte[] data) {

            ulong packetID = BitConverter.ToUInt64(data, 0);

            int stringSize = 0;
            for (; stringSize+8<data.Length; stringSize++)
                if (data[stringSize+8]==0)
                    break;

            string RPCName = Encoding.ASCII.GetString(data, 8, stringSize);

            byte[] clippedData = new byte[data.Length-9-stringSize];

            for (int i = 0; i<clippedData.Length; i++)
                clippedData[i]=data[i+9+stringSize];

            object[] args = RPCSerializer.GetCompleteDeserializedData(clippedData);

            return new CustomPacket(packetID, RPCName, args);
        }


        [CustomRPC("ChatMessage")]
        public static void ChatMessage(string s) {
            MultiplayerManager.allText.Add(s);
        }

        public static void RunReadyPackets() {
            while (packetsReady.Count>0) {
                CustomPacket p = packetsReady[0];
                packetsReady.RemoveAt(0);

                if (allRPCs.ContainsKey(p.RPCId)) {
                    allRPCs[p.RPCId].GetDelegate().Invoke(null,p.data);
                }
            }
        }

    }

    class CustomPacket {

        public ulong PACKETID;
        public string RPCId;
        public object[] data;

        public CustomPacket(ulong PACKETID, string RPCId, object[] data) {
            this.PACKETID=PACKETID;
            this.RPCId=RPCId;
            this.data=data;
        }
    }
}

