using System;
using System.Collections.Generic;
using WiiuVcExtractor.Libraries.Sdd1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WiiuVcExtractor.Libraries
{
    public class SnesSdd1Extractor
    {
        private static readonly byte[] SDD1_SIGNATURE = { 0x53, 0x44, 0x44, 0x31 }; // SDD1 ASCII
        private byte[] romData;
        private byte[] rawSdd1Data;
        private long sdd1DataOffset;
        private List<Sdd1Pointer> sdd1Pointers;

        public SnesSdd1Extractor(byte[] snesRomData, byte[] rawSdd1Data, long sdd1DataOffset)
        {
            this.sdd1DataOffset = sdd1DataOffset;
            this.romData = snesRomData;
            this.rawSdd1Data = rawSdd1Data;
            sdd1Pointers = new List<Sdd1Pointer>();
        }

        public byte[] ExtractSdd1Data()
        {
            Console.WriteLine("Extracting S-DD1 Data...");

            Console.WriteLine("Appended rom data size: {0}", rawSdd1Data.Length);

            byte[] processedRom = new byte[romData.Length];
            Array.Copy(romData, processedRom, romData.Length);

            // Find all SDD1 signatures in the rom and store them
            using (MemoryStream ms = new MemoryStream(romData))
            {
                using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding()))
                {
                    // Continue reading SDD1 pointer data until we run out
                    while (true)
                    {
                        long index = br.BaseStream.Position;

                        if (index + SDD1_SIGNATURE.Length >= br.BaseStream.Length)
                        {
                            break;
                        }

                        byte[] signatureBuffer = br.ReadBytes(SDD1_SIGNATURE.Length);

                        // If we didn't find a signature at this location, seek to the next character and restart the loop
                        if (!signatureBuffer.SequenceEqual(SDD1_SIGNATURE))
                        {
                            br.BaseStream.Seek(-3, SeekOrigin.Current);
                            continue;
                        }

                        // Store the pointer location in the rom (pointerLocation) and the
                        // offset in the rawSdd1Data (dataLocation)
                        // We must add the sdd1DataOffset to the read location offset to get to the right
                        // position in the rawSdd1Data (appendedData)
                        sdd1Pointers.Add(new Sdd1Pointer(index, br.ReadUInt32LE() + sdd1DataOffset));

                        // Set the previous data length based on the current offset - the last offset
                        if (sdd1Pointers.Count > 1)
                        {
                            sdd1Pointers[sdd1Pointers.Count - 2].dataLength = sdd1Pointers[sdd1Pointers.Count - 1].dataLocation - sdd1Pointers[sdd1Pointers.Count - 2].dataLocation;
                        }
                    }
                }
            }

            if (sdd1Pointers.Count == 0)
            {
                Console.WriteLine("No S-DD1 data found, continuing...");
                return processedRom;
            }

            sdd1Pointers[sdd1Pointers.Count - 1].dataLength = rawSdd1Data.Length - sdd1Pointers[sdd1Pointers.Count - 1].dataLocation;

            Console.WriteLine("{0} S-DD1 pointers found", sdd1Pointers.Count);
            Console.WriteLine("Reading S-DD1 data into memory...");

            Compressor compressor = new Compressor();

            byte[] outBuffer = new byte[0x40000];

            // Find the decompressed data for each pointer, compress it, and replace the first 8 bytes of each pointer location (SDD1UINT becomes the compressed data)
            foreach (var sdd1ptr in sdd1Pointers)
            {
                // Read the decompressed data
                byte[] decompressedData = new byte[sdd1ptr.dataLength];
                using (MemoryStream ms = new MemoryStream(rawSdd1Data))
                {
                    using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding()))
                    {
                        br.BaseStream.Seek(sdd1ptr.dataLocation, SeekOrigin.Begin);
                        decompressedData = br.ReadBytes((int)sdd1ptr.dataLength);
                    }
                }

                // Compress the decompressed data
                uint outLength = 0;
                byte[] compressedData = compressor.Compress(decompressedData, out outLength, outBuffer);

                // Replace with the recompressed data
                using (MemoryStream msb = new MemoryStream(processedRom))
                {
                    using (BinaryWriter bw = new BinaryWriter(msb, new ASCIIEncoding()))
                    {
                        bw.BaseStream.Seek(sdd1ptr.pointerLocation, SeekOrigin.Begin);
                        bw.Write(compressedData, 0, (int)outLength);
                    }
                }
            }

            Console.WriteLine("Replaced all S-DD1 pointers with compressed S-DD1 data");

            return processedRom;
        }
    }
}
