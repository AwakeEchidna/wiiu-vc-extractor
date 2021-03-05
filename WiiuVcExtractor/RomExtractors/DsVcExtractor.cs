namespace WiiuVcExtractor.RomExtractors
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml.Linq;
    using WiiuVcExtractor.FileTypes;

    /// <summary>
    /// DS VC rom extractor.
    /// </summary>
    public class DsVcExtractor : IRomExtractor
    {
        /// <summary>
        /// Beginning of apparent junk data.
        /// </summary>
        public const int StartJunkOffset = 0x1000;

        /// <summary>
        /// Beginning of game data, just after end of junk.
        /// </summary>
        public const int StartGameOffset = 0x4000;

        // Name of ds rom name dictionary
        private const string DSDictionaryXmlPath = "dsromnames.xml";

        // Nintendo logo at 0xC0
        private readonly byte[] nintenLogo =
        {
            0x24, 0xFF, 0xAE, 0x51, 0x69, 0x9A,
            0xA2, 0x21, 0x3D, 0x84, 0x82, 0x0A, 0x84, 0xE4, 0x09,
            0xAD, 0x11, 0x24, 0x8B, 0x98, 0xC0, 0x81, 0x7F, 0x21,
            0xA3, 0x52, 0xBE, 0x19, 0x93, 0x09, 0xCE, 0x20, 0x10,
            0x46, 0x4A, 0x4A, 0xF8, 0x27, 0x31, 0xEC, 0x58, 0xC7,
            0xE8, 0x33, 0x82, 0xE3, 0xCE, 0xBF, 0x85, 0xF4, 0xDF,
            0x94, 0xCE, 0x4B, 0x09, 0xC1, 0x94, 0x56, 0x8A, 0xC0,
            0x13, 0x72, 0xA7, 0xFC, 0x9F, 0x84, 0x4D, 0x73, 0xA3,
            0xCA, 0x9A, 0x61, 0x58, 0x97, 0xA3, 0x27, 0xFC, 0x03,
            0x98, 0x76, 0x23, 0x1D, 0xC7, 0x61, 0x03, 0x04, 0xAE,
            0x56, 0xBF, 0x38, 0x84, 0x00, 0x40, 0xA7, 0x0E, 0xFD,
            0xFF, 0x52, 0xFE, 0x03, 0x6F, 0x95, 0x30, 0xF1, 0x97,
            0xFB, 0xC0, 0x85, 0x60, 0xD6, 0x80, 0x25, 0xA9, 0x63,
            0xBE, 0x03, 0x01, 0x4E, 0x38, 0xE2, 0xF9, 0xA2, 0x34,
            0xFF, 0xBB, 0x3E, 0x03, 0x44, 0x78, 0x00, 0x90, 0xCB,
            0x88, 0x11, 0x3A, 0x94, 0x65, 0xC0, 0x7C, 0x63, 0x87,
            0xF0, 0x3C, 0xAF, 0xD6, 0x25, 0xE4, 0x8B, 0x38, 0x0A,
            0xAC, 0x72, 0x21, 0xD4, 0xF8, 0x07,
        };

        // First 8 bytes of secure area
        private readonly byte[] decSecArea =
        {
            0xFF, 0xDE, 0xFF, 0xE7, 0xFF, 0xDE,
            0xFF, 0xE7,
        };

        // Path to ds rom name dictionary
        private readonly string dsDictionaryPath;

        // Array of ds game serial numbers
        private readonly string[] dsMD5;

        // Array of ds game titles
        private readonly string[] dsGameTitles;

        // The file whose location will be accessed for reading data
        private readonly SrlFile srlFile;

        // VC names
        private readonly string vcName;

        private readonly bool verbose = false;

        // Game data to be edited and written to output file
        private byte[] game;

        // Location of output file
        private string finalPath;

        // DS name
        private string dsName;

        private bool hasName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DsVcExtractor"/> class.
        /// </summary>
        /// <param name="srlFile">SRL file to extract.</param>
        /// <param name="verbose">Whether to provide verbose output.</param>
        public DsVcExtractor(SrlFile srlFile, bool verbose)
        {
            this.srlFile = srlFile;
            this.verbose = verbose;

            this.dsDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DSDictionaryXmlPath);

            XElement dataFile = XElement.Load(this.dsDictionaryPath);
            this.dsMD5 = (from rom in dataFile.Descendants("rom")
                           select (string)rom.Attribute("md5")).ToArray();
            this.dsGameTitles = (from rom in dataFile.Descendants("game")
                            select (string)rom.Attribute("name")).ToArray();

            this.finalPath = Path.GetFileNameWithoutExtension(srlFile.Path) + "nds";

            this.vcName = Path.GetFileNameWithoutExtension(srlFile.Path);
            this.dsName = this.vcName;

            this.hasName = false;
        }

        /// <summary>
        /// Check for the Nintendo logo at offsets 0xC0 to 0x15B.
        /// </summary>
        /// <returns>Whether the provided SRL file is a valid rom.</returns>
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a Nintendo DS VC title...");

            if (this.srlFile != null)
            {
                if (!File.Exists(this.srlFile.Path))
                {
                    Console.WriteLine("Could not find SRL file at " +
                        this.srlFile.Path);
                    return false;
                }

                using FileStream fs = new FileStream(this.srlFile.Path, FileMode.Open, FileAccess.Read);
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

                // Read up to 0xC0
                br.ReadBytes(0xC0);

                // Check Nintendo logo
                for (int i = 0; i < this.nintenLogo.Length; i++)
                {
                    if (this.nintenLogo[i] != br.ReadByte())
                    {
                        Console.WriteLine("Not a valid DS VC Title");
                        return false;
                    }
                }

                Console.WriteLine("DS Rom Detected!");
                return true;
            }

            Console.WriteLine("Not a DS VC Title");
            return false;
        }

        /// <summary>
        /// Extracts DS ROM from SRL file.
        /// TODO: Finish decryption.
        /// </summary>
        /// <returns>Path to extracted rom.</returns>
        public string ExtractRom()
        {
            // Reads in game for editing
            using (FileStream fs = new FileStream(this.srlFile.Path, FileMode.Open, FileAccess.Read))
            {
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
                this.game = br.ReadBytes((int)fs.Length);
            }

            Console.WriteLine("Overwriting junk data...");

            // Overwrites junk data with zeroes
            for (int i = StartJunkOffset; i < StartGameOffset; i++)
            {
                this.game[i] = 0x00;
            }

            // First, check if encrypted - otherwise, skip decryption
            bool decCheck = true;

            for (int i = 0; i < 8 && decCheck; i++)
            {
                if (this.decSecArea[i] != this.game[0x4000 + i])
                {
                    decCheck = false;
                }
            }

            // TODO - actually implement the decryption lol
            //
            // If encrypted, proceed with decryption
            if (!decCheck)
            {
            }

            // Identify name of file using MD5 hash and set as output path
            if (this.verbose)
            {
                Console.WriteLine("Getting MD5 hash.");
            }

            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(this.game);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            string hashString = sBuilder.ToString();

            Console.WriteLine("MD5 of overwritten file is " + hashString);

            // Attempt to find matching MD5 and then gametitle
            // IF this fails, the SRL file name is used instead
            for (int i = 0; i < this.dsMD5.Length; i++)
            {
                if (hashString.ToUpper().CompareTo(this.dsMD5[i]) == 0)
                {
                    this.finalPath = this.finalPath.Substring(0, this.finalPath.Length - Path.GetFileName(this.finalPath).Length);

                    this.finalPath += this.dsGameTitles[i] + ".nds";

                    this.dsName = Path.GetFileNameWithoutExtension(this.finalPath);
                    this.hasName = true;

                    i = this.dsMD5.Length;
                }
            }

            Console.WriteLine("Virtual Console Title: " + this.vcName);
            if (this.hasName)
            {
                Console.WriteLine("DS Title: " + this.dsName);
            }
            else
            {
                Console.WriteLine("DS title not found");
            }

            Console.WriteLine("Writing game data...");

            // Ouputs final game file
            using (FileStream fs = new FileStream(this.finalPath, FileMode.Create))
            {
                using BinaryWriter bw = new BinaryWriter(fs, new ASCIIEncoding());
                bw.Write(this.game);
            }

            Console.WriteLine("DS rom has been created successfully at " +
                Path.GetFileName(this.finalPath));

            // Return location of final game file to Program.cs
            return Path.GetFileName(this.finalPath);
        }
    }
}
