namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// Extractor for .pkg files for PC Engine games.
    /// </summary>
    public class PkgFile
    {
        private readonly PkgHeader header;
        private readonly List<PkgContentFile> contentFiles;
        private readonly string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="PkgFile"/> class.
        /// </summary>
        /// <param name="pkgFilePath">path to the .pkg file.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public PkgFile(string pkgFilePath, bool verbose = false)
        {
            Console.WriteLine("Extracting PKG file...");

            this.contentFiles = new List<PkgContentFile>();

            this.path = pkgFilePath;

            try
            {
                this.header = new PkgHeader(this.path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not successfully read PKG file: " + ex.Message);
                return;
            }

            if (verbose)
            {
                Console.WriteLine("Successfully read PKG file header as:\n{0}", this.header.ToString());
            }

            // Detect each file within the PKG file after the header and store it in memory as a PkgContentFile, start after the header
            using FileStream fs = new FileStream(pkgFilePath, FileMode.Open, FileAccess.Read);

            // Skip header
            fs.Seek(this.header.Length, SeekOrigin.Begin);
            using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                // Read in each section, these are arranged as a LE UINT32 describing the size followed by a null-terminated filename and its content
                int sectionLength = br.ReadInt32LE();
                string sectionPath = br.ReadNullTerminatedString();
                byte[] sectionContent = br.ReadBytes(sectionLength);
                this.contentFiles.Add(new PkgContentFile(sectionPath, sectionContent));
            }
        }

        /// <summary>
        /// Gets associated .pkg header.
        /// </summary>
        public PkgHeader Header
        {
            get { return this.header; }
        }

        /// <summary>
        /// Gets content files stored in the .pkg file.
        /// </summary>
        public List<PkgContentFile> ContentFiles
        {
            get { return this.contentFiles; }
        }

        /// <summary>
        /// Gets path to the .pkg file.
        /// </summary>
        public string Path
        {
            get { return this.path; }
        }

        /// <summary>
        /// Whether the given path is a valid .pkg file.
        /// </summary>
        /// <param name="pkgFilePath">path to the .pkg file to validate.</param>
        /// <returns>true if value, false otherwise.</returns>
        public static bool IsPkg(string pkgFilePath)
        {
            try
            {
                PkgHeader header = new PkgHeader(pkgFilePath);
                return header.IsValid();
            }
            catch (Exception ex)
            {
                // If an exception is received, assume that the header could not be parsed successfully
                Console.WriteLine("Could not parse pkg header, this is likely not a pkg: {0}", ex.Message);
                return false;
            }
        }
    }
}
