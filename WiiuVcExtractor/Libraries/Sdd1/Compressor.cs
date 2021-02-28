using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.Libraries.Sdd1
{
    class Compressor
    {
        private List<byte>[] bitplaneBuffer;
        private List<byte> codewordsSequence;
        private List<byte>[] codewordBuffer;
        private BitplanesExtractor be;
        private ContextModel cm;
        private GolombCodeEncoder gce;
        private ProbabilityEstimationModule pem;
        private Interleaver interleaver;
        public Compressor()
        {
            codewordsSequence = new List<byte>();
            codewordBuffer = new List<byte>[8];
            bitplaneBuffer = new List<byte>[8];
            for (int i = 0; i < 8; i++)
            {
                bitplaneBuffer[i] = new List<byte>();
                codewordBuffer[i] = new List<byte>();
            }

            be = new BitplanesExtractor(ref bitplaneBuffer);
            cm = new ContextModel(ref bitplaneBuffer);
            gce = new GolombCodeEncoder(ref codewordsSequence, ref codewordBuffer);
            pem = new ProbabilityEstimationModule(ref cm, ref gce);
            interleaver = new Interleaver(ref codewordsSequence, ref codewordBuffer);
        }

        public byte[] Compress(byte[] inBuf, out UInt32 outLen, byte[] outBuf)
        {
            UInt32 minLength;
            byte[] buffer;

            outBuf = Compress(0, inBuf, out outLen, outBuf);

            minLength = outLen;
            buffer = new byte[outLen];
            for (UInt32 i = 0; i < outLen; i++)
            {
                buffer[i] = outBuf[i];
            }

            for (byte j=1; j < 16; j++)
            {
                outBuf = Compress(j, inBuf, out outLen, outBuf);
                if (outLen < minLength)
                {
                    minLength = outLen;
                    for (UInt32 i = 0; i < outLen; i++)
                    {
                        buffer[i] = outBuf[i];
                    }
                }
            }

            if (minLength < outLen)
            {
                outLen = minLength;
                for (UInt32 i = 0; i < minLength; i++)
                {
                    outBuf[i] = buffer[i];
                }
            }

            return outBuf;
        }

        public byte[] Compress(byte header, byte[] inBuf, out UInt32 outLen, byte[] outBuf)
        {
            // Step 1
            for (byte i = 0; i < 8; i++)
            {
                bitplaneBuffer[i].Clear();
            }
            be.PrepareComp(inBuf, header);
            be.Launch();

            // Step 2
            codewordsSequence.Clear();
            for (byte i = 0; i < 8; i++)
            {
                codewordBuffer[i].Clear();
            }
            cm.PrepareComp(header);
            pem.PrepareComp(header, (UInt16)inBuf.Length);
            gce.PrepareComp();
            pem.Launch();

            // Step 3
            interleaver.PrepareComp(header, outBuf);
            outBuf = interleaver.Launch(out outLen);

            return outBuf;
        }
    }
}
