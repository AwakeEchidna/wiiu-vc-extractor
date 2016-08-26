using System;
using System.Text;
using System.IO;

namespace WiiuVcExtractor
{
    public class NesVcExtractor
    {
        private const int PRG_PAGE_SIZE = 16384;
        private const int CHR_PAGE_SIZE = 8192;
        private const int NES_VC_ROM_OFFSET = 1808;
        private const int ALT_NES_VC_ROM_OFFSET = 1744;
        private const int NES_HEADER_LENGTH = 16;
        private static readonly int[] NES_HEADER_CHECK = { 0x4E, 0x45, 0x53 };
        private const int CHARACTER_BREAK = 0x1A;

        private const int BROKEN_NES_HEADER_OFFSET = 3;
        private const int PRG_PAGE_OFFSET = 4;
        private const int CHR_PAGE_OFFSET = 5;

        private byte[] nesRomHeader;
        private byte[] nesRomData;

        static public bool isValid(string rpxPath)
        {
            if (!File.Exists(rpxPath))
            {
                Console.WriteLine("Failed to find RPX file at " + rpxPath);
                return false;
            }

            using (FileStream fs = new FileStream(rpxPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // advance the binary reader past the offset of the VC file
                    br.ReadBytes(NES_VC_ROM_OFFSET);

                    byte[] header = br.ReadBytes(NES_HEADER_LENGTH);

                    // Validate the header
                    if (header[0] != NES_HEADER_CHECK[0] || header[1] != NES_HEADER_CHECK[1] || header[2] != NES_HEADER_CHECK[2])
                    {
                        Console.WriteLine("Failed to find valid NES header at offset " + NES_VC_ROM_OFFSET + ", checking alternate offset...");
                    }
                    else
                    {
                        Console.WriteLine("Found valid NES Header!");
                        return true;
                    }
                }
            }

            using (FileStream fs = new FileStream(rpxPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // advance the binary reader past the offset of the VC file
                    br.ReadBytes(ALT_NES_VC_ROM_OFFSET);

                    byte[] header = br.ReadBytes(NES_HEADER_LENGTH);

                    // Validate the header
                    if (header[0] != NES_HEADER_CHECK[0] || header[1] != NES_HEADER_CHECK[1] || header[2] != NES_HEADER_CHECK[2])
                    {
                        Console.WriteLine("Failed to find valid NES header at offset " + ALT_NES_VC_ROM_OFFSET + ", not an NES VC title.");
                    }
                    else
                    {
                        Console.WriteLine("Found valid NES Header!");
                        return true;
                    }
                }
            }

            return false;
        }

        public void extractRomFromVcDump(string rpxPath, string destinationPath)
        {
            if (!File.Exists(rpxPath))
            {
                Console.WriteLine("Failed to find RPX file at " + rpxPath);
                return;
            }

            // Read the RPX file into memory
            if (!readNesRomIntoMemory(rpxPath, NES_VC_ROM_OFFSET))
            {
                readNesRomIntoMemory(rpxPath, ALT_NES_VC_ROM_OFFSET);
            }

            // Write the header and rom data to an NES file
            writeNesRom(destinationPath);
        }

        private bool readNesRomIntoMemory(string rpxPath, int offset)
        {
            Console.WriteLine("Reading from " + rpxPath + "...");

            using (FileStream fs = new FileStream(rpxPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // advance the binary reader past the offset of the VC file
                    byte[] rpxBytes = br.ReadBytes(offset);

                    nesRomHeader = br.ReadBytes(NES_HEADER_LENGTH);

                    // Validate the header
                    if (nesRomHeader[0] != NES_HEADER_CHECK[0] || nesRomHeader[1] != NES_HEADER_CHECK[1] || nesRomHeader[2] != NES_HEADER_CHECK[2])
                    {
                        Console.WriteLine("Failed to find valid NES header at offset " + offset);
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Found valid NES Header!");
                    }

                    Console.WriteLine("Getting number of PRG and CHR pages...");

                    byte prgPages = nesRomHeader[PRG_PAGE_OFFSET];
                    byte chrPages = nesRomHeader[CHR_PAGE_OFFSET];

                    Console.WriteLine("PRG Pages: " + prgPages);
                    Console.WriteLine("CHR Pages: " + chrPages);

                    int prgPageSize = prgPages * PRG_PAGE_SIZE;
                    int chrPageSize = chrPages * CHR_PAGE_SIZE;

                    int romSize = prgPageSize + chrPageSize + NES_HEADER_LENGTH;
                    Console.WriteLine("Total NES rom size: " + romSize + " Bytes");

                    // Fix the NES header
                    Console.WriteLine("Fixing VC NES Header...");
                    nesRomHeader[BROKEN_NES_HEADER_OFFSET] = CHARACTER_BREAK;

                    Console.WriteLine("Getting rom data...");
                    nesRomData = br.ReadBytes(romSize);
                }
            }

            return true;
        }

        private void writeNesRom(string destinationPath)
        {
            Console.WriteLine("Writing to " + destinationPath + "...");
            using (BinaryWriter bw = new BinaryWriter(File.Open(destinationPath, FileMode.Create)))
            {
                Console.WriteLine("Writing NES rom header...");
                bw.Write(nesRomHeader);
                Console.WriteLine("Writing NES rom data...");
                bw.Write(nesRomData);
            }

            Console.WriteLine("NES rom has been created successfully at " + destinationPath);
        }
    }
}
