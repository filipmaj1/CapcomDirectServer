using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FMaj.CapcomDirectServer
{

    class PacketWriter : IDisposable
    {
        private readonly MemoryStream Stream;
        private readonly BinaryWriter Writer;
        private int CurrentSize;

        public PacketWriter(int size = 1)
        {
            Stream = new MemoryStream(size);
            Writer = new BinaryWriter(Stream);
        }

        public void Dispose()
        {
            Writer.Dispose();
            Stream.Dispose();
        }

        public byte[] Finish()
        {
            byte[] result = new byte[CurrentSize];
            Array.Copy(Stream.GetBuffer(), result, result.Length);
            return result;
        }

        public PacketWriter WriteByte(byte value)
        {
            Writer.Write((byte)value);
            CurrentSize += 1;
            return this;
        }
        
        public PacketWriter WriteInt16(short value)
        {
            Writer.Write((short)value);
            CurrentSize += 2;
            return this;
        }
        
        public PacketWriter WriteInt32(int value)
        {
            Writer.Write((int)value);
            CurrentSize += 4;
            return this;
        }

        public PacketWriter WriteUInt16(ushort value)
        {
            Writer.Write((ushort)value);
            CurrentSize += 2;
            return this;
        }

        public PacketWriter WriteUInt32(uint value)
        {
            Writer.Write((uint)value);
            CurrentSize += 4;
            return this;
        }

        public PacketWriter WriteString(string value, bool noLength = false)
        {
            byte[] stringBytes = Encoding.GetEncoding("shift_jis").GetBytes(value);
            if (!noLength)
                Writer.Write((byte)stringBytes.Length);
            Writer.Write(stringBytes);
            CurrentSize += stringBytes.Length + 1;
            return this;
        }

        public PacketWriter WriteCapcomID(string id)
        {
            byte[] stringBytes = Encoding.ASCII.GetBytes(id);
            Writer.Write(stringBytes, 0, 6);
            CurrentSize += 6;
            return this;
        }

        public PacketWriter WriteBattleCode(string battleCode)
        {
            byte[] stringBytes = Encoding.ASCII.GetBytes(battleCode);
            Writer.Write(stringBytes, 0, 14);
            CurrentSize += 14;
            return this;
        }

    }
}
