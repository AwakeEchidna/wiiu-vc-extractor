using System;
using System.IO;

namespace WiiuVcExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please provide the source rpx file and the destination rom name.");
                return;
            }

            string rpxPath = args[0];
            string destinationPath = args[1];

            if (!File.Exists(rpxPath))
            {
                Console.WriteLine("Could not find RPX file at " + rpxPath + ". Please ensure that your filename is correct.");
                return;
            }

            // Attempt to decompress the RPX file using wiiurpxtool
            if (!File.Exists("wiiurpxtool.exe"))
            {
                Console.WriteLine("Could not find wiiurpxtool.exe. Please ensure that it is in your working directory.");
                return;
            }

            Console.WriteLine("Found wiiurpxtool.exe, decompressing the RPX file...");

            System.Diagnostics.Process extractProcess = System.Diagnostics.Process.Start("wiiurpxtool.exe", "-d " + rpxPath );

            // Wait for extraction to complete...
            while (extractProcess.HasExited == false)
            {
                System.Threading.Thread.Sleep(500);
            }

            // TODO: Add a check to determine what kind of ROM it is prior to starting up the extractor
            RomPlatformIdentifier romIdentifier = new RomPlatformIdentifier();
            RomPlatform platform = romIdentifier.identifyRom(rpxPath);

            switch (platform)
            {
                case RomPlatform.NES:
                    NesVcExtractor nesExtract = new NesVcExtractor();
                    nesExtract.extractRomFromVcDump(rpxPath, destinationPath);
                    break;
                case RomPlatform.SNES:
                    SnesVcExtractor snesExtract = new SnesVcExtractor();
                    snesExtract.extractRomFromVcDump(rpxPath, destinationPath);
                    break;
            }
        }
    }
}
