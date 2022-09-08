using FMaj.CapcomDirectServer.States;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer
{
    class Client
    {
        // Connection stuff
        public readonly Socket socket;
        public readonly byte[] buffer = new byte[0xff];
        public int bufferSize = 0;
        private Server serverReference;

        // Player Info
        public byte[] individualID;
        public byte gameCode;

        public CapcomInfo capcom = new CapcomInfo();
        public UserProfile profile;
        public GameData gameData;

        // Current State
        private ClientState currentState;
        private Room currentRoom = null;
        private Client opponent = null;

        public Client(Server server, Socket socket)
        {
            this.serverReference = server;
            this.socket = socket;
        }

        public void SetIndividualID(byte[] individualID)
        {
            this.individualID = individualID;
        }

        public void SetGameCode(byte gameCode)
        {
            this.gameCode = gameCode;
        }

        public void SendMessage(Capcom.ServerOpcodes opcode, byte[] data = null)
        {
            if (socket == null)
                return;

            byte opcodeHI = (byte)(((short)opcode >> 8) & 0xFF);
            byte opcodeLO = (byte)((short)opcode & 0xFF);
            byte[] toSend = new byte[(data?.Length ?? 0) + 3];
            toSend[0] = opcodeHI;
            toSend[1] = (byte)((3 + (data?.Length ?? 0)) & 0xFF);
            toSend[2] = opcodeLO;
            if (data != null)
                Array.Copy(data, 0, toSend, 3, data.Length);
            socket.Send(toSend);

            Program.Log.Debug("Sending:\n" + Server.ByteArrayToHex(toSend));
        }

        public void SendBytes(byte[] packetBytes)
        {
            if (socket == null)
                return;

            try
            {
                socket.Send(packetBytes);
            } catch (Exception)
            {
                Disconnect();
            }
        }

        public void SetChallengee(Client foundClient)
        {
            opponent = foundClient;
        }

        public void SendChallenge(Client challenger, string comment)
        {
            opponent = challenger;
            PacketWriter writer = new PacketWriter();
            byte[] data = writer
                .WriteCapcomID(challenger.capcom.Id)
                .WriteByte(challenger.gameData.Rank)
                .WriteUInt16(challenger.gameData.Wins)
                .WriteUInt16(challenger.gameData.Losses)
                .WriteUInt16(challenger.gameData.Draws)
                .WriteString(challenger.capcom.Handle)
                .WriteByte((byte)Encoding.GetEncoding("shift_jis").GetByteCount(comment))
                .WriteByte(1)
                .WriteByte(1)
                .WriteString(comment, true)
                .Finish();
            SendMessage(ServerOpcodes.SendChallenge, data);
        }

        public Battle ChallengeAccepted(bool accepted)
        {
            PacketWriter writer = new PacketWriter();
            if (accepted)
            {
                Battle battle = serverReference.NewBattle(this, opponent);
                SetState(new VsSetupState(serverReference, this, opponent, battle));
                SendMessage(ServerOpcodes.SearchMatchMakingResult, writer.WriteByte((byte)SearchResult.MatchAccepted).Finish());
                return battle;
            }
            else
            {
                SetChallengee(null);
                SendMessage(ServerOpcodes.SearchMatchMakingResult, writer.WriteByte((byte)SearchResult.MatchRefused).Finish());
                return null;
            }
        }

        public void FoundMatch(Client opponent, Battle battle)
        {
            if (currentState is MatchMakingState)
                ((MatchMakingState)currentState).FoundMatch(opponent, battle);
        }

        public void VsSideResult(byte side)
        {
            if (currentState is VsSetupState)
                ((VsSetupState)currentState).VsSideResult(side);
        }

        public void OpponentDisconnected()
        {
            opponent = null;
            if (currentRoom != null)
                LeaveRoom();
            SetState(new MainMenuState(serverReference, this));
        }

        public void Disconnect()
        {
            if (socket == null)
                return;

            SendMessage(ServerOpcodes.Shutdown);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public override string ToString()
        {
            return capcom.ToString();
        }

        public String GetAddress()
        {
            return String.Format("{0}:{1}", (socket.RemoteEndPoint as IPEndPoint).Address, (socket.RemoteEndPoint as IPEndPoint).Port);
        }

        public void SetState(ClientState state)
        {
            if (currentState == null)
                Program.Log.Info($"{ToString()} is entering {state}.");
            else
                Program.Log.Info($"{ToString()} is moving from {currentState} to {state}.");

            if (currentState != null)
                currentState.OnExitState();
            currentState = state;
            currentState.OnEnterState();
        }

        public void JoinRoom(ushort roomNumber)
        {
            if (currentRoom != null)
                currentRoom.Remove(this);

            Room room = serverReference.GetRoom(gameCode, roomNumber);
            room.Add(this);
            currentRoom = room;
        }

        public void LeaveRoom()
        {
            if (currentRoom == null)
                return;

            currentRoom.Remove(this);
            currentRoom = null;
        }

        public void MessageRoom(byte[] msgData)
        {
            if (currentRoom == null)
                return;

            byte[] capcomID = Encoding.ASCII.GetBytes(capcom.Id);
            byte[] toSend = new byte[6 + msgData.Length];
            Array.Copy(capcomID, toSend, 6);
            Array.Copy(msgData, 0, toSend, 6, msgData.Length);
            currentRoom.SendMessageToRoom(this, toSend);
        }        

        public void RegisterOrUpdateUser(byte loginType, CapcomInfo info, UserProfile profile)
        {
            this.capcom = info;
            this.profile = profile;

            if (loginType == '%')
            {
                Database.UpdateCapcomAccount(capcom, profile);

                gameData = Database.GetGameData(capcom.Id, gameCode);
                if (gameData == null)
                    gameData = Database.CreateGameData(capcom.Id, gameCode);
            }
            else if (loginType == '*')
            {
                string individualIDString = BitConverter.ToString(individualID);
                capcom.Id = Database.CreateCapcomAccount(individualIDString, capcom, profile);
                gameData = Database.CreateGameData(capcom.Id, gameCode);
            }
        }

        public void LoadUser(string capcomID)
        {
            profile = Database.GetCapcomAccount(capcomID, ref capcom);
            gameData = Database.GetGameData(capcomID, gameCode);
            if (gameData == null)
                gameData = Database.CreateGameData(capcomID, gameCode);
        }

        public void ProcessPacket(ushort opcode, byte[] data)
        {
            // Any high level codes
            switch (opcode)
            {
                case 0x7506:
                    {
                        using MemoryStream memStream = new MemoryStream(data);
                        using BinaryReader reader = new BinaryReader(memStream);
                        string capcomID = Encoding.ASCII.GetString(reader.ReadBytes(6));
                        bool accepted = reader.ReadByte() == 1;
                        Battle battle = opponent.ChallengeAccepted(accepted);
                        SetState(new VsSetupState(serverReference, this, opponent, battle));
                        return;
                    }
                case 0x7007:
                    {
                        SetState(new MainMenuState(serverReference, this));
                        return;
                    }
            }

            if (currentState != null)
            {
                if (currentState.DoPacket(opcode, data))
                    return;
            }

            Program.Log.Debug(String.Format("Receiving unknown packet: ({0:X}):\n", opcode) + Server.ByteArrayToHex(data));
        }
    }
}
