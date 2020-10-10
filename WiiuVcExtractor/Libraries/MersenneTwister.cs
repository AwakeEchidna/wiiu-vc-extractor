/*
   A C-program for MT19937, with initialization improved 2002/1/26.
   Coded by Takuji Nishimura and Makoto Matsumoto.

   Before using, initialize the state by using init_genrand(seed)
   or init_by_array(init_key, key_length).

   Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
   All rights reserved.
   Copyright (C) 2005, Mutsuo Saito,
   All rights reserved.

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:

     1. Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.

     2. Redistributions in binary form must reproduce the above copyright
        notice, this list of conditions and the following disclaimer in the
        documentation and/or other materials provided with the distribution.

     3. The names of its contributors may not be used to endorse or promote
        products derived from this software without specific prior written
        permission.
*/

/*
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
   A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


   Any feedback is very welcome.
   http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html
   email: m-mat @ math.sci.hiroshima-u.ac.jp (remove space)
*/

/*
   A C#-program for MT19937, with initialization improved 2006/01/06.
   Coded by Mitil.

   Copyright (C) 2006, Mitil, All rights reserved.

   Any feedback is very welcome.
   URL: http://meisui.psk.jp/
   email: m-i-t-i-l [at@at] p-s-k . j-p
           (remove dash[-], and replace [at@at] --> @)
*/
namespace Meisui.Random
{
    /// <summary>
    /// Mersenne Twister implementation.
    /// </summary>
    public class MersenneTwister
    {
        /* Period parameters */
        private const short N = 624;
        private const short M = 397;
        private const uint MatrixA = (uint)0x9908b0df;   /* constant vector a */
        private const uint UpperMask = (uint)0x80000000; /* most significant w-r bits */
        private const uint LowerMask = 0x7fffffffU; /* least significant r bits */
        private uint[] mt; /* the array for the state vector  */
        private ushort mti; /* mti==N+1 means mt[N] is not initialized */
        private uint[] mag01;

