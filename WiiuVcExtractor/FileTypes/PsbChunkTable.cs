using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class PsbChunkTable
    {
        List<UInt32> offsets;
        List<UInt32> lengths;
        byte[] chunkData;

        public List<UInt32> Offsets { get { return offsets; } }
        public List<UInt32> Lengths { get { return lengths; } }
        public byte[] ChunkData { get { return chunkData; } }

        public PsbChunkTable(byte[] psbData, long chunkOffsetsOffset, long chunkLengthsOffset, long chunkDataOffset)
        {
            // Initialize the name table from the passed data
            using (MemoryStream ms = new MemoryStream(psbData))
            {
                ms.Seek(chunkOffsetsOffset, SeekOrigin.Begin);
                offsets = ReadChunkTableValues(ms);

                ms.Seek(chunkLengthsOffset, SeekOrigin.Begin);
                lengths = ReadChunkTableValues(ms);

                if (offsets.Count != lengths.Count)
                {
                    throw new InvalidOperationException("The lengths of the chunk offsets list and the chunk lengths list differ.");
                }

                // Only attempt to read in the chunks if they exist in the file
                if (offsets.Count > 0 && psbData.Length > chunkDataOffset)
                {
                    ms.Seek(chunkDataOffset, SeekOrigin.Begin);
                    // TODO: Add code to read in chunks, may not be necessary for the GBA extraction
                }
            }
        }

        private List<UInt32> ReadChunkTableValues(MemoryStream ms)
        {
            List<UInt32> valueList = new List<uint>();

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

                UInt32 value = 0;

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
