namespace WiiuVcExtractor.RomExtractors
{
    using System;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.FileTypes;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// FDS VC extractor.
    /// </summary>
    public class FdsVcExtractor : IRomExtractor
    {
        private const int FdsHeaderLength = 16;
        private const int VcNameLength = 8;
        private const int VcNamePadding = 8;
        private const int FdsDiskSize = 65500;
        private const int QdDiskSize = 0x10000;
        private const string NesDictionaryCsvPath = "nesromnames.csv";

        // Famicom Disk System header
        private static readonly byte[] FdsHeaderCheck =
        {
            0x01, 0x2A, 0x4E,
            0x49, 0x4E, 0x54, 0x45, 0x4E, 0x44, 0x4F, 0x2D, 0x48, 0x56, 0x43,
            0x2A,
        };

        private readonly RpxFile rpxFile;
        private readonly RomNameDictionary nesDictionary;
        private readonly byte[] fdsRomHeader;
        private readonly bool verbose;

        private string extractedRomPath;
        private string romName;
        private long romPosition;
        private string vcName;
        private long vcNamePosition;
        private int numberOfSides;

        private byte[] qdRomData;
        private byte[] fullGameDataQD;
        private byte[] fullGameDataFDS;

        /// <summary>
        /// Initializes a new instance of the <see cref="FdsVcExtractor"/> class.
        /// </summary>
        /// <param name="rpxFile">RPX file to parse.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public FdsVcExtractor(RpxFile rpxFile, bool verbose = false)
        {
            this.verbose = verbose;
            string nesDictionaryPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, NesDictionaryCsvPath);

            this.nesDictionary = new RomNameDictionary(nesDictionaryPath);
            this.fdsRomHeader = new byte[FdsHeaderLength];
            this.romPosition = 0;
            this.vcNamePosition = 0;
            this.numberOfSides = 1;

            this.rpxFile = rpxFile;
        }

