using System;
using System.Text;
using System.IO;
using System.Linq;

namespace WiiuVcExtractor
{
    public class SnesVcExtractor
    {
        private enum RomType { NotDetermined, Unknown, HiROM, LoROM };

        private const int SNES_VC_ROM_OFFSET = 1808;
        private const int SNES_HEADER_LENGTH = 64;
        private const int SNES_LOROM_HEADER_OFFSET = 0x7FC0;
        private const int SNES_HIROM_HEADER_OFFSET = 0xFFC0;

        private const int TITLE_OFFSET = 0;
        private const int ROM_SIZE_OFFSET = 23;
        private const int SRAM_SIZE_OFFSET = 24;
        private const int CHECKSUM_COMPLEMENT_OFFSET = 28;
        private const int CHECKSUM_OFFSET = 30;

        private static readonly int[] VALID_ROM_SIZES = { 0x9, 0xA, 0xB, 0xC, 0xD };
        private const int ROM_SIZE_BASE = 0x400;

        private byte[] loromHeader;
        private byte[] hiromHeader;

        private RomType romType = RomType.NotDetermined;
        private int romSize;
        private byte[] romData;

        static public bool isValid(string rpxPath)
        {
            if (!File.Exists(rpxPath))
            {
                Console.WriteLine("Failed to find RPX file at " + rpxPath);
                return false;
            }

            byte[] checkLoromHeader;
            byte[] checkHiromHeader;
            RomType romType = RomType.NotDetermined;

            using (FileStream fs = new FileStream(rpxPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // advance the binary reader past the offset of the VC file
                    byte[] rpxBytes = br.ReadBytes(SNES_VC_ROM_OFFSET);

                    byte[] bytesBeforeLorom = br.ReadBytes(SNES_LOROM_HEADER_OFFSET);

                    checkLoromHeader = br.ReadBytes(SNES_HEADER_LENGTH);

                    byte[] bytesBeforeHirom = br.ReadBytes(SNES_LOROM_HEADER_OFFSET);

                    checkHiromHeader = br.ReadBytes(SNES_HEADER_LENGTH);
                }
            }

            ushort loromChecksum = BitConverter.ToUInt16(checkLoromHeader, CHECKSUM_OFFSET);
            ushort loromChecksumCompliment = BitConverter.ToUInt16(checkLoromHeader, CHECKSUM_COMPLEMENT_OFFSET);

            ushort hiromChecksum = BitConverter.ToUInt16(checkHiromHeader, CHECKSUM_OFFSET);
            ushort hiromChecksumCompliment = BitConverter.ToUInt16(checkHiromHeader, CHECKSUM_COMPLEMENT_OFFSET);

            // First check if the checksum and the checksum compliment seem valid
            if (isChecksumValid(loromChecksum, loromChecksumCompliment))
            {
                romType = RomType.LoROM;
            }

            if (isChecksumValid(hiromChecksum, hiromChecksumCompliment))
            {
                // Now in indeterminate state
                if (romType == RomType.LoROM)
                {
                    romType = RomType.Unknown;
                }
                else
                {
                    romType = RomType.HiROM;
                }
            }

            if (romType == RomType.NotDetermined)
            {
                Console.WriteLine("Could not verify the checksum in the header with the checksum compliment for HiROM or LoROM, not an SNES VC title");
                return false;
            }
            else if (romType == RomType.Unknown)
            {
                if (isRomsizeValid(checkLoromHeader[ROM_SIZE_OFFSET]))
                {
                    romType = RomType.LoROM;
                }

                if (isRomsizeValid(checkHiromHeader[ROM_SIZE_OFFSET]))
                {
                    if (romType == RomType.LoROM)
                    {
                        Console.WriteLine("Could not determine type of rom header, not an SNES VC title");
                        return false;
                    }

                    romType = RomType.HiROM;
                }
            }

            Console.WriteLine("Found valid SNES Header!");
            return true;
        }

        public void extractRomFromVcDump(string rpxPath, string destinationPath)
        {
            if (!File.Exists(rpxPath))
            {
                Console.WriteLine("Failed to find RPX file at " + rpxPath);
                return;
            }

            // Get the rom type
            determineRomType(rpxPath);

            // Read the RPX file into memory
            readSnesRomIntoMemory(rpxPath);

            // Write the header and rom data to an SMC file
            writeSnesRom(destinationPath);
        }

        private void readSnesRomIntoMemory(string rpxPath)
        {
            Console.WriteLine("Reading from " + rpxPath + "...");

            using (FileStream fs = new FileStream(rpxPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    Console.WriteLine("Getting SNES rom data...");

                    // advance the binary reader past the offset of the VC file
                    byte[] rpxBytes = br.ReadBytes(SNES_VC_ROM_OFFSET);

                    romData = br.ReadBytes(romSize);
                }
            }
        }

