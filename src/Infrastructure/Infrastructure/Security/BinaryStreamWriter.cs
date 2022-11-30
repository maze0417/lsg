using System;
using System.IO;
using System.Text;
using LSG.SharedKernel.Extensions;

namespace LSG.Infrastructure.Security
{
    public class BinaryStreamWriter : IDisposable
    {
        protected readonly MemoryStream Stream = new MemoryStream();

        public BinaryStreamWriter AddByte(byte value)
        {
            Stream.WriteByte(value);
            return this;
        }


        public BinaryStreamWriter AddUInt(uint value)
        {
            var res = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            Stream.Write(res, 0, 4);
            return this;
        }

        public BinaryStreamWriter AddGuid(Guid value)
        {
            Stream.Write(value.ToByteArray(), 0, 16);
            return this;
        }

        public BinaryStreamWriter AddString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentException("String length cannot exceed " + ushort.MaxValue + " bytes (it was " +
                                            bytes.Length + ")");
            AddUShort((ushort)bytes.Length);
            Stream.Write(bytes, 0, bytes.Length);
            return this;
        }


        private void AddUShort(ushort s)
        {
            Stream.Write(BitConverter.GetBytes(s), 0, 2);
        }

        public BinaryStreamWriter AddHex(string value)
        {
            var bytes = value.ToBytesFromHexString();
            AddUShort((ushort)bytes.Length);
            Stream.Write(bytes, 0, bytes.Length);
            return this;
        }


        public byte[] ToBytesArray()
        {
            return Stream.ToArray();
        }

        protected BinaryStreamWriter AddULong(ulong value)
        {
            var res = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            Stream.Write(res, 0, 8);
            return this;
        }

        protected BinaryStreamWriter AddDouble(double value)
        {
            var res = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            Stream.Write(res, 0, 8);
            return this;
        }


        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}