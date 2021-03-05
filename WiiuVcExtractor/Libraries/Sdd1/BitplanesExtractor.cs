namespace WiiuVcExtractor.Libraries.Sdd1
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// S-DD1 compressor bitplanes extractor.
    /// </summary>
    public class BitplanesExtractor
    {
        private const byte HeaderMask = 0x0C;

        private readonly List<byte>[] bitplaneBuffer;
        private readonly byte[] bpBitInd;

        private byte bitplanesInfo;
        private ushort inputLength;
        private byte[] inputBuffer;
        private byte inBitInd;
        private byte currBitplane;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitplanesExtractor"/> class.
        /// </summary>
        /// <param name="bpBuffer">bitplane buffer.</param>
        public BitplanesExtractor(ref List<byte>[] bpBuffer)
        {
            this.bpBitInd = new byte[8];
            this.bitplaneBuffer = bpBuffer;
        }

        /// <summary>
        /// Prepare for compression.
        /// </summary>
        /// <param name="inBuffer">input data to compress.</param>
        /// <param name="header">S-DD1 header.</param>
        public void PrepareComp(byte[] inBuffer, byte header)
        {
            this.inputLength = (ushort)inBuffer.Length;
            this.inputBuffer = inBuffer;
            this.bitplanesInfo = (byte)(header & HeaderMask);
        }

        /// <summary>
        /// Launch bitplanes extractor.
        /// </summary>
        public void Launch()
        {
            --this.inputLength;
            switch (this.bitplanesInfo)
            {
                case 0x00:
                    this.currBitplane = 1;
                    break;
                case 0x04:
                    this.currBitplane = 7;
                    break;
                case 0x08:
                    this.currBitplane = 3;
                    break;
                case 0x0c:
                    this.inBitInd = 7;
                    for (byte i = 0; i < 8; i++)
                    {
                        this.bpBitInd[i] = 0;
                    }

                    break;
            }

            ushort counter = 0;

            do
            {
                switch (this.bitplanesInfo)
                {
                    case 0x00:
                        this.currBitplane ^= 0x01;
                        this.bitplaneBuffer[this.currBitplane].Add(this.inputBuffer[counter]);
                        break;
                    case 0x04:
                        this.currBitplane ^= 0x01;
                        if ((counter & 0x000f) == 0)
                        {
                            this.currBitplane = (byte)((this.currBitplane + 2) & 0x07);
                        }

                        this.bitplaneBuffer[this.currBitplane].Add(this.inputBuffer[counter]);
                        break;
                    case 0x08:
                        this.currBitplane ^= 0x01;
                        if ((counter & 0x000f) == 0)
                        {
                            this.currBitplane ^= 0x02;
                        }

                        this.bitplaneBuffer[this.currBitplane].Add(this.inputBuffer[counter]);
                        break;
                    case 0x0c:
                        for (byte i = 0; i < 8; i++)
                        {
                            this.PutBit(i, counter);
                        }

                        break;
                }
            }
            while (counter++ < this.inputLength);
        }

        private void PutBit(byte bitplane, ushort counter)
        {
            List<byte> currBPBuf = this.bitplaneBuffer[bitplane];
            byte currBitInd = this.bpBitInd[bitplane];

            if (currBitInd == 0)
            {
                currBPBuf.Add(0);
            }

            currBPBuf[currBPBuf.Count() - 1] |= (byte)(((this.inputBuffer[counter] & (0x80 >> this.inBitInd)) << this.inBitInd) >> currBitInd);

            currBitInd++;
            currBitInd &= 0x07;

            this.bpBitInd[bitplane] = currBitInd;
            this.inBitInd--;
            this.inBitInd &= 0x07;
        }
    }
}
