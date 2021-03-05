namespace WiiuVcExtractor.RomExtractors
{
    using System;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.FileTypes;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// GBA rom extractor.
    /// </summary>
    public class GbaVcExtractor : IRomExtractor
    {
        private const string GbaDictionaryCsvPath = "gbaromnames.csv";

        // private const int GbaEntryPointLength = 4;
        private const int GbaHeaderLength = 192;

        private const int HeaderBitmapOffset = 0x4;
        private const int HeaderTitleOffset = 0xA0;
        private const int HeaderTitleLength = 12;
        private const int HeaderGameCodeOffset = 0xAC;
        private const int HeaderGameCodeLength = 4;
        private const int HeaderMakerCodeOffset = 0xB0;
        private const int HeaderMakerCodeLength = 2;
        private const int HeaderFixedValueOffset = 0xB2;
        private const int HeaderFixedValueValue = 0x96;

        private const byte AsciiSpace = 0x20;
        private const byte AsciiZero = 0x30;
        private const byte AsciiZ = 0x5A;
        private const byte AsciiTilde = 0x7E;

        // Array of bytes matching the GBA logo at the beginning of the rom
        private static readonly byte[] GbaHeaderCheck =
        {
            0x24, 0xFF, 0xAE, 0x51, 0x69, 0x9A, 0xA2, 0x21, 0x3D, 0x84, 0x82, 0x0A,
            0x84, 0xE4, 0x09, 0xAD, 0x11, 0x24, 0x8B, 0x98, 0xC0, 0x81, 0x7F, 0x21, 0xA3, 0x52, 0xBE, 0x19,
            0x93, 0x09, 0xCE, 0x20, 0x10, 0x46, 0x4A, 0x4A, 0xF8, 0x27, 0x31, 0xEC, 0x58, 0xC7, 0xE8, 0x33,
            0x82, 0xE3, 0xCE, 0xBF, 0x85, 0xF4, 0xDF, 0x94, 0xCE, 0x4B, 0x09, 0xC1, 0x94, 0x56, 0x8A, 0xC0,
            0x13, 0x72, 0xA7, 0xFC, 0x9F, 0x84, 0x4D, 0x73, 0xA3, 0xCA, 0x9A, 0x61, 0x58, 0x97, 0xA3, 0x27,
            0xFC, 0x03, 0x98, 0x76, 0x23, 0x1D, 0xC7, 0x61, 0x03, 0x04, 0xAE, 0x56, 0xBF, 0x38, 0x84, 0x00,
            0x40, 0xA7, 0x0E, 0xFD, 0xFF, 0x52, 0xFE, 0x03, 0x6F, 0x95, 0x30, 0xF1, 0x97, 0xFB, 0xC0, 0x85,
            0x60, 0xD6, 0x80, 0x25, 0xA9, 0x63, 0xBE, 0x03, 0x01, 0x4E, 0x38, 0xE2, 0xF9, 0xA2, 0x34, 0xFF,
            0xBB, 0x3E, 0x03, 0x44, 0x78, 0x00, 0x90, 0xCB, 0x88, 0x11, 0x3A, 0x94, 0x65, 0xC0, 0x7C, 0x63,
            0x87, 0xF0, 0x3C, 0xAF, 0xD6, 0x25, 0xE4, 0x8B, 0x38, 0x0A, 0xAC, 0x72, 0x21, 0xD4, 0xF8, 0x07,
        };

        private readonly PsbFile psbFile;
        private readonly RomNameDictionary gbaDictionary;
        private readonly bool verbose;

        private byte[] gbaHeader;

        private string extractedRomPath;
        private string romCode;
        private string romName;

        /// <summary>
        /// Initializes a new instance of the <see cref="GbaVcExtractor"/> class.
        /// </summary>
        /// <param name="psbFile">PSB file to parse.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public GbaVcExtractor(PsbFile psbFile, bool verbose = false)
        {
            this.verbose = verbose;
            string gbaDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GbaDictionaryCsvPath);

            this.gbaDictionary = new RomNameDictionary(gbaDictionaryPath);

            this.psbFile = psbFile;
        }

        /// <summary>
        /// Extracts GBA rom from the PSB file.
        /// </summary>
        /// <returns>path to extracted rom.</returns>
        public string ExtractRom()
        {
            // Quiet down the console during the extraction valid rom check
            var consoleOutputStream = Console.Out;
            Console.SetOut(TextWriter.Null);
            if (this.IsValidRom())
            {
                Console.SetOut(consoleOutputStream);

                // Get the rom name from the dictionary
                this.romName = this.gbaDictionary.GetRomName(this.romCode);

                if (string.IsNullOrEmpty(this.romName))
                {
                    int titleLength = 0;
                    while (titleLength < HeaderTitleLength)
                    {
                        if (this.gbaHeader[HeaderTitleOffset + titleLength] == 0x00)
                        {
                            break;
                        }

                        titleLength++;
                    }

                    this.romName = Encoding.ASCII.GetString(this.gbaHeader, HeaderTitleOffset, titleLength);

                    // If a rom name could not be determined from the dictionary or rom, prompt the user
                    if (string.IsNullOrEmpty(this.romName))
                    {
                        Console.WriteLine("Could not determine GBA rom name, please enter your desired filename:");
                        this.romName = Console.ReadLine();
                    }
                }

                Console.WriteLine("GBA Rom Code: " + this.romCode);
                Console.WriteLine("GBA Title: " + this.romName);

                this.extractedRomPath = this.romName + ".gba";

                if (File.Exists(this.extractedRomPath))
                {
                    File.Delete(this.extractedRomPath);
                }

                Console.WriteLine("Writing to " + this.extractedRomPath + "...");

                File.Move(this.psbFile.DecompressedPath, this.extractedRomPath);

                Console.WriteLine("GBA rom has been created successfully at " + this.extractedRomPath);

                return this.extractedRomPath;
            }

            return string.Empty;
        }

        /// <summary>
        /// Whether the PSB file is a valid rom.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a GBA VC title...");

            // First check if this is a valid PSB file (need the alldata.psb.m):
            if (this.psbFile != null)
            {
                Console.WriteLine("Checking " + this.psbFile.DecompressedPath + "...");
                if (!File.Exists(this.psbFile.DecompressedPath))
                {
                    Console.WriteLine("Could not find decompressed rom at " + this.psbFile.DecompressedPath);
                    return false;
                }

                this.gbaHeader = new byte[GbaHeaderLength];

                // Read the rom's header into memory
                using (FileStream fs = new FileStream(this.psbFile.DecompressedPath, FileMode.Open, FileAccess.Read))
                {
                    using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                    this.gbaHeader = br.ReadBytes(GbaHeaderLength);
                }

                if (this.verbose)
                {
                    Console.WriteLine("GBA Header data: {0}", BitConverter.ToString(this.gbaHeader));
                    Console.WriteLine("Checking for GBA boot bitmap...");
                }

                // Check the GBA boot bitmap
                for (int i = HeaderBitmapOffset; i < HeaderBitmapOffset + GbaHeaderCheck.Length; i++)
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Bitmap Character[{0}]: 0x{1:X}", i, this.gbaHeader[i]);
                    }

                    if (this.gbaHeader[i] != GbaHeaderCheck[i - HeaderBitmapOffset])
                    {
                        if (this.verbose)
                        {
                            Console.WriteLine("Could not find GBA boot bitmap! GBA rom not found.");
                        }

                        return false;
                    }
                }

                if (this.verbose)
                {
                    Console.WriteLine("Checking for valid title...");
                }

                // Ensure the title is set to valid characters
                for (int i = HeaderTitleOffset; i < HeaderTitleOffset + HeaderTitleLength; i++)
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Title Character[{0}]: {1}", i, Convert.ToChar(this.gbaHeader[i]));
                    }

                    if ((this.gbaHeader[i] < AsciiSpace || this.gbaHeader[i] > AsciiTilde) && this.gbaHeader[i] != 0x00)
                    {
                        if (this.verbose)
                        {
                            Console.WriteLine("Title character is invalid! GBA rom not found.");
                        }

                        return false;
                    }
                }

                if (this.verbose)
                {
                    Console.WriteLine("Checking for valid game code...");
                }

                // Ensure the game code is set to valid characters
                for (int i = HeaderGameCodeOffset; i < HeaderGameCodeOffset + HeaderGameCodeLength; i++)
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Game Code Character[{0}]: {1}", i, Convert.ToChar(this.gbaHeader[i]));
                    }

                    if (this.gbaHeader[i] < AsciiZero || this.gbaHeader[i] > AsciiZ)
                    {
                        if (this.verbose)
                        {
                            Console.WriteLine("Game code character is invalid! GBA rom not found.");
                        }

                        return false;
                    }
                }

                this.romCode = Encoding.ASCII.GetString(this.gbaHeader, HeaderGameCodeOffset, HeaderGameCodeLength);

                if (this.verbose)
                {
                    Console.WriteLine("Checking for valid maker code...");
                }

                // Check the maker code
                for (int i = HeaderMakerCodeOffset; i < HeaderMakerCodeOffset + HeaderMakerCodeLength; i++)
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Maker Code Character[{0}]: {1}", i, Convert.ToChar(this.gbaHeader[i]));
                    }

                    if (this.gbaHeader[i] < AsciiZero || this.gbaHeader[i] > AsciiZ)
                    {
                        if (this.verbose)
                        {
                            Console.WriteLine("Maker code character is invalid! GBA rom not found.");
                        }

                        return false;
                    }
                }

                if (this.verbose)
                {
                    Console.WriteLine("Checking for valid fixed header value...");
                }

                if (this.gbaHeader[HeaderFixedValueOffset] != HeaderFixedValueValue)
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Header fixed value is invalid! GBA rom not found.");
                    }

                    return false;
                }

                Console.WriteLine("GBA Rom Detected!");
                return true;
            }

            if (this.verbose)
            {
                Console.WriteLine("PSB File is not set! Cannot detect GBA Rom.");
            }

            return false;
        }
    }
}
