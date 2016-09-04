using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor
{
    class PsbHeader
    {
        public const int HEADER_SIZE = 40;

        private const int SIGNATURE_SIZE = 4;
        private const int TYPE_HEADER_OFFSET = 4;
        private const int UNKNOWN_HEADER_OFFSET = 8;
        private const int NAMES_HEADER_OFFSET = 12;
        private const int STRINGS_HEADER_OFFSET = 16;
        private const int STRINGS_DATA_HEADER_OFFSET = 20;
        private const int CHUNK_OFFSETS_HEADER_OFFSET = 24;
        private const int CHUNK_LENGTHS_HEADER_OFFSET = 28;
        private const int CHUNK_DATA_HEADER_OFFSET = 32;
        private const int ENTRIES_HEADER_OFFSET = 36;

        private static readonly byte[] SIGNATURE_CHECK = { 0x50, 0x53, 0x42, 0x00 };

        private byte[] signature = new byte[SIGNATURE_SIZE];
        private UInt32 type;
        private UInt32 unknown;
        private UInt32 namesOffset;
        private UInt32 strings;
        private UInt32 stringsData;
        private UInt32 chunkOffsets;
        private UInt32 chunkLengths;
        private UInt32 chunkData;
        private UInt32 entries;

        public byte[] Signature {get {return this.signature;} }
        public UInt32 Type { get { return this.type; } }
        public UInt32 NamesOffset { get { return this.namesOffset; } }

        public PsbHeader(byte[] decompressedPsbData)
        {
            // Get the PSB's signature
            Array.Copy(decompressedPsbData, this.signature, SIGNATURE_SIZE);

            // Get the PSB's length
            this.type = BitConverter.ToUInt32(decompressedPsbData, TYPE_HEADER_OFFSET);
            this.unknown = BitConverter.ToUInt32(decompressedPsbData, UNKNOWN_HEADER_OFFSET);
            this.namesOffset = BitConverter.ToUInt32(decompressedPsbData, NAMES_HEADER_OFFSET);
            this.strings = BitConverter.ToUInt32(decompressedPsbData, STRINGS_HEADER_OFFSET);
            this.stringsData = BitConverter.ToUInt32(decompressedPsbData, STRINGS_DATA_HEADER_OFFSET);
            this.chunkOffsets = BitConverter.ToUInt32(decompressedPsbData, CHUNK_OFFSETS_HEADER_OFFSET);
            this.chunkLengths = BitConverter.ToUInt32(decompressedPsbData, CHUNK_LENGTHS_HEADER_OFFSET);
            this.chunkData = BitConverter.ToUInt32(decompressedPsbData, CHUNK_DATA_HEADER_OFFSET);
            this.entries = BitConverter.ToUInt32(decompressedPsbData, ENTRIES_HEADER_OFFSET);
        }

        public override string ToString()
        {
            return "Signature: " + BitConverter.ToString(this.signature) + Environment.NewLine + "Type: " + this.type;
        }

        public bool Valid()
        {
            for (int i = 0; i < signature.Length; i++)
            {
                if (signature[i] != SIGNATURE_CHECK[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
