using System;
using System.Text;
using System.IO;
using WiiuVcExtractor.FileTypes;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.RomExtractors
{
    public class PceVcExtractor : IRomExtractor
    {
        private const int PCE_HEADER_LENGTH = 16;

        private PkgFile pkgFile;

        private bool verbose;

        public PceVcExtractor(PkgFile pkgFile, bool verbose = false)
        {
            this.verbose = verbose;
            this.pkgFile = pkgFile;
        }

        public string ExtractRom()
        {
            if (verbose)
            {
                Console.WriteLine("Extracting rom from PKG file...");
                Console.WriteLine("Determining type of rom within PKG file (.pce or CD)");
            }

            // Find any PCE files within the pkg file and write them to complete extraction
            PkgContentFile pceFile = pkgFile.ContentFiles.Find(x => Path.GetExtension(x.Path).ToLower() == ".pce");
            if (pceFile != null)
            {
                if (verbose)
                {
                    Console.WriteLine(".pce file found!");
                }

                pceFile.Write();
                return pceFile.Path;
            }

            // If no PCE files exist, attempt to find and process an HCD file and recombine all content files into a usable format
            PkgContentFile hcdFile = pkgFile.ContentFiles.Find(x => Path.GetExtension(x.Path).ToLower() == ".hcd");
            if (hcdFile != null)
            {
                if (verbose)
                {
                    Console.WriteLine(".hcd file found!");
                }

                // .hcd file was found, create files for all content files
                foreach (var contentFile in pkgFile.ContentFiles)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Extracting {0}...", contentFile.Path);
                    }
                    contentFile.Write();
                }

                // TODO: Generate a .cue file for everything

                return hcdFile.Path;
            }


            return "";
        }

        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a PC Engine VC title...");

            string entryPointExtension = Path.GetExtension(pkgFile.Header.EntryPoint.ToLower());
            // Check whether the entry point is a .pce or .hcd file (case-insensitive)

            if (entryPointExtension == ".pce" || entryPointExtension == ".hcd")
            {
                Console.WriteLine("PC Engine VC Rom detected! Extension {0} was found in the {1} entry point.", entryPointExtension, pkgFile.Path);
                return true;
            }

            Console.WriteLine("Not a PC Engine VC Title");
            return false;
        }
    }
}
