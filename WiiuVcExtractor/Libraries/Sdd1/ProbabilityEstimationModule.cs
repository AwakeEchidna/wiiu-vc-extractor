using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.Libraries.Sdd1
{
    class ProbabilityEstimationModule
    {
        private ContextModel cm;
        private GolombCodeEncoder gce;
        UInt32 inputLength;
        private struct State
        {
            public byte codeNum;
            public byte nextIfMPS;
            public byte nextIfLPS;

            public State(byte code, byte mps, byte lps)
            {
                codeNum = code;
                nextIfMPS = mps;
                nextIfLPS = lps;
            }
        }

        private struct SDD1ContextInfo
        {
            public byte status;
            public byte MPS;
            public SDD1ContextInfo(byte stat, byte mps)
            {
                status = stat;
                MPS = mps;
            }
        }

        State[] stateEvolutionTable;
        SDD1ContextInfo[] contextInfo;

        public ProbabilityEstimationModule(ref ContextModel associatedCM, ref GolombCodeEncoder associatedGCE)
        {
            cm = associatedCM;
            cm.pem = this;
            gce = associatedGCE;
            contextInfo = new SDD1ContextInfo[32];

            stateEvolutionTable = new State[]
            {
                new State(0,25,25),
                new State(0, 2, 1),
                new State(0, 3, 1),
                new State(0, 4, 2),
                new State(0, 5, 3),
                new State(1, 6, 4),
                new State(1, 7, 5),
                new State(1, 8, 6),
                new State(1, 9, 7),
                new State(2,10, 8),
                new State(2,11, 9),
                new State(2,12,10),
                new State(2,13,11),
                new State(3,14,12),
                new State(3,15,13),
                new State(3,16,14),
                new State(3,17,15),
                new State(4,18,16),
                new State(4,19,17),
                new State(5,20,18),
                new State(5,21,19),
                new State(6,22,20),
                new State(6,23,21),
                new State(7,24,22),
                new State(7,24,23),
                new State(0,26, 1),
                new State(1,27, 2),
                new State(2,28, 4),
                new State(3,29, 8),
                new State(4,30,12),
                new State(5,31,16),
                new State(6,32,18),
                new State(7,24,22)
            };
        }

        public void PrepareComp(byte header, UInt16 length)
        {
            for (byte i = 0; i < 32; i++)
            {
                contextInfo[i].status = 0;
                contextInfo[i].MPS = 0;
            }
            inputLength = length;
            if (((header & 0x0c) != 0x0c) && ((length & 0x0001) != 0))
            {
                inputLength++;
            }
            inputLength <<= 3;
        }

        public byte GetMPS(byte context)
        {
            return contextInfo[context].MPS;
        }

        public void Launch()
        {
            byte bit;
            byte context;
            byte currStatus;
            byte endOfRun = 0;
            State currState;
            byte[] bitReturnBuffer = new byte[2];

            for (UInt32 i = 0; i < inputLength; i++)
            {
                bitReturnBuffer = cm.GetBit();
                bit = bitReturnBuffer[0];
                context = bitReturnBuffer[1];
                currStatus = contextInfo[context].status;
                currState = stateEvolutionTable[currStatus];
                bit ^= contextInfo[context].MPS;
                endOfRun = gce.PutBit(currState.codeNum, bit, endOfRun);
                if (endOfRun != 0)
                {
                    if (bit != 0)
                    {
                        if ((currStatus & 0xfe) == 0)
                        {
                            contextInfo[context].MPS ^= 0x01;
                        } 
                        contextInfo[context].status = currState.nextIfLPS;
                    }
                    else
                    {
                        contextInfo[context].status = currState.nextIfMPS;
                    }
                }
            }

            gce.FinishComp();
        }
    }
}
