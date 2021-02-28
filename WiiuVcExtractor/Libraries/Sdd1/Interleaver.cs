using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.Libraries.Sdd1
{
    class Interleaver
    {
        private List<byte> codewSequence;
        private List<byte>[] codewBuffer;
        byte[] bitInd;
        private int[] byteIndex;
        UInt32 outputLength;
        byte[] outputBuffer;
        UInt32 outputBufferIndex;
        byte oBitInd;

        public Interleaver(ref List<byte> associatedCWSeq, ref List<byte>[] cwBuf)
        {
            codewSequence = associatedCWSeq;
            codewBuffer = cwBuf;
            bitInd = new byte[8];
            byteIndex = new int[8];
        }

        public void PrepareComp(byte header, byte[] outBuf)
        {
            outputLength = 0;
            outputBufferIndex = 0;
            outputBuffer = outBuf;
            outputBuffer[outputBufferIndex] = (byte)(header << 4);
            oBitInd = 4;
            for (byte i = 0; i < 8; i++)
            {
                byteIndex[i] = 0;
                bitInd[i] = 0;
            }
        }

        public byte[] Launch(out UInt32 outLen)
        {
            for (int i = 0; i < codewSequence.Count; i++)
            {
                if (MoveBit(codewSequence[i]) != 0)
                {
                    for (byte j = 0; j < codewSequence[i]; j++)
                    {
                        MoveBit(codewSequence[i]);
                    }
                }
            }

            if (oBitInd != 0)
            {
                ++outputLength;
            }

            outLen = outputLength;

            return outputBuffer;
        }

        private byte MoveBit(byte codeNum)
        {
            if (oBitInd == 0)
            {
                outputBuffer[outputBufferIndex] = 0;
            }

            byte bit = (byte)((codewBuffer[codeNum][byteIndex[codeNum]] & (0x80 >> bitInd[codeNum])) << bitInd[codeNum]);
            outputBuffer[outputBufferIndex] |= (byte)(bit >> oBitInd);

            if ((++bitInd[codeNum] & 0x08) != 0)
            {
                bitInd[codeNum] = 0;
                byteIndex[codeNum]++;
            }

            if ((++oBitInd & 0x08) != 0)
            {
                oBitInd = 0;
                outputBufferIndex++;
                ++outputLength;
            }

            return bit;
        }
    }
}
