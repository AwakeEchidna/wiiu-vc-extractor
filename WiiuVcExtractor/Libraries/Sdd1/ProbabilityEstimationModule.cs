namespace WiiuVcExtractor.Libraries.Sdd1
{
    /// <summary>
    /// Probability Estimation Module for S-DD1 compression.
    /// </summary>
    public class ProbabilityEstimationModule
    {
        private readonly ContextModel cm;
        private readonly GolombCodeEncoder gce;
        private readonly State[] stateEvolutionTable;
        private readonly SDD1ContextInfo[] contextInfo;
        private uint inputLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilityEstimationModule"/> class.
        /// </summary>
        /// <param name="associatedCM">associated context model.</param>
        /// <param name="associatedGCE">associated golomb code encoder.</param>
        public ProbabilityEstimationModule(ref ContextModel associatedCM, ref GolombCodeEncoder associatedGCE)
        {
            this.cm = associatedCM;
            this.cm.PEM = this;
            this.gce = associatedGCE;
            this.contextInfo = new SDD1ContextInfo[32];

            this.stateEvolutionTable = new State[]
            {
                new State(0, 25, 25),
                new State(0, 2, 1),
                new State(0, 3, 1),
                new State(0, 4, 2),
                new State(0, 5, 3),
                new State(1, 6, 4),
                new State(1, 7, 5),
                new State(1, 8, 6),
                new State(1, 9, 7),
                new State(2, 10, 8),
                new State(2, 11, 9),
                new State(2, 12, 10),
                new State(2, 13, 11),
                new State(3, 14, 12),
                new State(3, 15, 13),
                new State(3, 16, 14),
                new State(3, 17, 15),
                new State(4, 18, 16),
                new State(4, 19, 17),
                new State(5, 20, 18),
                new State(5, 21, 19),
                new State(6, 22, 20),
                new State(6, 23, 21),
                new State(7, 24, 22),
                new State(7, 24, 23),
                new State(0, 26, 1),
                new State(1, 27, 2),
                new State(2, 28, 4),
                new State(3, 29, 8),
                new State(4, 30, 12),
                new State(5, 31, 16),
                new State(6, 32, 18),
                new State(7, 24, 22),
            };
        }

        /// <summary>
        /// Prepare for compression.
        /// </summary>
        /// <param name="header">S-DD1 header.</param>
        /// <param name="length">data length.</param>
        public void PrepareComp(byte header, ushort length)
        {
            for (byte i = 0; i < 32; i++)
            {
                this.contextInfo[i].Status = 0;
                this.contextInfo[i].MPS = 0;
            }

            this.inputLength = length;
            if (((header & 0x0c) != 0x0c) && ((length & 0x0001) != 0))
            {
                this.inputLength++;
            }

            this.inputLength <<= 3;
        }

        /// <summary>
        /// Get MPS value for given context ID.
        /// </summary>
        /// <param name="context">context ID.</param>
        /// <returns>MPS value.</returns>
        public byte GetMPS(byte context)
        {
            return this.contextInfo[context].MPS;
        }

        /// <summary>
        /// Launches the probability estimation module.
        /// </summary>
        public void Launch()
        {
            byte bit;
            byte context;
            byte currStatus;
            State currState;

            for (uint i = 0; i < this.inputLength; i++)
            {
                byte[] bitReturnBuffer = this.cm.GetBit();
                bit = bitReturnBuffer[0];
                context = bitReturnBuffer[1];
                currStatus = this.contextInfo[context].Status;
                currState = this.stateEvolutionTable[currStatus];
                bit ^= this.contextInfo[context].MPS;
                byte endOfRun = this.gce.PutBit(currState.CodeNum, bit);
                if (endOfRun != 0)
                {
                    if (bit != 0)
                    {
                        if ((currStatus & 0xfe) == 0)
                        {
                            this.contextInfo[context].MPS ^= 0x01;
                        }

                        this.contextInfo[context].Status = currState.NextIfLPS;
                    }
                    else
                    {
                        this.contextInfo[context].Status = currState.NextIfMPS;
                    }
                }
            }

            this.gce.FinishComp();
        }

        /// <summary>
        /// Struct representing the current PEM state.
        /// </summary>
        private struct State
        {
            public byte CodeNum;
            public byte NextIfMPS;
            public byte NextIfLPS;

            public State(byte code, byte mps, byte lps)
            {
                this.CodeNum = code;
                this.NextIfMPS = mps;
                this.NextIfLPS = lps;
            }
        }

        /// <summary>
        /// Struct representing context information for the S-DD1.
        /// </summary>
        private struct SDD1ContextInfo
        {
            public byte Status;
            public byte MPS;

            public SDD1ContextInfo(byte stat, byte mps)
            {
                this.Status = stat;
                this.MPS = mps;
            }
        }
    }
}
