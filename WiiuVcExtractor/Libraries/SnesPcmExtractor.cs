namespace WiiuVcExtractor.Libraries
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// SNES PCM audio extractor class.
    /// </summary>
    public class SnesPcmExtractor
    {
        private const int PCMBlockLength = 9;
        private const long DMIN = 2147483648;
        private static readonly byte[] PCMSignature = { 0x50, 0x43, 0x4D, 0x46 }; // PCMF in ASCII

        private readonly byte[] romData;
        private readonly byte[] rawPcmData;
        private readonly byte[] brrBuffer;
        private byte[] pcmData;
        private byte[] brrData;
        private int pcmDataOffset;

        private int p1;
        private int p2;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnesPcmExtractor"/> class.
        /// </summary>
        /// <param name="snesRomData">bytes of SNES rom data.</param>
        /// <param name="rawPcmData">bytes of raw PCM audio data.</param>
        public SnesPcmExtractor(byte[] snesRomData, byte[] rawPcmData)
        {
            this.p1 = 0;
            this.p2 = 0;
            this.brrBuffer = new byte[PCMBlockLength];
            this.pcmDataOffset = -1;
            this.romData = snesRomData;
            this.rawPcmData = rawPcmData;
        }

        /// <summary>
        /// Extracts PCM audio data and injects it as BRR into the rom data.
        /// </summary>
        /// <returns>rom data with all PCM audio converted to BRR.</returns>
        public byte[] ExtractPcmData()
        {
            Console.WriteLine("Extracting PCM Data...");

            byte[] processedRom = new byte[this.romData.Length];
            Array.Copy(this.romData, processedRom, this.romData.Length);

            // Find the first PCMSignature in the rom
            for (int i = 0; i < this.romData.Length; i++)
            {
                if (this.romData[i] == PCMSignature[0])
                {
                    if (this.romData.Length >= i + PCMSignature.Length)
                    {
                        if (this.romData[i + 1] == PCMSignature[1] && this.romData[i + 2] == PCMSignature[2] && this.romData[i + 3] == PCMSignature[3])
                        {
                            Console.WriteLine("Found the first PCM offset at " + i);
                            this.pcmDataOffset = i;
                            break;
                        }
                    }
                }
            }

            if (this.pcmDataOffset == -1)
            {
                Console.WriteLine("No PCM data found, continuing...");
                return processedRom;
            }

            Console.WriteLine("Reading PCM data into memory...");

            int pcmDataLength = this.romData.Length - this.pcmDataOffset;

            this.pcmData = new byte[pcmDataLength];
            this.brrData = new byte[pcmDataLength];

            // Copy all the data from pcmDataOffset into pcmData
            Array.Copy(this.romData, this.pcmDataOffset, this.pcmData, 0, pcmDataLength);
            Array.Copy(this.pcmData, this.brrData, pcmDataLength);

            using (MemoryStream ms = new MemoryStream(this.pcmData))
            {
                using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
                long lastPcmBlockOffset = 0;

                // Continue reading PCM data until we run out
                while (true)
                {
                    long index = br.BaseStream.Position;
                    long romOffset = index + this.pcmDataOffset;

                    if (index + PCMSignature.Length >= br.BaseStream.Length)
                    {
                        break;
                    }

                    byte[] signatureBuffer = br.ReadBytes(PCMSignature.Length);

                    // If we didn't find a signature at this location, seek to the next character and restart the loop
                    if (!signatureBuffer.SequenceEqual(PCMSignature))
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
                    this.EncodeBrrBlock(pcmBlockOffset);

                    if ((this.pcmData[index + 7] & 1) == 1)
                    {
                        this.brrBuffer[0] |= 1;
                    }

                    if ((this.pcmData[index + 7] & 2) == 2)
                    {
                        this.brrBuffer[0] |= 2;
                    }

                    using (MemoryStream msb = new MemoryStream(processedRom))
                    {
                        using BinaryWriter bw = new BinaryWriter(msb, new ASCIIEncoding());
                        bw.BaseStream.Seek(romOffset, SeekOrigin.Begin);

                        bw.Write(this.brrBuffer);
                    }

                    br.ReadBytes(1);

                    lastPcmBlockOffset = pcmBlockOffset;
                }
            }

            return processedRom;
        }

        private void EncodeBrrBlock(long pcmoffset)
        {
            using MemoryStream ms = new MemoryStream(this.rawPcmData);
            using BinaryReader br = new BinaryReader(ms, new ASCIIEncoding());
            short[] samples = new short[16];

            if ((pcmoffset * 2) + 32 > br.BaseStream.Length)
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
            Array.Clear(this.brrBuffer, 0, this.brrBuffer.Length);

            this.AdpcmBlockMash(samples);
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
                    double d = this.AdpcmMash(s, k, pcmSamples, false);
                    if (d < dmin)
                    {
                        kmin = k;
                        dmin = d;
                        smin = s;
                    }

                    if (dmin == 0.0)
                    {
                        break;
                    }
                }

                if (dmin == 0.0)
                {
                    break;
                }
            }

            this.brrBuffer[0] = (byte)((smin << 4) | (kmin << 2));
            this.AdpcmMash(smin, kmin, pcmSamples, true);
        }

        private double AdpcmMash(int shiftAmount, int filter, short[] pcmSamples, bool write)
        {
            double d2 = 0.0;
            int vlin = 0;
            int l1 = this.p1;
            int l2 = this.p2;
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
                    d -= 32768 * (d >> 24);
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
                dp = c << (shiftAmount - 1);

                if (shiftAmount > 12)
                {
                    dp = (dp >> 14) & ~0x7FF;
                }

                c &= 0x0f;
                l2 = l1;
                l1 = this.Sshort(this.Clamp16(vlin + dp) * 2);

                d = pcmSamples[i] - l1;
                d2 += (double)d * (double)d;

                if (write)
                {
                    this.brrBuffer[(i >> 1) + 1] |= (byte)(c << (4 - ((i & 0x01) << 2)));
                }
            }

            if (write)
            {
                this.p1 = l1;
                this.p2 = l2;
            }

            return d2;
        }

        private int Sshort(int n)
        {
            if (n > 0x7FFF)
            {
                return n - 0x10000;
            }
            else if (n < -0x8000)
            {
                return n & 0x7FFF;
            }

            return n;
        }

        private int Clamp16(int n)
        {
            if (n > 0x7FFF)
            {
                return 0x7FFF - (n >> 24);
            }

            return n;
        }
    }
}
