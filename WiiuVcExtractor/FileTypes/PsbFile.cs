namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// Represents a PSB file (typically used for GBA VC).
    /// </summary>
    public class PsbFile : IDisposable
    {
        private const string RomSubfilePath = "system/roms/";

        private readonly MdfPsbFile compressedPsbFile;
        private readonly PsbHeader header;
        private readonly List<string> names;
        private readonly List<string> strings;
        private readonly List<byte[]> subfiles;
        private readonly string decompressedPath;
        private readonly byte[] psbData;
        private readonly byte[] binData;
        private readonly string romPath; // path to rom on disk

        private PsbNameTable nameTable;
        private PsbChunkTable chunkTable;
        private PsbEntryTable entryTable;
        private byte[] romData;
        private string romName; // just the filename
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsbFile"/> class.
        /// </summary>
        /// <param name="psbFilePath">path to the PSB file.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public PsbFile(string psbFilePath, bool verbose = false)
        {
            try
            {
                if (verbose)
                {
                    Console.WriteLine("Attempting to read PSB file from {0}...", psbFilePath);
                }

                this.compressedPsbFile = new MdfPsbFile(psbFilePath, verbose);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not successfully read PSB file: " + ex.Message);
                return;
            }

            // Get the psb data from the file decompressed by the MdfPsbFile
            this.header = new PsbHeader(this.compressedPsbFile.DecompressedPath);
            this.psbData = File.ReadAllBytes(this.compressedPsbFile.DecompressedPath);

            this.names = new List<string>();
            this.strings = new List<string>();

            if (verbose)
            {
                Console.WriteLine("PSB Header information:\n{0}", this.header.ToString());
            }

            // Unpack the structures of the file
            this.UnpackNames();

            this.UnpackStrings();

            this.UnpackChunks();

            this.UnpackEntries();

            // Attempt to read in the alldata.bin file
            string absolutePath = Path.GetFullPath(psbFilePath);
            string binPath = Path.Combine(Path.GetDirectoryName(absolutePath), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(psbFilePath)) + ".bin");
            Console.WriteLine("Checking for PSB data file " + binPath + "...");

            if (!File.Exists(binPath))
            {
                throw new FileNotFoundException("Could not find PSB data file at " + binPath + ". Please ensure that your filename is correct.");
            }

            this.binData = File.ReadAllBytes(binPath);
            this.subfiles = new List<byte[]>();

            this.SplitSubfiles(verbose);

            if (this.romData != null && !string.IsNullOrEmpty(this.romName))
            {
                Console.WriteLine("Decompressing rom...");

                this.romPath = Path.Combine(Path.GetDirectoryName(absolutePath), this.romName);

                // Remove the temp file if it exists
                if (File.Exists(this.romPath))
                {
                    File.Delete(this.romPath);
                }

                File.WriteAllBytes(this.romPath, this.romData);

                MdfPsbFile romMdfFile = new MdfPsbFile(this.romPath);

                this.decompressedPath = romMdfFile.DecompressedPath;
            }
        }

        /// <summary>
        /// Gets the path to the decompressed PSB file.
        /// </summary>
        public string DecompressedPath
        {
            get { return this.decompressedPath; }
        }

        /// <summary>
        /// Gets the PSB chunk table.
        /// </summary>
        public PsbChunkTable ChunkTable
        {
            get { return this.chunkTable; }
        }

        /// <summary>
        /// Whether the provided path is a valid PSB file.
        /// </summary>
        /// <param name="psbFilePath">path to the PSB file to validate.</param>
        /// <returns>true if valid, false otherwise.</returns>
        public static bool IsPsb(string psbFilePath)
        {
            MdfHeader header = new MdfHeader(psbFilePath);
            return header.IsValid();
        }

        /// <summary>
        /// Converts the PSB file to a string summary.
        /// </summary>
        /// <returns>string summary of the PSB file's content.</returns>
        public override string ToString()
        {
            string returnString = string.Empty;

            returnString += "Files:" + Environment.NewLine;
            for (int i = 0; i < this.names.Count; i++)
            {
                returnString += this.names[i] + Environment.NewLine;
            }

            returnString += Environment.NewLine + "Strings:" + Environment.NewLine;
            for (int i = 0; i < this.strings.Count; i++)
            {
                returnString += this.strings[i] + Environment.NewLine;
            }

            return returnString;
        }

        /// <summary>
        /// Dispose PsbFile.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose PsbFile.
        /// </summary>
        /// <param name="disposing">whether disposing the file.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.compressedPsbFile.Dispose();
                }

                if (File.Exists(this.romPath))
                {
                    Console.WriteLine("Deleting file {0}", this.romPath);
                    File.Delete(this.romPath);
                }

                // set large fields to null (if any)
                this.disposedValue = true;
            }
        }

        private void UnpackNames()
        {
            this.nameTable = new PsbNameTable(this.psbData, this.header.NamesOffset);
            for (int i = 0; i < this.nameTable.Starts.Count; i++)
            {
                string nameBuffer = this.nameTable.GetName(i);
                this.names.Add(nameBuffer);
            }
        }

        private void UnpackStrings()
        {
            using MemoryStream ms = new MemoryStream(this.psbData);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            br.BaseStream.Seek(this.header.StringsOffset, SeekOrigin.Begin);

            List<uint> stringValueList = new List<uint>();

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

            uint value = 0;

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
                uint specificStringOffset = stringValueList[i];

                br.BaseStream.Seek(this.header.StringsDataOffset + specificStringOffset, SeekOrigin.Begin);

                string stringData = br.ReadNullTerminatedString();

                this.strings.Add(stringData);
            }
        }

        private void UnpackChunks()
        {
            this.chunkTable = new PsbChunkTable(this.psbData, this.header.ChunkOffsetsOffset, this.header.ChunkLengthsOffset, this.header.ChunkDataOffset);
        }

        private void UnpackEntries()
        {
            this.entryTable = new PsbEntryTable(this.psbData, this.header.EntriesOffset, this.names, this.strings);
        }

        private void SplitSubfiles(bool verbose = false)
        {
            using MemoryStream ms = new MemoryStream(this.binData);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            // Iterate through the file info table and read in the subfiles
            for (int i = 0; i < this.entryTable.FileInfoEntries.Count; i++)
            {
                string subfileName = this.names[(int)this.entryTable.FileInfoEntries[i].NameIndex];
                uint offset = this.entryTable.FileInfoEntries[i].Offset;
                uint length = this.entryTable.FileInfoEntries[i].Length;

                ms.Seek(offset, SeekOrigin.Begin);
                byte[] buffer = br.ReadBytes((int)length);

                if (verbose)
                {
                    Console.WriteLine("Checking if {0} contains the rom subfile path...", subfileName);
                }

                // Check if this is the rom subfile
                if (subfileName.Contains(RomSubfilePath))
                {
                    this.romData = buffer;
                    this.romName = Path.GetFileName(subfileName).ToLower();
                    Console.WriteLine("Found rom subfile at " + this.romName);
                    Console.WriteLine("    Offset: " + offset);
                    Console.WriteLine("    Length: " + length);
                }

                this.subfiles.Add(buffer);
            }
        }
    }
}
