using System;
using System.Text;
using System.IO;
using WiiuVcExtractor.FileTypes;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.RomExtractors
{
    public class NesVcExtractor : IRomExtractor
    {
        private static readonly byte[] NES_HEADER_CHECK = { 0x4E, 0x45, 0x53 };
        private const int NES_HEADER_LENGTH = 16;
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

        private byte[] nesRomHeader;
        private byte[] nesRomData;

        private bool verbose;

        public NesVcExtractor(RpxFile rpxFile, bool verbose = false)
        {
            this.verbose = verbose;
            string nesDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NES_DICTIONARY_CSV_PATH);

            nesDictionary = new RomNameDictionary(nesDictionaryPath);
            nesRomHeader = new byte[NES_HEADER_LENGTH];
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

                // Browse to the romPosition in the file and look for the WUP string 16 bytes before
                using (FileStream fs = new FileStream(rpxFile.DecompressedPath, FileMode.Open, FileAccess.Read))
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
                            Console.WriteLine("Could not determine NES rom name, please enter your desired filename:");
                            romName = Console.ReadLine();
                        }

                        Console.WriteLine("Virtual Console Title: " + vcName);
                        Console.WriteLine("NES Title: " + romName);

                        extractedRomPath = romName + ".nes";

                        br.ReadBytes(VC_NAME_PADDING);

                        // We are currently at the NES header's position again, read past it
                        br.ReadBytes(NES_HEADER_LENGTH);

                        // Determine the NES rom's size
                        Console.WriteLine("Getting number of PRG and CHR pages...");

                        byte prgPages = nesRomHeader[PRG_PAGE_OFFSET];
                        byte chrPages = nesRomHeader[CHR_PAGE_OFFSET];

                        Console.WriteLine("PRG Pages: " + prgPages);
                        Console.WriteLine("CHR Pages: " + chrPages);

                        int prgPageSize = prgPages * PRG_PAGE_SIZE;
                        int chrPageSize = chrPages * CHR_PAGE_SIZE;

                        int romSize = prgPageSize + chrPageSize + NES_HEADER_LENGTH;
                        Console.WriteLine("Total NES rom size: " + romSize + " Bytes");


                        Console.WriteLine("Fixing VC NES Header...");
                        nesRomHeader[BROKEN_NES_HEADER_OFFSET] = CHARACTER_BREAK;

                        Console.WriteLine("Getting rom data...");
                        nesRomData = br.ReadBytes(romSize - NES_HEADER_LENGTH);

                        Console.WriteLine("Writing to " + extractedRomPath + "...");

                        using (BinaryWriter bw = new BinaryWriter(File.Open(extractedRomPath, FileMode.Create)))
                        {
                            Console.WriteLine("Writing NES rom header...");
                            bw.Write(nesRomHeader, 0, NES_HEADER_LENGTH);
                            Console.WriteLine("Writing NES rom data...");
                            bw.Write(nesRomData);
                        }

                        Console.WriteLine("NES rom has been created successfully at " + extractedRomPath);
                    }
                }

            }

            return extractedRomPath;
        }

        // Determines if this is a valid NES ROM
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is an NES VC title...");

            // First check if this is a valid ELF file:
            if (rpxFile != null)
            {
                Console.WriteLine("Checking " + rpxFile.DecompressedPath + "...");
                if (!File.Exists(rpxFile.DecompressedPath))
                {
                    Console.WriteLine("Could not find decompressed RPX at " + rpxFile.DecompressedPath);
                    return false;
                }

                byte[] headerBuffer = new byte[NES_HEADER_LENGTH];

                // Search the decompressed RPX file for the NES header
                using (FileStream fs = new FileStream(rpxFile.DecompressedPath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        while (br.BaseStream.Position != br.BaseStream.Length)
                        {
                            byte[] buffer = br.ReadBytes(NES_HEADER_LENGTH);

                            // If the buffer matches the first byte of the NES 
                            // header, check the following 15 bytes
                            if (buffer[0] == NES_HEADER_CHECK[0])
                            {
                                Array.Copy(buffer, headerBuffer, NES_HEADER_LENGTH);

                                // Check the rest of the signature
                                if (headerBuffer[1] == NES_HEADER_CHECK[1] && headerBuffer[2] == NES_HEADER_CHECK[2])
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
                                        romPosition = br.BaseStream.Position - NES_HEADER_LENGTH;
                                        vcNamePosition = romPosition - 16;
                                        Array.Copy(headerBuffer, 0, nesRomHeader, 0, NES_HEADER_LENGTH);
                                        Console.WriteLine("NES Rom Detected!");
                                        return true;
                                    }
                                }
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
