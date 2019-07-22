using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class PkgHeader
    {
        private const int ENTRY_POINT_LENGTH = 0x40;
        private const int OPTIONS_LENGTH = 0x20;

        private UInt32 pkgLength;
        private UInt32 headerContentLength;
        private string headerFilename;

        // TODO: Appears to be flags for the emulator, but unclear as to the specific purpose
        private byte[] options;

        private string entryPoint;
        private byte[] entryPointBytes;

        // TODO: Appears to be identical to the first entry point in most cases, but more data is needed
        private string entryPoint2;
        private byte[] entryPoint2Bytes;

        // Store the total length of the file for later validation
        private long fileLength;

        public UInt32 PkgLength { get { return pkgLength; } }
        public UInt32 Length { get { return headerContentLength + (uint)headerFilename.Length + 9; } }
        public string Filename { get { return headerFilename; } }
        public string EntryPoint { get { return entryPoint; } }
        public string EntryPoint2 { get { return entryPoint2; } }

        public PkgHeader(string pkgFilePath)
        {
            // read in the pceconfig.bin information and interpret it as the file header
            using (FileStream fs = new FileStream(pkgFilePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // Read in the header (Add 4 to consider the length part of the size)
                    pkgLength = br.ReadUInt32LE() + 4;

                    // Add 4 to calculate the header length since we are considering the pkgLength to be part of it (for easier offset calculation elsewhere)
                    headerContentLength = br.ReadUInt32LE();
                    headerFilename = br.ReadNullTerminatedString();

                    options = br.ReadBytes(OPTIONS_LENGTH);

                    entryPointBytes = br.ReadBytes(ENTRY_POINT_LENGTH);
                    entryPoint2Bytes = br.ReadBytes(ENTRY_POINT_LENGTH);

                    // Parse the entry point name from each set of bytes
                    entryPoint = entryPointBytes.ReadNullTerminatedString();
                    entryPoint2 = entryPoint2Bytes.ReadNullTerminatedString();
                }
            }

            fileLength = new FileInfo(pkgFilePath).Length;
        }

        public bool IsValid()
        {
            // Ensure the interpreted length from the header matches the actual file length
            if (pkgLength != fileLength)
            {
                return false;
            }

            // Ensure the header length is non-zero
            if (headerContentLength < 1)
            {
                return false;
            }

            // Ensure header filename is populated
            if (String.IsNullOrEmpty(headerFilename))
            {
                return false;
            }

            // Ensure entry point is populated
            if (String.IsNullOrEmpty(entryPoint))
            {
                return false;
            }

            // Ensure entry point 2 is populated
            if (String.IsNullOrEmpty(entryPoint2))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "PkgHeader:\n" +
                   "pkgLength: " + pkgLength.ToString() + "\n" +
                   "headerLength" + Length.ToString() + "\n" +
                   "headerContentLength: " + headerContentLength.ToString() + "\n" +
                   "headerFilename: " + headerFilename + "\n" +
                   "entryPoint: " + entryPoint + "\n" +
                   "entryPoint2: " + entryPoint2 + "\n";
        }
    }
}
