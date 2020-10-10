namespace WiiuVcExtractor.RomExtractors
{
    using System;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.FileTypes;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// NES VC rom extractor.
    /// </summary>
    public class NesVcExtractor : IRomExtractor
    {
        private const int NesHeaderLength = 16;
        private const int VcNameLength = 8;
        private const int VcNamePadding = 8;
        private const int PrgPageSize = 16384;
        private const int ChrPageSize = 8192;
        private const int CharacterBreak = 0x1A;
        private const int BrokenNesHeaderOffset = 0x3;
        private const int PrgPageOffset = 0x4;
        private const int ChrPageOffset = 0x5;
        private const string NesDictionaryCsvPath = "nesromnames.csv";

        private static readonly byte[] NesHeaderCheck = { 0x4E, 0x45, 0x53 };

        private readonly RpxFile rpxFile;
        private readonly RomNameDictionary nesDictionary;
        private readonly byte[] nesRomHeader;
        private readonly bool verbose;

        private string extractedRomPath;
        private string romName;
        private long romPosition;
        private string vcName;
        private long vcNamePosition;
        private byte[] nesRomData;

        /// <summary>
        /// Initializes a new instance of the <see cref="NesVcExtractor"/> class.
        /// </summary>
        /// <param name="rpxFile">RPX file to parse.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public NesVcExtractor(RpxFile rpxFile, bool verbose = false)
        {
            this.verbose = verbose;
            string nesDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NesDictionaryCsvPath);

            this.nesDictionary = new RomNameDictionary(nesDictionaryPath);
            this.nesRomHeader = new byte[NesHeaderLength];
            this.romPosition = 0;
            this.vcNamePosition = 0;

            this.rpxFile = rpxFile;
        }

        /// <summary>
        /// Extracts NES rom from an RPX file.
        /// </summary>
        /// <returns>path of the extracted rom.</returns>
        public string ExtractRom()
        {
            // Quiet down the console during the extraction valid rom check
            var consoleOutputStream = Console.Out;
            Console.SetOut(TextWriter.Null);
            if (this.IsValidRom())
            {
                Console.SetOut(consoleOutputStream);

                // Browse to the romPosition in the file and look for the WUP string 16 bytes before
                using FileStream fs = new FileStream(this.rpxFile.DecompressedPath, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                br.BaseStream.Seek(this.vcNamePosition, SeekOrigin.Begin);

                // read in the VC rom name
                this.vcName = Encoding.ASCII.GetString(br.ReadBytes(VcNameLength));
                this.romName = this.nesDictionary.GetRomName(this.vcName);

                // If a rom name could not be determined, prompt the user
                if (string.IsNullOrEmpty(this.romName))
                {
                    Console.WriteLine("Could not determine NES rom name, please enter your desired filename:");
                    this.romName = Console.ReadLine();
                }

                Console.WriteLine("Virtual Console Title: " + this.vcName);
                Console.WriteLine("NES Title: " + this.romName);

                this.extractedRomPath = this.romName + ".nes";

                br.ReadBytes(VcNamePadding);

                // We are currently at the NES header's position again, read past it
                br.ReadBytes(NesHeaderLength);

                // Determine the NES rom's size
                Console.WriteLine("Getting number of PRG and CHR pages...");

                byte prgPages = this.nesRomHeader[PrgPageOffset];
                byte chrPages = this.nesRomHeader[ChrPageOffset];

                Console.WriteLine("PRG Pages: " + prgPages);
                Console.WriteLine("CHR Pages: " + chrPages);

                int prgPageSize = prgPages * PrgPageSize;
                int chrPageSize = chrPages * ChrPageSize;

                if (this.verbose)
                {
                    Console.WriteLine("PRG page size: {0}", prgPageSize);
                    Console.WriteLine("CHR page size: {0}", chrPageSize);
                }

                int romSize = prgPageSize + chrPageSize + NesHeaderLength;
                Console.WriteLine("Total NES rom size: " + romSize + " Bytes");

                // Fix the NES header
                Console.WriteLine("Fixing VC NES Header...");
                this.nesRomHeader[BrokenNesHeaderOffset] = CharacterBreak;

                Console.WriteLine("Getting rom data...");
                this.nesRomData = br.ReadBytes(romSize - NesHeaderLength);

                Console.WriteLine("Writing to " + this.extractedRomPath + "...");

                using (BinaryWriter bw = new BinaryWriter(File.Open(this.extractedRomPath, FileMode.Create)))
                {
                    Console.WriteLine("Writing NES rom header...");
                    bw.Write(this.nesRomHeader, 0, NesHeaderLength);
                    Console.WriteLine("Writing NES rom data...");
                    bw.Write(this.nesRomData);
                }

                Console.WriteLine("NES rom has been created successfully at " + this.extractedRomPath);
            }

            return this.extractedRomPath;
        }

        /// <summary>
        /// Whether the associated rom is valid.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is an NES VC title...");

            // First check if this is a valid ELF file:
            if (this.rpxFile != null)
            {
                Console.WriteLine("Checking " + this.rpxFile.DecompressedPath + "...");
                if (!File.Exists(this.rpxFile.DecompressedPath))
                {
                    Console.WriteLine("Could not find decompressed RPX at " + this.rpxFile.DecompressedPath);
                    return false;
                }

                byte[] headerBuffer = new byte[NesHeaderLength];

                // Search the decompressed RPX file for the NES header
                using FileStream fs = new FileStream(this.rpxFile.DecompressedPath, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    byte[] buffer = br.ReadBytes(NesHeaderLength);

                    // If the buffer matches the first byte of the NES header, check the following 15 bytes
                    if (buffer[0] == NesHeaderCheck[0])
                    {
                        Array.Copy(buffer, headerBuffer, NesHeaderLength);

                        // Check the rest of the signature
                        if (headerBuffer[1] == NesHeaderCheck[1] && headerBuffer[2] == NesHeaderCheck[2])
                        {
                            bool headerValid = true;

                            // Ensure the last 8 bytes of the header are 0x00
                            for (int i = 0; i < 8; i++)
                            {
                                if (headerBuffer[i + 8] != 0x00)
                                {
                                    headerValid = false;
                                }
                            }

                            if (headerValid)
                            {
                                // The rom position is a header length before the current stream position
                                this.romPosition = br.BaseStream.Position - NesHeaderLength;
                                this.vcNamePosition = this.romPosition - 16;
                                Array.Copy(headerBuffer, 0, this.nesRomHeader, 0, NesHeaderLength);
                                Console.WriteLine("NES Rom Detected!");
                                return true;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Not an NES VC Title");

            return false;
        }
    }
}
