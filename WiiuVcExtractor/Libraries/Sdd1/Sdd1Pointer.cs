namespace WiiuVcExtractor.Libraries.Sdd1
{
    /// <summary>
    /// S-DD1 pointer position in appended VC rom data.
    /// Appears as SDD1 followed by a single unsigned 4 byte integer indicating
    /// where the associated data can be found.
    /// </summary>
    public class Sdd1Pointer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Sdd1Pointer"/> class.
        /// </summary>
        /// <param name="ptrLocation">S-DD1 pointer location.</param>
        /// <param name="datLocation">S-DD1 decompressed data location.</param>
        public Sdd1Pointer(long ptrLocation, long datLocation)
        {
            this.PointerLocation = ptrLocation;
            this.DataLocation = datLocation;
            this.DataLength = 0;
        }

        /// <summary>
        /// Gets or sets location of the S-DD1 pointer in the SNES rom data.
        /// </summary>
        public long PointerLocation { get; set; }

        /// <summary>
        /// Gets or sets location of the decompressed S-DD1 data in the appended data after the SNES rom data.
        /// </summary>
        public long DataLocation { get; set; }

        /// <summary>
        /// Gets or sets length of the decompressed S-DD1 data in the appended data after the SNES rom data.
        /// </summary>
        public long DataLength { get; set; }
    }
}
