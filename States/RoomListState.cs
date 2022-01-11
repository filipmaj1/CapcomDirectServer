using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FMaj.CapcomDirectServer.States
{
    class RoomListState : ClientState
    {
        public RoomListState(Server server, Client client) :base(server, client)
        {

        }

        public override void OnEnterState()
        {
            using PacketWriter writer = new PacketWriter();
            client.SendMessage(Capcom.ServerOpcodes.EnterRoomList, writer.WriteByte(1).WriteByte(1).Finish());
        }

        public override void OnExitState()
        {
            using PacketWriter writer = new PacketWriter();
            client.SendMessage(Capcom.ServerOpcodes.EnterRoomList, writer.WriteByte(1).WriteByte(0).Finish());
        }

        public override void DoPacket(ushort opcode, byte[] data)
        {
            using MemoryStream memStream = new MemoryStream(data);
            using BinaryReader reader = new BinaryReader(memStream);
            using PacketWriter writer = new PacketWriter();
            switch (opcode)
            {
                case 0x7002:
                    client.SetState(new MainMenuState(server, client));
                    break;
                case 0x7004:
                    {
                        ushort roomCount = server.GetRoomCount(client.gameCode);
                        client.SendMessage(Capcom.ServerOpcodes.SendRoomCount, writer.WriteByte(1).WriteByte(1).WriteUInt16(roomCount).Finish());
                        break;
                    }
                case 0x7301:
                    {                        
                        ushort roomNumber = reader.ReadUInt16();
                        client.SetState(new ChatRoomState(server, client, roomNumber));
                        break;
                    }
                case 0x7401:
                    {
                        ushort roomNumber = reader.ReadUInt16();
                        Room room = server.GetRoom(client.gameCode, roomNumber);
                        if (room == null)
                        {
                            client.Disconnect();
                            return;
                        }
                        client.SendMessage(Capcom.ServerOpcodes.SendRoomInfo, writer.WriteUInt16(room.RoomNumber).WriteByte(room.IsAvailable()).WriteUInt16(room.Count).WriteString(room.RoomName).Finish());
                        break;
                    }                
            }
        }

        public override string ToString()
        {
            return "RoomListState";
        }
    }
}
