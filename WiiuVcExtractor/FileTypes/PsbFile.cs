using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class PsbFile
    {
        const string ROM_SUBFILE_PATH = "system/roms/";

        MdfPsbFile compressedPsbFile;
        PsbHeader header;
        PsbNameTable nameTable;
        PsbChunkTable chunkTable;
        PsbEntryTable entryTable;
        List<string> names;
        List<string> strings;
        List<byte[]> subfiles;

        string decompressedPath;

        private byte[] psbData;
        private byte[] binData;
        private byte[] romData;
        private string romPath;

        public string DecompressedPath { get { return decompressedPath; } }

        public static bool IsPsb(string psbFilePath)
        {
            MdfHeader header = new MdfHeader(psbFilePath);
            return header.IsValid();
        }

        public PsbFile(string psbFilePath)
        {
            try
            {
                compressedPsbFile = new MdfPsbFile(psbFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not successfully read PSB file: " + ex.Message);
                return;
            }

            // Get the psb data from the file decompressed by the MdfPsbFile
            header = new PsbHeader(compressedPsbFile.DecompressedPath);
            psbData = File.ReadAllBytes(compressedPsbFile.DecompressedPath);

            names = new List<string>();
            strings = new List<string>();

            //Console.WriteLine(header.ToString());

            // Unpack the structures of the file
            UnpackNames();

            UnpackStrings();

            UnpackChunks();

            UnpackEntries();

            // Attempt to read in the alldata.bin file
            string binPath = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(psbFilePath)) + ".bin";
            Console.WriteLine("Checking for PSB data file " + binPath + "...");

            if (!File.Exists(binPath))
            {
                throw new FileNotFoundException("Could not find PSB data file at " + binPath + ". Please ensure that your filename is correct.");
            }

            binData = File.ReadAllBytes(binPath);
            subfiles = new List<byte[]>();

            SplitSubfiles();

            if (romData != null && !String.IsNullOrEmpty(romPath))
            {
                Console.WriteLine("Decompressing rom...");

                // Remove the temp file if it exists
                if (File.Exists(romPath))
                {
                    File.Delete(romPath);
                }

                File.WriteAllBytes(romPath, romData);

                MdfPsbFile romMdfFile = new MdfPsbFile(romPath);

                decompressedPath = romMdfFile.DecompressedPath;
            }
        }

        ~PsbFile()
        {
            // Cleanup
            if (File.Exists(romPath))
            {
                File.Delete(romPath);
            }
        }

        private void UnpackNames()
        {
            nameTable = new PsbNameTable(psbData, header.NamesOffset);

            string nameBuffer = "";
            for (int i = 0; i < nameTable.Starts.Count; i++)
            {
                nameBuffer = nameTable.GetName(i);
                names.Add(nameBuffer);
            }
        }

        private void UnpackStrings()
        {
            using (MemoryStream ms = new MemoryStream(psbData))
            {
                using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding()))
                {
                    br.BaseStream.Seek(header.StringsOffset, SeekOrigin.Begin);

                    List<UInt32> stringValueList = new List<uint>();

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
                        count = br.ReadUInt16LE();
                    }
                    else if (countByteSize == 4)
                    {
                        count = br.ReadUInt32LE();
                    }

                    byte entrySizeType = br.ReadByte();
                    int entryByteSize = entrySizeType - 12;

                    UInt32 value = 0;

                    // Read in the string offset values
                    for (int i = 0; i < count; i++)
                    {
                        if (entryByteSize == 1)
                        {
                            value = br.ReadByte();
                        }
                        else if (entryByteSize == 2)
                        {
                            value = br.ReadUInt16LE();
                        }
                        else if (entryByteSize == 4)
                        {
                            value = br.ReadUInt32LE();
                        }

                        stringValueList.Add(value);
                    }

                    // Get the strings
                    for (int i = 0; i < stringValueList.Count; i++)
                    {
                        UInt32 specificStringOffset = stringValueList[i];

                        br.BaseStream.Seek(header.StringsDataOffset + specificStringOffset, SeekOrigin.Begin);

                        string stringData = br.ReadNullTerminatedString();

                        strings.Add(stringData);
                    }
                }
            }
        }

        private void UnpackChunks()
        {
            chunkTable = new PsbChunkTable(psbData, header.ChunkOffsetsOffset, header.ChunkLengthsOffset, header.ChunkDataOffset);
        }

        private void UnpackEntries()
        {
            entryTable = new PsbEntryTable(psbData, header.EntriesOffset, names, strings);
        }

        private void SplitSubfiles()
        {
            using (MemoryStream ms = new MemoryStream(binData))
            {
                using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding()))
                {
                    // Iterate through the file info table and read in the subfiles
                    for (int i = 0; i < entryTable.FileInfoEntries.Count; i++)
                    {
                        string subfileName = names[(int)entryTable.FileInfoEntries[i].NameIndex];
                        uint offset = entryTable.FileInfoEntries[i].Offset;
                        uint length = entryTable.FileInfoEntries[i].Length;

                        ms.Seek(offset, SeekOrigin.Begin);
                        byte[] buffer = br.ReadBytes((int)length);

                        // Check if this is the rom subfile
                        if (subfileName.Contains(ROM_SUBFILE_PATH))
                        {
                            romData = buffer;
                            romPath = Path.GetFileName(subfileName).ToLower();
                            Console.WriteLine("Found rom subfile at " + romPath);
                            Console.WriteLine("    Offset: " + offset);
                            Console.WriteLine("    Length: " + length);
                        }

                        subfiles.Add(buffer);
                    }
                }
            }
        }

        public override string ToString()
        {
            string returnString = "";

            returnString += "Files:" + Environment.NewLine;
            for (int i = 0; i < names.Count; i++)
            {
                returnString += names[i] + Environment.NewLine;
            }

            returnString += Environment.NewLine + "Strings:" + Environment.NewLine;
            for (int i = 0; i < strings.Count; i++)
            {
                returnString += strings[i] + Environment.NewLine;
            }

            return returnString;
        }
    }
}
