using System;

namespace WiiuVcExtractor.FileTypes
{
    public class RpxSectionHeaderSort : IEquatable<RpxSectionHeaderSort>, IComparable<RpxSectionHeaderSort>
    {
        public UInt32 index;
        public UInt32 offset;

        public int CompareTo(RpxSectionHeaderSort other)
        {
            if (other == null)
                return 1;
            else
                return this.offset.CompareTo(other.offset);
        }

        public bool Equals(RpxSectionHeaderSort other)
        {
            if (other == null) return false;
            return (this.offset.Equals(other.offset));
        }

        public override string ToString()
        {
            return "RpxSectionHeaderSort:\n" +
                   "index: " + index.ToString() + "\n" +
                   "offset: 0x" + String.Format("{0:X}", offset) + "\n";
        }
    }
}
