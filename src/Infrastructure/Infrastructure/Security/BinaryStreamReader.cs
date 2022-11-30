using System;
using System.IO;
using System.Text;

namespace LSG.Infrastructure.Security
{
    public class BinaryStreamReader : IDisposable
    {
        protected readonly MemoryStream Stream;

        public BinaryStreamReader(byte[] array)
        {
            Stream = new MemoryStream(array);
        }

        public byte ReadByte()
        {
            return (byte) Stream.ReadByte();
        }

        public uint ReadUInt()
        {
            var arr = new byte[4];
            Stream.Read(arr, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(arr);
            return BitConverter.ToUInt32(arr, 0);
        }

      

        public decimal ReadDouble()
        {
            var arr = new byte[8];
            Stream.Read(arr, 0, 8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(arr);
            return Convert.ToDecimal(BitConverter.ToDouble(arr, 0));
        }

        public int ReadInt()
        {
            var arr = new byte[4];
            Stream.Read(arr, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(arr);
            return BitConverter.ToInt32(arr, 0);
        }

        public Guid ReadGuid()
        {
            var arr = new byte[16];
            Stream.Read(arr, 0, 16);
            return new Guid(arr);
        }

        public string ReadHex()
        {
            var arr = new byte[2];
            Stream.Read(arr, 0, 2);
            var offset = BitConverter.ToUInt16(arr, 0);

            arr = new byte[offset];
            Stream.Read(arr, 0, 16);
            return BitConverter.ToString(arr, 0).Replace("-", "").ToLower();
        }

        public string ReadString()
        {
            var arr = new byte[2];
            Stream.Read(arr, 0, 2);
            var offset = BitConverter.ToUInt16(arr, 0);

            arr = new byte[offset];
            Stream.Read(arr, 0, offset);
            return Encoding.UTF8.GetString(arr);
        }

        public ulong ReadULong()
        {
            var arr = new byte[8];
            Stream.Read(arr, 0, 8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(arr);
            return BitConverter.ToUInt64(arr, 0);
        }

        public long ReadLong()
        {
            var arr = new byte[8];
            Stream.Read(arr, 0, 8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(arr);
            return BitConverter.ToInt64(arr, 0);
        }

        public short ReadShort()
        {
            var arr = new byte[2];
            Stream.Read(arr, 0, 2);
            return BitConverter.ToInt16(arr, 0);
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}