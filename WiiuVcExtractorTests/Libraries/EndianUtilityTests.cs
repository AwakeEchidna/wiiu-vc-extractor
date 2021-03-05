namespace WiiuVcExtractorTests.Libraries
{
    using System;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;
    using Xunit;

    public class EndianUtilityTests
    {
        public EndianUtilityTests()
        {
            // Reset endianness to system endianness before each test
            EndianUtility.Endianness = BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;
        }

        [Fact]
        public void ReverseBE_OnBESystem_DoesNothing()
        {
            EndianUtility.Endianness = Endianness.BigEndian;

            var byteArray = new byte[] { 0, 1, 2, 3 };
            var result = EndianUtility.ReverseBE(byteArray);

            Assert.Equal(byteArray, result);
        }

        [Fact]
        public void ReverseBE_OnLESystem_ReversesBytes()
        {
            EndianUtility.Endianness = Endianness.LittleEndian;

            var byteArray = new byte[] { 0, 1, 2, 3 };
            var reversedArray = new byte[] { 3, 2, 1, 0 };

            var result = EndianUtility.ReverseBE(byteArray);

            Assert.Equal(reversedArray, result);
        }

        [Fact]
        public void ReverseLE_OnBESystem_ReversesBytes()
        {
            EndianUtility.Endianness = Endianness.BigEndian;

            var byteArray = new byte[] { 0, 1, 2, 3 };
            var reversedArray = new byte[] { 3, 2, 1, 0 };

            var result = EndianUtility.ReverseLE(byteArray);

            Assert.Equal(reversedArray, result);
        }

        [Fact]
        public void ReverseLE_OnLESystem_DoesNothing()
        {
            EndianUtility.Endianness = Endianness.LittleEndian;

            var byteArray = new byte[] { 0, 1, 2, 3 };
            var result = EndianUtility.ReverseLE(byteArray);

            Assert.Equal(byteArray, result);
        }

        [Fact]
        public void ReadUInt16BE_WithValidUint16_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadUInt16BE(br);
            Assert.Equal(4096u, result);
        }

        [Fact]
        public void ReadUInt16BE_WithInvalidUint16_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadUInt16BE(br));
        }

        [Fact]
        public void ReadInt16BE_WithValidInt16_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0xA0, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadInt16BE(br);
            Assert.Equal(-24576, result);
        }

        [Fact]
        public void ReadInt16BE_WithInvalidInt16_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadInt16BE(br));
        }

        [Fact]
        public void ReadUInt32BE_WithValidUint32_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x20, 0x00, 0x10, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadUInt32BE(br);
            Assert.Equal(536875008u, result);
        }

        [Fact]
        public void ReadUInt32BE_WithInvalidUint32_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0x20, 0x00 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadUInt32BE(br));
        }

        [Fact]
        public void ReadInt32BE_WithValidInt32_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0xA0, 0x01, 0xBE, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadInt32BE(br);
            Assert.Equal(-1610498560, result);
        }

        [Fact]
        public void ReadInt32BE_WithInvalidInt32_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0xFF, 0xFF });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadInt32BE(br));
        }

        [Fact]
        public void WriteUInt16BE_WithUInt16_WritesData()
        {
            using MemoryStream ms = new MemoryStream(new byte[2]);
            using BinaryWriter bw = new BinaryWriter(ms);
            EndianUtility.WriteUInt16BE(bw, 1234);
            ms.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            Assert.Equal(1234, EndianUtility.ReadUInt16BE(br));
        }

        [Fact]
        public void WriteUInt32BE_WithUInt32_WritesData()
        {
            using MemoryStream ms = new MemoryStream(new byte[4]);
            using BinaryWriter bw = new BinaryWriter(ms);
            EndianUtility.WriteUInt32BE(bw, 12345678u);
            ms.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            Assert.Equal(12345678u, EndianUtility.ReadUInt32BE(br));
        }

        [Fact]
        public void ReadUInt16LE_WithValidUint16_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadUInt16LE(br);
            Assert.Equal(16u, result);
        }

        [Fact]
        public void ReadUInt16LE_WithInvalidUint16_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadUInt16LE(br));
        }

        [Fact]
        public void ReadInt16LE_WithValidInt16_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0xA0, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadInt16LE(br);
            Assert.Equal(160, result);
        }

        [Fact]
        public void ReadInt16LE_WithInvalidInt16_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadInt16LE(br));
        }

        [Fact]
        public void ReadUInt24LE_WithValidUInt24_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0x0, 0xB });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadUInt24LE(br);
            Assert.Equal(720912u, result);
        }

        [Fact]
        public void ReadUInt24LE_WithInvalidUInt24_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadUInt24LE(br));
        }

        [Fact]
        public void ReadUInt32LE_WithValidUInt32_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x01, 0xEE, 0xA0, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadUInt32LE(br);
            Assert.Equal(10546689u, result);
        }

        [Fact]
        public void ReadUInt32LE_WithInvalidUInt32_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0x00, 0xBB });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadUInt32LE(br));
        }

        [Fact]
        public void ReadInt32LE_WithValidInt32_ReadsData()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x01, 0xEE, 0xA0, 0x0 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            var result = EndianUtility.ReadInt32LE(br);
            Assert.Equal(10546689, result);
        }

        [Fact]
        public void ReadInt32LE_WithInvalidInt32_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0x00, 0xBB });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadInt32LE(br));
        }

        [Fact]
        public void WriteUInt16LE_WithUInt16_WritesData()
        {
            using MemoryStream ms = new MemoryStream(new byte[2]);
            using BinaryWriter bw = new BinaryWriter(ms);
            EndianUtility.WriteUInt16LE(bw, 1234);
            ms.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            Assert.Equal(1234u, EndianUtility.ReadUInt16LE(br));
        }

        [Fact]
        public void WriteUInt32LE_WithUInt32_WritesData()
        {
            using MemoryStream ms = new MemoryStream(new byte[4]);
            using BinaryWriter bw = new BinaryWriter(ms);
            EndianUtility.WriteUInt32LE(bw, 12345678u);
            ms.Seek(0, SeekOrigin.Begin);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            Assert.Equal(12345678u, EndianUtility.ReadUInt32LE(br));
        }

        [Fact]
        public void ReadBytesRequired_WithEnoughBytes_ReadsBytes()
        {
            var byteArray = new byte[] { 0, 1, 2, 3 };
            using MemoryStream ms = new MemoryStream(byteArray);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            var result = EndianUtility.ReadBytesRequired(br, 4);

            Assert.Equal(byteArray, result);
        }

        [Fact]
        public void ReadBytesRequired_WithoutEnoughBytes_ThrowsException()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x10, 0x00, 0xBB });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            Assert.Throws<System.IO.EndOfStreamException>(() => EndianUtility.ReadBytesRequired(br, 100));
        }

        [Fact]
        public void ReadNullTerminatedString_WithStream_ReadsString()
        {
            using MemoryStream ms = new MemoryStream(new byte[] { 0x63, 0x61, 0x74, 0x00, 0x55 });
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            var result = EndianUtility.ReadNullTerminatedString(br);

            Assert.Equal("cat", result);
        }

        [Fact]
        public void ReadNullTerminatedString_WithByteArray_ReadsString()
        {
            var byteArray = new byte[] { 0x63, 0x61, 0x74, 0x00, 0x55 };

            var result = EndianUtility.ReadNullTerminatedString(byteArray);

            Assert.Equal("cat", result);
        }
    }
}
