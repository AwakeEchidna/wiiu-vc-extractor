namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Ionic.Zlib;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// RPX file.
    /// </summary>
    public class RpxFile : IDisposable
    {
        private readonly RpxHeader header;
        private readonly List<RpxSectionHeader> sectionHeaders;
        private readonly List<RpxSectionHeaderSort> sectionHeaderIndices;
        private readonly List<uint> crcs;
        private readonly string path;
        private readonly string decompressedPath;
        private readonly ulong crcDataOffset;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RpxFile"/> class.
        /// </summary>
        /// <param name="rpxFilePath">path to the RPX file.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public RpxFile(string rpxFilePath, bool verbose = false)
        {
            Console.WriteLine("Decompressing RPX file...");

            this.path = rpxFilePath;
            this.decompressedPath = this.path + ".extract";

            // Remove the temp file if it exists
            if (File.Exists(this.decompressedPath))
            {
                if (verbose)
                {
                    Console.WriteLine("Removing file " + System.IO.Path.GetFullPath(this.decompressedPath));
                }

                File.Delete(this.decompressedPath);
            }

            this.crcDataOffset = 0;

            this.header = new RpxHeader(this.path);

            using (FileStream fs = new FileStream(this.decompressedPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using BinaryWriter bw = new BinaryWriter(fs, new ASCIIEncoding());
                bw.Write(this.header.Identity);
                EndianUtility.WriteUInt16BE(bw, this.header.Type);
                EndianUtility.WriteUInt16BE(bw, this.header.Machine);
                EndianUtility.WriteUInt32BE(bw, this.header.Version);
                EndianUtility.WriteUInt32BE(bw, this.header.EntryPoint);
                EndianUtility.WriteUInt32BE(bw, this.header.PhOffset);
                EndianUtility.WriteUInt32BE(bw, this.header.SectionHeaderOffset);
                EndianUtility.WriteUInt32BE(bw, this.header.Flags);
                EndianUtility.WriteUInt16BE(bw, this.header.EhSize);
                EndianUtility.WriteUInt16BE(bw, this.header.PhEntSize);
                EndianUtility.WriteUInt16BE(bw, this.header.PhNum);
                EndianUtility.WriteUInt16BE(bw, this.header.ShEntSize);
                EndianUtility.WriteUInt16BE(bw, this.header.SectionHeaderCount);
                EndianUtility.WriteUInt16BE(bw, this.header.ShStrIndex);

                EndianUtility.WriteUInt32BE(bw, 0x00000000);
                EndianUtility.WriteUInt32BE(bw, 0x00000000);
                EndianUtility.WriteUInt32BE(bw, 0x00000000);

                while ((ulong)bw.BaseStream.Position < this.header.SectionHeaderDataElfOffset)
                {
                    bw.Write((byte)0);
                }

                while (bw.BaseStream.Position % 0x40 != 0)
                {
                    bw.Write((byte)0);
                    this.header.SectionHeaderDataElfOffset++;
                }
            }

            this.sectionHeaderIndices = new List<RpxSectionHeaderSort>();
            this.sectionHeaders = new List<RpxSectionHeader>(this.header.SectionHeaderCount);
            this.crcs = new List<uint>(this.header.SectionHeaderCount);

            if (verbose)
            {
                Console.WriteLine(this.header.ToString());
            }

            using (FileStream fs = new FileStream(this.path, FileMode.Open, FileAccess.Read))
            {
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

                // Seek to the Section Header Offset in the file
                br.BaseStream.Seek(this.header.SectionHeaderOffset, SeekOrigin.Begin);

                // Read in all of the section headers
                for (uint i = 0; i < this.header.SectionHeaderCount; i++)
                {
                    this.crcs.Add(0);

                    // Read in the bytes for the section header
                    byte[] buffer = br.ReadBytes(RpxSectionHeader.SectionHeaderLength);

                    // Create a new section header and add it to the list
                    RpxSectionHeader newSectionHeader = new RpxSectionHeader(buffer);
                    this.sectionHeaders.Add(newSectionHeader);

                    if (newSectionHeader.Offset != 0)
                    {
                        RpxSectionHeaderSort sectionHeaderIndex = new RpxSectionHeaderSort
                        {
                            Index = i,
                            Offset = newSectionHeader.Offset,
                        };
                        this.sectionHeaderIndices.Add(sectionHeaderIndex);

                        if (verbose)
                        {
                            Console.WriteLine(sectionHeaderIndex.ToString());
                        }
                    }

                    if (verbose)
                    {
                        Console.WriteLine(newSectionHeader.ToString());
                    }
                }
            }

            this.sectionHeaderIndices.Sort();

            // Iterate through all of the section header indices
            for (int i = 0; i < this.sectionHeaderIndices.Count; i++)
            {
                if (verbose)
                {
                    Console.WriteLine(this.sectionHeaderIndices[i].ToString());
                }
                else
                {
                    Console.Write(".");
                }

                // Seek to the correct part of the file
                RpxSectionHeader currentSectionHeader = this.sectionHeaders[(int)this.sectionHeaderIndices[i].Index];
                ulong position = currentSectionHeader.Offset;

                using (FileStream fs = new FileStream(this.path, FileMode.Open, FileAccess.Read))
                {
                    using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                    br.BaseStream.Seek((long)position, SeekOrigin.Begin);

                    currentSectionHeader.Offset = (uint)br.BaseStream.Position;

                    if ((currentSectionHeader.Flags & RpxSectionHeader.SectionHeaderRplZlib) == RpxSectionHeader.SectionHeaderRplZlib)
                    {
                        uint dataSize = currentSectionHeader.Size - 4;
                        currentSectionHeader.Size = EndianUtility.ReadUInt32BE(br);
                        uint blockSize = RpxSectionHeader.ChunkSize;
                        uint have;
                        byte[] bufferIn = new byte[RpxSectionHeader.ChunkSize];
                        byte[] bufferOut = new byte[RpxSectionHeader.ChunkSize];

                        ZlibCodec compressor = new ZlibCodec();
                        compressor.InitializeInflate(true);
                        compressor.AvailableBytesIn = 0;
                        compressor.NextIn = 0;

                        while (dataSize > 0)
                        {
                            blockSize = RpxSectionHeader.ChunkSize;
                            if (dataSize < blockSize)
                            {
                                blockSize = dataSize;
                            }

                            dataSize -= blockSize;

                            bufferIn = br.ReadBytes((int)blockSize);
                            compressor.NextIn = 0;
                            compressor.InputBuffer = bufferIn;
                            compressor.AvailableBytesIn = bufferIn.Length;
                            compressor.OutputBuffer = bufferOut;

                            do
                            {
                                compressor.AvailableBytesOut = (int)RpxSectionHeader.ChunkSize;
                                compressor.NextOut = 0;
                                compressor.Inflate(FlushType.None);

                                have = RpxSectionHeader.ChunkSize - (uint)compressor.AvailableBytesOut;

                                // write the data
                                using (FileStream outFs = new FileStream(this.decompressedPath, FileMode.Append))
                                {
                                    using BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding());
                                    bw.Write(bufferOut, 0, (int)have);
                                }

                                this.crcs[(int)this.sectionHeaderIndices[i].Index] = this.Crc32Rpx(this.crcs[(int)this.sectionHeaderIndices[i].Index], bufferOut, have);
                            }
                            while (compressor.AvailableBytesOut == 0);

                            currentSectionHeader.Flags &= ~RpxSectionHeader.SectionHeaderRplZlib;
                        }
                    }
                    else
                    {
                        uint dataSize = currentSectionHeader.Size;
                        uint blockSize = RpxSectionHeader.ChunkSize;

                        while (dataSize > 0)
                        {
                            byte[] data = new byte[RpxSectionHeader.ChunkSize];
                            blockSize = RpxSectionHeader.ChunkSize;

                            if (dataSize < blockSize)
                            {
                                blockSize = dataSize;
                            }

                            dataSize -= blockSize;

                            data = br.ReadBytes((int)blockSize);

                            // Write out the section bytes
                            using (FileStream outFs = new FileStream(this.decompressedPath, FileMode.Append))
                            {
                                using BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding());
                                bw.Write(data);
                            }

                            this.crcs[(int)this.sectionHeaderIndices[i].Index] = this.Crc32Rpx(this.crcs[(int)this.sectionHeaderIndices[i].Index], data, blockSize);
                        }
                    }

                    // Pad out the section on a 0x40 byte boundary
                    using (FileStream outFs = new FileStream(this.decompressedPath, FileMode.Append))
                    {
                        using BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding());
                        while (bw.BaseStream.Position % 0x40 != 0)
                        {
                            bw.Write((byte)0);
                        }
                    }

                    if ((currentSectionHeader.Type & RpxSectionHeader.SectionHeaderRplCrcs) == RpxSectionHeader.SectionHeaderRplCrcs)
                    {
                        this.crcs[(int)this.sectionHeaderIndices[i].Index] = 0;
                        this.crcDataOffset = currentSectionHeader.Offset;
                    }
                }

                this.sectionHeaders[(int)this.sectionHeaderIndices[i].Index] = currentSectionHeader;
            }

            // Fix the output headers
            // TODO: This is not currently accurate vs. wiiurpx tool so may need to investigate
            using (FileStream outFs = new FileStream(this.decompressedPath, FileMode.Open, FileAccess.Write))
            {
                using BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding());
                bw.Seek((int)this.header.SectionHeaderOffset, SeekOrigin.Begin);

                for (uint i = 0; i < this.header.SectionHeaderCount; i++)
                {
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Name);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Type);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Flags);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Address);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Offset);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Size);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Link);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].Info);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].AddrAlign);
                    EndianUtility.WriteUInt32BE(bw, this.sectionHeaders[(int)i].EntSize);
                }

                using FileStream fs = new FileStream(this.path, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

                // Seek to the Section Header Offset in the file
                br.BaseStream.Seek((long)this.crcDataOffset, SeekOrigin.Begin);

                for (uint i = 0; i < this.header.SectionHeaderCount; i++)
                {
                    EndianUtility.WriteUInt32BE(bw, this.crcs[(int)i]);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Decompression complete.");
        }

        /// <summary>
        /// Gets the compressed RPX file path.
        /// </summary>
        public string Path
        {
            get { return this.path; }
        }

        /// <summary>
        /// Gets the decompressed RPX file path (typically rpx_file_name.rpx.extract).
        /// </summary>
        public string DecompressedPath
        {
            get { return this.decompressedPath; }
        }

        /// <summary>
        /// Whether the given file is a valid RPX file.
        /// </summary>
        /// <param name="rpxFilePath">path to the file.</param>
        /// <returns>true if valid, false otherwise.</returns>
        public static bool IsRpx(string rpxFilePath)
        {
            RpxHeader header = new RpxHeader(rpxFilePath);
            return header.IsValid();
        }

        /// <summary>
        /// Dispose RpxFile.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose RpxFile.
        /// </summary>
        /// <param name="disposing">whether disposing the file.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                if (File.Exists(this.decompressedPath))
                {
                    Console.WriteLine("Deleting decompressed file {0}", this.decompressedPath);
                    File.Delete(this.decompressedPath);
                }

                // set large fields to null (if any)
                this.disposedValue = true;
            }
        }

        private uint Crc32Rpx(uint crc, byte[] buffer, uint len)
        {
            uint[] crcTable = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;

                for (uint j = 0; j < 8; j++)
                {
                    if ((c & 1) == 1)
                    {
                        c = 0xedb88320U ^ (c >> 1);
                    }
                    else
                    {
                        c >>= 1;
                    }
                }

                crcTable[i] = c;
            }

            crc = ~crc;
            for (uint i = 0; i < len; i++)
            {
                crc = (crc >> 8) ^ crcTable[(crc ^ buffer[i]) & 0xff];
            }

            return ~crc;
        }
    }
}
