namespace WiiuVcExtractor.FileTypes
{
    using System;

    /// <summary>
    /// RPX section header sorter.
    /// </summary>
    public class RpxSectionHeaderSort : IEquatable<RpxSectionHeaderSort>, IComparable<RpxSectionHeaderSort>
    {
        /// <summary>
        /// Gets or sets RPX section index.
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// Gets or sets RPX section offset.
        /// </summary>
        public uint Offset { get; set; }

        /// <summary>
        /// Compares RPX section headers.
        /// </summary>
        /// <param name="other">other section header to compare.</param>
        /// <returns>A signed number indicating the relative values of this instance and value.</returns>
        public int CompareTo(RpxSectionHeaderSort other)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                return this.Offset.CompareTo(other.Offset);
            }
        }

        /// <summary>
        /// Compares RPX section headers for equality.
        /// </summary>
        /// <param name="other">other section header to compare.</param>
        /// <returns>true if equal, false otherwise.</returns>
        public bool Equals(RpxSectionHeaderSort other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Offset.Equals(other.Offset);
        }

        /// <summary>
        /// Creates string representation of the RPX section header sort.
        /// </summary>
        /// <returns>string representation of the RPX section header sort.</returns>
        public override string ToString()
        {
            return "RpxSectionHeaderSort:\n" +
                   "index: " + this.Index.ToString() + "\n" +
                   "offset: 0x" + string.Format("{0:X}", this.Offset) + "\n";
        }
    }
}
