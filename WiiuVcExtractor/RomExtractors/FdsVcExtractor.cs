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
        private static readonly byte[] FDS_HEADER_CHECK = { 0x01, 0x2A, 0x4E,
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
        private byte[] nesRomData;

        private bool verbose;

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

                        Console.WriteLine("Virtual Console Title: " + vcName);
                        Console.WriteLine("FDS Title: " + romName);

                        extractedRomPath = romName + ".fds";

                        br.ReadBytes(VC_NAME_PADDING);

                        // We are currently at the FDS header's position again, 
                        // read past it
                        br.ReadBytes(FDS_HEADER_LENGTH);

                        //
                        // TODO:
                        // 1 - determine if disk has 1 or 2 sides
                        //   - probably only 1, as 2 would need to be flipped
                        // 2 - determine size
                        //   - should be 65500 bytes
                        //
                        // Determine the FDS rom's size
                        Console.WriteLine("Getting number of PRG and CHR pages...");

                        byte prgPages = fdsRomHeader[PRG_PAGE_OFFSET];
                        byte chrPages = fdsRomHeader[CHR_PAGE_OFFSET];

                        Console.WriteLine("PRG Pages: " + prgPages);
                        Console.WriteLine("CHR Pages: " + chrPages);

                        int prgPageSize = prgPages * PRG_PAGE_SIZE;
                        int chrPageSize = chrPages * CHR_PAGE_SIZE;

                        // All FDS roms are 65500 bytes
                        int romSize = 65500;
                        Console.WriteLine("Total FDS rom size: " + romSize + " Bytes");

                        Console.WriteLine("Getting rom data...");
                        nesRomData = br.ReadBytes(romSize - FDS_HEADER_LENGTH);

                        Console.WriteLine("Writing to " + extractedRomPath + "...");

                        using (BinaryWriter bw = new BinaryWriter(File.Open(
                            extractedRomPath, FileMode.Create)))
                        {
                            Console.WriteLine("Writing FDS rom header...");
                            bw.Write(fdsRomHeader, 0, FDS_HEADER_LENGTH);
                            Console.WriteLine("Writing FDS rom data...");
                            bw.Write(nesRomData);
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
            Console.WriteLine("Checking if this is an Famicom Disk System VC title...");

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

            Console.WriteLine("Not an FDS VC Title");

            return false;
        }
    }
}
