using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class PsbHeader
    {
        public const int PSB_HEADER_LENGTH = 40;

        private static readonly byte[] PSB_SIGNATURE = { 0x50, 0x53, 0x42, 0x00 };
        private const int PSB_SIGNATURE_LENGTH = 4;

        private const int HEADER_TITLE_OFFSET = 0;
        private const int HEADER_TITLE_LENGTH = 21;
        private const int HEADER_ROM_SIZE_OFFSET = 23;
        private const int HEADER_SRAM_SIZE_OFFSET = 24;
        private const int HEADER_FIXED_VALUE_OFFSET = 26;
        private const int HEADER_CHECKSUM_COMPLEMENT_OFFSET = 28;
        private const int HEADER_CHECKSUM_OFFSET = 30;

        private const int HEADER_TYPE_OFFSET = 4;
        private const int HEADER_UNKNOWN_OFFSET = 8;
        private const int HEADER_NAMES_OFFSET = 12;
        private const int HEADER_STRINGS_OFFSET = 16;
        private const int HEADER_STRINGS_DATA_OFFSET = 20;
        private const int HEADER_CHUNK_OFFSETS_OFFSET = 24;
        private const int HEADER_CHUNK_LENGTHS_OFFSET = 28;
        private const int HEADER_CHUNK_DATA_OFFSET = 32;
        private const int HEADER_ENTRIES_OFFSET = 36;

        private byte[] signature;
        private UInt32 type;
        private UInt32 unknown;
        private UInt32 namesOffset;
        private UInt32 stringsOffset;
        private UInt32 stringsDataOffset;
        private UInt32 chunkOffsetsOffset;
        private UInt32 chunkLengthsOffset;
        private UInt32 chunkDataOffset;
        private UInt32 entriesOffset;

        public UInt32 NamesOffset { get { return namesOffset; } }
        public UInt32 StringsOffset { get { return stringsOffset; } }
        public UInt32 StringsDataOffset { get { return stringsDataOffset; } }
        public UInt32 ChunkOffsetsOffset { get { return chunkOffsetsOffset; } }
        public UInt32 ChunkLengthsOffset { get { return chunkLengthsOffset; } }
        public UInt32 ChunkDataOffset { get { return chunkDataOffset; } }
        public UInt32 EntriesOffset { get { return entriesOffset; } }

        public PsbHeader(string psbPath)
        {
            signature = new byte[PSB_SIGNATURE_LENGTH];

            using (FileStream fs = new FileStream(psbPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // Read in the header
                    signature = br.ReadBytes(PSB_SIGNATURE_LENGTH);
                    type = EndianUtility.ReadUInt32LE(br);
                    unknown = EndianUtility.ReadUInt32LE(br);
                    namesOffset = EndianUtility.ReadUInt32LE(br);
                    stringsOffset = EndianUtility.ReadUInt32LE(br);
                    stringsDataOffset = EndianUtility.ReadUInt32LE(br);
                    chunkOffsetsOffset = EndianUtility.ReadUInt32LE(br);
                    chunkLengthsOffset = EndianUtility.ReadUInt32LE(br);
                    chunkDataOffset = EndianUtility.ReadUInt32LE(br);
                    entriesOffset = EndianUtility.ReadUInt32LE(br);
                }
            }
        }

        public bool IsValid()
        {
            // Check that the signature is correct
            if (signature[0] != PSB_SIGNATURE[0] ||
                signature[1] != PSB_SIGNATURE[1] ||
                signature[2] != PSB_SIGNATURE[2] ||
                signature[3] != PSB_SIGNATURE[3])
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "PsbHeader:\n" +
                   "signature: " + BitConverter.ToString(signature) + "\n" +
                   "type: " + type.ToString() + "\n" +
                   "unknown: " + unknown.ToString() + "\n" +
                   "namesOffset: " + namesOffset.ToString() + "\n" +
                   "stringsOffset: " + stringsOffset.ToString() + "\n" +
                   "stringsDataOffset: " + stringsDataOffset.ToString() + "\n" +
                   "chunkOffsetsOffset: " + chunkOffsetsOffset.ToString() + "\n" +
                   "chunkLengthsOffset: " + chunkLengthsOffset.ToString() + "\n" +
                   "chunkDataOffset: " + chunkDataOffset.ToString() + "\n" +
                   "entriesOffset: " + entriesOffset.ToString() + "\n";
        }
    }
}
