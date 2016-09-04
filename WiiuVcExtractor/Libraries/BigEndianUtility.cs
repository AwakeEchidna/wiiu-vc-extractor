using System;
using System.IO;

namespace WiiuVcExtractor.Libraries
{
    public static class BigEndianUtility
    {
        public static byte[] Reverse(this byte[] b)
        {
            // Only reverse if we are on a little endian system
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            
            return b;
        }

        public static UInt16 ReadUInt16BE(this BinaryReader br)
        {
            return BitConverter.ToUInt16(br.ReadBytesRequired(sizeof(UInt16)).Reverse(), 0);
        }

        public static Int16 ReadInt16BE(this BinaryReader br)
        {
            return BitConverter.ToInt16(br.ReadBytesRequired(sizeof(Int16)).Reverse(), 0);
        }

        public static UInt32 ReadUInt32BE(this BinaryReader br)
        {
            return BitConverter.ToUInt32(br.ReadBytesRequired(sizeof(UInt32)).Reverse(), 0);
        }

        public static Int32 ReadInt32BE(this BinaryReader br)
        {
            return BitConverter.ToInt32(br.ReadBytesRequired(sizeof(Int32)).Reverse(), 0);
        }

        public static void WriteUInt16BE(this BinaryWriter bw, UInt16 value)
        {
            bw.Write(BitConverter.GetBytes(value).Reverse());
        }

        public static void WriteUInt32BE(this BinaryWriter bw, UInt32 value)
        {
            bw.Write(BitConverter.GetBytes(value).Reverse());
        }

        public static byte[] ReadBytesRequired(this BinaryReader br, int byteCount)
        {
            var result = br.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

            return result;
        }
    }
}
