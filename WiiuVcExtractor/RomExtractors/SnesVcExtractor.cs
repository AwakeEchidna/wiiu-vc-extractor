namespace WiiuVcExtractor.RomExtractors
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// SNES VC extractor.
    /// </summary>
    public class SnesVcExtractor : IRomExtractor
    {
        private const int VcHeaderSize = 0x30;
        private const int SnesHeaderLength = 32;
        private const int SnesLoRomHeaderOffset = 0x7FC0;
        private const int SnesHiRomHeaderOffset = 0xFFC0;

        private const int HeaderTitleOffset = 0;
        private const int HeaderTitleLength = 21;
        private const int HeaderRomSizeOffset = 23;

        // private const int HeaderSramSizeOffset = 24;
        // private const int HeaderFixedValueOffset = 26;
        private const int HeaderChecksumComplementOffset = 28;
        private const int HeaderChecksumOffset = 30;

        private const int RomSizeBase = 0x400;
        private const string SnesDictionaryCsvPath = "snesromnames.csv";
        private const string SnesSizeCsvPath = "snesromsizes.csv";

        private const byte AsciiSpace = 0x20;
        private const byte AsciiZero = 0x30;
        private const byte AsciiZ = 0x5A;

        private static readonly byte[] SnesWupHeaderCheck = { 0x57, 0x55, 0x50, 0x2D, 0x4A };
        private static readonly int[] ValidRomSizes = { 0x9, 0xA, 0xB, 0xC, 0xD };

        private readonly string decompressedRomPath;
        private readonly RomNameDictionary snesDictionary;
        private readonly RomSizeDictionary snesSizeDictionary;
        private readonly bool verbose;

        private SnesHeaderType headerType;

        private string extractedRomPath;
        private string romName;
        private long romPosition;
        private string vcName;
        private long vcNamePosition;
        private uint fileSize;
        private long fileSizePosition;
        private uint sdd1Offset;
        private long sdd1OffsetPosition;
        private uint sdd1DataOffset;

        private byte[] snesLoRomHeader;
        private byte[] snesHiRomHeader;
        private byte[] snesRomData;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnesVcExtractor"/> class.
        /// </summary>
        /// <param name="decompressedRomPath">path to the decompressed rom.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public SnesVcExtractor(string decompressedRomPath, bool verbose = false)
        {
            this.verbose = verbose;
            string snesDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SnesDictionaryCsvPath);
            string snesSizePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SnesSizeCsvPath);

            this.snesDictionary = new RomNameDictionary(snesDictionaryPath);
            this.snesSizeDictionary = new RomSizeDictionary(snesSizePath);
            this.snesLoRomHeader = new byte[SnesHeaderLength];
            this.snesHiRomHeader = new byte[SnesHeaderLength];
            this.headerType = SnesHeaderType.NotDetermined;
            this.romPosition = 0;
            this.vcNamePosition = 0;
            this.sdd1Offset = 0;
            this.sdd1OffsetPosition = 0;
            this.fileSize = 0;
            this.fileSizePosition = 0;
            this.decompressedRomPath = decompressedRomPath;
        }

        private enum SnesHeaderType
        {
            NotDetermined,
            Unknown,
            HiROM,
            LoROM,
        }

        /// <summary>
        /// Extracts the SNES rom.
        /// </summary>
        /// <returns>path to the extracted rom.</returns>
        public string ExtractRom()
        {
            // Quiet down the console during the extraction valid rom check
            var consoleOutputStream = Console.Out;
            Console.SetOut(TextWriter.Null);
            if (this.IsValidRom())
            {
                Console.SetOut(consoleOutputStream);

                byte[] header = new byte[SnesHeaderLength];

                switch (this.headerType)
                {
                    case SnesHeaderType.HiROM:
                        header = this.snesHiRomHeader;
                        break;
                    case SnesHeaderType.LoROM:
                        header = this.snesLoRomHeader;
                        break;
                }

                // Attempt to get the game title from the dictionary
                this.romName = this.snesDictionary.GetRomName(this.vcName);
                string romHeaderName = this.GetRomName(header);

                if (string.IsNullOrEmpty(this.romName))
                {
                    this.romName = romHeaderName;

                    // If a rom name could not be determined from the dictionary or rom, prompt the user
                    if (string.IsNullOrEmpty(this.romName))
                    {
                        Console.WriteLine("Could not determine SNES rom name, please enter your desired filename:");
                        this.romName = Console.ReadLine();
                    }
                }

                Console.WriteLine("Virtual Console Title: " + this.vcName);
                Console.WriteLine("SNES Title: " + this.romName);
                Console.WriteLine("SNES Header Name: " + romHeaderName);

                this.extractedRomPath = this.romName + ".sfc";

                Console.WriteLine("Getting size of rom...");
                int romSize = this.GetRomSize(header[HeaderRomSizeOffset]);

                if (this.verbose)
                {
                    Console.WriteLine("Rom size from header is {0} bytes.", romSize);
                }

                romSize = this.snesSizeDictionary.GetRomSize(romHeaderName, romSize);

                if (this.verbose)
                {
                    Console.WriteLine("Actual rom size is {0} bytes.", romSize);
                }

                Console.WriteLine("Total SNES rom size: " + romSize + " Bytes");

                Console.WriteLine("Getting rom data...");

                byte[] appendedData;

                // Browse to the romPosition in the file
                using (FileStream fs = new FileStream(this.decompressedRomPath, FileMode.Open, FileAccess.Read))
                {
                    using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                    if (this.verbose)
                    {
                        Console.WriteLine("Browsing to 0x{0:X} in {1} to read in the rom data...", this.romPosition, this.decompressedRomPath);
                    }

                    br.BaseStream.Seek(this.romPosition, SeekOrigin.Begin);

                    this.snesRomData = br.ReadBytes(romSize);

                    // Read in all data until the end of the file after the rom data based on parsed file size
                    appendedData = br.ReadBytes((int)(this.fileSize - (VcHeaderSize + romSize)));
                }

                SnesPcmExtractor pcmExtract = new SnesPcmExtractor(this.snesRomData, appendedData);
                this.snesRomData = pcmExtract.ExtractPcmData();

                if (this.sdd1DataOffset > 0)
                {
                    Console.WriteLine("Rom uses S-DD1, recompressing S-DD1 data...");
                    long appendedDataSdd1DataOffset = this.sdd1DataOffset - (VcHeaderSize + romSize);
                    SnesSdd1Extractor sdd1Extract = new SnesSdd1Extractor(this.snesRomData, appendedData, appendedDataSdd1DataOffset);
                    this.snesRomData = sdd1Extract.ExtractSdd1Data();
                }
                else
                {
                    Console.WriteLine("Rom does not appear to use S-DD1");
                }

                Console.WriteLine("Writing to " + this.extractedRomPath + "...");

                using (BinaryWriter bw = new BinaryWriter(File.Open(this.extractedRomPath, FileMode.Create)))
                {
                    Console.WriteLine("Writing SNES rom data...");
                    bw.Write(this.snesRomData);
                }

                Console.WriteLine("SNES rom has been created successfully at " + this.extractedRomPath);
            }

            return this.extractedRomPath;
        }

        /// <summary>
        /// Whether the associated file is a valid SNES rom.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is an SNES VC title...");

            // First check if this is a valid ELF file:
            if (this.decompressedRomPath != null)
            {
                Console.WriteLine("Checking " + this.decompressedRomPath + "...");
                if (!File.Exists(this.decompressedRomPath))
                {
                    Console.WriteLine("Could not find decompressed rom at " + this.decompressedRomPath);
                    return false;
                }

                if (this.verbose)
                {
                    Console.WriteLine("Checking for the SNES WUP header");
                }

                // Search the decompressed RPX file for the WUP-F specification before the SNES rom's data
                using FileStream fs = new FileStream(this.decompressedRomPath, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    byte[] buffer = br.ReadBytes(16);

                    if (buffer[4] == SnesWupHeaderCheck[0] &&
                        buffer[5] == SnesWupHeaderCheck[1] &&
                        buffer[6] == SnesWupHeaderCheck[2] &&
                        buffer[7] == SnesWupHeaderCheck[3] &&
                        buffer[8] == SnesWupHeaderCheck[4])
                    {
                        // The buffer matches the expected WUP-J string, make sure the last three chars are
                        // valid and it is padded
                        if (buffer[9] >= AsciiZero && buffer[9] <= AsciiZ &&
                            buffer[10] >= AsciiZero && buffer[10] <= AsciiZ &&
                            buffer[11] >= AsciiZero && buffer[11] <= AsciiZ &&
                            buffer[12] == 0x00 &&
                            buffer[13] == 0x00 &&
                            buffer[14] == 0x00 &&
                            buffer[15] == 0x00)
                        {
                            this.romPosition = br.BaseStream.Position;
                            this.fileSizePosition = this.romPosition - 44;   // 0x04 in the header
                            this.sdd1OffsetPosition = this.romPosition - 24; // 0x18 in the header
                            this.vcNamePosition = this.romPosition - 12;     // 0x24 in the header
                            this.vcName = Encoding.ASCII.GetString(buffer, 4, 8);

                            // Seek back 44 bytes to gather the fileSize position
                            br.BaseStream.Seek(this.fileSizePosition, SeekOrigin.Begin);
                            this.fileSize = br.ReadUInt32LE();

                            // Seek back 24 bytes to gather the S-DD1 data offset
                            br.BaseStream.Seek(this.sdd1OffsetPosition, SeekOrigin.Begin);
                            this.sdd1Offset = br.ReadUInt32LE();

                            // Seek to the S-DD1 data offset and read the S-DD1 header (if any)
                            br.BaseStream.Seek(this.romPosition - VcHeaderSize + this.sdd1Offset, SeekOrigin.Begin);
                            this.sdd1DataOffset = br.ReadUInt32LE();

                            Console.WriteLine("File size is 0x{0:X}", this.fileSize);
                            Console.WriteLine("Virtual Console Title offset is 0x{0:X}", this.vcNamePosition);
                            Console.WriteLine("S-DD1 offset is 0x{0:X}", this.sdd1Offset);
                            Console.WriteLine("S-DD1 data offset is 0x{0:X}", this.sdd1DataOffset);

                            this.DetermineHeaderType();

                            if (this.headerType == SnesHeaderType.HiROM || this.headerType == SnesHeaderType.LoROM)
                            {
                                Console.WriteLine("SNES Rom Detected!");
                                return true;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Not an SNES VC Title");

            return false;
        }

        private void DetermineHeaderType()
        {
            if (this.verbose)
            {
                Console.WriteLine("Determining SNES Rom header type (LoROM or HiROM)");
            }

            // Read in the headers
            using (FileStream fs = new FileStream(this.decompressedRomPath, FileMode.Open, FileAccess.Read))
            {
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                if (this.verbose)
                {
                    Console.WriteLine("Seeking to 0x{0:X} to check for possible LoROM header.", this.romPosition + SnesLoRomHeaderOffset);
                }

                // Seek to the lorom header location and read the lorom data
                br.BaseStream.Seek(this.romPosition + SnesLoRomHeaderOffset, SeekOrigin.Begin);
                this.snesLoRomHeader = br.ReadBytes(SnesHeaderLength);

                if (this.verbose)
                {
                    Console.WriteLine("Seeking to 0x{0:X} to check for possible HiROM header.", this.romPosition + SnesHiRomHeaderOffset);
                }

                // Seek to the hirom header location and read the hirom data
                br.BaseStream.Seek(this.romPosition + SnesHiRomHeaderOffset, SeekOrigin.Begin);
                this.snesHiRomHeader = br.ReadBytes(SnesHeaderLength);
            }

            // Check each header to verify which is correct
            if (this.IsValidHeader(this.snesLoRomHeader))
            {
                if (this.IsValidHeader(this.snesHiRomHeader))
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Could not determine header type since both HiROM and LoROM headers were valid, defaulting to LoROM.");
                    }

                    // Both appear to be valid, use LoROM by default
                    this.headerType = SnesHeaderType.LoROM;
                }
                else
                {
                    this.headerType = SnesHeaderType.LoROM;
                }
            }
            else if (this.IsValidHeader(this.snesHiRomHeader))
            {
                this.headerType = SnesHeaderType.HiROM;
            }

            if (this.verbose)
            {
                Console.WriteLine("SNES header type is " + this.headerType.ToString());
            }
        }

        private bool IsValidHeader(byte[] headerData)
        {
            if (this.verbose)
            {
                Console.WriteLine("Checking potential header: {0}", BitConverter.ToString(headerData));
                Console.WriteLine("Checking for valid SNES header title");
            }

            // Ensure that the title piece of the header is valid (space or higher ASCII value)
            for (int i = 0; i < HeaderTitleLength; i++)
            {
                if (headerData[i] < AsciiSpace)
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Header is not valid");
                    }

                    return false;
                }
            }

            if (this.verbose)
            {
                Console.WriteLine("Title: {0}", this.GetRomName(headerData));
                Console.WriteLine("Ensure header rom size 0x{0:X} is valid.", headerData[HeaderRomSizeOffset]);
            }

            // Ensure the rom size of the header is valid
            if (!ValidRomSizes.Contains(headerData[HeaderRomSizeOffset]))
            {
                if (this.verbose)
                {
                    Console.WriteLine("Header rom size 0x{0:X} is invalid.", headerData[HeaderRomSizeOffset]);
                }

                return false;
            }

            // Ensure the checksum of the header is valid
            ushort headerChecksum = BitConverter.ToUInt16(headerData, HeaderChecksumOffset);
            ushort headerChecksumComplement = BitConverter.ToUInt16(headerData, HeaderChecksumComplementOffset);

            if (this.verbose)
            {
                Console.WriteLine("Ensure header checksum 0x{0:X} is valid.", headerChecksum);
            }

            if ((ushort)~headerChecksum != headerChecksumComplement)
            {
                if (this.verbose)
                {
                    Console.WriteLine("Header rom checksum is invalid. Checksum: 0x{0:X} Compliment: 0x{1:X}", headerChecksum, headerChecksumComplement);
                }

                return false;
            }

            return true;
        }

        private int GetRomSize(byte romSize)
        {
            if (ValidRomSizes.Contains(romSize))
            {
                return RomSizeBase << romSize;
            }

            if (this.verbose)
            {
                Console.WriteLine("Invalid rom size 0x{0:X} was provided.", romSize);
            }

            return 0x00;
        }

        private string GetRomName(byte[] header)
        {
            if (header.Length >= HeaderTitleLength)
            {
                return Encoding.ASCII.GetString(header, HeaderTitleOffset, HeaderTitleLength);
            }

            return string.Empty;
        }
    }
}
