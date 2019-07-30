using System;
using System.Text;
using System.IO;
using WiiuVcExtractor.FileTypes;
using System.Xml.Linq;
using System.Linq;
using System.Security.Cryptography;

namespace WiiuVcExtractor.RomExtractors
{
    public class DsVcExtractor : IRomExtractor
    {
        // Nintendo logo at 0xC0
        private readonly byte[] nintenLogo = {0x24, 0xFF, 0xAE, 0x51, 0x69, 0x9A,
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
                        0xAC, 0x72, 0x21, 0xD4, 0xF8, 0x07};

        // First 8 bytes of secure area
        private readonly byte[] decSecArea = {0xFF, 0xDE, 0xFF, 0xE7, 0xFF, 0xDE,
            0xFF, 0xE7};

        // Name of ds rom name dictionary
        private const string DS_DICTIONARY_XML_PATH = "dsromnames.xml";

        // Path to ds rom name dictionary
        private string dsDictionaryPath;

        // Array of ds game serial numbers
        private string[] dsMD5;
        // Array of ds game titles
        private string[] dsGameTitles;

        // The file whose location will be accessed for reading data
        private SrlFile srlFile;

        // Beginning of apparent junk data
        public const int startJunkOffset = 0x1000;

        // Beginning of game data, just after end of junk
        public const int startGameOffset = 0x4000;

        // Game data to be edited and written to output file
        private byte[] game;

        // Location of output file
        private string finalPath;

        private bool verbose = false;

        // Constructor - set verbosity, path to srl file, and rom dictionary
        public DsVcExtractor(SrlFile srlFile, bool verbose)
        {
            this.srlFile = srlFile;
            this.verbose = verbose;

            dsDictionaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                DS_DICTIONARY_XML_PATH);

            XElement dataFile = XElement.Load(dsDictionaryPath);
            dsMD5 = (from rom in dataFile.Descendants("rom")
                           select (string)rom.Attribute("md5")).ToArray();
            dsGameTitles = (from rom in dataFile.Descendants("game")
                            select (string)rom.Attribute("name")).ToArray();

            finalPath = srlFile.Path.Substring(0, srlFile.Path.Length - 3) + "nds";
        }

        // Check for the Nintendo logo at offsets 0xC0 to 0x15B
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a Nintendo DS VC title...");

            if (srlFile != null)
            {
                if (!File.Exists(srlFile.Path))
                {
                    Console.WriteLine("Could not find SRL file at " +
                        srlFile.Path);
                    return false;
                }

                using (FileStream fs = new FileStream(srlFile.Path, FileMode.Open,
                    FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                    {
                        // Read up to 0xC0
                        br.ReadBytes(0xC0);

                        // Check Nintendo logo
                        for (int i = 0; i < nintenLogo.Length; i++)
                        {
                            if (nintenLogo[i] != br.ReadByte())
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }
            }

            Console.WriteLine("Not a DS VC Title");
            return false;
        }

        // TODO - finsih encryption
        //
        // Corrects ROM dump
        public string ExtractRom()
        {
            // Reads in game for editing
            using (FileStream fs = new FileStream(srlFile.Path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    game = br.ReadBytes((int)fs.Length);
                }
            }

            // Overwrites junk data with zeroes
            for (int i = startJunkOffset; i < startGameOffset; i++)
            {
                game[i] = 0x00;
            }

            // First, check if encrypted - otherwise, skip decryption
            bool decCheck = true;

            for(int i = 0; i < 8 && decCheck; i++)
            {
                if(decSecArea[i] != game[0x4000 + i])
                {
                    decCheck = false;
                }
            }

            // TODO - actually implement the decryption lol
            //
            // If encrypted, proceed with decryption
            if(!decCheck)
            {

            }

            // TODO - get names for both encrypted and decrypted roms
            //
            // Identify name of file using MD5 hash and set as output path
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(game);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            string hashString = sBuilder.ToString();

            // Attempt to find matching MD5 and then gametitle
            // IF this fails, the SRL file name is used instead
            for(int i = 0; i < dsMD5.Length; i++)
            {
                if(hashString.ToUpper().CompareTo(dsMD5[i]) == 0)
                {
                    finalPath = finalPath.Substring(0, finalPath.Length - 
                        Path.GetFileName(finalPath).Length);
                    finalPath += dsGameTitles[i] + ".nds";

                    i = dsMD5.Length;
                }
            }

            // Ouputs final game file
            using (FileStream fs = new FileStream(finalPath, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs, new ASCIIEncoding()))
                {
                    bw.Write(game);
                }
            }

            // Return location of final game file to Program.cs
            return finalPath;
        }
    }
}
