using System;
using System.Text;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    class RpxHeader
    {
        private static readonly byte[] ELF_SIGNATURE = { 0x7F, 0x45, 0x4C, 0x46 };
        private const int ELF_SIGNATURE_LENGTH = 0x10;
        private const UInt16 ELF_TYPE = 0xFE01;

        byte[] identity;
        UInt16 type;
        UInt16 machine;
        UInt32 version;
        UInt32 entryPoint;
        UInt32 phOffset;
        UInt32 shOffset;
        UInt32 flags;
        UInt16 ehSize;
        UInt16 phEntSize;
        UInt16 phNum;
        UInt16 shEntSize;
        UInt16 shNum;
        UInt16 shStrIndex;

        UInt64 sHeaderDataElfOffset;


        #region Accessors
        public UInt16 SectionHeaderCount { get { return shNum; } }
        public UInt32 SectionHeaderOffset { get { return shOffset; } }
        public UInt64 SectionHeaderDataElfOffset { get { return sHeaderDataElfOffset; } set { sHeaderDataElfOffset = SectionHeaderDataElfOffset; } }

        public byte[] Identity { get { return identity; } }
        public UInt16 Type { get { return type; } }
        public UInt16 Machine { get { return machine; } }
        public UInt32 Version { get { return version; } }
        public UInt32 EntryPoint { get { return entryPoint; } }
        public UInt32 PhOffset { get { return phOffset; } }
        public UInt32 Flags { get { return flags; } }
        public UInt16 EhSize { get { return ehSize; } }
        public UInt16 PhEntSize { get { return phEntSize; } }
        public UInt16 PhNum { get { return phNum; } }
        public UInt16 ShEntSize { get { return shEntSize; } }
        public UInt16 ShStrIndex { get { return shStrIndex; } }
        #endregion

        public RpxHeader(string rpxFilePath)
        {
            identity = new byte[ELF_SIGNATURE_LENGTH];

            using (FileStream fs = new FileStream(rpxFilePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // Read in the header
                    //
                    // read first 16 bytes
                    identity = br.ReadBytes(ELF_SIGNATURE_LENGTH);
                    // read next 2 bytes (offsets 0x10 and 0x11)
                    type = EndianUtility.ReadUInt16BE(br);
                    // read offset 0x13
                    machine = EndianUtility.ReadUInt16BE(br);
                    // read offset 0x17
                    version = EndianUtility.ReadUInt32BE(br);
                    // read offsets 0x18 through 0x1B
                    entryPoint = EndianUtility.ReadUInt32BE(br);
                    // read ? (the value is zero)
                    phOffset = EndianUtility.ReadUInt32BE(br);
                    // read offset 0x23
                    shOffset = EndianUtility.ReadUInt32BE(br);
                    // read ? (the value is zero)
                    flags = EndianUtility.ReadUInt32BE(br);
                    // read offset 0x29
                    ehSize = EndianUtility.ReadUInt16BE(br);
                    // read ? (the value is zero)
                    phEntSize = EndianUtility.ReadUInt16BE(br);
                    // read ? (the value is zero)
                    phNum = EndianUtility.ReadUInt16BE(br);
                    // read offset 0x2F
                    shEntSize = EndianUtility.ReadUInt16BE(br);
                    // read offset 0x31
                    shNum = EndianUtility.ReadUInt16BE(br);
                    // read offset 0x33
                    shStrIndex = EndianUtility.ReadUInt16BE(br);
                }
            }

            sHeaderDataElfOffset = (ulong)(shOffset + (shNum * shEntSize));
        }

        public bool IsValid()
        {
            // Check that the signature is correct
            if (identity[0] != ELF_SIGNATURE[0] ||
                identity[1] != ELF_SIGNATURE[1] ||
                identity[2] != ELF_SIGNATURE[2] ||
                identity[3] != ELF_SIGNATURE[3])
            {
                return false;
            }

            // Check that the type is correct
            if (type != ELF_TYPE)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "RpxHeader:\n" +
                   "identity: " + BitConverter.ToString(identity) + "\n" +
                   "type: " + type.ToString() + "\n" +
                   "machine: " + machine.ToString() + "\n" +
                   "version: " + version.ToString() + "\n" +
                   "entryPoint: 0x" + String.Format("{0:X}", entryPoint) + "\n" +
                   "phOffset: 0x" + String.Format("{0:X}", phOffset) + "\n" +
                   "shOffset: 0x" + String.Format("{0:X}", shOffset) + "\n" +
                   "flags: " + flags.ToString() + "\n" +
                   "ehSize: " + ehSize.ToString() + "\n" +
                   "phEntSize: " + phEntSize.ToString() + "\n" +
                   "phNum: " + phNum.ToString() + "\n" +
                   "shEntSize: " + shEntSize.ToString() + "\n" +
                   "shNum: " + shNum.ToString() + "\n" +
                   "shStrIndex: " + shStrIndex.ToString() + "\n" +
                   "sHeaderDataElfOffset: 0x" + String.Format("{0:X}", sHeaderDataElfOffset) + "\n";
        }
    }
}
