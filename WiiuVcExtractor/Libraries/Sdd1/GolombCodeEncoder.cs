using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.Libraries.Sdd1
{
    class GolombCodeEncoder
    {
        private List<byte> codewSequence;
        private List<byte>[] codewBuffer;
        byte[] bitInd;
        byte[] MPScount;

        public GolombCodeEncoder(ref List<byte> associatedCWSeq, ref List<byte>[] cwBuf)
        {
            codewSequence = associatedCWSeq;
            codewBuffer = cwBuf;
            bitInd = new byte[8];
            MPScount = new byte[8];
        }

        public void PrepareComp()
        {
            for (byte i = 0; i < 8; i++)
            {
                MPScount[i] = 0;
                bitInd[i] = 0;
            }
        }

        public byte PutBit(byte codeNum, byte bit, byte endOfRun)
        {
            byte rMPScount = MPScount[codeNum];

            if (MPScount[codeNum] == 0)
            {
                codewSequence.Add(codeNum);
            }

            if (bit != 0)
            {
                endOfRun = 1;
                OutputBit(codeNum, 1);
                for (byte i = 0, aux = 0x01; i < codeNum; i++, aux<<=1)
                {
                    byte auxOutputBit = 0;
                    if ((MPScount[codeNum] & aux) == 0)
                    {
                        auxOutputBit = 1;
                    }

                    OutputBit(codeNum, auxOutputBit);
                }
                MPScount[codeNum] = 0;
            } 
            else
            { 
                if (++(MPScount[codeNum]) == (1<<codeNum))
                {
                    endOfRun = 1;
                    OutputBit(codeNum, 0);
                    MPScount[codeNum] = 0;
                }
                else
                {
                    endOfRun = 0;
                }
            }

            return endOfRun;
        }

        public void FinishComp()
        {
            for (byte i = 0; i < 8; i++)
            {
                if (MPScount[i] != 0)
                {
                    OutputBit(i, 0);
                }
            }
        }

        public void OutputBit(byte codeNum, byte bit)
        {
            byte rBitInd = bitInd[codeNum];
            List<byte> rCodewBuffer = codewBuffer[codeNum];
            byte oBit = 0;
                
            if (bit != 0)
            {
                oBit = (byte)(0x80 >> bitInd[codeNum]);
            }

            if (bitInd[codeNum] == 0)
            {
                codewBuffer[codeNum].Add(0);
            }

            codewBuffer[codeNum][codewBuffer[codeNum].Count - 1] |= oBit;

            ++bitInd[codeNum];
            bitInd[codeNum] &= 0x07;
        }
    }
}
