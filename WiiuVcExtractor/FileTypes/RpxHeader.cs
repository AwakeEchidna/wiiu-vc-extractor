namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// RPX file header.
    /// </summary>
    internal class RpxHeader
    {
        private const int ElfSignatureLength = 0x10;
        private const ushort ElfType = 0xFE01;
        private static readonly byte[] ElfSignature = { 0x7F, 0x45, 0x4C, 0x46 };

        private readonly byte[] identity;
        private readonly ushort type;
        private readonly ushort machine;
        private readonly uint version;
        private readonly uint entryPoint;
        private readonly uint phOffset;
        private readonly uint shOffset;
        private readonly uint flags;
        private readonly ushort ehSize;
        private readonly ushort phEntSize;
        private readonly ushort phNum;
        private readonly ushort shEntSize;
        private readonly ushort shNum;
        private readonly ushort shStrIndex;
        private ulong sHeaderDataElfOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="RpxHeader"/> class.
        /// </summary>
        /// <param name="rpxFilePath">path to RPX file.</param>
        public RpxHeader(string rpxFilePath)
        {
            this.identity = new byte[ElfSignatureLength];

            using (FileStream fs = new FileStream(rpxFilePath, FileMode.Open, FileAccess.Read))
            {
                using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

                // Read in the header
                this.identity = br.ReadBytes(ElfSignatureLength);
                this.type = EndianUtility.ReadUInt16BE(br);
                this.machine = EndianUtility.ReadUInt16BE(br);
                this.version = EndianUtility.ReadUInt32BE(br);
                this.entryPoint = EndianUtility.ReadUInt32BE(br);
                this.phOffset = EndianUtility.ReadUInt32BE(br);
                this.shOffset = EndianUtility.ReadUInt32BE(br);
                this.flags = EndianUtility.ReadUInt32BE(br);
                this.ehSize = EndianUtility.ReadUInt16BE(br);
                this.phEntSize = EndianUtility.ReadUInt16BE(br);
                this.phNum = EndianUtility.ReadUInt16BE(br);
                this.shEntSize = EndianUtility.ReadUInt16BE(br);
                this.shNum = EndianUtility.ReadUInt16BE(br);
                this.shStrIndex = EndianUtility.ReadUInt16BE(br);
            }

            this.sHeaderDataElfOffset = (ulong)(this.shOffset + (this.shNum * this.shEntSize));
        }

        /// <summary>
        /// Gets count of section headers.
        /// </summary>
        public ushort SectionHeaderCount
        {
            get { return this.shNum; }
        }

        /// <summary>
        /// Gets section headers' offset.
        /// </summary>
        public uint SectionHeaderOffset
        {
            get { return this.shOffset; }
        }

        /// <summary>
        /// Gets or sets section header data offset.
        /// </summary>
        public ulong SectionHeaderDataElfOffset
        {
            get { return this.sHeaderDataElfOffset; } set { this.sHeaderDataElfOffset = this.SectionHeaderDataElfOffset; }
        }

        /// <summary>
        /// Gets identity (ELF signature).
        /// </summary>
        public byte[] Identity
        {
            get { return this.identity; }
        }

        /// <summary>
        /// Gets type.
        /// </summary>
        public ushort Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Gets machine.
        /// </summary>
        public ushort Machine
        {
            get { return this.machine; }
        }

        /// <summary>
        /// Gets version.
        /// </summary>
        public uint Version
        {
            get { return this.version; }
        }

        /// <summary>
        /// Gets entry point.
        /// </summary>
        public uint EntryPoint
        {
            get { return this.entryPoint; }
        }

        /// <summary>
        /// Gets program header offset.
        /// </summary>
        public uint PhOffset
        {
            get { return this.phOffset; }
        }

        /// <summary>
        /// Gets flags.
        /// </summary>
        public uint Flags
        {
            get { return this.flags; }
        }

        /// <summary>
        /// Gets header size.
        /// </summary>
        public ushort EhSize
        {
            get { return this.ehSize; }
        }

        /// <summary>
        /// Gets program header table entry size.
        /// </summary>
        public ushort PhEntSize
        {
            get { return this.phEntSize; }
        }

        /// <summary>
        /// Gets count of program header table entries.
        /// </summary>
        public ushort PhNum
        {
            get { return this.phNum; }
        }

        /// <summary>
        /// Gets section header entry size.
        /// </summary>
        public ushort ShEntSize
        {
            get { return this.shEntSize; }
        }

        /// <summary>
        /// Gets section header index for the entry containing section names.
        /// </summary>
        public ushort ShStrIndex
        {
            get { return this.shStrIndex; }
        }

        /// <summary>
        /// Whether the RPX file is valid.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValid()
        {
            // Check that the signature is correct
            if (this.identity[0] != ElfSignature[0] ||
                this.identity[1] != ElfSignature[1] ||
                this.identity[2] != ElfSignature[2] ||
                this.identity[3] != ElfSignature[3])
            {
                return false;
            }

            // Check that the type is correct
            if (this.type != ElfType)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates string representation of the RPX file.
        /// </summary>
        /// <returns>string representation of the file.</returns>
        public override string ToString()
        {
            return "RpxHeader:\n" +
                   "identity: " + BitConverter.ToString(this.identity) + "\n" +
                   "type: " + this.type.ToString() + "\n" +
                   "machine: " + this.machine.ToString() + "\n" +
                   "version: " + this.version.ToString() + "\n" +
                   "entryPoint: 0x" + string.Format("{0:X}", this.entryPoint) + "\n" +
                   "phOffset: 0x" + string.Format("{0:X}", this.phOffset) + "\n" +
                   "shOffset: 0x" + string.Format("{0:X}", this.shOffset) + "\n" +
                   "flags: " + this.flags.ToString() + "\n" +
                   "ehSize: " + this.ehSize.ToString() + "\n" +
                   "phEntSize: " + this.phEntSize.ToString() + "\n" +
                   "phNum: " + this.phNum.ToString() + "\n" +
                   "shEntSize: " + this.shEntSize.ToString() + "\n" +
                   "shNum: " + this.shNum.ToString() + "\n" +
                   "shStrIndex: " + this.shStrIndex.ToString() + "\n" +
                   "sHeaderDataElfOffset: 0x" + string.Format("{0:X}", this.sHeaderDataElfOffset) + "\n";
        }
    }
}
