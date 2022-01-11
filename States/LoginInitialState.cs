using System;
using System.Collections.Generic;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class LoginInitialState : ClientState
    {
        private byte[] individualID;
        private byte gameCode;
        private string capcomID;
        private byte loginType;

        public LoginInitialState(Server server, Client client) :base(server, client) { }

        public override void OnEnterState()
        {
            client.SendMessage(ServerOpcodes.GetIndividualID);
            client.SendMessage(ServerOpcodes.GetGameCode);
            client.SendMessage(ServerOpcodes.GetUserId);
        }

        public override void OnExitState() { }

        public override void DoPacket(ushort opcode, byte[] data)
        {
            switch (opcode)
            {
                case 0x7101:
                    individualID = new byte[data.Length];
                    Array.Copy(data, individualID, data.Length);
                    client.SetIndividualID(individualID);
                    break;
                case 0x7102:
                    gameCode = data[0];
                    client.SetGameCode(gameCode);
                    break;
                case 0x7106:
                    capcomID = Encoding.GetEncoding("shift_jis").GetString(data, 0, 6);
                    loginType = data[6];
                    if (loginType == '%' || loginType == '*')
                        client.SetState(new LoginUserCreateOrUpdateState(server, client, capcomID, loginType));
                    else if (loginType == '#')
                        client.SetState(new LoginPostBattleState(server, client, capcomID));
                    else
                        client.Disconnect();
                    break;
            }
        }
        public override string ToString()
        {
            return "LoginInitialState";
        }
    }
}
