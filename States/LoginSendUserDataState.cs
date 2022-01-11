using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class LoginSendUserDataState : ClientState
    {
        GameData gameData;

        public LoginSendUserDataState(Server server, Client client) :base(server, client)
        {
            gameData = client.gameData;
        }

        public override void OnEnterState()
        {
            using PacketWriter writer = new PacketWriter(9);
            client.SendMessage(Capcom.ServerOpcodes.GoodConnect, writer.WriteByte(0).WriteUInt16(6).WriteCapcomID(client.capcom.Id).Finish());
        }

        public override void OnExitState()
        {
            
        }

        public override void DoPacket(ushort opcode, byte[] data)
        {
            using MemoryStream memStream = new MemoryStream(data);
            using BinaryReader reader = new BinaryReader(memStream);
            using PacketWriter writer = new PacketWriter();
            switch (opcode)
            {
                case 0x7202:
                    client.SendMessage(ServerOpcodes.SetUserRank, writer.WriteByte(gameData.Rank).Finish());
                    break;
                case 0x7203:
                    client.SendMessage(ServerOpcodes.SetUserWinLose, writer.WriteUInt16(gameData.Wins).WriteUInt16(gameData.Losses).WriteUInt16(gameData.Draws).Finish());
                    break;
                case 0x7204:
                    client.SendMessage(ServerOpcodes.SetUserRanking, writer.WriteUInt16(gameData.Ranking).WriteUInt16(200).Finish());
                    break;
                case 0x7206:
                    client.SendMessage(ServerOpcodes.SetUserMoney, writer.WriteUInt32(gameData.SpentMoney).Finish());
                    break;
                case 0x7207:
                    client.SendMessage(ServerOpcodes.SetUserTime, writer.WriteUInt32(gameData.PlayTime).Finish());
                    break;
                case 0x7205:
                    {
                        string segaMessage = server.GetSegaMessage();
                        client.SendMessage(Capcom.ServerOpcodes.SetSegaMessage, writer.WriteString(segaMessage).Finish());
                        break;
                    }
                case 0x720A:
                    if (data[0] > 3)
                        data[0] = 0;

                    byte[] blah = new byte[2 * 0x40];
                    int count = 0;
                    for (int i = (0x40 * data[0]); i < (0x40 * (data[0] + 1)); i++)
                    {
                        blah[count] = (byte)(i + 4);
                        blah[count + 1] = (byte)((i % 0x60) + 0x21);
                        count += 2;
                    }
                    string msg2 = "Testing what exactly goes here... 1";
                    int strSize2 = Encoding.ASCII.GetByteCount(msg2);
                    byte[] msgBytes2 = new byte[1 + 1 + 0x80];
                    msgBytes2[0] = data[0];
                    msgBytes2[1] = 0x80;
                    Array.Copy(blah, 0, msgBytes2, 2, 0x80);
                    client.SendMessage(ServerOpcodes.SetUserMessage, msgBytes2);
                    break;
                case 0x7E01:
                    client.SendMessage(ServerOpcodes.SetUserMoney, BitConverter.GetBytes(gameData.SpentMoney));
                    break;
                case 0x7C01:
                    client.SetState(new MainMenuState(server, client));
                    break;
            }
        }

        public override string ToString()
        {
            return "LoginSendUserDataState";
        }
    }
}
