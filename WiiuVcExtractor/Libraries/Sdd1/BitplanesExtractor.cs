using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.Libraries.Sdd1
{
    class BitplanesExtractor
    {
        private const byte HEADER_MASK = 0x0C;
        private byte bitplanesInfo;
        private UInt16 inputLength;
        private byte[] inputBuffer;
        private byte inBitInd;
        private byte currBitplane;
        private List<byte>[] bitplaneBuffer;
        private byte[] bpBitInd;
        
        public BitplanesExtractor(ref List<byte>[] bpBuffer)
        {
            bpBitInd = new byte[8];
            bitplaneBuffer = bpBuffer;
        }

        public void PrepareComp(byte[] inBuffer, byte header)
        {
            inputLength = (UInt16)inBuffer.Length;
            inputBuffer = inBuffer;
            bitplanesInfo = (byte)(header & HEADER_MASK);
        }

        public void Launch()
        {
            --inputLength;
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
                case 0x0c:
                    inBitInd = 7;
                    for (byte i = 0; i < 8; i++)
                    {
                        bpBitInd[i] = 0;
                    }
                    break;
            }

            UInt16 counter = 0;

            do
            {
                switch (bitplanesInfo)
                {
                    case 0x00:
                        currBitplane ^= 0x01;
                        bitplaneBuffer[currBitplane].Add(inputBuffer[counter]);
                        break;
                    case 0x04:
                        currBitplane ^= 0x01;
                        if ((counter & 0x000f) == 0)
                        {
                            currBitplane = ((byte)((currBitplane + 2) & 0x07));
                        }
                        bitplaneBuffer[currBitplane].Add(inputBuffer[counter]);
                        break;
                    case 0x08:
                        currBitplane ^= 0x01;
                        if ((counter & 0x000f) == 0)
                        {
                            currBitplane ^= 0x02;
                        }
                        bitplaneBuffer[currBitplane].Add(inputBuffer[counter]);
                        break;
                    case 0x0c:
                        for (byte i = 0; i < 8; i++) PutBit(i, counter);
                        break;
                }
            } while (counter++ < inputLength);
        }

        private void PutBit(byte bitplane, UInt16 counter)
        {
            List<byte> currBPBuf = bitplaneBuffer[bitplane];
            byte currBitInd = bpBitInd[bitplane];

            if (currBitInd == 0)
            {
                currBPBuf.Add(0);
            }

            currBPBuf[currBPBuf.Count() - 1] |= (byte)((((inputBuffer[counter]) & (0x80 >> inBitInd)) << inBitInd) >> currBitInd);

            currBitInd++;
            currBitInd &= 0x07;

            bpBitInd[bitplane] = currBitInd;
            inBitInd--;
            inBitInd &= 0x07;
        }
    }
}