        /// <summary>
        /// Extracts FDS rom from RPX file.
        /// </summary>
        /// <returns>path to the extracted rom.</returns>
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
                using FileStream fs = new FileStream(this.rpxFile.DecompressedPath, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                br.BaseStream.Seek(this.vcNamePosition, SeekOrigin.Begin);

                // read in the VC rom name
                this.vcName = Encoding.ASCII.GetString(br.ReadBytes(VcNameLength));
                this.romName = this.nesDictionary.GetRomName(this.vcName);

                // If a rom name could not be determined, prompt the user
                if (string.IsNullOrEmpty(this.romName))
                {
                    Console.WriteLine("Could not determine rom name, " +
                        "please enter your desired filename:");
                    this.romName = Console.ReadLine();
                }

                Console.WriteLine("Virtual Console Title: " + this.vcName);
                Console.WriteLine("FDS Title: " + this.romName);

                this.extractedRomPath = this.romName + ".fds";

                br.ReadBytes(VcNamePadding);

                // We are currently at the FDS header's position again,
                // read past it
                br.ReadBytes(FdsHeaderLength);

                // Determine the rom's size - find number of disk sides
                //
                // All FDS disk sides are 65500 bytes (0xFFDC bytes)
                //
                // These are in QuickDisk format, which are either
                // 0x10000, 0x20000, 0x30000, or 0x40000 bytes in length, depending on number of sides
                using (FileStream fsDskChk = new FileStream(this.rpxFile.DecompressedPath, FileMode.Open, FileAccess.Read))
                {
                    using BinaryReader brDskChk = new BinaryReader(fsDskChk, new ASCIIEncoding());

                    // Bool to account for correct header
                    bool headerValid = true;

                    // First side is known to exist, so seek ahead to next side
                    brDskChk.BaseStream.Seek(this.vcNamePosition, SeekOrigin.Begin);
                    brDskChk.ReadBytes(VcNameLength);
                    brDskChk.ReadBytes(VcNamePadding);

                    // OK, currently at beginning of first side
                    // Now, read to the next side
                    brDskChk.ReadBytes(QdDiskSize);

                    // Check for Nintendo header until it doesn't match
                    while (headerValid)
                    {
                        // Check header
                        // Ensure the rest of the header is valid, except final byte (manufacturer code)
                        // Read in 2nd disk header
                        byte[] headerBuffer = brDskChk.ReadBytes(FdsHeaderLength);

                        // Iterate through buffer and header
                        for (int i = 0; i < FdsHeaderCheck.Length && headerValid; i++)
                        {
                            // Compare byte at buffer position to corresponding byte in header
                            if (headerBuffer[i] != FdsHeaderCheck[i])
                            {
                                // If they don't match, header is wrong - end loops
                                headerValid = false;
                            }
                        }

                        // If the header is valid, increment side count and continue
                        if (headerValid)
                        {
                            this.numberOfSides++;

                            // Now, read to the next side - account for header already read
                            brDskChk.ReadBytes(QdDiskSize - FdsHeaderLength);
                        }
                    }
                }

                // Set size of full QD and FDS game using number of disks
                this.fullGameDataQD = new byte[QdDiskSize * this.numberOfSides];
                this.fullGameDataFDS = new byte[FdsDiskSize * this.numberOfSides];

                Console.WriteLine("Number of Disks: " + this.numberOfSides);
                Console.WriteLine("Getting rom data...");

                // From the position at the end of the header, read the rest of the rom
                this.qdRomData = br.ReadBytes(-FdsHeaderLength + (QdDiskSize * this.numberOfSides));

                if (this.verbose)
                {
                    Console.WriteLine("FDS QD rom data size: {0}", this.qdRomData.Length);
                }

                // Copy the FDS header (determined by IsValidRom) and the rom data to a full-game byte array
                Buffer.BlockCopy(this.fdsRomHeader, 0, this.fullGameDataQD, 0, this.fdsRomHeader.Length);
                Buffer.BlockCopy(this.qdRomData, 0, this.fullGameDataQD, this.fdsRomHeader.Length, this.qdRomData.Length);

                Console.WriteLine("Writing to " + this.extractedRomPath + "...");

                using (BinaryWriter bw = new BinaryWriter(File.Open(
                        this.extractedRomPath, FileMode.Create)))
                {
                    // Einstein95's qd2fds.py
                    // Convert QD to FDS
                    //
                    // Convert each side of disk, then insert each into FDS output game data array
                    for (int disk = 0; disk < this.numberOfSides; disk++)
                    {
                        // Get current disk data
                        byte[] currentDisk = new byte[QdDiskSize];
                        Buffer.BlockCopy(this.fullGameDataQD, disk * QdDiskSize, currentDisk, 0, QdDiskSize);

                        // Remove bytes at offsets 0x38 and 0x39
                        for (int i = 0x38; i + 2 < currentDisk.Length; i++)
                        {
                            currentDisk[i] = currentDisk[i + 2];
                            currentDisk[i + 2] = 0;
                        }

                        int position = 0x3A;

                        try
                        {
                            while (currentDisk[position + 2] == 3)
                            {
                                // Delete 2 bytes
                                for (int i = position; i + 2 < currentDisk.Length; i++)
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
                        Buffer.BlockCopy(currentDisk, 0, this.fullGameDataFDS, disk * FdsDiskSize, FdsDiskSize);
                    }

                    Console.WriteLine("Total FDS rom size: " + this.fullGameDataFDS.Length + " Bytes");

                    Console.WriteLine("Writing rom data...");
                    bw.Write(this.fullGameDataFDS);
                }

                Console.WriteLine("Famicom Disk System rom has been " +
                    "created successfully at " + this.extractedRomPath);
            }

            return this.extractedRomPath;
        }

        /// <summary>
        /// Determines if this is a valid FDS ROM.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a Famicom Disk System VC title...");

            // First check if this is a valid ELF file:
            if (this.rpxFile != null)
            {
                Console.WriteLine("Checking " + this.rpxFile.DecompressedPath + "...");
                if (!File.Exists(this.rpxFile.DecompressedPath))
                {
                    Console.WriteLine("Could not find decompressed RPX at " +
                        this.rpxFile.DecompressedPath);
                    return false;
                }

                byte[] headerBuffer = new byte[FdsHeaderLength];

                // Search the decompressed RPX file for the FDS header
                using FileStream fs = new FileStream(this.rpxFile.DecompressedPath, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    byte[] buffer = br.ReadBytes(FdsHeaderLength);

                    // Check the FDS header
                    if (buffer[0] == FdsHeaderCheck[0])
                    {
                        Array.Copy(buffer, headerBuffer, FdsHeaderLength);

                        bool headerValid = true;

                        // Ensure the rest of the header is valid, except final byte (manufacturer code)
                        for (int i = 1; i < FdsHeaderCheck.Length && headerValid; i++)
                        {
                            if (headerBuffer[i] != FdsHeaderCheck[i])
                            {
                                headerValid = false;
                            }
                        }

                        if (headerValid)
                        {
                            // The rom position is a header length
                            // before the current stream position
                            this.romPosition = br.BaseStream.Position -
                                FdsHeaderLength;
                            this.vcNamePosition = this.romPosition - 16;
                            Array.Copy(headerBuffer, 0, this.fdsRomHeader, 0, FdsHeaderLength);
                            Console.WriteLine("Famicom Disk System Rom " +
                                "Detected!");
                            return true;
                        }
                    }
                }
            }

            Console.WriteLine("Not a FDS VC Title");

            return false;
        }
    }
}
