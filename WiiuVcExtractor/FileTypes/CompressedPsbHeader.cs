using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor
{
    class CompressedPsbHeader
    {
        public const int HEADER_SIZE = 8;

        private const int SIGNATURE_SIZE = 4;
        private const int LENGTH_HEADER_OFFSET = 4;
        private const int LENGTH_SIZE = 4;
        private static readonly byte[] SIGNATURE_CHECK = { 0x6D, 0x64, 0x66, 0x00 };

        private byte[] signature = new byte[SIGNATURE_SIZE];
        private UInt32 length;

        public byte[] Signature { get { return this.signature; } }
        public UInt32 Length { get { return this.length; } }

        public CompressedPsbHeader(byte[] rawPsbData)
        {
            // Get the compressed PSB's signature
            Array.Copy(rawPsbData, this.signature, SIGNATURE_SIZE);

            // Get the compressed PSB's length
            this.length = BitConverter.ToUInt32(rawPsbData, LENGTH_HEADER_OFFSET);
        }

        public override string ToString()
        {
            return "Signature: " + BitConverter.ToString(this.signature) + Environment.NewLine + "Length: " + this.length;
        }

        public bool Valid()
        {
            for (int i = 0; i < signature.Length; i++)
            {
                if (signature[i] != SIGNATURE_CHECK[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
