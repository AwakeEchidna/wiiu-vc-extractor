namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// PSB file name table.
    /// </summary>
    public class PsbNameTable
    {
        private readonly List<uint> offsets;
        private readonly List<uint> jumps;
        private readonly List<uint> starts;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsbNameTable"/> class.
        /// </summary>
        /// <param name="psbData">PSB data byte array.</param>
        /// <param name="namesOffset">Beginning of the names table in the PSB data.</param>
        public PsbNameTable(byte[] psbData, long namesOffset)
        {
            // Initialize the name table from the passed data
            using MemoryStream ms = new MemoryStream(psbData);
            ms.Seek(namesOffset, SeekOrigin.Begin);

            this.offsets = this.ReadNameTableValues(ms);
            this.jumps = this.ReadNameTableValues(ms);
            this.starts = this.ReadNameTableValues(ms);
        }

        /// <summary>
        /// Gets PSB name table offsets (in bytes).
        /// </summary>
        public List<uint> Offsets
        {
            get { return this.offsets; }
        }

        /// <summary>
        /// Gets PSB name table jumps.
        /// </summary>
        public List<uint> Jumps
        {
            get { return this.jumps; }
        }

        /// <summary>
        /// Gets PSB name table starts.
        /// </summary>
        public List<uint> Starts
        {
            get { return this.starts; }
        }

        /// <summary>
        /// Gets the name at a given index of the PSB name table.
        /// </summary>
        /// <param name="index">name index.</param>
        /// <returns>retrieved name.</returns>
        public string GetName(int index)
        {
            uint a = this.starts[index];

            // Follow one jump to skip the terminating NUL
            uint b = this.jumps[(int)a];

            string returnString = string.Empty;

            while (b != 0)
            {
                uint c = this.jumps[(int)b];

                uint d = this.offsets[(int)c];

                uint e = b - d;

                returnString = Convert.ToChar(e) + returnString;

                b = c;
            }

            return returnString;
        }

        private List<uint> ReadNameTableValues(MemoryStream ms)
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
