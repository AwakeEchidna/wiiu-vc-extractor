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
            0x2A};
        private const int FDS_HEADER_LENGTH = 16;
        private const int VC_NAME_LENGTH = 8;
        private const int VC_NAME_PADDING = 8;
        private const int fdsDiskSize = 65500;
        private const int qdDiskSize = 0x10000;
        private const string NES_DICTIONARY_CSV_PATH = "nesromnames.csv";

        private RpxFile rpxFile;
        private RomNameDictionary nesDictionary;

        private string extractedRomPath;
        private string romName;
        private long romPosition;
        private string vcName;
        private long vcNamePosition;
        private int numberOfSides;

        private byte[] fdsRomHeader;
        private byte[] qdRomData;
        private byte[] fullGameDataQD;
        private byte[] fullGameDataFDS;

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
            numberOfSides = 1;

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

                        // Determine the rom's size - find number of disk sides
                        //
                        // All FDS disk sides are 65500 bytes (0xFFDC bytes)
                        //
                        // These are in QuickDisk format, which are either
                        // 0x10000, 0x20000, 0x30000, or 0x40000 bytes in length, depending on number of sides
                        using (FileStream fsDskChk = new FileStream(rpxFile.DecompressedPath,
                                FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader brDskChk = new BinaryReader(fsDskChk, new ASCIIEncoding()))
                            {
                                // Bool to account for correct header
                                bool headerValid = true;

                                // First side is known to exist, so seek ahead to next side
                                brDskChk.BaseStream.Seek(vcNamePosition, SeekOrigin.Begin);
                                brDskChk.ReadBytes(VC_NAME_LENGTH);
                                brDskChk.ReadBytes(VC_NAME_PADDING);
                                // OK, currently at beginning of first side
                                // Now, read to the next side
                                brDskChk.ReadBytes(qdDiskSize);

                                // Check for Nintendo header until it doesn't match
                                while (headerValid)
                                {
                                    // Check header
                                    // Ensure the rest of the header is valid, except final byte (manufacturer code)
                                    // Read in 2nd disk header
                                    byte[] headerBuffer = brDskChk.ReadBytes(FDS_HEADER_LENGTH);

                                    // Iterate through buffer and header
                                    for (int i = 0; i < FDS_HEADER_CHECK.Length && headerValid; i++)
                                    {
                                        // Compare byte at buffer position to corresponding byte in header
                                        if (headerBuffer[i] != FDS_HEADER_CHECK[i])
                                        {
                                            // If they don't match, header is wrong - end loops
                                            headerValid = false;
                                        }
                                    }

                                    // If the header is valid, increment side count and continue
                                    if (headerValid)
                                    {
                                        numberOfSides++;
                                        // Now, read to the next side - account for header already read
                                        brDskChk.ReadBytes(qdDiskSize - FDS_HEADER_LENGTH);
                                    }                                    
                                }
                            }
                        }

                        // Set size of full QD and FDS game using number of disks
                        fullGameDataQD = new byte[qdDiskSize * numberOfSides];
                        fullGameDataFDS = new byte[fdsDiskSize * numberOfSides];

                        Console.WriteLine("Number of Disks: " + numberOfSides);
                        Console.WriteLine("Getting rom data...");

                        // From the position at the end of the header, read the rest of the rom
                        qdRomData = br.ReadBytes(-FDS_HEADER_LENGTH + qdDiskSize * numberOfSides);

                        // Copy the FDS header (determined by IsValidRom) and the rom data to a full-game byte array
                        Buffer.BlockCopy(fdsRomHeader, 0, fullGameDataQD, 0, fdsRomHeader.Length);
                        Buffer.BlockCopy(qdRomData, 0, fullGameDataQD, fdsRomHeader.Length, qdRomData.Length);

                        Console.WriteLine("Writing to " + extractedRomPath + "...");

                        using (BinaryWriter bw = new BinaryWriter(File.Open(
                                extractedRomPath, FileMode.Create)))
                        {
                            // Einstein95's qd2fds.py
                            // Convert QD to FDS
                            //
                            // Convert each side of disk, then insert each into FDS output game data array
                            for (int disk = 0; disk < numberOfSides; disk++)
                            {
                                // Get current disk data
                                byte[] currentDisk = new byte[qdDiskSize];
                                Buffer.BlockCopy(fullGameDataQD, disk*qdDiskSize, currentDisk, 0, qdDiskSize);

                                // Remove bytes at offsets 0x38 and 0x39
                                for (int i = 0x38; i + 2 < currentDisk.Length; i++)
                                {
                                    currentDisk[i] = currentDisk[i + 2];
                                    currentDisk[i + 2] = 0;
                                }

                                int position = 0x3A;

                                try
                                {
                                    while(currentDisk[position+2] == 3)
                                    {
                                        // Delete 2 bytes
                                        for(int i = position; i+2 < currentDisk.Length; i++)
                                        {
                                            currentDisk[i] = currentDisk[i + 2];
                                            currentDisk[i + 2] = 0;
                                        }

                                        int end2 = currentDisk[position + 0xD];
                                        int end1 = currentDisk[position + 0xE];
                                        string fileSizeText = end1.ToString("X2") + end2.ToString("X2");
                                        int fileSize = int.Parse(fileSizeText, System.Globalization.NumberStyles.HexNumber);

                                        // Delete 2 bytes
                                        for (int i = position + 0x10; i + 2 < currentDisk.Length; i++)
                                        {
                                            currentDisk[i] = currentDisk[i + 2];
                                            currentDisk[i + 2] = 0;
                                        }

                                        position += 0x11 + fileSize;
                                    }
                                }
                                catch (IndexOutOfRangeException)
                                {
                                }

                                // Delete 2 bytes
                                for (int i = position; i + 2 < currentDisk.Length; i++)
                                {
                                    currentDisk[i] = currentDisk[i + 2];
                                    currentDisk[i + 2] = 0;
                                }

                                // Copy current disk data to the full FDS game data array at the correct position for the disk
                                Buffer.BlockCopy(currentDisk, 0, fullGameDataFDS, disk * fdsDiskSize, fdsDiskSize);
                            }

                            Console.WriteLine("Total FDS rom size: " + fullGameDataFDS.Length + " Bytes");

                            Console.WriteLine("Writing rom data...");
                            bw.Write(fullGameDataFDS);
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

                                // Ensure the rest of the header is valid, except final byte (manufacturer code)
                                for (int i = 1; i < FDS_HEADER_CHECK.Length && headerValid; i++)
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
