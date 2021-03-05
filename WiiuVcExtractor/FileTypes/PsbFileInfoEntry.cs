namespace WiiuVcExtractor.FileTypes
{
    /// <summary>
    /// PSB file info entry.
    /// </summary>
    public class PsbFileInfoEntry
    {
        private readonly uint nameIndex;
        private readonly uint length;
        private readonly uint offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsbFileInfoEntry"/> class.
        /// </summary>
        /// <param name="nameIndex">file name index.</param>
        /// <param name="length">file length.</param>
        /// <param name="offset">file offset in bytes.</param>
        public PsbFileInfoEntry(uint nameIndex, uint length, uint offset)
        {
            this.nameIndex = nameIndex;
            this.length = length;
            this.offset = offset;
        }

        /// <summary>
        /// Gets the name index for the file.
        /// </summary>
        public uint NameIndex
        {
            get { return this.nameIndex; }
        }

        /// <summary>
        /// Gets the length of the file.
        /// </summary>
        public uint Length
        {
            get { return this.length; }
        }

        /// <summary>
        /// Gets the offset of the file in bytes.
        /// </summary>
        public uint Offset
        {
            get { return this.offset; }
        }
    }
}
