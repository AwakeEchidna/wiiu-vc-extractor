using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiuVcExtractor.Libraries.Sdd1
{
    public class Sdd1Pointer
    {
        public long pointerLocation;
        public long dataLocation;
        public long dataLength;

        public Sdd1Pointer(long ptrLocation, long datLocation)
        {
            pointerLocation = ptrLocation;
            dataLocation = datLocation;
            dataLength = 0;
        }
    }
}
