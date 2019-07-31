using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using WiiuVcExtractor.Libraries;

namespace WiiuVcExtractor.FileTypes
{
    public class SrlFile
    {
        // Location of input file
        string path;

        bool verbose;

        // Return location of input file
        public string Path { get { return path; } }

        // Constructor - set path of srl file and output file, and verbosity
        public SrlFile(string srlPath, bool verbose = false)
        {
            path = srlPath;

            this.verbose = verbose;
        }

        // Check file extension of file path
        public static bool IsSrl(string srlPath)
        {
            if (System.IO.Path.GetExtension(srlPath).CompareTo(".srl") == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
