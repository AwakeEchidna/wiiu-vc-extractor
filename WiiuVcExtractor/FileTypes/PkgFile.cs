using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiuVcExtractor.Libraries;
using System.IO;

namespace WiiuVcExtractor.FileTypes
{
    // Extractor for .pkg files for PC Engine games
    public class PkgFile
    {
        PkgHeader header;
        List<PkgContentFile> contentFiles;
        bool verbose;
        string path;

        public PkgHeader Header { get { return header; } }
        public List<PkgContentFile> ContentFiles { get { return contentFiles; } }
        public string Path { get { return path; } }

        public static bool IsPkg(string pkgFilePath)
        {
            try
            {
                PkgHeader header = new PkgHeader(pkgFilePath);
                return header.IsValid();
            } catch (Exception ex)
            {
                // If an exception is received, assume that the header could not be parsed successfully
                return false;
            }
        }

        public PkgFile(string pkgFilePath, bool verbose = false)
        {
            Console.WriteLine("Extracting PKG file...");
            this.verbose = verbose;

            contentFiles = new List<PkgContentFile>();

            path = pkgFilePath;

            try
            {
                header = new PkgHeader(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not successfully read PKG file: " + ex.Message);
                return;
            }

            if (verbose)
            {
                Console.WriteLine("Successfully read PKG file header as:\n{0}", header.ToString());
            }

            // Detect each file within the PKG file after the header and store it in memory as a PkgContentFile, start after the header
            using (FileStream fs = new FileStream(pkgFilePath, FileMode.Open, FileAccess.Read))
            {
                // Skip header
                fs.Seek(header.Length, SeekOrigin.Begin);
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        // Read in each section, these are arranged as a LE UINT32 describing the size followed by a null-terminated filename and its content
                        int sectionLength = br.ReadInt32LE();
                        string sectionPath = br.ReadNullTerminatedString();
                        byte[] sectionContent = br.ReadBytes(sectionLength);
                        contentFiles.Add(new PkgContentFile(sectionPath, sectionContent));
                    }
                }
            }
        }
    }
}
