namespace WiiuVcExtractor.FileTypes
{
    using System;

    /// <summary>
    /// SRL file for DS rom extraction.
    /// </summary>
    public class SrlFile
    {
        // Location of input file
        private readonly string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="SrlFile"/> class.
        /// </summary>
        /// <param name="srlPath">path to SRL file.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public SrlFile(string srlPath, bool verbose = false)
        {
            this.path = srlPath;

            if (verbose)
            {
                Console.WriteLine("Constructing SRC file at {0}", srlPath);
            }
        }

        /// <summary>
        /// Gets location of input file.
        /// </summary>
        public string Path
        {
            get { return this.path; }
        }

        /// <summary>
        /// Whether the given path is an SRL file.
        /// </summary>
        /// <param name="srlPath">path to file.</param>
        /// <returns>true if it is an SRL file, false otherwise.</returns>
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
