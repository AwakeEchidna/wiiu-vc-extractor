using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Ionic.Zlib;

using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class RpxFile
    {
        RpxHeader header;
        List<RpxSectionHeader> sectionHeaders;
        List<RpxSectionHeaderSort> sectionHeaderIndices;
        List<UInt32> crcs;
        string path;
        string decompressedPath;

        UInt64 crcDataOffset;

        bool verbose;

        public string Path { get { return path; } }
        public string DecompressedPath { get { return decompressedPath; } }

        public static bool IsRpx(string rpxFilePath)
        {
            RpxHeader header = new RpxHeader(rpxFilePath);
            return header.IsValid();
        }

        public RpxFile(string rpxFilePath, bool verbose = false)
        {
            Console.WriteLine("Decompressing RPX file...");
            this.verbose = verbose;

            path = rpxFilePath;
            decompressedPath = path + ".extract";

            // Remove the temp file if it exists
            if (File.Exists(decompressedPath))
            {
                if (verbose)
                {
                    Console.WriteLine("Removing file " + System.IO.Path.GetFullPath(decompressedPath));
                }
                File.Delete(decompressedPath);
            }

            crcDataOffset = 0;

            header = new RpxHeader(path);

            // Begin writing header to *.rpx.extract
            using (FileStream fs = new FileStream(decompressedPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs, new ASCIIEncoding()))
                {
                    bw.Write(header.Identity);
                    EndianUtility.WriteUInt16BE(bw, header.Type);
                    EndianUtility.WriteUInt16BE(bw, header.Machine);
                    EndianUtility.WriteUInt32BE(bw, header.Version);
                    EndianUtility.WriteUInt32BE(bw, header.EntryPoint);
                    EndianUtility.WriteUInt32BE(bw, header.PhOffset);
                    EndianUtility.WriteUInt32BE(bw, header.SectionHeaderOffset);
                    EndianUtility.WriteUInt32BE(bw, header.Flags);
                    EndianUtility.WriteUInt16BE(bw, header.EhSize);
                    EndianUtility.WriteUInt16BE(bw, header.PhEntSize);
                    EndianUtility.WriteUInt16BE(bw, header.PhNum);
                    EndianUtility.WriteUInt16BE(bw, header.ShEntSize);
                    EndianUtility.WriteUInt16BE(bw, header.SectionHeaderCount);
                    EndianUtility.WriteUInt16BE(bw, header.ShStrIndex);

                    EndianUtility.WriteUInt32BE(bw, 0x00000000);
                    EndianUtility.WriteUInt32BE(bw, 0x00000000);
                    EndianUtility.WriteUInt32BE(bw, 0x00000000);

                    while ((ulong)bw.BaseStream.Position < header.SectionHeaderDataElfOffset)
                    {
                        bw.Write((byte)0);
                    }

                    while (bw.BaseStream.Position % 0x40 != 0)
                    {
                        bw.Write((byte)0);
                        header.SectionHeaderDataElfOffset++;
                    }
                }
            }

            sectionHeaderIndices = new List<RpxSectionHeaderSort>();
            sectionHeaders = new List<RpxSectionHeader>(header.SectionHeaderCount);
            crcs = new List<uint>(header.SectionHeaderCount);

            if (verbose)
            {
                Console.WriteLine(header.ToString());
            }

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // Seek to the Section Header Offset in the file
                    br.BaseStream.Seek(header.SectionHeaderOffset,SeekOrigin.Begin);

                    // Read in all of the section headers
                    for (UInt32 i = 0; i < header.SectionHeaderCount; i++)
                    {
                        crcs.Add(0);

                        // Read in the bytes for the section header
                        byte[] buffer = br.ReadBytes(RpxSectionHeader.SECTION_HEADER_LENGTH);

                        // Create a new section header and add it to the list
                        RpxSectionHeader newSectionHeader = new RpxSectionHeader(buffer);
                        sectionHeaders.Add(newSectionHeader);

                        if (newSectionHeader.Offset != 0)
                        {
                            RpxSectionHeaderSort sectionHeaderIndex = new RpxSectionHeaderSort();
                            sectionHeaderIndex.index = i;
                            sectionHeaderIndex.offset = newSectionHeader.Offset;
                            sectionHeaderIndices.Add(sectionHeaderIndex);

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
            }

            sectionHeaderIndices.Sort();

            // Iterate through all of the section header indices
            for (int i = 0; i < sectionHeaderIndices.Count; i++)
            {
                if (verbose)
                {
                    Console.WriteLine(sectionHeaderIndices[i].ToString());
                }

                // Seek to the correct part of the file
                RpxSectionHeader currentSectionHeader = sectionHeaders[(int)sectionHeaderIndices[i].index];
                UInt64 position = currentSectionHeader.Offset;

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        br.BaseStream.Seek((long)position, SeekOrigin.Begin);

                        currentSectionHeader.Offset = (uint)br.BaseStream.Position;

                        if ((currentSectionHeader.Flags & RpxSectionHeader.SECTION_HEADER_RPL_ZLIB) == RpxSectionHeader.SECTION_HEADER_RPL_ZLIB)
                        {
                            UInt32 dataSize = currentSectionHeader.Size - 4;
                            currentSectionHeader.Size = EndianUtility.ReadUInt32BE(br);
                            UInt32 blockSize = RpxSectionHeader.CHUNK_SIZE;
                            UInt32 have;
                            byte[] bufferIn = new byte[RpxSectionHeader.CHUNK_SIZE];
                            byte[] bufferOut = new byte[RpxSectionHeader.CHUNK_SIZE];

                            ZlibCodec compressor = new ZlibCodec();
                            compressor.InitializeInflate(true);
                            compressor.AvailableBytesIn = 0;
                            compressor.NextIn = 0;

                            while (dataSize > 0)
                            {
                                blockSize = RpxSectionHeader.CHUNK_SIZE;
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
                                    compressor.AvailableBytesOut = (int)RpxSectionHeader.CHUNK_SIZE;
                                    compressor.NextOut = 0;
                                    compressor.Inflate(FlushType.None);

                                    have = RpxSectionHeader.CHUNK_SIZE - (uint)compressor.AvailableBytesOut;

                                    // write the data
                                    using (FileStream outFs = new FileStream(decompressedPath, FileMode.Append))
                                    {
                                        using (BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding()))
                                        {
                                            bw.Write(bufferOut, 0, (int)have);
                                        }
                                    }

                                    crcs[(int)sectionHeaderIndices[i].index] = Crc32Rpx(crcs[(int)sectionHeaderIndices[i].index], bufferOut, have);
                                } while (compressor.AvailableBytesOut == 0);

                                currentSectionHeader.Flags &= ~RpxSectionHeader.SECTION_HEADER_RPL_ZLIB;
                            }
                        }
                        else
                        {
                            UInt32 dataSize = currentSectionHeader.Size;
                            UInt32 blockSize = RpxSectionHeader.CHUNK_SIZE;

                            while (dataSize > 0)
                            {
                                byte[] data = new byte[RpxSectionHeader.CHUNK_SIZE];
                                blockSize = RpxSectionHeader.CHUNK_SIZE;

                                if (dataSize < blockSize)
                                {
                                    blockSize = dataSize;
                                }

                                dataSize -= blockSize;

                                data = br.ReadBytes((int)blockSize);

                                // Write out the section bytes
                                using (FileStream outFs = new FileStream(decompressedPath, FileMode.Append))
                                {
                                    using (BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding()))
                                    {
                                        bw.Write(data);
                                    }
                                }

                                crcs[(int)sectionHeaderIndices[i].index] = Crc32Rpx(crcs[(int)sectionHeaderIndices[i].index], data, blockSize);
                            }
                        }

                        // Pad out the section on a 0x40 byte boundary
                        using (FileStream outFs = new FileStream(decompressedPath, FileMode.Append))
                        {
                            using (BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding()))
                            {
                                while(bw.BaseStream.Position % 0x40 != 0)
                                {
                                    bw.Write((byte)0);
                                }
                            }
                        }

                        if ((currentSectionHeader.Type & RpxSectionHeader.SECTION_HEADER_RPL_CRCS) == RpxSectionHeader.SECTION_HEADER_RPL_CRCS)
                        {
                            crcs[(int)sectionHeaderIndices[i].index] = 0;
                            crcDataOffset = currentSectionHeader.Offset;
                        }
                    }
                }

                sectionHeaders[(int)sectionHeaderIndices[i].index] = currentSectionHeader;
            }

            // Fix the output headers
            // TODO: This is not currently accurate vs. wiiurpx tool so may need to investigate
            using (FileStream outFs = new FileStream(decompressedPath, FileMode.Open, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(outFs, new ASCIIEncoding()))
                {
                    bw.Seek((int)header.SectionHeaderOffset, SeekOrigin.Begin);

                    for (UInt32 i = 0; i < header.SectionHeaderCount; i++)
                    {
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Name);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Type);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Flags);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Address);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Offset);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Size);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Link);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].Info);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].AddrAlign);
                        EndianUtility.WriteUInt32BE(bw, sectionHeaders[(int)i].EntSize);
                    }

                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                        {
                            // Seek to the Section Header Offset in the file
                            br.BaseStream.Seek((long)crcDataOffset, SeekOrigin.Begin);

                            for (UInt32 i = 0; i < header.SectionHeaderCount; i++)
                            {
                                EndianUtility.WriteUInt32BE(bw, crcs[(int)i]);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Decompression complete.");
        }

        ~RpxFile()
        {
            // Attempt to clean up the decompressed file if it exists
            if (File.Exists(decompressedPath))
            {
                File.Delete(decompressedPath);
            }
        }

        UInt32 Crc32Rpx(UInt32 crc, byte[] buffer, UInt32 len)
        {
            UInt32[] crcTable = new UInt32[256];
            for (UInt32 i = 0; i < 256; i++)
            {
                UInt32 c = i;

                for (UInt32 j = 0; j < 8; j++)
                {

                    if ((c & 1) == 1)
                        c = (UInt32)0xedb88320L ^ (c >> 1);
                    else
                        c = c >> 1;
                }
                crcTable[i] = c;
            }
            crc = ~crc;
            for (UInt32 i = 0; i < len; i++)
                crc = (crc >> 8) ^ crcTable[(crc ^ buffer[i]) & 0xff];
            return ~crc;
        }
    }
}
