namespace WiiuVcExtractor.FileTypes
{
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// RPX section header.
    /// </summary>
    internal class RpxSectionHeader
    {
        /// <summary>
        /// Length of the section header in bytes.
        /// </summary>
        public const int SectionHeaderLength = 40;

        /// <summary>
        /// Flag indicating zlib compression.
        /// </summary>
        public const uint SectionHeaderRplZlib = 0x08000000;

        /// <summary>
        /// Flag indicating CRC section.
        /// </summary>
        public const uint SectionHeaderRplCrcs = 0x80000003;

        /// <summary>
        /// Section chunk size.
        /// </summary>
        public const uint ChunkSize = 16384;

        private readonly uint name;
        private readonly uint type;
        private readonly uint address;
        private readonly uint link;
        private readonly uint info;
        private readonly uint addrAlign;
        private readonly uint entSize;
        private uint offset;
        private uint size;
        private uint flags;

        /// <summary>
        /// Initializes a new instance of the <see cref="RpxSectionHeader"/> class.
        /// </summary>
        /// <param name="sectionBytes">bytes for the section to parse.</param>
        public RpxSectionHeader(byte[] sectionBytes)
        {
            using MemoryStream ms = new MemoryStream(sectionBytes);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());

            // Read in the header
            this.name = EndianUtility.ReadUInt32BE(br);
            this.type = EndianUtility.ReadUInt32BE(br);
            this.flags = EndianUtility.ReadUInt32BE(br);
            this.address = EndianUtility.ReadUInt32BE(br);
            this.offset = EndianUtility.ReadUInt32BE(br);
            this.size = EndianUtility.ReadUInt32BE(br);
            this.link = EndianUtility.ReadUInt32BE(br);
            this.info = EndianUtility.ReadUInt32BE(br);
            this.addrAlign = EndianUtility.ReadUInt32BE(br);
            this.entSize = EndianUtility.ReadUInt32BE(br);
        }

        /// <summary>
        /// Gets section name.
        /// </summary>
        public uint Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets section type.
        /// </summary>
        public uint Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Gets or sets section flags.
        /// </summary>
        public uint Flags
        {
            get { return this.flags; } set { this.flags = this.Flags; }
        }

        /// <summary>
        /// Gets address.
        /// </summary>
        public uint Address
        {
            get { return this.address; }
        }

        /// <summary>
        /// Gets or sets section offset.
        /// </summary>
        public uint Offset
        {
            get { return this.offset; } set { this.offset = this.Offset; }
        }

        /// <summary>
        /// Gets or sets section size.
        /// </summary>
        public uint Size
        {
            get { return this.size; } set { this.size = this.Size; }
        }

        /// <summary>
        /// Gets link.
        /// </summary>
        public uint Link
        {
            get { return this.link; }
        }

        /// <summary>
        /// Gets info.
        /// </summary>
        public uint Info
        {
            get { return this.info; }
        }

        /// <summary>
        /// Gets address alignment.
        /// </summary>
        public uint AddrAlign
        {
            get { return this.addrAlign; }
        }

        /// <summary>
        /// Gets entry size.
        /// </summary>
        public uint EntSize
        {
            get { return this.entSize; }
        }

        /// <summary>
        /// Creates string representation of the RPX section header.
        /// </summary>
        /// <returns>string representation of the RPX section header.</returns>
        public override string ToString()
        {
            return "RpxSectionHeader:\n" +
                   "name: " + this.name.ToString() + "\n" +
                   "type: " + this.type.ToString() + "\n" +
                   "flags: " + this.flags.ToString() + "\n" +
                   "address: 0x" + string.Format("{0:X}", this.address) + "\n" +
                   "offset: 0x" + string.Format("{0:X}", this.offset) + "\n" +
                   "size: " + this.size.ToString() + "\n" +
                   "link: " + this.link.ToString() + "\n" +
                   "info: " + this.info.ToString() + "\n" +
                   "addrAlign: " + this.addrAlign.ToString() + "\n" +
                   "entSize: " + this.entSize.ToString() + "\n";
        }
    }
}
