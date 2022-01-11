using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class MatchMakingState : ClientState
    {
        private readonly MatchMakingScope scope;
        private readonly Room chatroom;
        private bool foundMatch = false;

        public MatchMakingState(Server server, Client client, MatchMakingScope scope, Room chatroom = null) :base(server, client)
        {
            this.scope = scope;
            this.chatroom = chatroom;
        }

        public override void OnEnterState()
        {
            // Add to match making
            server.GetMatchMaker().AddClient(client, scope, chatroom);

            // Send confirmation
            using PacketWriter writer = new PacketWriter();
            if (scope == MatchMakingScope.Chatroom)
                client.SendMessage(ServerOpcodes.ChatroomMatchMakingResult, writer.WriteByte((byte)MatchMakingResult.EnableCancel).Finish());
            else if(scope == MatchMakingScope.Any)
                client.SendMessage(ServerOpcodes.RankedMatchMakingResult, writer.WriteByte((byte)MatchMakingResult.EnableCancel).Finish());
            else if (scope == MatchMakingScope.Ranked)
                client.SendMessage(ServerOpcodes.BeginnerMatchMakingResult, writer.WriteByte((byte)MatchMakingResult.EnableCancel).Finish());
        }

        public override void OnExitState()
        {
            server.GetMatchMaker().RemoveClient(client);

            // Send confirmation
            if (foundMatch)
            {
                using PacketWriter writer = new PacketWriter();
                if (scope == MatchMakingScope.Chatroom)
                    client.SendMessage(ServerOpcodes.ChatroomMatchMakingResult, writer.WriteByte((byte)MatchMakingResult.Found).Finish());
                else if (scope == MatchMakingScope.Any)
                    client.SendMessage(ServerOpcodes.RankedMatchMakingResult, writer.WriteByte((byte)MatchMakingResult.Found).Finish());
                else if (scope == MatchMakingScope.Ranked)
                    client.SendMessage(ServerOpcodes.BeginnerMatchMakingResult, writer.WriteByte((byte)MatchMakingResult.Found).Finish());
            }
        }

        public override void DoPacket(ushort opcode, byte[] data)
        {
            using MemoryStream memStream = new MemoryStream(data);
            using BinaryReader reader = new BinaryReader(memStream);
            using PacketWriter writer = new PacketWriter();
            switch (opcode)
            {      
                // Cancel Matchmaking
                case 0x7504:
                    {
                        client.SendMessage(ServerOpcodes.CancelMatchMaking, writer.WriteByte(1).Finish());
                        if (scope == MatchMakingScope.Chatroom)
                            client.SetState(new ChatRoomState(server, client, chatroom.RoomNumber, true));
                        else
                            client.SetState(new MainMenuState(server, client));
                        break;
                    }
                // Sent Message (Chatroom Scope)
                case 0x7B01:
                    {
                        client.MessageRoom(data);
                        break;
                    }
            }
        }

        public void FoundMatch(Client opponent, Battle battle)
        {
            foundMatch = true; 
            client.SetState(new VsSetupState(server, client, opponent, battle));
        }

        public override string ToString()
        {            
            return $"MatchMakingState({scope})";
        }
    }
}
