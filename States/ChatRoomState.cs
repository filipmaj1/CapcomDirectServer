using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class ChatRoomState : ClientState
    {
        private readonly ushort roomNumber;
        private readonly bool fromMatchMaking;

        public ChatRoomState(Server server, Client client, ushort roomNumber, bool fromMatchMaking = false) :base(server, client)
        {
            this.roomNumber = roomNumber;
            this.fromMatchMaking = fromMatchMaking;
        }

        public override void OnEnterState()
        {
            client.JoinRoom(roomNumber);

            if (!fromMatchMaking)
            {
                using PacketWriter writer = new PacketWriter();
                client.SendMessage(ServerOpcodes.EnterRoom, writer.WriteUInt16(roomNumber).WriteByte(1).Finish());
            }
        }

        public override void OnExitState()
        {
        }

        public override bool DoPacket(ushort opcode, byte[] data)
        {
            using MemoryStream memStream = new MemoryStream(data);
            using BinaryReader reader = new BinaryReader(memStream);
            using PacketWriter writer = new PacketWriter();
            switch (opcode)
            {      
                // Left Chatroom
                case 0x7302:
                    {
                        client.LeaveRoom();
                        client.SendMessage(ServerOpcodes.ExitRoom, writer.WriteByte(1).Finish());
                        client.SetState(new RoomListState(server, client));
                        return true;
                    }
                // Room Info Request
                case 0x7401:
                    {
                        ushort roomNumber = reader.ReadUInt16();
                        Room room = server.GetRoom(client.gameCode, client.currentGenre, roomNumber);
                        if (room == null)
                        {
                            client.Disconnect();
                            return true;
                        }
                        client.SendMessage(ServerOpcodes.SendRoomInfo, writer.WriteUInt16(room.RoomNumber).WriteByte(room.IsAvailable()).WriteUInt16(room.Count).WriteString(room.RoomName).Finish());
                        return true;
                    }
                // Room Info Request
                case 0x7403:
                    {
                        ushort roomNumber = reader.ReadUInt16();
                        Room room = server.GetRoom(client.gameCode, client.currentGenre, roomNumber);
                        if (room == null)
                        {
                            client.Disconnect();
                            return true;
                        }

                        client.SendMessage(ServerOpcodes.UpdateRoomInfo, writer.WriteUInt16(room.RoomNumber).WriteByte(room.IsAvailable()).WriteUInt16(room.Count).Finish());
                        return true;
                    }
                // Sent Message
                case 0x7B01:
                    {
                        client.MessageRoom(data);
                        return true;
                    }
                // Started Matchmaking (Chatroom)
                case 0x7501:
                    {
                        Room room = server.GetRoom(client.gameCode, client.currentGenre, roomNumber);
                        client.SetState(new MatchMakingState(server, client, MatchMakingScope.Chatroom, room));
                        return true;
                    }
                // Entered search and started search
                case 0x7505:
                    {
                        byte[] capcomIDBytes = reader.ReadBytes(6);
                        string capcomID = Encoding.ASCII.GetString(capcomIDBytes);
                        byte commentSize = reader.ReadByte();
                        byte[] commentBytes = reader.ReadBytes(commentSize);
                        string comment = Encoding.GetEncoding("shift_jis").GetString(commentBytes);

                        Client foundClient = server.FindClientByID(capcomID);
                        if (foundClient != null && !foundClient.Equals(client))
                        {
                            client.SetChallengee(foundClient);
                            foundClient.SendChallenge(client, comment);
                        }
                        else
                            client.SendMessage(ServerOpcodes.SearchMatchMakingResult, writer.WriteByte((byte)SearchResult.UserIsNotOnline1).Finish());
                        return true;
                    }
                default: return false;
            }
        }

        public override string ToString()
        {
            return "ChatRoomState";
        }
    }
}
