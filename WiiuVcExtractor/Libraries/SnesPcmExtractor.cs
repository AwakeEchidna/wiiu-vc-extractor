using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WiiuVcExtractor.Libraries
{
    public class SnesPcmExtractor
    {
        private static readonly byte[] PCM_SIGNATURE = { 0x50, 0x43, 0x4D, 0x46 }; // PCMF ASCII
        private const int PCM_BLOCK_LENGTH = 9;
        private byte[] romData;
        private byte[] rawPcmData;
        private byte[] pcmData;
        private byte[] brrBuffer;
        private byte[] brrData;
        private int pcmDataOffset;
        private const long DMIN = 2147483648;
        private int p1, p2;

        public SnesPcmExtractor(byte[] snesRomData, byte[] rawPcmData)
        {
            p1 = 0;
            p2 = 0;
            brrBuffer = new byte[9];
            this.pcmDataOffset = -1;
            this.romData = snesRomData;
            this.rawPcmData = rawPcmData;
        }

        public byte[] ExtractPcmData()
        {
            Console.WriteLine("Extracting PCM Data...");

            byte[] processedRom = new byte[romData.Length];
            Array.Copy(romData, processedRom, romData.Length);

            // Find the first PCM_SIGNATURE in the rom
            for (int i = 0; i < romData.Length; i++)
            {
                if (romData[i] == PCM_SIGNATURE[0])
                {
                    if (romData.Length >= i + PCM_SIGNATURE.Length)
                    {
                        if (romData[i + 1] == PCM_SIGNATURE[1] && romData[i + 2] == PCM_SIGNATURE[2] && romData[i + 3] == PCM_SIGNATURE[3])
                        {
                            Console.WriteLine("Found the first PCM offset at " + i);
                            pcmDataOffset = i;
                            break;
                        }
                    }
                }
            }

            if (pcmDataOffset == -1)
            {
                throw new InvalidOperationException("Could not find any PCM data within the SNES VC rom.");
            }

            Console.WriteLine("Reading PCM data into memory...");

            int pcmDataLength = romData.Length - pcmDataOffset;

            pcmData = new byte[pcmDataLength];
            brrData = new byte[pcmDataLength];

            // Copy all the data from pcmDataOffset into pcmData
            Array.Copy(romData, pcmDataOffset, pcmData, 0, pcmDataLength);
            Array.Copy(pcmData, brrData, pcmDataLength);

            using (MemoryStream ms = new MemoryStream(pcmData))
            {
                using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding()))
                {
                    long lastPcmBlockOffset = 0;

                    // Continue reading PCM data until we run out
                    while (true)
                    {
                        long index = br.BaseStream.Position;
                        long romOffset = index + pcmDataOffset;

                        if (index + PCM_SIGNATURE.Length >= br.BaseStream.Length)
                        {
                            break;
                        }

                        byte[] signatureBuffer = br.ReadBytes(PCM_SIGNATURE.Length);

                        // If we didn't find a signature at this location, seek to the next character and restart the loop
                        if (!signatureBuffer.SequenceEqual(PCM_SIGNATURE))
                        {
                            br.BaseStream.Seek(-3, SeekOrigin.Current);
                            continue;
                        }

                        long pcmBlockOffset = br.ReadUInt32LE();
                        pcmBlockOffset &= 0xffffff;

                        if (((pcmBlockOffset % 16) != 0) || pcmBlockOffset < lastPcmBlockOffset)
                        {
                            pcmBlockOffset = lastPcmBlockOffset + 16;
                        }

                        // Encode the brr block
                        EncodeBrrBlock(pcmBlockOffset);

                        if ((pcmData[index + 7] & 1) == 1)
                        {
                            brrBuffer[0] |= 1;
                        }

                        if ((pcmData[index + 7] & 2) == 2)
                        {
                            brrBuffer[0] |= 2;
                        }

                        using (MemoryStream msb = new MemoryStream(processedRom))
                        {
                            using (BinaryWriter bw = new BinaryWriter(msb, new ASCIIEncoding()))
                            {
                                bw.BaseStream.Seek(romOffset, SeekOrigin.Begin);

                                bw.Write(brrBuffer);
                            }
                        }

                        br.ReadBytes(1);

                        lastPcmBlockOffset = pcmBlockOffset;
                    }
                }
            }

            return processedRom;
        }

        private void EncodeBrrBlock(long pcmoffset)
        {
            using (MemoryStream ms = new MemoryStream(rawPcmData))
            {
                using (BinaryReader br = new BinaryReader(ms, new ASCIIEncoding()))
                {
                    short[] samples = new short[16];

                    if (pcmoffset * 2 + 32 > br.BaseStream.Length)
                    {
                        Console.WriteLine("INVALID PCM OFFSET: " + (pcmoffset * 2));
                        return;
                    }

                    br.BaseStream.Seek(pcmoffset * 2, SeekOrigin.Begin);

                    for (int i = 0; i < 16; i++)
                    {
                        samples[i] = br.ReadInt16BE();
                    }

                    // Set all elements in the brrBuffer to 0
                    Array.Clear(brrBuffer, 0, brrBuffer.Length);

                    AdpcmBlockMash(samples);
                }
            }
        }

        private void AdpcmBlockMash(short[] pcmSamples)
        {
            int smin = 0;
            int kmin = 0;
            double dmin = DMIN;

            for (int s = 13; s >= 0; s--)
            {
                for (int k = 0; k < 4; k++)
                {
                    double d = AdpcmMash(s, k, pcmSamples, false);
                    if (d < dmin)
                    {
                        kmin = k;
                        dmin = d;
                        smin = s;
                    }

                    if (dmin == 0.0)
                        break;
                }

                if (dmin == 0.0)
                    break;
            }

            brrBuffer[0] = (byte)((smin << 4) | (kmin << 2));
            AdpcmMash(smin, kmin, pcmSamples, true);
        }

        private double AdpcmMash(int shiftAmount, int filter, short[] pcmSamples, bool write)
        {
            double d2 = 0.0;
            int vlin = 0;
            int l1 = p1;
            int l2 = p2;
            int step = 1 << shiftAmount;

            for (int i = 0; i < 16; i++)
            {
                switch (filter)
                {
                    case 0:
                        break;

                    case 1:
                        vlin = l1 >> 1;
                        vlin += (-l1) >> 5;
                        break;

                    case 2:
                        vlin = l1;
                        vlin += (-(l1 + (l1 >> 1))) >> 5;
                        vlin -= l2 >> 1;
                        vlin += l2 >> 5;
                        break;

                    default:
                        vlin = l1;
                        vlin += (-(l1 + (l1 << 2) + (l1 << 3))) >> 7;
                        vlin -= l2 >> 1;
                        vlin += (l2 + (l2 >> 1)) >> 4;
                        break;
                }

                int d = (pcmSamples[i] >> 1) - vlin;
                int da = Math.Abs(d);

                if (da > 16384 && da < 32768)
                {
                    d = d - 32768 * (d >> 24);          
                }

                int dp = d + (step << 2) + (step >> 2);
                int c = 0;

                if (dp > 0)
                {
                    if (step > 1)
                    {
                        c = dp / (step >> 1);
                    }
                    else
                    {
                        c = dp << 1;
                    }

                    if (c > 15)
                    {
                        c = 15;
                    }
                }

                c -= 8;
                dp = (c << (shiftAmount - 1));

                if (shiftAmount > 12)
                {
                    dp = (dp >> 14) & ~0x7FF;
                }

                c &= 0x0f;
                l2 = l1;
                l1 = sshort(clamp16(vlin + dp) * 2);

                d = pcmSamples[i] - l1;
                d2 += (double)d * (double)d;

                if (write)
                {
                    brrBuffer[(i >> 1) + 1] |= (byte)(c << (4 - ((i & 0x01) << 2)));
                }
            }

            if (write)
            {
                p1 = l1;
                p2 = l2;
            }

            return d2;
        }

        private int sshort(int n)
        {
            if (n > 0x7FFF)
            {
                return (n - 0x10000);
            }
            else if (n < -0x8000)
            {
                return n & 0x7FFF;
            }

            return n;
        }

        private int clamp16(int n)
        {
            if (n > 0x7FFF)
            {
                return (0x7FFF - (n >> 24));
            }

            return n;
        }
    }
}