        private void determineRomType(string rpxPath)
        {
            Console.WriteLine("Reading from " + rpxPath + "...");

            using (FileStream fs = new FileStream(rpxPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    Console.WriteLine("Getting SNES headers...");

                    // advance the binary reader past the offset of the VC file
                    byte[] rpxBytes = br.ReadBytes(SNES_VC_ROM_OFFSET);

                    byte[] bytesBeforeLorom = br.ReadBytes(SNES_LOROM_HEADER_OFFSET);

                    loromHeader = br.ReadBytes(SNES_HEADER_LENGTH);

                    byte[] bytesBeforeHirom = br.ReadBytes(SNES_LOROM_HEADER_OFFSET);

                    hiromHeader = br.ReadBytes(SNES_HEADER_LENGTH);
                }  
            }

            // Determine whether the rom is lorom or hirom
            Console.WriteLine("Determining if this is HiROM or LoROM...");

            ushort loromChecksum = BitConverter.ToUInt16(loromHeader, CHECKSUM_OFFSET);
            ushort loromChecksumCompliment = BitConverter.ToUInt16(loromHeader, CHECKSUM_COMPLEMENT_OFFSET);

            ushort hiromChecksum = BitConverter.ToUInt16(hiromHeader, CHECKSUM_OFFSET);
            ushort hiromChecksumCompliment = BitConverter.ToUInt16(hiromHeader, CHECKSUM_COMPLEMENT_OFFSET);

            Console.WriteLine("Checking LoROM...");
            // First check if the checksum and the checksum compliment seem valid
            if (isChecksumValid(loromChecksum, loromChecksumCompliment, true))
            {
                Console.WriteLine("Rom may be LoROM");
                romType = RomType.LoROM;
            }

            Console.WriteLine("Checking HiROM...");
            if (isChecksumValid(hiromChecksum, hiromChecksumCompliment, true))
            {
                Console.WriteLine("Rom may be HiROM");

                // Now in indeterminate state
                if (romType == RomType.LoROM)
                {
                    romType = RomType.Unknown;
                }
                else
                {
                    romType = RomType.HiROM;
                }
            }

            if ( romType == RomType.NotDetermined )
            {
                Console.WriteLine("Could not verify the checksum in the header with the checksum compliment for HiROM or LoROM, FAILURE");
                return;
            }
            else if (romType == RomType.Unknown)
            {
                Console.WriteLine("HiROM and LoROM both have valid checksums, checking rom size field...");

                Console.WriteLine("LoROM rom size: " + getRomSize(loromHeader[ROM_SIZE_OFFSET]));
                Console.WriteLine("HiROM rom size: " + getRomSize(hiromHeader[ROM_SIZE_OFFSET]));

                if (isRomsizeValid(loromHeader[ROM_SIZE_OFFSET]))
                {
                    Console.WriteLine("LoROM has a valid rom size field");
                    romType = RomType.LoROM;
                }
                else
                {
                    Console.WriteLine("LoROM does not have a valid rom size field");
                }

                if (isRomsizeValid(hiromHeader[ROM_SIZE_OFFSET]))
                {
                    Console.WriteLine("HiROM has a valid rom size field");
                    if (romType == RomType.LoROM)
                    {
                        Console.WriteLine("Could not determine type of rom, FAILURE");
                        return;
                    }

                    romType = RomType.HiROM;   
                }
                else
                {
                    Console.WriteLine("HiROM does not have a valid rom size field");
                }
            }

            switch (romType)
            {
                case RomType.HiROM:
                    romSize = getRomSize(hiromHeader[ROM_SIZE_OFFSET]);
                    break;
                case RomType.LoROM:
                    romSize = getRomSize(loromHeader[ROM_SIZE_OFFSET]);
                    break;
            }

            Console.WriteLine(romType.ToString() + " detected!");
            Console.WriteLine("Rom size is " + romSize + " bytes");
        }

        private void writeSnesRom(string destinationPath)
        {
            Console.WriteLine("Writing to " + destinationPath + "...");
            using (BinaryWriter bw = new BinaryWriter(File.Open(destinationPath, FileMode.Create)))
            {
                Console.WriteLine("Writing SNES rom data...");
                bw.Write(romData);
            }

            Console.WriteLine("SNES rom has been created successfully at " + destinationPath);
        }

        private static bool isChecksumValid(ushort checksum, ushort checksumCompliment, bool writeMessages=false)
        {
            if (writeMessages)
            {
                Console.WriteLine("Checksum: " + checksum);
                Console.WriteLine("Checksum compliment: " + checksumCompliment);
            }

            return ((ushort)~(checksum) == checksumCompliment);
        }

        private static bool isRomsizeValid(byte romSize)
        {
            return (VALID_ROM_SIZES.Contains(romSize));
        }

        private static int getRomSize(byte romSize)
        {
            return (ROM_SIZE_BASE << romSize);
        }
    }
}
