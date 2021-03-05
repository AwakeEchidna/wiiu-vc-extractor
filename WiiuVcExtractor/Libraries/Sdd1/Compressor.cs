namespace WiiuVcExtractor.Libraries.Sdd1
{
    using System.Collections.Generic;

    /// <summary>
    /// S-DD1 compressor.
    /// </summary>
    public class Compressor
    {
        private readonly List<byte>[] bitplaneBuffer;
        private readonly List<byte> codewordsSequence;
        private readonly List<byte>[] codewordBuffer;
        private readonly BitplanesExtractor be;
        private readonly ContextModel cm;
        private readonly GolombCodeEncoder gce;
        private readonly ProbabilityEstimationModule pem;
        private readonly Interleaver interleaver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Compressor"/> class.
        /// </summary>
        public Compressor()
        {
            this.codewordsSequence = new List<byte>();
            this.codewordBuffer = new List<byte>[8];
            this.bitplaneBuffer = new List<byte>[8];
            for (int i = 0; i < 8; i++)
            {
                this.bitplaneBuffer[i] = new List<byte>();
                this.codewordBuffer[i] = new List<byte>();
            }

            this.be = new BitplanesExtractor(ref this.bitplaneBuffer);
            this.cm = new ContextModel(ref this.bitplaneBuffer);
            this.gce = new GolombCodeEncoder(ref this.codewordsSequence, ref this.codewordBuffer);
            this.pem = new ProbabilityEstimationModule(ref this.cm, ref this.gce);
            this.interleaver = new Interleaver(ref this.codewordsSequence, ref this.codewordBuffer);
        }

        /// <summary>
        /// Compresses provided data in S-DD1 format.
        /// </summary>
        /// <param name="inBuf">data to compress.</param>
        /// <param name="outLen">length of the compressed output.</param>
        /// <param name="outBuf">buffer to store compressed output.</param>
        /// <returns>compressed S-DD1 data.</returns>
        public byte[] Compress(byte[] inBuf, out uint outLen, byte[] outBuf)
        {
            uint minLength;
            byte[] buffer;

            outBuf = this.Compress(0, inBuf, out outLen, outBuf);

            minLength = outLen;
            buffer = new byte[outLen];
            for (uint i = 0; i < outLen; i++)
            {
                buffer[i] = outBuf[i];
            }

            for (byte j = 1; j < 16; j++)
            {
                outBuf = this.Compress(j, inBuf, out outLen, outBuf);
                if (outLen < minLength)
                {
                    minLength = outLen;
                    for (uint i = 0; i < outLen; i++)
                    {
                        buffer[i] = outBuf[i];
                    }
                }
            }

            if (minLength < outLen)
            {
                outLen = minLength;
                for (uint i = 0; i < minLength; i++)
                {
                    outBuf[i] = buffer[i];
                }
            }

            return outBuf;
        }

        /// <summary>
        /// Compresses provided data in S-DD1 format.
        /// </summary>
        /// <param name="header">S-DD1 header.</param>
        /// <param name="inBuf">data to compress.</param>
        /// <param name="outLen">length of the compressed output.</param>
        /// <param name="outBuf">buffer to store compressed output.</param>
        /// <returns>compressed S-DD1 data.</returns>
        public byte[] Compress(byte header, byte[] inBuf, out uint outLen, byte[] outBuf)
        {
            // Step 1
            for (byte i = 0; i < 8; i++)
            {
                this.bitplaneBuffer[i].Clear();
            }

            this.be.PrepareComp(inBuf, header);
            this.be.Launch();

            // Step 2
            this.codewordsSequence.Clear();
            for (byte i = 0; i < 8; i++)
            {
                this.codewordBuffer[i].Clear();
            }

            this.cm.PrepareComp(header);
            this.pem.PrepareComp(header, (ushort)inBuf.Length);
            this.gce.PrepareComp();
            this.pem.Launch();

            // Step 3
            this.interleaver.PrepareComp(header, outBuf);
            outBuf = this.interleaver.Launch(out outLen);

            return outBuf;
        }
    }
}
