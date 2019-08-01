using System;
using System.Collections.Generic;
using System.IO;
using WiiuVcExtractor.RomExtractors;
using WiiuVcExtractor.FileTypes;

namespace WiiuVcExtractor
{
    class Program
    {
        private const string WIIU_VC_EXTRACTOR_VERSION = "0.8.0";

        static void PrintUsage()
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("Wii U Virtual Console Extractor " + WIIU_VC_EXTRACTOR_VERSION);
            Console.WriteLine("=====================================");
            Console.WriteLine("Extracts roms from Virtual Console games dumped by DDD or from the SNES Mini.");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("wiiuvcextractor [-v] [rpx_or_psb.m_file]");
            Console.WriteLine("  - Extract a rom from a Virtual Console dump");
            Console.WriteLine("");
            Console.WriteLine("wiiuvcextractor --version");
            Console.WriteLine("  - Display current version");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Usage Examples:");
            Console.WriteLine("wiiuvcextractor alldata.psb.m");
            Console.WriteLine("wiiuvcextractor WUP-FAME.rpx");
            Console.WriteLine("wiiuvcextractor CLV-P-SAAAE.sfrom");
            Console.WriteLine("wiiuvcextractor pce.pkg");
            Console.WriteLine("wiiuvcextractor -v WUP-JBBE.rpx");
        }

        static void PrintVersion()
        {
            Console.WriteLine(WIIU_VC_EXTRACTOR_VERSION);
        }

        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                PrintUsage();
                return;
            }

            if (args.Length == 1)
            {
                if (args[0] == "--version")
                {
                    PrintVersion();
                    return;
                }
            }

            bool verbose = false;
            if (args[0] == "-v")
            {
                verbose = true;
                Console.WriteLine("Verbose output mode is set");
            }

            string sourcePath = args[args.Length - 1];

            if (verbose)
            {
                Console.WriteLine("Source extract file is " + Path.GetFullPath(sourcePath));
            }

            if (!File.Exists(sourcePath))
            {
                Console.WriteLine("Could not find file at " + sourcePath + ". Please ensure that your filename is correct.");
                return;
            }

            Console.WriteLine("============================================================================");
            Console.WriteLine("Starting extraction of rom from " + sourcePath + "...");
            Console.WriteLine("============================================================================");

            string extractedRomPath = "";

            RpxFile rpxFile = null;
            PsbFile psbFile = null;
            PkgFile pkgFile = null;
            SrlFile srlFile = null;

            // Identifies filetype of the file argument,
            // then instantiates file with file's location and verbose
            if (RpxFile.IsRpx(sourcePath))
            {
                Console.WriteLine("RPX file detected!");
                rpxFile = new RpxFile(sourcePath, verbose);
            }
            else if (PsbFile.IsPsb(sourcePath))
            {
                Console.WriteLine("PSB file detected!");
                psbFile = new PsbFile(sourcePath, verbose);
            }
            else if (PkgFile.IsPkg(sourcePath))
            {
                pkgFile = new PkgFile(sourcePath, verbose);
            }
            else if (SrlFile.IsSrl(sourcePath))
            {
                Console.WriteLine("SRL file detected!");
                srlFile = new SrlFile(sourcePath, verbose);
            }

            // Create the list of rom extractors
            List<IRomExtractor> romExtractors = new List<IRomExtractor>();

            if (rpxFile != null)
            {
                romExtractors.Add(new NesVcExtractor(rpxFile, verbose));
                romExtractors.Add(new SnesVcExtractor(rpxFile.DecompressedPath, verbose));
                romExtractors.Add(new FdsVcExtractor(rpxFile, verbose));
            }
            else if (psbFile != null)
            {
                romExtractors.Add(new GbaVcExtractor(psbFile, verbose));
            }
            else if (pkgFile != null)
            {
                romExtractors.Add(new PceVcExtractor(pkgFile, verbose));
            }
            else if (Path.GetExtension(sourcePath) == ".sfrom")
            {
                romExtractors.Add(new SnesVcExtractor(sourcePath, verbose));
            }
            else if (srlFile != null)
            {
                romExtractors.Add(new DsVcExtractor(srlFile, verbose));
            }

            // Check with each extractor until a valid rom is found,
            // Then extract the rom with the appropriate extractor
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
