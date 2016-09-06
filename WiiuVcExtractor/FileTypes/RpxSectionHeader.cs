using System;
using System.Text;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    class RpxSectionHeader
    {
        // Length of the section header in bytes
        public const int SECTION_HEADER_LENGTH = 40;
        public const UInt32 SECTION_HEADER_RPL_ZLIB = 0x08000000;
        public const UInt32 SECTION_HEADER_RPL_CRCS = 0x80000003;
        public const UInt32 CHUNK_SIZE = 16384;

        UInt32 name;
        UInt32 type;
        UInt32 flags;
        UInt32 address;
        UInt32 offset;
        UInt32 size;
        UInt32 link;
        UInt32 info;
        UInt32 addrAlign;
        UInt32 entSize;

        
        public UInt32 Name { get { return name; } }
        public UInt32 Type { get { return type; } }
        public UInt32 Flags { get { return flags; } set { flags = Flags; } }
        public UInt32 Address { get { return address; } }
        public UInt32 Offset { get { return offset; } set { offset = Offset; } }
        public UInt32 Size { get { return size; } set { size = Size; } }
        public UInt32 Link { get { return link; } }
        public UInt32 Info { get { return info; } }
        public UInt32 AddrAlign { get { return addrAlign; } }
        public UInt32 EntSize { get { return entSize; } }

        public RpxSectionHeader(byte[] sectionBytes)
        {
            using (MemoryStream ms = new MemoryStream(sectionBytes))
            {
                using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding()))
                {
                    // Read in the header
                    name = EndianUtility.ReadUInt32BE(br);
                    type = EndianUtility.ReadUInt32BE(br);
                    flags = EndianUtility.ReadUInt32BE(br);
                    address = EndianUtility.ReadUInt32BE(br);
                    offset = EndianUtility.ReadUInt32BE(br);
                    size = EndianUtility.ReadUInt32BE(br);
                    link = EndianUtility.ReadUInt32BE(br);
                    info = EndianUtility.ReadUInt32BE(br);
                    addrAlign = EndianUtility.ReadUInt32BE(br);
                    entSize = EndianUtility.ReadUInt32BE(br);
                }
            }
        }

        public override string ToString()
        {


            return "RpxSectionHeader:\n" +
                   "name: " + name.ToString() + "\n" +
                   "type: " + type.ToString() + "\n" +
                   "flags: " + flags.ToString() + "\n" +
                   "address: 0x" + String.Format("{0:X}", address) + "\n" +
                   "offset: 0x" + String.Format("{0:X}", offset) + "\n" +
                   "size: " + size.ToString() + "\n" +
                   "link: " + link.ToString() + "\n" +
                   "info: " + info.ToString() + "\n" +
                   "addrAlign: " + addrAlign.ToString() + "\n" +
                   "entSize: " + entSize.ToString() + "\n";
        }
    }
}
