using System.IO;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class MainMenuState : ClientState
    {
        public MainMenuState(Server server, Client client) :base(server, client)
        {

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
                // Enter Battle Room
                case 0x7001:
                    client.SetState(new RoomListState(server, client));
                    break;
                // Enter Unranked Matchmaking
                case 0x7502:
                    client.SetState(new MatchMakingState(server, client, MatchMakingScope.Any));
                    break;
                // Enter Beginner (Ranked) Matchmaking
                case 0x7503:
                    client.SetState(new MatchMakingState(server, client, MatchMakingScope.Ranked));
                    break;
                // Entered search menu and started search
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
                        break;
                    }
                // Changing "Room Genres". This client exits the room list state, fires this, then enters again. Graphically doesn't change.
                case 0x7005:
                    {
                        client.SendMessage(Capcom.ServerOpcodes.ChangeRoomGenre, writer.WriteByte(1).WriteByte(1).WriteByte(1).WriteByte(1).Finish());
                        break;
                    }
            }
        }

        public override string ToString()
        {
            return "MainMenuState";
        }
    }
}
