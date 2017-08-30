using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WiiuVcExtractor.FileTypes;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.RomExtractors
{
    public class GbaVcExtractor : IRomExtractor
    {
        private const string GBA_DICTIONARY_CSV_PATH = "gbaromnames.csv";

        private const int GBA_HEADER_LENGTH = 192;

        // Array of bytes matching the GBA logo at the beginning of the rom
        private static readonly byte[] GBA_HEADER_CHECK = {
            0x24, 0xFF, 0xAE, 0x51, 0x69, 0x9A, 0xA2, 0x21, 0x3D, 0x84, 0x82, 0x0A,
            0x84, 0xE4, 0x09, 0xAD, 0x11, 0x24, 0x8B, 0x98, 0xC0, 0x81, 0x7F, 0x21, 0xA3, 0x52, 0xBE, 0x19,
            0x93, 0x09, 0xCE, 0x20, 0x10, 0x46, 0x4A, 0x4A, 0xF8, 0x27, 0x31, 0xEC, 0x58, 0xC7, 0xE8, 0x33,
            0x82, 0xE3, 0xCE, 0xBF, 0x85, 0xF4, 0xDF, 0x94, 0xCE, 0x4B, 0x09, 0xC1, 0x94, 0x56, 0x8A, 0xC0,
            0x13, 0x72, 0xA7, 0xFC, 0x9F, 0x84, 0x4D, 0x73, 0xA3, 0xCA, 0x9A, 0x61, 0x58, 0x97, 0xA3, 0x27,
            0xFC, 0x03, 0x98, 0x76, 0x23, 0x1D, 0xC7, 0x61, 0x03, 0x04, 0xAE, 0x56, 0xBF, 0x38, 0x84, 0x00,
            0x40, 0xA7, 0x0E, 0xFD, 0xFF, 0x52, 0xFE, 0x03, 0x6F, 0x95, 0x30, 0xF1, 0x97, 0xFB, 0xC0, 0x85,
            0x60, 0xD6, 0x80, 0x25, 0xA9, 0x63, 0xBE, 0x03, 0x01, 0x4E, 0x38, 0xE2, 0xF9, 0xA2, 0x34, 0xFF,
            0xBB, 0x3E, 0x03, 0x44, 0x78, 0x00, 0x90, 0xCB, 0x88, 0x11, 0x3A, 0x94, 0x65, 0xC0, 0x7C, 0x63,
            0x87, 0xF0, 0x3C, 0xAF, 0xD6, 0x25, 0xE4, 0x8B, 0x38, 0x0A, 0xAC, 0x72, 0x21, 0xD4, 0xF8, 0x07
        };

        private const int HEADER_BITMAP_OFFSET = 0x4;
        private const int HEADER_TITLE_OFFSET = 0xA0;
        private const int HEADER_TITLE_LENGTH = 12;
        private const int HEADER_GAME_CODE_OFFSET = 0xAC;
        private const int HEADER_GAME_CODE_LENGTH = 4;
        private const int HEADER_MAKER_CODE_OFFSET = 0xB0;
        private const int HEADER_MAKER_CODE_LENGTH = 2;
        private const int HEADER_FIXED_VALUE_OFFSET = 0xB2;
        private const int HEADER_FIXED_VALUE_VALUE = 0x96;

        private const byte ASCII_SPACE = 0x20;
        private const byte ASCII_ZERO = 0x30;
        private const byte ASCII_Z = 0x5A;

        private PsbFile psbFile;
        private RomNameDictionary gbaDictionary;
        private byte[] gbaHeader;

        private string extractedRomPath;
        private string romCode;
        private string romName;

        private bool verbose;

        public GbaVcExtractor(string dumpPath, PsbFile psbFile, bool verbose = false)
        {
            this.verbose = verbose;
            string gbaDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GBA_DICTIONARY_CSV_PATH);

            gbaDictionary = new RomNameDictionary(gbaDictionaryPath);

            this.psbFile = psbFile;
        }

        public string ExtractRom()
        {
            // Quiet down the console during the extraction valid rom check
            var consoleOutputStream = Console.Out;
            Console.SetOut(TextWriter.Null);
            if (this.IsValidRom())
            {
                Console.SetOut(consoleOutputStream);

                // Get the rom name from the dictionary
                romName = gbaDictionary.getRomName(romCode);

                if (String.IsNullOrEmpty(romName))
                {
                    int titleLength = 0;
                    while (titleLength < HEADER_TITLE_LENGTH)
                    {
                        if (gbaHeader[HEADER_TITLE_OFFSET + titleLength] == 0x00)
                        {
                            break;
                        }

                        titleLength++;
                    }

                    romName = Encoding.ASCII.GetString(gbaHeader, HEADER_TITLE_OFFSET, titleLength);

                    // If a rom name could not be determined from the dictionary or rom, prompt the user
                    if (String.IsNullOrEmpty(romName))
                    {
                        Console.WriteLine("Could not determine GBA rom name, please enter your desired filename:");
                        romName = Console.ReadLine();
                    }
                }

                Console.WriteLine("GBA Rom Code: " + romCode);
                Console.WriteLine("GBA Title: " + romName);

                extractedRomPath = romName + ".gba";

                if (File.Exists(extractedRomPath))
                {
                    File.Delete(extractedRomPath);
                }

                Console.WriteLine("Writing to " + extractedRomPath + "...");

                File.Move(psbFile.DecompressedPath, extractedRomPath);

                Console.WriteLine("GBA rom has been created successfully at " + extractedRomPath);

                return extractedRomPath;
            }

            return "";
        }

        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a GBA VC title...");

            // First check if this is a valid PSB file (need the alldata.psb.m):
            if (psbFile != null)
            {
                Console.WriteLine("Checking " + psbFile.DecompressedPath + "...");
                if (!File.Exists(psbFile.DecompressedPath))
                {
                    Console.WriteLine("Could not find decompressed rom at " + psbFile.DecompressedPath);
                    return false;
                }

                gbaHeader = new byte[GBA_HEADER_LENGTH];

                // Read the rom's header into memory
                using (FileStream fs = new FileStream(psbFile.DecompressedPath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        gbaHeader = br.ReadBytes(GBA_HEADER_LENGTH);
                    }
                }

                if (verbose)
                {
                    Console.WriteLine("GBA Header data: {0}", BitConverter.ToString(gbaHeader));
                    Console.WriteLine("Checking for GBA boot bitmap...");
                }

                // Check the GBA boot bitmap
                for (int i = HEADER_BITMAP_OFFSET; i < HEADER_BITMAP_OFFSET + GBA_HEADER_CHECK.Length; i++)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Bitmap Character[{0}]: 0x{1:X}", i, gbaHeader[i]);
                    }

                    if (gbaHeader[i] != GBA_HEADER_CHECK[i - HEADER_BITMAP_OFFSET])
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Could not find GBA boot bitmap! GBA rom not found.");
                        }
                        return false;
                    }
                }

                if (verbose)
                {
                    Console.WriteLine("Checking for valid title...");
                }

                // Ensure the title is set to valid characters
                for (int i = HEADER_TITLE_OFFSET; i < HEADER_TITLE_OFFSET + HEADER_TITLE_LENGTH; i++)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Title Character[{0}]: {1}", i, Convert.ToChar(gbaHeader[i]));
                    }

                    if ((gbaHeader[i] < ASCII_SPACE || gbaHeader[i] > ASCII_Z) && gbaHeader[i] != 0x00)
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Title character is invalid! GBA rom not found.");
                        }
                        return false;
                    }
                }

                if (verbose)
                {
                    Console.WriteLine("Checking for valid game code...");
                }

                // Ensure the game code is set to valid characters
                for (int i = HEADER_GAME_CODE_OFFSET; i < HEADER_GAME_CODE_OFFSET + HEADER_GAME_CODE_LENGTH; i++)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Game Code Character[{0}]: {1}", i, Convert.ToChar(gbaHeader[i]));
                    }

                    if (gbaHeader[i] < ASCII_ZERO || gbaHeader[i] > ASCII_Z)
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Game code character is invalid! GBA rom not found.");
                        }
                        return false;
                    }
                }

                romCode = Encoding.ASCII.GetString(gbaHeader, HEADER_GAME_CODE_OFFSET, HEADER_GAME_CODE_LENGTH);

                if (verbose)
                {
                    Console.WriteLine("Checking for valid maker code...");
                }

                // Check the maker code
                for (int i = HEADER_MAKER_CODE_OFFSET; i < HEADER_MAKER_CODE_OFFSET + HEADER_MAKER_CODE_LENGTH; i++)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Maker Code Character[{0}]: {1}", i, Convert.ToChar(gbaHeader[i]));
                    }

                    if (gbaHeader[i] < ASCII_ZERO || gbaHeader[i] > ASCII_Z)
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Maker code character is invalid! GBA rom not found.");
                        }
                        return false;
                    }
                }

                if (verbose)
                {
                    Console.WriteLine("Checking for valid fixed header value...");
                }

                if (gbaHeader[HEADER_FIXED_VALUE_OFFSET] != HEADER_FIXED_VALUE_VALUE)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Header fixed value is invalid! GBA rom not found.");
                    }
                    return false;
                }

                Console.WriteLine("GBA Rom Detected!");
                return true;
            }

            return false;
        }
    }
}
