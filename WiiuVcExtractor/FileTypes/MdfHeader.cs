using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    class MdfHeader
    {
        public const int MDF_HEADER_LENGTH = 8;

        private static readonly byte[] MDF_SIGNATURE = { 0x6D, 0x64, 0x66, 0x00 };
        private const int MDF_SIGNATURE_LENGTH = 4;

        private byte[] signature;
        private UInt32 length;

        public UInt32 Length { get { return length; } }

        public MdfHeader(string psbPath)
        {
            signature = new byte[MDF_SIGNATURE_LENGTH];

            using (FileStream fs = new FileStream(psbPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // Read in the header
                    signature = br.ReadBytes(MDF_SIGNATURE_LENGTH);
                    length = EndianUtility.ReadUInt32LE(br);
                }
            }
        }

        public bool IsValid()
        {
            // Check that the signature is correct
            if (signature[0] != MDF_SIGNATURE[0] ||
                signature[1] != MDF_SIGNATURE[1] ||
                signature[2] != MDF_SIGNATURE[2] ||
                signature[3] != MDF_SIGNATURE[3])
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "MdfHeader:\n" +
                   "signature: " + BitConverter.ToString(signature) + "\n" +
                   "length: " + length.ToString() + "\n";
        }
    }
}
