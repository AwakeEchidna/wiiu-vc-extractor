using System;
using System.Text;
using System.IO;
using System.Linq;
using WiiuVcExtractor.FileTypes;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.RomExtractors
{
    public class SnesVcExtractor : IRomExtractor
    {
        private enum SnesHeaderType { NotDetermined, Unknown, HiROM, LoROM };

        private static readonly byte[] SNES_WUP_HEADER_CHECK = { 0x57, 0x55, 0x50, 0x2D, 0x4A };
        private const int SNES_HEADER_LENGTH = 32;
        private const int SNES_LOROM_HEADER_OFFSET = 0x7FC0;
        private const int SNES_HIROM_HEADER_OFFSET = 0xFFC0;

        private const int HEADER_TITLE_OFFSET = 0;
        private const int HEADER_TITLE_LENGTH = 21;
        private const int HEADER_ROM_SIZE_OFFSET = 23;
        private const int HEADER_SRAM_SIZE_OFFSET = 24;
        private const int HEADER_FIXED_VALUE_OFFSET = 26;
        private const int HEADER_CHECKSUM_COMPLEMENT_OFFSET = 28;
        private const int HEADER_CHECKSUM_OFFSET = 30;

        private const int ROM_SIZE_BASE = 0x400;
        private static readonly int[] VALID_ROM_SIZES = { 0x9, 0xA, 0xB, 0xC, 0xD };

        private const string SNES_DICTIONARY_CSV_PATH = "snesromnames.csv";
        private const string SNES_SIZE_CSV_PATH = "snesromsizes.csv";

        private const byte ASCII_SPACE = 0x20;
        private const byte ASCII_ZERO = 0x30;
        private const byte ASCII_Z = 0x5A;

        private string decompressedRomPath;
        private RomNameDictionary snesDictionary;
        private RomSizeDictionary snesSizeDictionary;
        private SnesHeaderType headerType;

        private string extractedRomPath;
        private string romName;
        private long romPosition;
        private string vcName;
        private long vcNamePosition;

        private byte[] snesLoRomHeader;
        private byte[] snesHiRomHeader;
        private byte[] snesRomData;

        private bool verbose;

        public SnesVcExtractor(string decompressedRomPath, bool verbose = false)
        {
            this.verbose = verbose;
            string snesDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SNES_DICTIONARY_CSV_PATH);
            string snesSizePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SNES_SIZE_CSV_PATH);

            snesDictionary = new RomNameDictionary(snesDictionaryPath);
            snesSizeDictionary = new RomSizeDictionary(snesSizePath);
            snesLoRomHeader = new byte[SNES_HEADER_LENGTH];
            snesHiRomHeader = new byte[SNES_HEADER_LENGTH];
            headerType = SnesHeaderType.NotDetermined;
            romPosition = 0;
            vcNamePosition = 0;

            this.decompressedRomPath = decompressedRomPath;
        }

        public string ExtractRom()
        {
            // Quiet down the console during the extraction valid rom check
            var consoleOutputStream = Console.Out;
            Console.SetOut(TextWriter.Null);
            if (this.IsValidRom())
            {
                Console.SetOut(consoleOutputStream);

                byte[] header = new byte[SNES_HEADER_LENGTH];

                switch (headerType)
                {
                    case SnesHeaderType.HiROM:
                        header = snesHiRomHeader;
                        break;
                    case SnesHeaderType.LoROM:
                        header = snesLoRomHeader;
                        break;
                }

                // Attempt to get the game title from the dictionary
                romName = snesDictionary.getRomName(vcName);
                string romHeaderName = GetRomName(header);

                if (String.IsNullOrEmpty(romName))
                {
                    romName = romHeaderName;

                    // If a rom name could not be determined from the dictionary or rom, prompt the user
                    if (String.IsNullOrEmpty(romName))
                    {
                        Console.WriteLine("Could not determine SNES rom name, please enter your desired filename:");
                        romName = Console.ReadLine();
                    }
                }

                Console.WriteLine("Virtual Console Title: " + vcName);
                Console.WriteLine("SNES Title: " + romName);
                Console.WriteLine("SNES Header Name: " + romHeaderName);

                extractedRomPath = romName + ".sfc";

                Console.WriteLine("Getting size of rom...");
                int romSize = GetRomSize(header[HEADER_ROM_SIZE_OFFSET]);

                if (verbose)
                {
                    Console.WriteLine("Rom size from header is {0} bytes.", romSize);
                }

                romSize = snesSizeDictionary.GetRomSize(romHeaderName, romSize);

                if (verbose)
                {
                    Console.WriteLine("Actual rom size is {0} bytes.", romSize);
                }

                Console.WriteLine("Total SNES rom size: " + romSize + " Bytes");

                Console.WriteLine("Getting rom data...");

                byte[] pcmData;

                // Browse to the romPosition in the file
                using (FileStream fs = new FileStream(decompressedRomPath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Browsing to 0x{0:X} in {1} to read in the rom data...", romPosition, decompressedRomPath);
                        }

                        br.BaseStream.Seek(romPosition, SeekOrigin.Begin);

                        snesRomData = br.ReadBytes(romSize);
                        pcmData = br.ReadBytes((int)(br.BaseStream.Length - romPosition + romSize));
                    }
                }

                SnesPcmExtractor pcmExtract = new SnesPcmExtractor(snesRomData, pcmData);
                snesRomData = pcmExtract.ExtractPcmData();

                Console.WriteLine("Writing to " + extractedRomPath + "...");

                using (BinaryWriter bw = new BinaryWriter(File.Open(extractedRomPath, FileMode.Create)))
                {
                    Console.WriteLine("Writing SNES rom data...");
                    bw.Write(snesRomData);
                }

                Console.WriteLine("SNES rom has been created successfully at " + extractedRomPath);
            }

            return extractedRomPath;
        }

        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is an SNES VC title...");

            // First check if this is a valid ELF file:
            if (decompressedRomPath != null)
            {
                Console.WriteLine("Checking " + decompressedRomPath + "...");
                if (!File.Exists(decompressedRomPath))
                {
                    Console.WriteLine("Could not find decompressed rom at " + decompressedRomPath);
                    return false;
                }

                byte[] headerBuffer = new byte[16];

                if (verbose)
                {
                    Console.WriteLine("Checking for the SNES WUP header");
                }

                // Search the decompressed RPX file for the WUP-F specification before the SNES rom's data
                using (FileStream fs = new FileStream(decompressedRomPath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        while (br.BaseStream.Position != br.BaseStream.Length)
                        {
                            byte[] buffer = br.ReadBytes(16);

                            if (buffer[4] == SNES_WUP_HEADER_CHECK[0] &&
                                buffer[5] == SNES_WUP_HEADER_CHECK[1] &&
                                buffer[6] == SNES_WUP_HEADER_CHECK[2] &&
                                buffer[7] == SNES_WUP_HEADER_CHECK[3] &&
                                buffer[8] == SNES_WUP_HEADER_CHECK[4])
                            {
                                // The buffer matches the expected WUP-J string, make sure the last three chars are
                                // valid and it is padded
                                if ((buffer[9]  >= ASCII_ZERO && buffer[9]  <= ASCII_Z) &&
                                    (buffer[10] >= ASCII_ZERO && buffer[10] <= ASCII_Z) &&
                                    (buffer[11] >= ASCII_ZERO && buffer[11] <= ASCII_Z) &&
                                    buffer[12] == 0x00 &&
                                    buffer[13] == 0x00 &&
                                    buffer[14] == 0x00 &&
                                    buffer[15] == 0x00)
                                {
                                    romPosition = br.BaseStream.Position;
                                    vcNamePosition = romPosition - 12;
                                    vcName = Encoding.ASCII.GetString(buffer, 4, 8);

                                    DetermineHeaderType();

                                    if (headerType == SnesHeaderType.HiROM || headerType == SnesHeaderType.LoROM)
                                    {
                                        Console.WriteLine("SNES Rom Detected!");
                                        return true;
                                    }
                                }
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
            if (verbose)
            {
                Console.WriteLine("Determining SNES Rom header type (LoROM or HiROM)");
            }

            // Read in the headers
            using (FileStream fs = new FileStream(decompressedRomPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    if (verbose)
                    {
                        Console.WriteLine("Seeking to 0x{0:X} to check for possible LoROM header.", romPosition + SNES_LOROM_HEADER_OFFSET);
                    }

                    // Seek to the lorom header location and read the lorom data
                    br.BaseStream.Seek(romPosition + SNES_LOROM_HEADER_OFFSET, SeekOrigin.Begin);
                    snesLoRomHeader = br.ReadBytes(SNES_HEADER_LENGTH);

                    if (verbose)
                    {
                        Console.WriteLine("Seeking to 0x{0:X} to check for possible HiROM header.", romPosition + SNES_HIROM_HEADER_OFFSET);
                    }

                    // Seek to the hirom header location and read the hirom data
                    br.BaseStream.Seek(romPosition + SNES_HIROM_HEADER_OFFSET, SeekOrigin.Begin);
                    snesHiRomHeader = br.ReadBytes(SNES_HEADER_LENGTH);
                }
            }

            // Check each header to verify which is correct
            if (IsValidHeader(snesLoRomHeader))
            {
                if (IsValidHeader(snesHiRomHeader))
                {
                    if (verbose)
                    {
                        Console.WriteLine("Could not determine header type since both HiROM and LoROM headers were valid.");
                    }
                    // Both appear to be valid, could not determine which is the correct header
                    headerType = SnesHeaderType.Unknown;
                }
                else
                {
                    headerType = SnesHeaderType.LoROM;
                }
            }
            else if (IsValidHeader(snesHiRomHeader))
            {
                headerType = SnesHeaderType.HiROM;
            }

            if (verbose)
            {
                Console.WriteLine("SNES header type is " + headerType.ToString());
            }
        }

        private bool IsValidHeader(byte[] headerData)
        {
            if (verbose)
            {
                Console.WriteLine("Checking potential header: {0}", BitConverter.ToString(headerData));
                Console.WriteLine("Checking for valid SNES header title");
            }
            // Ensure that the title piece of the header is valid (space or higher ASCII value)
            for (int i = 0; i < HEADER_TITLE_LENGTH; i++)
            {
                if (headerData[i] < ASCII_SPACE)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Header is not valid");
                    }
                    return false;
                }
            }     

            if (verbose)
            {
                Console.WriteLine("Title: {0}", GetRomName(headerData));
                Console.WriteLine("Ensure header rom size 0x{0:X} is valid.", headerData[HEADER_ROM_SIZE_OFFSET]);
            }

            // Ensure the rom size of the header is valid
            if (!VALID_ROM_SIZES.Contains(headerData[HEADER_ROM_SIZE_OFFSET]))
            {
                if (verbose)
                {
                    Console.WriteLine("Header rom size 0x{0:X} is invalid.", headerData[HEADER_ROM_SIZE_OFFSET]);
                }
                return false;
            }

            // Ensure the checksum of the header is valid
            ushort headerChecksum = BitConverter.ToUInt16(headerData, HEADER_CHECKSUM_OFFSET);
            ushort headerChecksumComplement = BitConverter.ToUInt16(headerData, HEADER_CHECKSUM_COMPLEMENT_OFFSET);

            if (verbose)
            {
                Console.WriteLine("Ensure header checksum 0x{0:X} is valid.", headerChecksum);
            }

            if ((ushort)~(headerChecksum) != headerChecksumComplement)
            {
                if (verbose)
                {
                    Console.WriteLine("Header rom checksum is invalid. Checksum: 0x{0:X} Compliment: 0x{1:X}", headerChecksum, headerChecksumComplement);
                }
                return false;
            }

            return true;
        }

        private int GetRomSize(byte romSize)
        {
            if (VALID_ROM_SIZES.Contains(romSize))
            {
                return (ROM_SIZE_BASE << romSize);
            }

            if (verbose)
            {
                Console.WriteLine("Invalid rom size 0x{0:X} was provided.", romSize);
            }
            
            return 0x00;
        }

        private string GetRomName(byte[] header)
        {
            if (header.Length >= HEADER_TITLE_LENGTH)
            {
                return Encoding.ASCII.GetString(header, HEADER_TITLE_OFFSET, HEADER_TITLE_LENGTH);
            }

            return "";
        }
    }
}
