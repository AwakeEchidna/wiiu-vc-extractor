namespace WiiuVcExtractor.FileTypes
{
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// .pkg file header for PC Engine games.
    /// </summary>
    public class PkgHeader
    {
        private const int EntryPointLength = 0x40;
        private const int OptionsLength = 0x20;

        private readonly uint pkgLength;
        private readonly uint headerContentLength;
        private readonly string headerFilename;

        // Appears to be flags for the emulator, but unclear as to the specific purpose
        private readonly byte[] options;

        private readonly string entryPoint;
        private readonly byte[] entryPointBytes;

        // Appears to be identical to the first entry point in most cases, but more data is needed
        private readonly string entryPoint2;
        private readonly byte[] entryPoint2Bytes;

        // Store the total length of the file for later validation
        private readonly long fileLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="PkgHeader"/> class.
        /// </summary>
        /// <param name="pkgFilePath">path to the .pkg file.</param>
        public PkgHeader(string pkgFilePath)
        {
            // read in the pceconfig.bin information and interpret it as the file header
            using (FileStream fs = new FileStream(pkgFilePath, FileMode.Open, FileAccess.Read))
            {
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

                // Read in the header (Add 4 to consider the length part of the size)
                this.pkgLength = br.ReadUInt32LE() + 4;

                // Add 4 to calculate the header length since we are considering the pkgLength to be part of it (for easier offset calculation elsewhere)
                this.headerContentLength = br.ReadUInt32LE();
                this.headerFilename = br.ReadNullTerminatedString();

                this.options = br.ReadBytes(OptionsLength);

                this.entryPointBytes = br.ReadBytes(EntryPointLength);
                this.entryPoint2Bytes = br.ReadBytes(EntryPointLength);

                // Parse the entry point name from each set of bytes
                this.entryPoint = this.entryPointBytes.ReadNullTerminatedString();
                this.entryPoint2 = this.entryPoint2Bytes.ReadNullTerminatedString();
            }

            this.fileLength = new FileInfo(pkgFilePath).Length;
        }

        /// <summary>
        /// Gets the length of the .pkg file.
        /// </summary>
        public uint PkgLength
        {
            get { return this.pkgLength; }
        }

        /// <summary>
        /// Gets the length of the .pkg file header.
        /// </summary>
        public uint Length
        {
            get { return this.headerContentLength + (uint)this.headerFilename.Length + 9; }
        }

        /// <summary>
        /// Gets the filename from the .pkg file header.
        /// </summary>
        public string Filename
        {
            get { return this.headerFilename; }
        }

        /// <summary>
        /// Gets the options of the .pkg file.
        /// </summary>
        public byte[] Options
        {
            get { return this.options; }
        }

        /// <summary>
        /// Gets the first entry point of the .pkg file.
        /// </summary>
        public string EntryPoint
        {
            get { return this.entryPoint; }
        }

        /// <summary>
        /// Gets the second entry point of the .pkg file.
        /// </summary>
        public string EntryPoint2
        {
            get { return this.entryPoint2; }
        }

        /// <summary>
        /// Whether the .pkg file header is valid.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValid()
        {
            // Ensure the interpreted length from the header matches the actual file length
            if (this.pkgLength != this.fileLength)
            {
                return false;
            }

            // Ensure the header length is non-zero
            if (this.headerContentLength < 1)
            {
                return false;
            }

            // Ensure header filename is populated
            if (string.IsNullOrEmpty(this.headerFilename))
            {
                return false;
            }

            // Ensure entry point is populated
            if (string.IsNullOrEmpty(this.entryPoint))
            {
                return false;
            }

            // Ensure entry point 2 is populated
            if (string.IsNullOrEmpty(this.entryPoint2))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generates a string summary of the .pkg file header.
        /// </summary>
        /// <returns>string summary of the .pkg file header.</returns>
        public override string ToString()
        {
            return "PkgHeader:\n" +
                   "pkgLength: " + this.pkgLength.ToString() + "\n" +
                   "headerLength" + this.Length.ToString() + "\n" +
                   "headerContentLength: " + this.headerContentLength.ToString() + "\n" +
                   "headerFilename: " + this.headerFilename + "\n" +
                   "entryPoint: " + this.entryPoint + "\n" +
                   "entryPoint2: " + this.entryPoint2 + "\n";
        }
    }
}
