using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FMaj.CapcomDirectServer
{
    class UserProfile
    {
        public string RealName { get; }
        public string Address { get; }
        public ushort Age { get; }
        public string Profession { get; }
        public bool DirectMail { get; }

        public UserProfile(bool directMail, string realName, string address, ushort age, string profession)
        {
            this.DirectMail = directMail;
            this.RealName = realName;
            this.Address = address;
            this.Age = age;
            this.Profession = profession;
        }

        public static UserProfile Parse(byte[] data)
        {
            bool directMail = data[0] == 1;
            byte age = data[2];

            int nameSize = data[4];
            int nameOffset = 5;
            int addressOffset = nameOffset + nameSize + 1;
            int addressSize = data[nameOffset + nameSize];
            int professionOffset = addressOffset + addressSize + 1;
            int professionSize = data[addressOffset + addressSize];

            // Client adds '5' to every byte for "S E C U R I T Y".
            for (int i = 0; i < nameSize; i++)
                data[nameOffset + i] -= (byte) '5';
            for (int i = 0; i < addressSize; i++)
                data[addressOffset + i] -= (byte) '5';
            for (int i = 0; i < professionSize; i++)
                data[professionOffset + i] -= (byte) '5';

            string realName = Encoding.GetEncoding("shift_jis").GetString(data, nameOffset, nameSize);
            string address = Encoding.GetEncoding("shift_jis").GetString(data, addressOffset, addressSize);
            string profession = Encoding.GetEncoding("shift_jis").GetString(data, professionOffset, professionSize);

            return new UserProfile(directMail, realName, address, age, profession);
        }
    }
}
