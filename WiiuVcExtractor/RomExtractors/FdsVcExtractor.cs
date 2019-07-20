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

                        // TODO:
                        // 1 - determine if disk has 1 or 2 sides
                        //    - probably only 1, as 2 would need to be flipped
                        // 2 - determine size
                        //    - should be 65500 bytes
                        //
                        // Determine the FDS rom's size
                       
                        // All FDS disks are 65500 bytes, 
                        // but Lost Levels has 34 extra zeros
                        int romSize = 65500;
                        int zeroSize = 65534;
                        Console.WriteLine("Total FDS rom size: " + romSize + " Bytes");

                        Console.WriteLine("Getting rom data...");

                        if (isLL)
                        {
                            fdsRomData = br.ReadBytes(zeroSize - FDS_HEADER_LENGTH);
                        }
                        else
                        {
                            fdsRomData = br.ReadBytes(romSize - FDS_HEADER_LENGTH);
                        }

                        Console.WriteLine("Writing to " + extractedRomPath + "...");

                        using (BinaryWriter bw = new BinaryWriter(File.Open(
                            extractedRomPath, FileMode.Create)))
                        {
                            Console.WriteLine("Writing FDS rom header...");
                            bw.Write(fdsRomHeader, 0, FDS_HEADER_LENGTH);

                            Console.WriteLine("Writing FDS rom data...");

                            // if Lost Levels, remove zeros
                            //
                            // potentially remove condition if all FDS games
                            // have zeros
                            if (isLL)
                            {
                                byte[] tempFdsRomData = new byte[romSize - FDS_HEADER_LENGTH];

                                // reduce offsets by header-length to account for
                                // size of fdsRomData
                                //for(int i = 0; i < zerosLL.Length; i++)
                                //{
                                 //   zerosLL[i] -= FDS_HEADER_LENGTH;
                                //}

                                /*
                                int skip = 0;
                                // iterate through both tempFdsRomData
                                // and fdsRomData, skipping zeros
                                for(int t = 0; t < tempFdsRomData.Length; t++)
                                {
                                    if (t == zerosLL[skip])
                                    {
                                        skip++;
                                    }
                                    tempFdsRomData[t] = fdsRomData[t+skip];
                                }
                                */
                                /*
                                Buffer.BlockCopy(fdsRomData, 0, tempFdsRomData, 0, 36);
                                Buffer.BlockCopy(fdsRomData, 38, tempFdsRomData, 36, 6);
                                Buffer.BlockCopy(fdsRomData, 46, tempFdsRomData, 42, 16);
                                Buffer.BlockCopy(fdsRomData, 64, tempFdsRomData, 58, 225);
                                Buffer.BlockCopy(fdsRomData, 291, tempFdsRomData, 283, 16);
                                Buffer.BlockCopy(fdsRomData, 309, tempFdsRomData, 299, 8193);
                                Buffer.BlockCopy(fdsRomData, 8504, tempFdsRomData, 8492, 16);
                                Buffer.BlockCopy(fdsRomData, 8522, tempFdsRomData, 8508, 65);
                                Buffer.BlockCopy(fdsRomData, 8589, tempFdsRomData, 8573, 16);
                                Buffer.BlockCopy(fdsRomData, 8607, tempFdsRomData, 8589, 32769);
                                Buffer.BlockCopy(fdsRomData, 41378, tempFdsRomData, 41358, 16);
                                Buffer.BlockCopy(fdsRomData, 41396, tempFdsRomData, 41374, 3632);
                                Buffer.BlockCopy(fdsRomData, 45030, tempFdsRomData, 45006, 16);
                                Buffer.BlockCopy(fdsRomData, 45048, tempFdsRomData, 45022, 3280);
                                Buffer.BlockCopy(fdsRomData, 48330, tempFdsRomData, 48302, 16);
                                Buffer.BlockCopy(fdsRomData, 48348, tempFdsRomData, 48318, 3917);
                                Buffer.BlockCopy(fdsRomData, 52267, tempFdsRomData, 52235, 16);
                                Buffer.BlockCopy(fdsRomData, 52285, tempFdsRomData, 52251, romSize - FDS_HEADER_LENGTH - 52251);
                                */
                                // Corrects three incorrect bytes
                                tempFdsRomData[8768] = 0x58;
                                tempFdsRomData[33471] = 0x4A;
                                tempFdsRomData[33481] = 0x4A;

                                bw.Write(tempFdsRomData);
                            }
                            else
                            {
                                bw.Write(fdsRomData);
                            }
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
