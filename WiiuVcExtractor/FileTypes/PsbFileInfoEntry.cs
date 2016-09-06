using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.FileTypes
{
    public class PsbFileInfoEntry
    {
        private uint nameIndex;
        private uint length;
        private uint offset;

        public uint NameIndex { get { return nameIndex; } }
        public uint Length { get { return length; } }
        public uint Offset { get { return offset; } }

        public PsbFileInfoEntry(uint nameIndex, uint length, uint offset)
        {
            this.nameIndex = nameIndex;
            this.length = length;
            this.offset = offset;
        }
    }
}
