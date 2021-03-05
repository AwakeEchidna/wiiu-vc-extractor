namespace WiiuVcExtractor.RomExtractors
{
    using System;
    using System.IO;
    using WiiuVcExtractor.FileTypes;

    /// <summary>
    /// PCE VC rom extractor.
    /// </summary>
    public class PceVcExtractor : IRomExtractor
    {
        // private const int PceHeaderLength = 16;
        private readonly PkgFile pkgFile;
        private readonly bool verbose;

        /// <summary>
        /// Initializes a new instance of the <see cref="PceVcExtractor"/> class.
        /// </summary>
        /// <param name="pkgFile">PKG file to extract.</param>
        /// <param name="verbose">whether to enable verbose output.</param>
        public PceVcExtractor(PkgFile pkgFile, bool verbose = false)
        {
            this.verbose = verbose;
            this.pkgFile = pkgFile;
        }

        /// <summary>
        /// Extracts a PCE rom.
        /// </summary>
        /// <returns>path to extracted rom.</returns>
        public string ExtractRom()
        {
            if (this.verbose)
            {
                Console.WriteLine("Extracting rom from PKG file...");
                Console.WriteLine("Determining type of rom within PKG file (.pce or CD)");
            }

            // Find any PCE files within the pkg file and write them to complete extraction
            PkgContentFile pceFile = this.pkgFile.ContentFiles.Find(x => Path.GetExtension(x.Path).ToLower() == ".pce");
            if (pceFile != null)
            {
                if (this.verbose)
                {
                    Console.WriteLine(".pce file found!");
                }

                pceFile.Write();
                return pceFile.Path;
            }

            // If no PCE files exist, attempt to find and process an HCD file and recombine all content files into a usable format
            PkgContentFile hcdFile = this.pkgFile.ContentFiles.Find(x => Path.GetExtension(x.Path).ToLower() == ".hcd");
            if (hcdFile != null)
            {
                if (this.verbose)
                {
                    Console.WriteLine(".hcd file found!");
                }

                // .hcd file was found, create files for all content files
                foreach (var contentFile in this.pkgFile.ContentFiles)
                {
                    if (this.verbose)
                    {
                        Console.WriteLine("Extracting {0}...", contentFile.Path);
                    }

                    contentFile.Write();
                }

                /*
                // TODO: Generate a .cue file for everything
                */

                return hcdFile.Path;
            }

            return string.Empty;
        }

        /// <summary>
        /// Whether the rom is valid.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValidRom()
        {
            Console.WriteLine("Checking if this is a PC Engine VC title...");

            string entryPointExtension = Path.GetExtension(this.pkgFile.Header.EntryPoint.ToLower());

            // Check whether the entry point is a .pce or .hcd file (case-insensitive)
            if (entryPointExtension == ".pce" || entryPointExtension == ".hcd")
            {
                Console.WriteLine("PC Engine VC Rom detected! Extension {0} was found in the {1} entry point.", entryPointExtension, this.pkgFile.Path);
                return true;
            }

            Console.WriteLine("Not a PC Engine VC Title");
            return false;
        }
    }
}
