namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.IO;

    /// <summary>
    /// Individual file stored within a .pkg file.
    /// </summary>
    public class PkgContentFile
    {
        private readonly string path;
        private readonly byte[] content;

        /// <summary>
        /// Initializes a new instance of the <see cref="PkgContentFile"/> class.
        /// </summary>
        /// <param name="path">path of the content file.</param>
        /// <param name="contentBytes">content of the file in a byte array.</param>
        public PkgContentFile(string path, byte[] contentBytes)
        {
            this.path = path;
            this.content = contentBytes;
        }

        /// <summary>
        /// Gets path of the content file.
        /// </summary>
        public string Path
        {
            get { return this.path; }
        }

        /// <summary>
        /// Gets content of the file.
        /// </summary>
        public byte[] Content
        {
            get { return this.content; }
        }

        /// <summary>
        /// Writes the content file to a given path or to a relative path if not provided.
        /// </summary>
        /// <param name="writePath">path to write the content file in the filesystem.</param>
        public void Write(string writePath = "")
        {
            if (string.IsNullOrEmpty(writePath))
            {
                writePath = this.path;
            }

            // Create the parent directory
            Directory.CreateDirectory(Directory.GetParent(writePath).ToString());

            using BinaryWriter bw = new BinaryWriter(File.Open(writePath, FileMode.Create));
            Console.WriteLine("Writing content file {0} to {1}", this.path, writePath);
            bw.Write(this.content);
        }
    }
}
