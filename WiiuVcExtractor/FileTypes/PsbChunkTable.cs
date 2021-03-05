namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// PSB file chunk table.
    /// </summary>
    public class PsbChunkTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PsbChunkTable"/> class.
        /// </summary>
        /// <param name="psbData">The PSB data to parse.</param>
        /// <param name="chunkOffsetsOffset">The offset to begin reading chunk offset data.</param>
        /// <param name="chunkLengthsOffset">The offset to begin reading chunk length data.</param>
        /// <param name="chunkDataOffset">The offset to begni reading chunk data.</param>
        public PsbChunkTable(byte[] psbData, long chunkOffsetsOffset, long chunkLengthsOffset, long chunkDataOffset)
        {
            // Initialize the name table from the passed data
            using MemoryStream ms = new MemoryStream(psbData);
            ms.Seek(chunkOffsetsOffset, SeekOrigin.Begin);
            this.Offsets = this.ReadChunkTableValues(ms);

            ms.Seek(chunkLengthsOffset, SeekOrigin.Begin);
            this.Lengths = this.ReadChunkTableValues(ms);

            if (this.Offsets.Count != this.Lengths.Count)
            {
                throw new InvalidOperationException("The lengths of the chunk offsets list and the chunk lengths list differ.");
            }

            // Only attempt to read in the chunks if they exist in the file
            if (this.Offsets.Count > 0 && psbData.Length > chunkDataOffset)
            {
                ms.Seek(chunkDataOffset, SeekOrigin.Begin);

                // TODO: Add code to read in chunks, may not be necessary for the GBA extraction
            }
        }

        /// <summary>
        /// Gets PSB chunk offsets. Each offset indicates when a chunk begins in the PSB chunk table.
        /// </summary>
        public List<uint> Offsets { get; }

        /// <summary>
        /// Gets PSB chunk lengths.
        /// </summary>
        public List<uint> Lengths { get; }

        /// <summary>
        /// Gets PSB chunk data (currently unused).
        /// </summary>
        public byte[] ChunkData { get; }

        private List<uint> ReadChunkTableValues(MemoryStream ms)
        {
            List<uint> valueList = new List<uint>();

            using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding(), true))
            {
                // get the offset information
                byte type = br.ReadByte();

                // Get the size of each object in bytes
                int countByteSize = type - 12;
                uint count = 0;

                if (countByteSize == 1)
                {
                    count = br.ReadByte();
                }
                else if (countByteSize == 2)
                {
                    count = EndianUtility.ReadUInt16LE(br);
                }
                else if (countByteSize == 4)
                {
                    count = EndianUtility.ReadUInt32LE(br);
                }

                byte entrySizeType = br.ReadByte();
                int entryByteSize = entrySizeType - 12;

                uint value = 0;

                // Read in the values
                for (int i = 0; i < count; i++)
                {
                    if (countByteSize == 1)
                    {
                        value = br.ReadByte();
                    }

                    if (entryByteSize == 2)
                    {
                        value = EndianUtility.ReadUInt16LE(br);
                    }
                    else if (entryByteSize == 4)
                    {
                        value = EndianUtility.ReadUInt32LE(br);
                    }

                    valueList.Add(value);
                }
            }

            return valueList;
        }
    }
}
