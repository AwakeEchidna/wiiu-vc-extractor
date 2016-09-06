using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class PsbNameTable
    {
        List<UInt32> offsets;
        List<UInt32> jumps;
        List<UInt32> starts;

        public List<UInt32> Offsets { get { return offsets; } }
        public List<UInt32> Jumps { get { return jumps; } }
        public List<UInt32> Starts { get { return starts; } }

        public PsbNameTable(byte[] psbData, long namesOffset)
        {
            // Initialize the name table from the passed data
            using (MemoryStream ms = new MemoryStream(psbData))
            {
                ms.Seek(namesOffset, SeekOrigin.Begin);

                offsets = ReadNameTableValues(ms);
                jumps = ReadNameTableValues(ms);
                starts = ReadNameTableValues(ms);
            }
        }

        public string GetName(int index)
        {
            uint a = starts[index];

            // Follow one jump to skip the terminating NUL
            uint b = jumps[(int)a];

            string returnString = "";

            while (b != 0)
            {
                uint c = jumps[(int)b];

                uint d = offsets[(int)c];

                uint e = b - d;

                returnString = Convert.ToChar(e) + returnString;

                b = c;
            }

            return returnString;
        }

        private List<UInt32> ReadNameTableValues(MemoryStream ms)
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
                    if (entryByteSize == 1)
                    {
                        value = br.ReadByte();
                    }
                    else if (entryByteSize == 2)
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
