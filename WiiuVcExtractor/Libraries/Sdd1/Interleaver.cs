namespace WiiuVcExtractor.Libraries.Sdd1
{
    using System.Collections.Generic;

    /// <summary>
    /// S-DD1 compression interleaver.
    /// </summary>
    public class Interleaver
    {
        private readonly List<byte> codewSequence;
        private readonly List<byte>[] codewBuffer;
        private readonly byte[] bitInd;
        private readonly int[] byteIndex;
        private uint outputLength;
        private byte[] outputBuffer;
        private uint outputBufferIndex;
        private byte oBitInd;

        /// <summary>
        /// Initializes a new instance of the <see cref="Interleaver"/> class.
        /// </summary>
        /// <param name="associatedCWSeq">associated codeword sequence.</param>
        /// <param name="cwBuf">codeword buffer.</param>
        public Interleaver(ref List<byte> associatedCWSeq, ref List<byte>[] cwBuf)
        {
            this.codewSequence = associatedCWSeq;
            this.codewBuffer = cwBuf;
            this.bitInd = new byte[8];
            this.byteIndex = new int[8];
        }

        /// <summary>
        /// Prepare for compression.
        /// </summary>
        /// <param name="header">header.</param>
        /// <param name="outBuf">output buffer.</param>
        public void PrepareComp(byte header, byte[] outBuf)
        {
            this.outputLength = 0;
            this.outputBufferIndex = 0;
            this.outputBuffer = outBuf;
            this.outputBuffer[this.outputBufferIndex] = (byte)(header << 4);
            this.oBitInd = 4;
            for (byte i = 0; i < 8; i++)
            {
                this.byteIndex[i] = 0;
                this.bitInd[i] = 0;
            }
        }

        /// <summary>
        /// Launch interleaver.
        /// </summary>
        /// <param name="outLen">output length.</param>
        /// <returns>processed output buffer.</returns>
        public byte[] Launch(out uint outLen)
        {
            for (int i = 0; i < this.codewSequence.Count; i++)
            {
                if (this.MoveBit(this.codewSequence[i]) != 0)
                {
                    for (byte j = 0; j < this.codewSequence[i]; j++)
                    {
                        this.MoveBit(this.codewSequence[i]);
                    }
                }
            }

            if (this.oBitInd != 0)
            {
                ++this.outputLength;
            }

            outLen = this.outputLength;

            return this.outputBuffer;
        }

        private byte MoveBit(byte codeNum)
        {
            if (this.oBitInd == 0)
            {
                this.outputBuffer[this.outputBufferIndex] = 0;
            }

            byte bit = (byte)((this.codewBuffer[codeNum][this.byteIndex[codeNum]] & (0x80 >> this.bitInd[codeNum])) << this.bitInd[codeNum]);
            this.outputBuffer[this.outputBufferIndex] |= (byte)(bit >> this.oBitInd);

            if ((++this.bitInd[codeNum] & 0x08) != 0)
            {
                this.bitInd[codeNum] = 0;
                this.byteIndex[codeNum]++;
            }

            if ((++this.oBitInd & 0x08) != 0)
            {
                this.oBitInd = 0;
                this.outputBufferIndex++;
                ++this.outputLength;
            }

            return bit;
        }
    }
}
