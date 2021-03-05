namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// PSB file entry table.
    /// </summary>
    public class PsbEntryTable
    {
        private readonly List<uint> names;
        private readonly List<uint> offsets;
        private readonly List<PsbFileInfoEntry> fileInfoEntries;
        private readonly float version;
        private readonly string id;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsbEntryTable"/> class.
        /// </summary>
        /// <param name="psbData">PSB file content.</param>
        /// <param name="entriesOffset">offset to the beginning of the file info entries in bytes.</param>
        /// <param name="nameStrings">filename string list.</param>
        /// <param name="strings">PSB string string list.</param>
        public PsbEntryTable(byte[] psbData, long entriesOffset, List<string> nameStrings, List<string> strings)
        {
            long entryDataOffset = 0;

            // Initialize the name table from the passed data
            using MemoryStream ms = new MemoryStream(psbData);
            ms.Seek(entriesOffset, SeekOrigin.Begin);

            byte type = (byte)ms.ReadByte();
            if (type != 0x21)
            {
                throw new InvalidOperationException("The type of the entries data is incorrect.");
            }

            this.names = this.ReadEntryTableValues(ms);
            this.offsets = this.ReadEntryTableValues(ms);

            entryDataOffset = ms.Position;

            if (this.names.Count != this.offsets.Count)
            {
                throw new InvalidOperationException("The lengths of the entry names list and the entry offsets list differ.");
            }

            for (int i = 0; i < this.names.Count; i++)
            {
                string nameString = nameStrings[(int)this.names[i]];
                long offset = entryDataOffset + this.offsets[i];

                ms.Seek(offset, SeekOrigin.Begin);

                switch (nameString)
                {
                    case "id":
                        this.id = this.ReadId(ms, strings);
                        break;

                    case "version":
                        this.version = this.ReadVersion(ms);
                        break;

                    case "file_info":
                        this.fileInfoEntries = this.ReadFileInfo(ms, nameStrings);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets file info entries for the PSB file.
        /// </summary>
        public List<PsbFileInfoEntry> FileInfoEntries
        {
            get { return this.fileInfoEntries; }
        }

        /// <summary>
        /// Gets PSB file version.
        /// </summary>
        public float Version
        {
            get { return this.version; }
        }

        /// <summary>
        /// Gets PSB file ID.
        /// </summary>
        public string Id
        {
            get { return this.id; }
        }

        private List<uint> ReadEntryTableValues(MemoryStream ms)
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

        private float ReadVersion(MemoryStream ms)
        {
            float returnFloat = 0.0F;

            using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding(), true))
            {
                byte type = br.ReadByte();
                if (type != 30)
                {
                    throw new InvalidOperationException("The type of the version in the PSB file is not a 4-byte float.");
                }

                returnFloat = br.ReadSingle();
            }

            return returnFloat;
        }

        private string ReadId(MemoryStream ms, List<string> strings)
        {
            uint stringIndex = 0;

            using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding(), true))
            {
                byte type = br.ReadByte();
                if (type < 21 || type > 24)
                {
                    throw new InvalidOperationException("The type of the id in the PSB file is not an index into the strings array.");
                }

                int byteSize = type - 20;

                if (byteSize == 1)
                {
                    stringIndex = br.ReadByte();
                }
                else if (byteSize == 2)
                {
                    stringIndex = br.ReadUInt16LE();
                }
                else if (byteSize == 4)
                {
                    stringIndex = br.ReadUInt32LE();
                }
            }

            if (stringIndex >= strings.Count)
            {
                throw new InvalidOperationException("The string index of the id in the PSB file is not a valid index in the strings array.");
            }

            return strings[(int)stringIndex];
        }

        private List<PsbFileInfoEntry> ReadFileInfo(MemoryStream ms, List<string> nameStrings)
        {
            List<uint> fileNameIndexes = new List<uint>();
            List<uint> fileOffsets = new List<uint>();
            List<PsbFileInfoEntry> fileInfos = new List<PsbFileInfoEntry>();
            long entryDataOffset = 0;

            using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding(), true))
            {
                byte type = br.ReadByte();
                if (type != 33)
                {
                    throw new InvalidOperationException("The type of the id in the PSB file is not for a FileInfo section.");
                }

                fileNameIndexes = this.ReadEntryTableValues(ms);
                fileOffsets = this.ReadEntryTableValues(ms);

                entryDataOffset = ms.Position;

                if (fileNameIndexes.Count != fileOffsets.Count)
                {
                    throw new InvalidOperationException("The lengths of the FileInfo entry names list and the FileInfo entry offsets list differ.");
                }

                // populate the file info list
                for (int i = 0; i < fileNameIndexes.Count; i++)
                {
                    string nameString = nameStrings[(int)fileNameIndexes[i]];
                    long offset = entryDataOffset + fileOffsets[i];

                    ms.Seek(offset, SeekOrigin.Begin);

                    byte fiType = br.ReadByte();

                    if (fiType != 32)
                    {
                        throw new InvalidOperationException("The type of the id in the FileInfo section is not for an object array.");
                    }

                    List<uint> fiOffsets = new List<uint>();
                    fiOffsets = this.ReadEntryTableValues(ms);

                    long baseOffset = ms.Position;

                    if (fiOffsets.Count != 2)
                    {
                        throw new InvalidOperationException("The number of FileInfo offsets if not 2.");
                    }

                    // Get the file offset and length
                    uint fileOffset = 0;
                    uint fileLength = 0;

                    ms.Seek(baseOffset + fiOffsets[0], SeekOrigin.Begin);
                    fileOffset = this.ReadFileInfoMetadata(ms);

                    ms.Seek(baseOffset + fiOffsets[1], SeekOrigin.Begin);
                    fileLength = this.ReadFileInfoMetadata(ms);

                    PsbFileInfoEntry fi = new PsbFileInfoEntry(fileNameIndexes[i], fileLength, fileOffset);
                    fileInfos.Add(fi);
                }
            }

            return fileInfos;
        }

        private uint ReadFileInfoMetadata(MemoryStream ms)
        {
            uint returnValue = 0;

            using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding(), true))
            {
                byte type = br.ReadByte();

                if (type == 4)
                {
                    returnValue = 0;
                }
                else if (type >= 5 && type <= 12)
                {
                    int intByteSize = type - 4;

                    if (intByteSize == 1)
                    {
                        returnValue = br.ReadByte();
                    }
                    else if (intByteSize == 2)
                    {
                        returnValue = br.ReadUInt16LE();
                    }
                    else if (intByteSize == 3)
                    {
                        returnValue = br.ReadUInt24LE();
                    }
                    else if (intByteSize == 4)
                    {
                        returnValue = br.ReadUInt32LE();
                    }
                    else
                    {
                        Console.WriteLine("intByteSize: " + intByteSize);
                    }
                }
            }

            return returnValue;
        }
    }
}
