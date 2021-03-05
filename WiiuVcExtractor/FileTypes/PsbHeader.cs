namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// PSB file header.
    /// </summary>
    public class PsbHeader
    {
        /// <summary>
        /// Length of a PSB file header in bytes.
        /// </summary>
        public const int PsbHeaderLength = 40;

        private const int PsbSignatureLength = 4;

        /*
        // Header offsets and lengths (currently unused)
        // private const int HeaderTitleOffset = 0;
        // private const int HeaderTitleLength = 21;
        // private const int HeaderRomSizeOffset = 23;
        // private const int HeaderSramSizeOffset = 24;
        // private const int HeaderFixedValueOffset = 26;
        // private const int HeaderChecksumComplementOffset = 28;
        // private const int HeaderChecksumOffset = 30;
        // private const int HeaderTypeOffset = 4;
        // private const int HeaderUnknownOffset = 8;
        // private const int HeaderNamesOffset = 12;
        // private const int HeaderStringsOffset = 16;
        // private const int HeaderStringsDataOffset = 20;
        // private const int HeaderChunkOffsetsOffset = 24;
        // private const int HeaderChunkLengthsOffset = 28;
        // private const int HeaderChunkDataOffset = 32;
        // private const int HeaderEntriesOffset = 36;
        */

        private static readonly byte[] PsbSignature = { 0x50, 0x53, 0x42, 0x00 };

        private readonly byte[] signature;
        private readonly uint type;
        private readonly uint unknown;
        private readonly uint namesOffset;
        private readonly uint stringsOffset;
        private readonly uint stringsDataOffset;
        private readonly uint chunkOffsetsOffset;
        private readonly uint chunkLengthsOffset;
        private readonly uint chunkDataOffset;
        private readonly uint entriesOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsbHeader"/> class.
        /// </summary>
        /// <param name="psbPath">path to the PSB file.</param>
        public PsbHeader(string psbPath)
        {
            this.signature = new byte[PsbSignatureLength];

            using FileStream fs = new FileStream(psbPath, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

            // Read in the header
            this.signature = br.ReadBytes(PsbSignatureLength);
            this.type = EndianUtility.ReadUInt32LE(br);
            this.unknown = EndianUtility.ReadUInt32LE(br);
            this.namesOffset = EndianUtility.ReadUInt32LE(br);
            this.stringsOffset = EndianUtility.ReadUInt32LE(br);
            this.stringsDataOffset = EndianUtility.ReadUInt32LE(br);
            this.chunkOffsetsOffset = EndianUtility.ReadUInt32LE(br);
            this.chunkLengthsOffset = EndianUtility.ReadUInt32LE(br);
            this.chunkDataOffset = EndianUtility.ReadUInt32LE(br);
            this.entriesOffset = EndianUtility.ReadUInt32LE(br);
        }

        /// <summary>
        /// Gets PSB file names offset in bytes.
        /// </summary>
        public uint NamesOffset
        {
            get { return this.namesOffset; }
        }

        /// <summary>
        /// Gets PSB file strings offset in bytes.
        /// </summary>
        public uint StringsOffset
        {
            get { return this.stringsOffset; }
        }

        /// <summary>
        /// Gets PSB file strings data offset in bytes.
        /// </summary>
        public uint StringsDataOffset
        {
            get { return this.stringsDataOffset; }
        }

        /// <summary>
        /// Gets PSB file chunk offsets offset in bytes.
        /// </summary>
        public uint ChunkOffsetsOffset
        {
            get { return this.chunkOffsetsOffset; }
        }

        /// <summary>
        /// Gets PSB file chunk lengths offset in bytes.
        /// </summary>
        public uint ChunkLengthsOffset
        {
            get { return this.chunkLengthsOffset; }
        }

        /// <summary>
        /// Gets PSB file chunk data offset in bytes.
        /// </summary>
        public uint ChunkDataOffset
        {
            get { return this.chunkDataOffset; }
        }

        /// <summary>
        /// Gets PSB file entries offset in bytes.
        /// </summary>
        public uint EntriesOffset
        {
            get { return this.entriesOffset; }
        }

        /// <summary>
        /// whether the PSB header is valid.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValid()
        {
            // Check that the signature is correct
            if (this.signature[0] != PsbSignature[0] ||
                this.signature[1] != PsbSignature[1] ||
                this.signature[2] != PsbSignature[2] ||
                this.signature[3] != PsbSignature[3])
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Provides a string representation of the PSB header.
        /// </summary>
        /// <returns>string representation of the PSB header.</returns>
        public override string ToString()
        {
            return "PsbHeader:\n" +
                   "signature: " + BitConverter.ToString(this.signature) + "\n" +
                   "type: " + this.type.ToString() + "\n" +
                   "unknown: " + this.unknown.ToString() + "\n" +
                   "namesOffset: " + this.namesOffset.ToString() + "\n" +
                   "stringsOffset: " + this.stringsOffset.ToString() + "\n" +
                   "stringsDataOffset: " + this.stringsDataOffset.ToString() + "\n" +
                   "chunkOffsetsOffset: " + this.chunkOffsetsOffset.ToString() + "\n" +
                   "chunkLengthsOffset: " + this.chunkLengthsOffset.ToString() + "\n" +
                   "chunkDataOffset: " + this.chunkDataOffset.ToString() + "\n" +
                   "entriesOffset: " + this.entriesOffset.ToString() + "\n";
        }
    }
}
