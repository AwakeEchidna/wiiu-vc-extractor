using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WiiuVcExtractor.FileTypes
{
    // Individual file stored within a .pkg file
    public class PkgContentFile
    {
        string path;
        byte[] content;

        public string Path { get { return path; } }
        public byte[] Content { get { return content; } }

        public PkgContentFile(string path, byte[] contentBytes)
        {
            this.path = path;
            this.content = contentBytes;
        }

        // Writes the content file to a given path or to a relative path if not provided
        public void Write(string writePath = "")
        {
            if (String.IsNullOrEmpty(writePath))
            {
                writePath = path;
            }

            using (BinaryWriter bw = new BinaryWriter(File.Open(writePath, FileMode.Create)))
            {
                Console.WriteLine("Writing content file {0} to {1}", path, writePath);
                bw.Write(content);
            }
        }
    }
}
