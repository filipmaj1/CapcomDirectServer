using System;
using System.Collections.Generic;
using System.Text;
using static FMaj.CapcomDirectServer.Capcom;

namespace FMaj.CapcomDirectServer.States
{
    class LoginUserCreateOrUpdateState : ClientState
    {
        // Bitflags to record login data retrieved so far
        const int LOGIN_USER_HANDLE = 0b00000001;
        const int LOGIN_EMAIL = 0b00000010;
        const int LOGIN_TELEPHONE = 0b00000100;
        const int LOGIN_USER_PROFILE = 0b00001000;

        private byte loginFlags = 0x0;
        private byte loginType;

        private CapcomInfo capcomInfo;
        private UserProfile profile;

        public LoginUserCreateOrUpdateState(Server server, Client client, string capcomID, byte loginType) :base(server, client)
        {
            this.loginType = loginType;
            this.capcomInfo = new CapcomInfo();
            capcomInfo.Id = capcomID;
        }

        public override void OnEnterState()
        {
            client.SendMessage(ServerOpcodes.GetUserHandle);
            client.SendMessage(ServerOpcodes.GetEmail);
            client.SendMessage(ServerOpcodes.GetTelephoneNo);
            client.SendMessage(ServerOpcodes.GetProfile);
        }

        public override void OnExitState()
        {
            
        }

        public override void DoPacket(ushort opcode, byte[] data)
        {
            switch (opcode)
            {
                case 0x7107:
                    capcomInfo.Handle = Encoding.GetEncoding("shift_jis").GetString(data, 1, data[0]);
                    loginFlags |= LOGIN_USER_HANDLE;
                    break;
                case 0x7108:
                    capcomInfo.Email = Encoding.GetEncoding("shift_jis").GetString(data, 1, data[0]);
                    loginFlags |= LOGIN_EMAIL;
                    break;
                case 0x7109:
                    // Client adds '5' to every byte for "S E C U R I T Y" /eyeroll
                    for (int i = 1; i < data.Length; i++)
                        data[i] -= (byte)'5';

                    capcomInfo.TelephoneNumber = Encoding.GetEncoding("shift_jis").GetString(data, 1, data[0]);
                    loginFlags |= LOGIN_TELEPHONE;
                    break;
                case 0x710C:
                    profile = UserProfile.Parse(data);
                    loginFlags |= LOGIN_USER_PROFILE;
                    break;
            }

            if (loginFlags == 0b1111)
            {
                client.RegisterOrUpdateUser(loginType, capcomInfo, profile);
                client.SetState(new LoginSendUserDataState(server, client));
            }
        }

        public override string ToString()
        {
            return "LoginUserCreateOrUpdateState";
        }
    }
}