        /// <summary>
        /// Initializes a new instance of the <see cref="MersenneTwister"/> class with a set seed.
        /// </summary>
        /// <param name="s">random seed.</param>
        public MersenneTwister(uint s)
        {
            this.MT();
            this.Init_genrand(s);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MersenneTwister"/> class with a random seed.
        /// coded by Mitil. 2006/01/04.
        /// </summary>
        public MersenneTwister()
        {
            this.MT();

            // auto generate seed for .NET
            uint[] seed_key = new uint[6];
            byte[] rnseed = new byte[8];

            seed_key[0] = (uint)System.DateTime.Now.Millisecond;
            seed_key[1] = (uint)System.DateTime.Now.Second;
            seed_key[2] = (uint)System.DateTime.Now.DayOfYear;
            seed_key[3] = (uint)System.DateTime.Now.Year;
            System.Security.Cryptography.RandomNumberGenerator rn
                = new System.Security.Cryptography.RNGCryptoServiceProvider();
            rn.GetNonZeroBytes(rnseed);

            seed_key[4] = ((uint)rnseed[0] << 24) | ((uint)rnseed[1] << 16)
                | ((uint)rnseed[2] << 8) | ((uint)rnseed[3]);
            seed_key[5] = ((uint)rnseed[4] << 24) | ((uint)rnseed[5] << 16)
                | ((uint)rnseed[6] << 8) | ((uint)rnseed[7]);

            this.Init_by_array(seed_key);

            rn = null;
            seed_key = null;
            rnseed = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MersenneTwister"/> class with a random seed.
        /// </summary>
        /// <param name="init_key">initialization key.</param>
        public MersenneTwister(uint[] init_key)
        {
            this.MT();

            this.Init_by_array(init_key);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MersenneTwister"/> class.
        /// </summary>
        ~MersenneTwister()
        {
            this.mt = null;
            this.mag01 = null;
        }

        /// <summary>
        /// generates a random number on [0,0xffffffff]-Interval.
        /// </summary>
        /// <returns>Generated int32.</returns>
        public uint Genrand_Int32()
        {
            uint y;

            if (this.mti >= N)
            { /* generate N words at one time */
                short kk;

                // if init_genrand() has not been called
                if (this.mti == N + 1)
                {
                    this.Init_genrand(5489); /* a default initial seed is used */
                }

                for (kk = 0; kk < N - M; kk++)
                {
                    y = ((this.mt[kk] & UpperMask) | (this.mt[kk + 1] & LowerMask)) >> 1;
                    this.mt[kk] = this.mt[kk + M] ^ this.mag01[this.mt[kk + 1] & 1] ^ y;
                }

                for (; kk < N - 1; kk++)
                {
                    y = ((this.mt[kk] & UpperMask) | (this.mt[kk + 1] & LowerMask)) >> 1;
                    this.mt[kk] = this.mt[kk + (M - N)] ^ this.mag01[this.mt[kk + 1] & 1] ^ y;
                }

                y = ((this.mt[N - 1] & UpperMask) | (this.mt[0] & LowerMask)) >> 1;
                this.mt[N - 1] = this.mt[M - 1] ^ this.mag01[this.mt[0] & 1] ^ y;

                this.mti = 0;
            }

            y = this.mt[this.mti++];

            /* Tempering */
            y ^= y >> 11;
            y ^= (y << 7) & 0x9d2c5680;
            y ^= (y << 15) & 0xefc60000;
            y ^= y >> 18;

            return y;
        }

        /// <summary>
        /// generates a random number on [0,0x7fffffff]-Interval.
        /// </summary>
        /// <returns>Generated int31 (bitshifted int32).</returns>
        public uint Genrand_Int31()
        {
            return this.Genrand_Int32() >> 1;
        }

        /* These real versions are due to Isaku Wada, 2002/01/09 added */

        /// <summary>
        /// generates a random number on [0,1]-real-Interval.
        /// </summary>
        /// <returns>Generated real1.</returns>
        public double Genrand_real1()
        {
            return this.Genrand_Int32() * ((double)1.0 / 4294967295.0);
            /* divided by 2^32-1 */
        }

        /// <summary>
        /// generates a random number on [0,1)-real-Interval.
        /// </summary>
        /// <returns>Generated real2.</returns>
        public double Genrand_real2()
        {
            return this.Genrand_Int32() * ((double)1.0 / 4294967296.0);
            /* divided by 2^32 */
        }

        /// <summary>
        /// generates a random number on (0,1)-real-Interval.
        /// </summary>
        /// <returns>Generated real3.</returns>
        public double Genrand_real3()
        {
            return (((double)this.Genrand_Int32()) + 0.5) * ((double)1.0 / 4294967296.0);
            /* divided by 2^32 */
        }

        /// <summary>
        /// generates a random number on [0,1) with 53-bit resolution.
        /// </summary>
        /// <returns>Generated res53.</returns>
        public double Genrand_res53()
        {
            uint a = this.Genrand_Int32() >> 5, b = this.Genrand_Int32() >> 6;
            return (((double)a * 67108864.0) + b) * ((double)1.0 / 9007199254740992.0);
        }

        private void MT()
        {
            this.mt = new uint[N];

            this.mag01 = new uint[] { 0, MatrixA };
            /* mag01[x] = x * MATRIX_A  for x=0,1 */

            this.mti = N + 1;
        }

        /* initializes mt[N] with a seed */
        private void Init_genrand(uint s)
        {
            this.mt[0] = s;

            for (this.mti = 1; this.mti < N; this.mti++)
            {
                this.mt[this.mti] = (1812433253U * (this.mt[this.mti - 1] ^ (this.mt[this.mti - 1] >> 30))) + this.mti;
                /* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. */
                /* In the previous versions, MSBs of the seed affect   */
                /* only MSBs of the array mt[].                        */
                /* 2002/01/09 modified by Makoto Matsumoto             */
            }
        }

        /* initialize by an array with array-length */
        /* init_key is the array for initializing keys */
        /* key_length is its length */
        /* slight change for C++, 2004/2/26 */
        private void Init_by_array(uint[] init_key)
        {
            uint i, j;
            int k;
            int key_length = init_key.Length;

            this.Init_genrand(19650218);
            i = 1;
            j = 0;
            k = key_length < N ? N : key_length;

            for (; k > 0; k--)
            {
                this.mt[i] = (this.mt[i] ^ ((this.mt[i - 1] ^ (this.mt[i - 1] >> 30)) * 1664525U))
                    + init_key[j] + (uint)j; /* non linear */
                i++;
                j++;
                if (i >= N)
                {
                    this.mt[0] = this.mt[N - 1];
                    i = 1;
                }

                if (j >= key_length)
                {
                    j = 0;
                }
            }

            for (k = N - 1; k > 0; k--)
            {
                this.mt[i] = (this.mt[i] ^ ((this.mt[i - 1] ^ (this.mt[i - 1] >> 30)) * 1566083941U))
                    - (uint)i; /* non linear */
                i++;
                if (i >= N)
                {
                    this.mt[0] = this.mt[N - 1];
                    i = 1;
                }
            }

            this.mt[0] = 0x80000000; /* MSB is 1; assuring non-zero initial array */
        }
    }
}