namespace WiiuVcExtractor.Libraries
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides various helper methods for converting between big-endian and little-endian data.
    /// </summary>
    public static class EndianUtility
    {
        static EndianUtility()
        {
            Endianness = BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;
        }

        /// <summary>
        /// Gets or sets endianness of the current system.
        /// </summary>
        public static Endianness Endianness { get; set; }

        /// <summary>
        /// Reverses a big-endian byte array if on a little-endian system to convert it to big-endian.
        /// If the current system is big-endian, no modification is performed.
        /// </summary>
        /// <param name="b">byte array to potentially reverse.</param>
        /// <returns>big-endian byte array.</returns>
        public static byte[] ReverseBE(this byte[] b)
        {
            // Only reverse if we are on a little endian system
            if (Endianness == Endianness.LittleEndian)
            {
                Array.Reverse(b);
            }

            return b;
        }

        /// <summary>
        /// Reverses a byte array if on a big-endian system to convert it to little-endian.
        /// If the current system is little-endian, no modification is performed.
        /// </summary>
        /// <param name="b">byte array to potentially reverse.</param>
        /// <returns>little-endian byte array.</returns>
        public static byte[] ReverseLE(this byte[] b)
        {
            // Only reverse if we are on a big endian system
            if (Endianness == Endianness.BigEndian)
            {
                Array.Reverse(b);
            }

            return b;
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from a big-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>unsigned 16-bit integer in current system endianness.</returns>
        public static ushort ReadUInt16BE(this BinaryReader br)
        {
            return BitConverter.ToUInt16(br.ReadBytesRequired(sizeof(ushort)).ReverseBE(), 0);
        }

        /// <summary>
        /// Reads a signed 16-bit integer from a big-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>signed 16-bit integer in current system endianness.</returns>
        public static short ReadInt16BE(this BinaryReader br)
        {
            return BitConverter.ToInt16(br.ReadBytesRequired(sizeof(short)).ReverseBE(), 0);
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from a big-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>unsigned 32-bit integer in current system endianness.</returns>
        public static uint ReadUInt32BE(this BinaryReader br)
        {
            return BitConverter.ToUInt32(br.ReadBytesRequired(sizeof(uint)).ReverseBE(), 0);
        }

        /// <summary>
        /// Reads a signed 32-bit integer from a big-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>signed 32-bit integer in current system endianness.</returns>
        public static int ReadInt32BE(this BinaryReader br)
        {
            return BitConverter.ToInt32(br.ReadBytesRequired(sizeof(int)).ReverseBE(), 0);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer in big-endian format.
        /// </summary>
        /// <param name="bw">binary reader used to write the data.</param>
        /// <param name="value">value to write.</param>
        public static void WriteUInt16BE(this BinaryWriter bw, ushort value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseBE());
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer in big-endian format.
        /// </summary>
        /// <param name="bw">binary reader used to write the data.</param>
        /// <param name="value">value to write.</param>
        public static void WriteUInt32BE(this BinaryWriter bw, uint value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseBE());
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from a little-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>unsigned 16-bit integer in current system endianness.</returns>
        public static ushort ReadUInt16LE(this BinaryReader br)
        {
            return BitConverter.ToUInt16(br.ReadBytesRequired(sizeof(ushort)).ReverseLE(), 0);
        }

        /// <summary>
        /// Reads a signed 16-bit integer from a little-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>signed 16-bit integer in current system endianness.</returns>
        public static short ReadInt16LE(this BinaryReader br)
        {
            return BitConverter.ToInt16(br.ReadBytesRequired(sizeof(short)).ReverseLE(), 0);
        }

        /// <summary>
        /// Reads an unsigned 24-bit integer from a little-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>unsigned 24-bit integer in current system endianness.</returns>
        public static uint ReadUInt24LE(this BinaryReader br)
        {
            byte[] returnArray = new byte[4];
            byte[] buffer = br.ReadBytesRequired(3);

            returnArray[0] = buffer[0];
            returnArray[1] = buffer[1];
            returnArray[2] = buffer[2];
            returnArray[3] = 0;

            return BitConverter.ToUInt32(returnArray.ReverseLE(), 0);
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from a little-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>unsigned 32-bit integer in current system endianness.</returns>
        public static uint ReadUInt32LE(this BinaryReader br)
        {
            return BitConverter.ToUInt32(br.ReadBytesRequired(sizeof(uint)).ReverseLE(), 0);
        }

        /// <summary>
        /// Reads a signed 32-bit integer from a little-endian source.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <returns>signed 32-bit integer in current system endianness.</returns>
        public static int ReadInt32LE(this BinaryReader br)
        {
            return BitConverter.ToInt32(br.ReadBytesRequired(sizeof(int)).ReverseLE(), 0);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer in little-endian format.
        /// </summary>
        /// <param name="bw">binary reader used to write the data.</param>
        /// <param name="value">value to write.</param>
        public static void WriteUInt16LE(this BinaryWriter bw, ushort value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseLE());
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer in little-endian format.
        /// </summary>
        /// <param name="bw">binary reader used to write the data.</param>
        /// <param name="value">value to write.</param>
        public static void WriteUInt32LE(this BinaryWriter bw, uint value)
        {
            bw.Write(BitConverter.GetBytes(value).ReverseLE());
        }

        /// <summary>
        /// Reads a certain number of bytes from a stream and throws an exception is fewer bytes are received.
        /// </summary>
        /// <param name="br">binary reader used to read the data.</param>
        /// <param name="byteCount">number of bytes to read.</param>
        /// <returns>bytes read.</returns>
        public static byte[] ReadBytesRequired(this BinaryReader br, int byteCount)
        {
            var result = br.ReadBytes(byteCount);

            if (result.Length != byteCount)
            {
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));
            }

            return result;
        }

        /// <summary>
        /// Reads a null-terminated string from a stream.
        /// </summary>
        /// <param name="stream">binary reader used to read the data.</param>
        /// <returns>null-terminated string.</returns>
        public static string ReadNullTerminatedString(this BinaryReader stream)
        {
            string str = string.Empty;
            char ch;
            while ((int)(ch = stream.ReadChar()) != 0)
            {
                str = str + ch;
            }

            return str;
        }

        /// <summary>
        /// Reads a null-terminated string from a byte array.
        /// </summary>
        /// <param name="bytes">byte array to read.</param>
        /// <returns>null-terminated string.</returns>
        public static string ReadNullTerminatedString(this byte[] bytes)
        {
            string str = string.Empty;

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using BinaryReader br = new BinaryReader(ms);
                str = br.ReadNullTerminatedString();
            }

            return str;
        }
    }
}
