using System;
using System.Collections.Generic;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class LoginPostBattleState : ClientState
    {
        string capcomID;
        string battleCode;

        public LoginPostBattleState(Server server, Client client, string capcomID) :base(server, client)
        {
            this.capcomID = capcomID;
        }

        public override void OnEnterState()
        {
            client.LoadUser(capcomID);
            client.SendMessage(ServerOpcodes.GetBattleCode);
        }

        public override void OnExitState()
        {
        }

        public override bool DoPacket(ushort opcode, byte[] data)
        {
            switch (opcode)
            {
                case 0x7103:
                    battleCode = Encoding.ASCII.GetString(data, 0, 14);
                    client.SendMessage(ServerOpcodes.GetBattleData);
                    return true;
                case 0x710A:
                    client.SetState(new LoginSendUserDataState(server, client));
                    return true;
                default: return false;
            }
        }
    }
}
