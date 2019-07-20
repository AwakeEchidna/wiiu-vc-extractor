using System;
using System.Text;
using System.IO;
using WiiuVcExtractor.FileTypes;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.RomExtractors
{
    public class FdsVcExtractor : IRomExtractor
    {
        // Famicom Disk System header
        private static readonly byte[] FDS_HEADER_CHECK = {0x01, 0x2A, 0x4E,
            0x49, 0x4E, 0x54, 0x45, 0x4E, 0x44, 0x4F, 0x2D, 0x48, 0x56, 0x43,
            0x2A, 0x01};
        private const int FDS_HEADER_LENGTH = 16;
        private const int VC_NAME_LENGTH = 8;
        private const int VC_NAME_PADDING = 8;
        private const int PRG_PAGE_SIZE = 16384;
        private const int CHR_PAGE_SIZE = 8192;
        private const int CHARACTER_BREAK = 0x1A;
        private const int BROKEN_NES_HEADER_OFFSET = 0x3;
        private const int PRG_PAGE_OFFSET = 0x4;
        private const int CHR_PAGE_OFFSET = 0x5;
        private const string NES_DICTIONARY_CSV_PATH = "nesromnames.csv";

        private RpxFile rpxFile;
        private RomNameDictionary nesDictionary;

        private string extractedRomPath;
        private string romName;
        private long romPosition;
        private string vcName;
        private long vcNamePosition;

        private byte[] fdsRomHeader;
        private byte[] fdsRomData;
        private byte[] fullGameData;

        private bool verbose;

        // byte array containing offsets of extra pairs of zeros
        private UInt32[] zerosLL = {0x34, 0x35, 0x3C, 0x3D, 0x4E, 0x4F, 0x131,
            0x132, 0x143, 0x144, 0x2146, 0x2147, 0x2158, 0x2159, 0x219B, 0x219C,
            0x21AD, 0x21AE, 0xA1b0, 0xa1b1, 0xa1c2, 0xa1c3, 0xaff4, 0xaff5,
            0xb006, 0xb007, 0xbcd8, 0xbcd9, 0xbcea, 0xbceb, 0xcc39, 0xcc3a,
            0xcc4b, 0xcc4c};

        private bool isLL = false;

        public FdsVcExtractor(RpxFile rpxFile, bool verbose = false)
        {
            this.verbose = verbose;
            string nesDictionaryPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, NES_DICTIONARY_CSV_PATH);

            nesDictionary = new RomNameDictionary(nesDictionaryPath);
            fdsRomHeader = new byte[FDS_HEADER_LENGTH];
            romPosition = 0;
            vcNamePosition = 0;

            this.rpxFile = rpxFile;
        }

        public string ExtractRom()
        {
            // Quiet down the console during the extraction valid rom check
            var consoleOutputStream = Console.Out;
            Console.SetOut(TextWriter.Null);
            if (this.IsValidRom())
            {
                Console.SetOut(consoleOutputStream);

                // Browse to the romPosition in the file and look for the WUP 
                // string 16 bytes before
                using (FileStream fs = new FileStream(rpxFile.DecompressedPath, 
                    FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        br.BaseStream.Seek(vcNamePosition, SeekOrigin.Begin);

                        // read in the VC rom name
                        vcName = Encoding.ASCII.GetString(br.ReadBytes(VC_NAME_LENGTH));
                        romName = nesDictionary.getRomName(vcName);

                        // If a rom name could not be determined, prompt the user
                        if (String.IsNullOrEmpty(romName))
                        {
                            Console.WriteLine("Could not determine rom name, " +
                                "please enter your desired filename:");
                            romName = Console.ReadLine();
                        }

                        if(vcName.Equals("WUP-FA9E"))
                        {
                            isLL = true;
                        }

                        Console.WriteLine("Virtual Console Title: " + vcName);
                        Console.WriteLine("FDS Title: " + romName);

                        extractedRomPath = romName + ".fds";

                        br.ReadBytes(VC_NAME_PADDING);

                        // We are currently at the FDS header's position again, 
                        // read past it
                        br.ReadBytes(FDS_HEADER_LENGTH);

                        // Determine the FDS rom's size
                        //
                        // All FDS disks are 65500 bytes, 
                        // but these are in QD format, which is either
                        // 0x10000 or 0x20000 in length, depending on $ of disks
                        //
                        // Since these are Wii U VC titles, they are 1 disk
                        int romSize = 65500;

                        Console.WriteLine("Total FDS rom size: " + romSize + " Bytes");

                        Console.WriteLine("Getting rom data...");

                        fdsRomData = br.ReadBytes(romSize - FDS_HEADER_LENGTH);

                        fullGameData = new byte[romSize];
                        Buffer.BlockCopy(fdsRomHeader, 0, fullGameData, 0, fdsRomHeader.Length);
                        Buffer.BlockCopy(fdsRomData, 0, fullGameData, fdsRomHeader.Length, fdsRomData.Length);

                        Console.WriteLine("Writing to " + extractedRomPath + "...");

                        using (BinaryWriter bw = new BinaryWriter(File.Open(
                            extractedRomPath, FileMode.Create)))
                        {
                            // Convert QD to FDS
                            //
                            // Remove bytes at offsets 0x38 and 0x39
                            for (int i = 0x38; i + 2 < fullGameData.Length; i++)
                            {
                                fullGameData[i] = fullGameData[i + 2];
                                fullGameData[i + 2] = 0;
                            }

                            int position = 0x3A;

                            try
                            {
                                while(fullGameData[position+2] == 3)
                                {
                                    // Delete 2 bytes
                                    for(int i = position; i+2 < fullGameData.Length; i++)
                                    {
                                        fullGameData[i] = fullGameData[i + 2];
                                        fullGameData[i + 2] = 0;
                                    }

                                    int end2 = fullGameData[position + 0xD];
                                    int end1 = fullGameData[position + 0xE];
                                    string fileSizeText = end1.ToString("X2") + end2.ToString("X2");
                                    int fileSize = int.Parse(fileSizeText, System.Globalization.NumberStyles.HexNumber);

                                    // Delete 2 bytes
                                    for (int i = position + 0x10; i + 2 < fullGameData.Length; i++)
                                    {
                                        fullGameData[i] = fullGameData[i + 2];
                                        fullGameData[i + 2] = 0;
                                    }

                                    position += 0x11 + fileSize;
                                }
                            }
                            catch (IndexOutOfRangeException)
                            {
                            }

                            // Delete 2 bytes
                            for (int i = position; i + 2 < fullGameData.Length; i++)
                            {
                                fullGameData[i] = fullGameData[i + 2];
                                fullGameData[i + 2] = 0;
                            }

                            // if Lost Levels, correct three bytes
                            if (isLL)
                            {
                                fullGameData[8784] = 0x58;
                                fullGameData[33487] = 0x4A;
                                fullGameData[33497] = 0x4A;
                            }

                            Console.WriteLine("Writing rom data...");
                            bw.Write(fullGameData);
                        }

                        Console.WriteLine("Famicom Disk System rom has been " +
                            "created successfully at " + extractedRomPath);
                    }
                }

            }

            return extractedRomPath;
        }

        // Determines if this is a valid FDS ROM
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a Famicom Disk System VC title...");

            // First check if this is a valid ELF file:
            if (rpxFile != null)
            {
                Console.WriteLine("Checking " + rpxFile.DecompressedPath + "...");
                if (!File.Exists(rpxFile.DecompressedPath))
                {
                    Console.WriteLine("Could not find decompressed RPX at " + 
                        rpxFile.DecompressedPath);
                    return false;
                }

                byte[] headerBuffer = new byte[FDS_HEADER_LENGTH];

                // Search the decompressed RPX file for the FDS header
                using (FileStream fs = new FileStream(rpxFile.DecompressedPath, 
                    FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        while (br.BaseStream.Position != br.BaseStream.Length)
                        {
                            byte[] buffer = br.ReadBytes(FDS_HEADER_LENGTH);

                            // Check the FDS header
                            if (buffer[0] == FDS_HEADER_CHECK[0])
                            {
                                Array.Copy(buffer, headerBuffer, FDS_HEADER_LENGTH);

                                bool headerValid = true;

                                // Ensure the rest of the header is valid
                                for (int i = 1; i < 16 && headerValid; i++)
                                {
                                    if (headerBuffer[i] != FDS_HEADER_CHECK[i])
                                    {
                                        headerValid = false;
                                    }
                                }

                                if (headerValid)
                                {
                                    // The rom position is a header length 
                                    // before the current stream position
                                    romPosition = br.BaseStream.Position - 
                                        FDS_HEADER_LENGTH;
                                    vcNamePosition = romPosition - 16;
                                    Array.Copy(headerBuffer, 0, fdsRomHeader, 0, 
                                        FDS_HEADER_LENGTH);
                                    Console.WriteLine("Famicom Disk System Rom " +
                                        "Detected!");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Not a FDS VC Title");

            return false;
        }
    }
}
