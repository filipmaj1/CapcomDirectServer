using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class VsSetupState : ClientState
    {
        private Client opponent;
        private Battle battle;

        public VsSetupState(Server server, Client client, Client opponent, Battle battle) :base(server, client)
        {
            this.opponent = opponent;
            this.battle = battle;
        }

        public override void OnEnterState()
        {
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
                // Left Chatroom (only in chatroom scope)
                case 0x7302:
                    {
                        client.LeaveRoom();
                        client.SendMessage(ServerOpcodes.ExitRoom, writer.WriteByte(1).Finish());
                        client.SetState(new RoomListState(server, client));
                        break;
                    }
                case 0x7601:
                    client.SendMessage(ServerOpcodes.SendOpponentUserId, writer.WriteCapcomID(opponent.capcom.Id).Finish());
                    break;
                case 0x7602:
                    client.SendMessage(ServerOpcodes.SendOpponentHandle, writer.WriteString(opponent.capcom.Handle).Finish());
                    break;
                case 0x7603:
                    client.SendMessage(ServerOpcodes.SendOpponentRank, writer.WriteByte(opponent.gameData.Rank).Finish());
                    break;
                case 0x7604:
                    client.SendMessage(ServerOpcodes.SendOpponentWinLose, writer.WriteUInt16(opponent.gameData.Wins).WriteUInt16(opponent.gameData.Losses).WriteUInt16(opponent.gameData.Draws).Finish());
                    break;                    
                case 0x7701:
                    byte sideChosen = reader.ReadByte();
                    battle.SetSide(client, sideChosen);
                    break;
                case 0x7702:
                    client.SendMessage(ServerOpcodes.SendBattleCode, writer.WriteBattleCode(battle.BattleCode).Finish());
                    break;
                case 0x7507:
                    client.SendMessage(ServerOpcodes.Unknown1, writer.WriteByte(1).Finish());
                    break;
                case 0x7606: //Calling and waiting side recieve.
                    {
                        //dreamPiKillPacket();
                        client.SendMessage(ServerOpcodes.Unknown2, writer.WriteString("atdt1234567").Finish());
                        break;
                    }
                case 0x7703: // Only calling side recieves (Side 1)
                    {
                        //dreamPiKillPacket();
                        client.SendMessage(ServerOpcodes.SendModemMessage, writer.WriteString("atdt,," + battle.getSide2PhoneNumber()).Finish());
                        break;
                    }
            }
        }

        [System.Obsolete("No longer needed", true)]
        private void dreamPiKillPacket()
        {
            //DreamPi PPP Kill Packet
            try
            {
                using (System.Net.Sockets.TcpClient killPacketClient = new System.Net.Sockets.TcpClient(client.GetAddress().Split(":")[0], 65433))
                {
                    Byte[] killPacketData = System.Text.Encoding.ASCII.GetBytes("ppp_kill");
                    using (System.Net.Sockets.NetworkStream killPacketStream = killPacketClient.GetStream())
                    {
                        killPacketStream.Write(killPacketData, 0, killPacketData.Length);
                    }
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Program.Log.Error("SocketException: {0}", e);
            }
        }

        public void VsSideResult(byte side)
        {
            using PacketWriter writer = new PacketWriter();
            client.SendMessage(ServerOpcodes.SendModemDirection, writer.WriteByte(side).Finish());
        }

        public override string ToString()
        {
            return "VsSetupState";
        }
    }
}
