using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Timers;
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

            client.loginTimeoutTimer = new System.Timers.Timer(120000);
            client.loginTimeoutTimer.Elapsed += (s, e) =>
            {
                Program.Log.Info("{0}:{1} did not finish login after two minutes. Disconnecting...", (client.socket.RemoteEndPoint as IPEndPoint).Address, (client.socket.RemoteEndPoint as IPEndPoint).Port);
                server.banIP((client.socket.RemoteEndPoint as IPEndPoint).Address);
                client.Disconnect(false);
                client.endLoginTimeout();
            };
            client.loginTimeoutTimer.Start();
        }

        public override void OnExitState() 
        {
            client.endLoginTimeout();
        }

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
                    Database.storeUserIP(capcomID, client);
                    if (loginType == '%' || loginType == '*')
                    {
                        client.SetState(new LoginUserCreateOrUpdateState(server, client, capcomID, loginType));
                    }
                    else if (loginType == '#')
                    {
                        client.SetState(new LoginPostBattleState(server, client, capcomID));
                    }
                    else
                        client.Disconnect();
                    break;
                default: //Maybe this will stop portscanners from screwing us
                    Program.Log.Info("{0} did not send a known opcode during login. Maybe it's a portscanner?", (client.socket.RemoteEndPoint as IPEndPoint).Address);
                    client.Disconnect(false);
                    break;
            }
        }
        public override string ToString()
        {
            return "LoginInitialState";
        }
    }
}
