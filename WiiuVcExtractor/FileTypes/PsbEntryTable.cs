using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class PsbEntryTable
    {
        List<UInt32> names;
        List<UInt32> offsets;
        List<PsbFileInfoEntry> fileInfoEntries;
        float version;
        string id;

        public List<PsbFileInfoEntry> FileInfoEntries { get { return fileInfoEntries; } }

        public PsbEntryTable(byte[] psbData, long entriesOffset, List<String> nameStrings, List<String> strings)
        {
            long entryDataOffset = 0;

            // Initialize the name table from the passed data
            using (MemoryStream ms = new MemoryStream(psbData))
            {
                ms.Seek(entriesOffset, SeekOrigin.Begin);

                byte type = (byte)ms.ReadByte();
                if (type != 0x21)
                {
                    throw new InvalidOperationException("The type of the entries data is incorrect.");
                }

                names = ReadEntryTableValues(ms);
                offsets = ReadEntryTableValues(ms);

                entryDataOffset = ms.Position;

                if (names.Count != offsets.Count)
                {
                    throw new InvalidOperationException("The lengths of the entry names list and the entry offsets list differ.");
                }

                for (int i = 0; i < names.Count; i++)
                {
                    string nameString = nameStrings[(int)names[i]];
                    long offset = entryDataOffset + offsets[i];

                    ms.Seek(offset, SeekOrigin.Begin);

                    switch(nameString)
                    {
                        case "id":
                            id = ReadId(ms, strings);
                            break;

                        case "version":
                            version = ReadVersion(ms);
                            break;

                        case "file_info":
                            fileInfoEntries = ReadFileInfo(ms, nameStrings);
                            break;
                    }
                }
            }
        }

        private List<UInt32> ReadEntryTableValues(MemoryStream ms)
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
            List<UInt32> fileNameIndexes = new List<UInt32>();
            List<UInt32> fileOffsets = new List<UInt32>();
            List<PsbFileInfoEntry> fileInfos = new List<PsbFileInfoEntry>();
            long entryDataOffset = 0;

            using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding(), true))
            {
                byte type = br.ReadByte();
                if (type != 33)
                {
                    throw new InvalidOperationException("The type of the id in the PSB file is not for a FileInfo section.");
                }

                fileNameIndexes = ReadEntryTableValues(ms);
                fileOffsets = ReadEntryTableValues(ms);

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

                    List<UInt32> fiOffsets = new List<uint>();
                    fiOffsets = ReadEntryTableValues(ms);

                    long baseOffset = ms.Position;

                    if (fiOffsets.Count != 2)
                    {
                        throw new InvalidOperationException("The number of FileInfo offsets if not 2.");
                    }

                    // Get the file offset and length
                    uint fileOffset = 0;
                    uint fileLength = 0;

                    ms.Seek(baseOffset + fiOffsets[0], SeekOrigin.Begin);
                    fileOffset = ReadFileInfoMetadata(ms);

                    ms.Seek(baseOffset + fiOffsets[1], SeekOrigin.Begin);
                    fileLength = ReadFileInfoMetadata(ms);

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
