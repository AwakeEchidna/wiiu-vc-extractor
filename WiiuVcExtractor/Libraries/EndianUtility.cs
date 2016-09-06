using System;
using System.IO;

namespace WiiuVcExtractor.Libraries
{
    public static class EndianUtility
    {
        public static byte[] ReverseBE(this byte[] b)
        {
            // Only reverse if we are on a little endian system
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            
            return b;
        }

        public static byte[] ReverseLE(this byte[] b)
        {
            // Only reverse if we are on a big endian system
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }

            return b;
        }

        public static UInt16 ReadUInt16BE(this BinaryReader br)
        {
            return BitConverter.ToUInt16(br.ReadBytesRequired(sizeof(UInt16)).ReverseBE(), 0);
        }

        public static Int16 ReadInt16BE(this BinaryReader br)
        {
            return BitConverter.ToInt16(br.ReadBytesRequired(sizeof(Int16)).ReverseBE(), 0);
        }

        public static UInt32 ReadUInt32BE(this BinaryReader br)
        {
            return BitConverter.ToUInt32(br.ReadBytesRequired(sizeof(UInt32)).ReverseBE(), 0);
        }

        public static Int32 ReadInt32BE(this BinaryReader br)
        {
            return BitConverter.ToInt32(br.ReadBytesRequired(sizeof(Int32)).ReverseBE(), 0);
        }

        public static void WriteUInt16BE(this BinaryWriter bw, UInt16 value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseBE());
        }

        public static void WriteUInt32BE(this BinaryWriter bw, UInt32 value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseBE());
        }

        public static UInt16 ReadUInt16LE(this BinaryReader br)
        {
            return BitConverter.ToUInt16(br.ReadBytesRequired(sizeof(UInt16)).ReverseLE(), 0);
        }

        public static Int16 ReadInt16LE(this BinaryReader br)
        {
            return BitConverter.ToInt16(br.ReadBytesRequired(sizeof(Int16)).ReverseLE(), 0);
        }

        public static UInt32 ReadUInt24LE(this BinaryReader br)
        {
            byte[] returnArray = new byte[4];
            byte[] buffer = br.ReadBytesRequired(3);

            returnArray[0] = buffer[0];
            returnArray[1] = buffer[1];
            returnArray[2] = buffer[2];
            returnArray[3] = 0;

            return BitConverter.ToUInt32(returnArray.ReverseLE(), 0);
        }

        public static UInt32 ReadUInt32LE(this BinaryReader br)
        {
            return BitConverter.ToUInt32(br.ReadBytesRequired(sizeof(UInt32)).ReverseLE(), 0);
        }

        public static Int32 ReadInt32LE(this BinaryReader br)
        {
            return BitConverter.ToInt32(br.ReadBytesRequired(sizeof(Int32)).ReverseLE(), 0);
        }

        public static void WriteUInt16LE(this BinaryWriter bw, UInt16 value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseLE());
        }

        public static void WriteUInt32LE(this BinaryWriter bw, UInt32 value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseLE());
        }

        public static byte[] ReadBytesRequired(this BinaryReader br, int byteCount)
        {
            var result = br.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

            return result;
        }

        public static string ReadNullTerminatedString(this BinaryReader stream)
        {
            string str = "";
            char ch;
            while ((int)(ch = stream.ReadChar()) != 0)
                str = str + ch;
            return str;
        }
    }
}
