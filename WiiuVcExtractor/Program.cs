using System;
using System.Collections.Generic;
using System.IO;
using WiiuVcExtractor.RomExtractors;
using WiiuVcExtractor.FileTypes;

namespace WiiuVcExtractor
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("===============================");
            Console.WriteLine("Wii U Virtual Console Extractor");
            Console.WriteLine("===============================");
            Console.WriteLine("Extracts roms from Virtual Console games dumped by DDD.");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("wiiuvcextractor [rpx_or_psb.m_file]");
            Console.WriteLine("");
            Console.WriteLine("Usage Examples:");
            Console.WriteLine("wiiuvcextractor alldata.psb.m");
            Console.WriteLine("wiiuvcextractor WUP-FAME.rpx");

        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                PrintUsage();
                return;
            }

            string sourcePath = args[0];

            if (!File.Exists(sourcePath))
            {
                Console.WriteLine("Could not find source file at " + sourcePath + ". Please ensure that your filename is correct.");
                return;
            }

            Console.WriteLine("============================================================================");
            Console.WriteLine("Starting extraction of rom from " + sourcePath + "...");
            Console.WriteLine("============================================================================");

            string extractedRomPath = "";

            // Attempt to create the RPX file
            RpxFile rpxFile = null;
            if (RpxFile.IsRpx(sourcePath))
            {
                Console.WriteLine("RPX file detected!");
                rpxFile = new RpxFile(sourcePath);
            }



            // Create the list of rom extractors
            List<IRomExtractor> romExtractors = new List<IRomExtractor>();

            if (rpxFile != null)
            {
                romExtractors.Add(new NesVcExtractor(sourcePath, rpxFile));
                romExtractors.Add(new SnesVcExtractor(sourcePath, rpxFile));
            }

            foreach (var romExtractor in romExtractors)
            {
                if (romExtractor.IsValidRom())
                {
                    extractedRomPath = romExtractor.ExtractRom();
                    break;
                }
            }

            if (!String.IsNullOrEmpty(extractedRomPath))
            {
                Console.WriteLine("============================================================================");
                Console.WriteLine(sourcePath + " has been extracted to " + extractedRomPath + " successfully.");
                Console.WriteLine("============================================================================");
            }
            else
            {
                Console.WriteLine("============================================================================");
                Console.WriteLine("FAILURE: Could not successfully identify the rom type for " + sourcePath);
                Console.WriteLine("============================================================================");
            }

            
        }
    }
}
