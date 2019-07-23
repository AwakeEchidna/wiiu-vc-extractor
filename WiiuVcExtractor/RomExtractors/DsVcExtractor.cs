using System;
using System.Text;
using System.IO;
using WiiuVcExtractor.FileTypes;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.RomExtractors
{
    public class DsVcExtractor : IRomExtractor
    {
        
        // T O D O
        //private const string DS_DICTIONARY_CSV_PATH = "";

        private SrlFile srlFile;
        // T O D O
        //private RomNameDictionary dsDictionary;

        public const int startJunkOffset = 0x1000;
        public const int startGameOffset = 0x4000;

        private byte[] game;

        private bool verbose = false;

        public DsVcExtractor(SrlFile srlFile, bool verbose)
        {
            this.verbose = verbose;

            // T O D O
            //string dsDictionaryPath = Path.Combine(
            //    AppDomain.CurrentDomain.BaseDirectory, DS_DICTIONARY_CSV_PATH);

            //dsDictionary = new RomNameDictionary(dsDictionaryPath);

            this.srlFile = srlFile;
        }

        // Check for the Nintendo logo at offset 0xC0 to 0x15B
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a Nintendo DS VC title...");

            if (srlFile != null)
            {
                if (!File.Exists(srlFile.Path))
                {
                    Console.WriteLine("Could not find SRL at " +
                        srlFile.Path);
                    return false;
                }
            }

            return true;
        }

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

            // Encrypts file for compatibility with original hardware


            // Ouputs final game file
            using (FileStream fs = new FileStream(srlFile.FinalPath, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs, new ASCIIEncoding()))
                {
                    bw.Write(game);
                }
            }

            return srlFile.FinalPath;
        }
    }
}
