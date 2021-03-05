namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.IO;
    using System.Text;
    using WiiuVcExtractor.Libraries;

    /// <summary>
    /// MDF header class.
    /// </summary>
    public class MdfHeader
    {
        /// <summary>
        /// Length of the MDF header in bytes.
        /// </summary>
        public const int MDFHeaderLength = 8;

        private const int MDFSignatureLength = 4;

        private static readonly byte[] MDFSignature = { 0x6D, 0x64, 0x66, 0x00 };

        private readonly byte[] signature;
        private readonly uint length;

        /// <summary>
        /// Initializes a new instance of the <see cref="MdfHeader"/> class.
        /// </summary>
        /// <param name="psbPath">path to the PSB file to read.</param>
        public MdfHeader(string psbPath)
        {
            this.signature = new byte[MDFSignatureLength];

            using FileStream fs = new FileStream(psbPath, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

            // Read in the header
            this.signature = br.ReadBytes(MDFSignatureLength);
            this.length = EndianUtility.ReadUInt32LE(br);
        }

        /// <summary>
        /// Gets the length of the associated MDF data.
        /// </summary>
        public uint Length
        {
            get { return this.length; }
        }

        /// <summary>
        /// Whether the current header is valid.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        public bool IsValid()
        {
            // Check that the signature is correct
            if (this.signature[0] != MDFSignature[0] ||
                this.signature[1] != MDFSignature[1] ||
                this.signature[2] != MDFSignature[2] ||
                this.signature[3] != MDFSignature[3])
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generates string representation of the header.
        /// </summary>
        /// <returns>string representation of the header.</returns>
        public override string ToString()
        {
            return "MdfHeader:\n" +
                   "signature: " + BitConverter.ToString(this.signature) + "\n" +
                   "length: " + this.length.ToString() + "\n";
        }
    }
}
