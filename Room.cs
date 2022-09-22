using System;
using System.Collections.Generic;
using System.Text;

namespace FMaj.CapcomDirectServer
{
    class Room
    {
        public byte GameCode { get; }
        public byte GenreCode { get; }
        public ushort RoomNumber { get; }
        public string RoomName { get; set; }
        public ushort MaxUsers { get; }
        public ushort Count { get => (ushort) members.Count; }

        private List<Client> members = new List<Client>();
        
        public Room(byte gamecode, byte genrecode, ushort roomNumber, string roomName, ushort maxUsers)
        {
            this.GameCode = gamecode;
            this.GenreCode = genrecode;
            this.RoomNumber = roomNumber;
            this.RoomName = roomName;
            this.MaxUsers = maxUsers;
        }

        public void Add(Client user)
        {
            lock (members)
            {
                members.Add(user);
                foreach (Client userInRoom in members)
                {
                    if (!user.Equals(userInRoom))
                        userInRoom.SendMessage(Capcom.ServerOpcodes.UpdateRoomInfo, Capcom.UpdateRoomInfoBytes(RoomNumber, 1, (ushort) members.Count));
                }
            }
        }

        public void Remove(Client user)
        {
            lock (members)
            {
                members.Remove(user);
                foreach (Client userInRoom in members)
                    userInRoom.SendMessage(Capcom.ServerOpcodes.UpdateRoomInfo, Capcom.UpdateRoomInfoBytes(RoomNumber, 1, (ushort)members.Count));
            }
        }

        public Client[] GetMembers()
        {
            lock (members)
            {
                Client[] currentMembers = new Client[members.Count];
                members.CopyTo(currentMembers);
                return currentMembers;
            }
        }

        public void SendMessageToRoom(Client sender, byte[] message)
        {
            foreach (Client userInRoom in members)
                userInRoom.SendMessage(Capcom.ServerOpcodes.ChatMessage, message);
        }

        public byte IsAvailable()
        {
            return Count <= MaxUsers ? (byte) 1 : (byte) 0;
        }
    }
}
