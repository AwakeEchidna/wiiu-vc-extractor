namespace WiiuVcExtractor.Libraries.Sdd1
{
    using System.Collections.Generic;

    /// <summary>
    /// S-DD1 context model.
    /// </summary>
    public class ContextModel
    {
        private readonly List<byte>[] bitplaneBuffer;
        private readonly byte[] bpBitInd;
        private readonly int[] byteIndex;
        private readonly ushort[] prevBitplaneBits;
        private byte bitplanesInfo;
        private byte contextBitsInfo;
        private byte bitNumber;
        private byte currBitplane;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextModel"/> class.
        /// </summary>
        /// <param name="bpBuffer">bitplane buffer.</param>
        public ContextModel(ref List<byte>[] bpBuffer)
        {
            this.prevBitplaneBits = new ushort[8];
            this.bpBitInd = new byte[8];
            this.byteIndex = new int[8];
            this.bitplaneBuffer = bpBuffer;
        }

        /// <summary>
        /// Gets or sets associated Probability Estimation Module.
        /// </summary>
        public ProbabilityEstimationModule PEM { get; set; }

        /// <summary>
        /// Prepare for compression.
        /// </summary>
        /// <param name="header">S-DD1 header.</param>
        public void PrepareComp(byte header)
        {
            this.bitplanesInfo = (byte)(header & 0x0c);
            this.contextBitsInfo = (byte)(header & 0x03);
            for (int i = 0; i < 8; i++)
            {
                this.byteIndex[i] = 0;
                this.bpBitInd[i] = 0;
                this.prevBitplaneBits[i] = 0;
            }

            this.bitNumber = 0;
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
            }
        }

        /// <summary>
        /// Gets bit from the bitplane.
        /// </summary>
        /// <returns>array with [bit, context].</returns>
        public byte[] GetBit()
        {
            byte bit;
            byte currContext;

            switch (this.bitplanesInfo)
            {
                case 0x00:
                    this.currBitplane ^= 0x01;
                    break;
                case 0x04:
                    this.currBitplane ^= 0x01;
                    if ((this.bitNumber & 0x7f) == 0)
                    {
                        this.currBitplane = (byte)((this.currBitplane + 2) & 0x07);
                    }

                    break;
                case 0x08:
                    this.currBitplane ^= 0x01;
                    if ((this.bitNumber & 0x7f) == 0)
                    {
                        this.currBitplane ^= 0x02;
                    }

                    break;
                case 0x0c:
                    this.currBitplane = (byte)(this.bitNumber & 0x07);
                    break;
            }

            currContext = (byte)((this.currBitplane & 0x01) << 4);

            switch (this.contextBitsInfo)
            {
                case 0x00:
                    currContext |= (byte)(((this.prevBitplaneBits[this.currBitplane] & 0x01c0) >> 5) | (this.prevBitplaneBits[this.currBitplane] & 0x0001));
                    break;
                case 0x01:
                    currContext |= (byte)(((this.prevBitplaneBits[this.currBitplane] & 0x0180) >> 5) | (this.prevBitplaneBits[this.currBitplane] & 0x0001));
                    break;
                case 0x02:
                    currContext |= (byte)(((this.prevBitplaneBits[this.currBitplane] & 0x00c0) >> 5) | (this.prevBitplaneBits[this.currBitplane] & 0x0001));
                    break;
                case 0x03:
                    currContext |= (byte)(((this.prevBitplaneBits[this.currBitplane] & 0x0180) >> 5) | (this.prevBitplaneBits[this.currBitplane] & 0x0003));
                    break;
            }

            if (this.byteIndex[this.currBitplane] == this.bitplaneBuffer[this.currBitplane].Count)
            {
                bit = this.PEM.GetMPS(currContext);
            }
            else
            {
                bit = 0;

                if ((this.bitplaneBuffer[this.currBitplane][this.byteIndex[this.currBitplane]] & (0x80 >> this.bpBitInd[this.currBitplane])) != 0)
                {
                    bit = 1;
                }

                if (((++this.bpBitInd[this.currBitplane]) & 0x08) != 0)
                {
                    this.bpBitInd[this.currBitplane] = 0;
                    this.byteIndex[this.currBitplane]++;
                }
            }

            this.prevBitplaneBits[this.currBitplane] <<= 1;
            this.prevBitplaneBits[this.currBitplane] |= bit;

            this.bitNumber++;

            byte[] returnArray = new byte[2];
            returnArray[0] = bit;
            returnArray[1] = currContext;

            return returnArray;
        }
    }
}
