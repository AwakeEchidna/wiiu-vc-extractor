using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WiiuVcExtractor
{
    public enum RomPlatform { Unknown, NES, SNES };

    public class RomPlatformIdentifier
    {
        private const int RPX_HEADER_SIZE = 4;
        private static readonly byte[] RPX_HEADER_CHECK = { 0x7F, 0x45, 0x4C, 0x46 };

        public RomPlatform identifyRom(string rpxPath)
        {
            if (isRpxValid(rpxPath))
            {
                if (NesVcExtractor.isValid(rpxPath))
                {
                    return RomPlatform.NES;
                }

                if (SnesVcExtractor.isValid(rpxPath))
                {
                    return RomPlatform.SNES;
                }
            }

            return RomPlatform.Unknown;
        }

        private bool isRpxValid(string rpxPath)
        {
            using (FileStream fs = new FileStream(rpxPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    // advance the binary reader past the offset of the VC file
                    byte[] header = br.ReadBytes(RPX_HEADER_SIZE);

                    // Validate the header
                    if (header[0] != RPX_HEADER_CHECK[0] || header[1] != RPX_HEADER_CHECK[1] || header[2] != RPX_HEADER_CHECK[2] || header[3] != RPX_HEADER_CHECK[3])
                    {
                        Console.WriteLine("Failed to find valid RPX header at offset 0, not a valid RPX file");
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Found valid RPX Header!");
                        return true;
                    }
                }
            }
        }
    }
}
