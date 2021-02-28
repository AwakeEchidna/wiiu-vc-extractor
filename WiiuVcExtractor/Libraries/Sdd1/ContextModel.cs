using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.Libraries.Sdd1
{
    class ContextModel
    {
        private byte bitplanesInfo;
        private byte contextBitsInfo;
        private List<byte>[] bitplaneBuffer;
        private byte[] bpBitInd;
        private byte bitNumber;
        private int[] byteIndex;
        private byte currBitplane;
        private UInt16[] prevBitplaneBits;
        public ProbabilityEstimationModule pem;

        public ContextModel(ref List<byte>[] bpBuffer)
        {
            prevBitplaneBits = new UInt16[8];
            bpBitInd = new byte[8];
            byteIndex = new int[8];
            bitplaneBuffer = bpBuffer;
        }

        public void PrepareComp(byte header)
        {
            bitplanesInfo = (byte)(header & 0x0c);
            contextBitsInfo = (byte)(header & 0x03);
            for (int i = 0; i < 8; i++)
            {
                byteIndex[i] = 0;
                bpBitInd[i] = 0;
                prevBitplaneBits[i] = 0;
            }
            bitNumber = 0;
            switch (bitplanesInfo)
            {
                case 0x00:
                    currBitplane = 1;
                    break;
                case 0x04:
                    currBitplane = 7;
                    break;
                case 0x08:
                    currBitplane = 3;
                    break;
            }
        }

        // returns array with [bit, context]
        public byte[] GetBit()
        {
            byte bit;
            byte currContext;

            switch (bitplanesInfo)
            {
                case 0x00:
                    currBitplane ^= 0x01;
                    break;
                case 0x04:
                    currBitplane ^= 0x01;
                    if ((bitNumber & 0x7f) == 0)
                    {
                        currBitplane = (byte)((currBitplane + 2) & 0x07);
                    }
                    break;
                case 0x08:
                    currBitplane ^= 0x01;
                    if ((bitNumber & 0x7f) == 0)
                    {
                        currBitplane ^= 0x02; 
                    }
                    break;
                case 0x0c:
                    currBitplane = (byte)(bitNumber & 0x07);
                    break;
            }

            // Use this where context_bits is used
            // prevBitplaneBits[currBitplane]

            currContext = (byte)((currBitplane & 0x01) << 4);

            switch (contextBitsInfo)
            {
                case 0x00:
                    currContext |= (byte)(((prevBitplaneBits[currBitplane] & 0x01c0) >> 5) | (prevBitplaneBits[currBitplane] & 0x0001));
                    break;
                case 0x01:
                    currContext |= (byte)(((prevBitplaneBits[currBitplane] & 0x0180) >> 5) | (prevBitplaneBits[currBitplane] & 0x0001));
                    break;
                case 0x02:
                    currContext |= (byte)(((prevBitplaneBits[currBitplane] & 0x00c0) >> 5) | (prevBitplaneBits[currBitplane] & 0x0001));
                    break;
                case 0x03:
                    currContext |= (byte)(((prevBitplaneBits[currBitplane] & 0x0180) >> 5) | (prevBitplaneBits[currBitplane] & 0x0003));
                    break;
            }

            if (byteIndex[currBitplane] == bitplaneBuffer[currBitplane].Count)
            {
                bit = pem.GetMPS(currContext);
            }
            else
            {
                bit = 0;

                if (((bitplaneBuffer[currBitplane][byteIndex[currBitplane]]) & (0x80 >> bpBitInd[currBitplane])) != 0)
                {
                    bit = 1;
                }

                if (((++bpBitInd[currBitplane]) & 0x08) != 0)
                {
                    bpBitInd[currBitplane] = 0;
                    byteIndex[currBitplane]++;
                }
            }

            prevBitplaneBits[currBitplane] <<= 1;
            prevBitplaneBits[currBitplane] |= bit;

            bitNumber++;

            byte[] returnArray = new byte[2];
            returnArray[0] = bit;
            returnArray[1] = currContext;

            return returnArray;
        }
    }
}
