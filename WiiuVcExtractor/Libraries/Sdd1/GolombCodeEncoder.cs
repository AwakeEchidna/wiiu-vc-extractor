namespace WiiuVcExtractor.Libraries.Sdd1
{
    using System.Collections.Generic;

    /// <summary>
    /// Golomb code encoder for S-DD1 compression.
    /// </summary>
    public class GolombCodeEncoder
    {
        private readonly List<byte> codewSequence;
        private readonly List<byte>[] codewBuffer;
        private readonly byte[] bitInd;
        private readonly byte[] mpsCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="GolombCodeEncoder"/> class.
        /// </summary>
        /// <param name="associatedCWSeq">associated codeword sequence.</param>
        /// <param name="cwBuf">codeword buffer.</param>
        public GolombCodeEncoder(ref List<byte> associatedCWSeq, ref List<byte>[] cwBuf)
        {
            this.codewSequence = associatedCWSeq;
            this.codewBuffer = cwBuf;
            this.bitInd = new byte[8];
            this.mpsCount = new byte[8];
        }

        /// <summary>
        /// Prepare for compression.
        /// </summary>
        public void PrepareComp()
        {
            for (byte i = 0; i < 8; i++)
            {
                this.mpsCount[i] = 0;
                this.bitInd[i] = 0;
            }
        }

        /// <summary>
        /// Put bit into the codeword buffer.
        /// </summary>
        /// <param name="codeNum">code number.</param>
        /// <param name="bit">bit to put.</param>
        /// <returns>byte representing the end of the run.</returns>
        public byte PutBit(byte codeNum, byte bit)
        {
            byte endOfRun;

            if (this.mpsCount[codeNum] == 0)
            {
                this.codewSequence.Add(codeNum);
            }

            if (bit != 0)
            {
                endOfRun = 1;
                this.OutputBit(codeNum, 1);
                for (byte i = 0, aux = 0x01; i < codeNum; i++, aux <<= 1)
                {
                    byte auxOutputBit = 0;
                    if ((this.mpsCount[codeNum] & aux) == 0)
                    {
                        auxOutputBit = 1;
                    }

                    this.OutputBit(codeNum, auxOutputBit);
                }

                this.mpsCount[codeNum] = 0;
            }
            else
            {
                if (++this.mpsCount[codeNum] == (1 << codeNum))
                {
                    endOfRun = 1;
                    this.OutputBit(codeNum, 0);
                    this.mpsCount[codeNum] = 0;
                }
                else
                {
                    endOfRun = 0;
                }
            }

            return endOfRun;
        }

        /// <summary>
        /// Finish compression.
        /// </summary>
        public void FinishComp()
        {
            for (byte i = 0; i < 8; i++)
            {
                if (this.mpsCount[i] != 0)
                {
                    this.OutputBit(i, 0);
                }
            }
        }

        /// <summary>
        /// Output bit.
        /// </summary>
        /// <param name="codeNum">code number.</param>
        /// <param name="bit">bit to output.</param>
        public void OutputBit(byte codeNum, byte bit)
        {
            byte oBit = 0;

            if (bit != 0)
            {
                oBit = (byte)(0x80 >> this.bitInd[codeNum]);
            }

            if (this.bitInd[codeNum] == 0)
            {
                this.codewBuffer[codeNum].Add(0);
            }

            this.codewBuffer[codeNum][^1] |= oBit;

            ++this.bitInd[codeNum];
            this.bitInd[codeNum] &= 0x07;
        }
    }
}
