namespace WiiuVcExtractor.Libraries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WiiuVcExtractor.Libraries.Sdd1;

    /// <summary>
    /// SNES S-DD1 extractor (recompresses S-DD1 data into the SNES rom).
    /// </summary>
    public class SnesSdd1Extractor
    {
        private static readonly byte[] Sdd1Signature = { 0x53, 0x44, 0x44, 0x31 }; // SDD1 ASCII
        private readonly byte[] romData;
        private readonly byte[] rawSdd1Data;
        private readonly long sdd1DataOffset;
        private readonly List<Sdd1Pointer> sdd1Pointers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnesSdd1Extractor"/> class.
        /// </summary>
        /// <param name="snesRomData">SNES rom data.</param>
        /// <param name="rawSdd1Data">Decompressed S-DD1 data.</param>
        /// <param name="sdd1DataOffset">Offset to the beginning of the S-DD1 data in rawSdd1Data.</param>
        public SnesSdd1Extractor(byte[] snesRomData, byte[] rawSdd1Data, long sdd1DataOffset)
        {
            this.sdd1DataOffset = sdd1DataOffset;
            this.romData = snesRomData;
            this.rawSdd1Data = rawSdd1Data;
            this.sdd1Pointers = new List<Sdd1Pointer>();
        }

        /// <summary>
        /// Extracts the decompressed S-DD1 data, compresses it, and injects it into the SNES rom data.
        /// </summary>
        /// <returns>processed SNES rom data.</returns>
        public byte[] ExtractSdd1Data()
        {
            Console.WriteLine("Extracting S-DD1 Data...");

            Console.WriteLine("Appended rom data size: {0}", this.rawSdd1Data.Length);

            byte[] processedRom = new byte[this.romData.Length];
            Array.Copy(this.romData, processedRom, this.romData.Length);

            // Find all SDD1 signatures in the rom and store them
            using (MemoryStream ms = new MemoryStream(this.romData))
            {
                using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

                // Continue reading SDD1 pointer data until we run out
                while (true)
                {
                    long index = br.BaseStream.Position;

                    if (index + Sdd1Signature.Length >= br.BaseStream.Length)
                    {
                        break;
                    }

                    byte[] signatureBuffer = br.ReadBytes(Sdd1Signature.Length);

                    // If we didn't find a signature at this location, seek to the next character and restart the loop
                    if (!signatureBuffer.SequenceEqual(Sdd1Signature))
                    {
                        br.BaseStream.Seek(-3, SeekOrigin.Current);
                        continue;
                    }

                    // Store the pointer location in the rom (PointerLocation) and the
                    // offset in the rawSdd1Data (DataLocation)
                    // We must add the sdd1DataOffset to the read location offset to get to the right
                    // position in the rawSdd1Data (appendedData)
                    this.sdd1Pointers.Add(new Sdd1Pointer(index, br.ReadUInt32LE() + this.sdd1DataOffset));

                    // Set the previous data length based on the current offset - the last offset
                    if (this.sdd1Pointers.Count > 1)
                    {
                        this.sdd1Pointers[^2].DataLength = this.sdd1Pointers[^1].DataLocation - this.sdd1Pointers[^2].DataLocation;
                    }
                }
            }

            if (this.sdd1Pointers.Count == 0)
            {
                Console.WriteLine("No S-DD1 data found, continuing...");
                return processedRom;
            }

            this.sdd1Pointers[^1].DataLength = this.rawSdd1Data.Length - this.sdd1Pointers[^1].DataLocation;

            Console.WriteLine("{0} S-DD1 pointers found", this.sdd1Pointers.Count);
            Console.WriteLine("Reading S-DD1 data into memory...");

            Compressor compressor = new Compressor();

            byte[] outBuffer = new byte[0x40000];

            int pointerCount = 0;

            // Find the decompressed data for each pointer, compress it, and replace the first 8 bytes of each pointer location (SDD1UINT becomes the compressed data)
            foreach (var sdd1ptr in this.sdd1Pointers)
            {
                pointerCount++;

                // Output a period each 50 pointers to indicate progress
                if (pointerCount % 50 == 0)
                {
                    Console.Write(".");
                }

                // Read the decompressed data
                byte[] decompressedData = new byte[sdd1ptr.DataLength];
                using (MemoryStream ms = new MemoryStream(this.rawSdd1Data))
                {
                    using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
                    br.BaseStream.Seek(sdd1ptr.DataLocation, SeekOrigin.Begin);
                    decompressedData = br.ReadBytes((int)sdd1ptr.DataLength);
                }

                // Compress the decompressed data
                byte[] compressedData = compressor.Compress(decompressedData, out uint outLength, outBuffer);

                // Replace with the recompressed data
                using MemoryStream msb = new MemoryStream(processedRom);
                using BinaryWriter bw = new BinaryWriter(msb, new ASCIIEncoding());
                bw.BaseStream.Seek(sdd1ptr.PointerLocation, SeekOrigin.Begin);
                bw.Write(compressedData, 0, (int)outLength);
            }

            Console.WriteLine();
            Console.WriteLine("Replaced all S-DD1 pointers with compressed S-DD1 data");

            return processedRom;
        }
    }
}
