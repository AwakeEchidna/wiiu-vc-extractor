namespace WiiuVcExtractor.FileTypes
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Ionic.Zlib;
    using Meisui.Random;

    /// <summary>
    /// MDF PSB file.
    /// </summary>
    public class MdfPsbFile : IDisposable
    {
        private const string FixedSeed = "MX8wgGEJ2+M47";
        private const byte XorKeyLength = 0x50;

        private readonly MdfHeader mdfHeader;
        private readonly byte[] xorKey;
        private readonly bool verbose;
        private readonly string path;
        private readonly string decompressedPath;
        private byte[] mdfData;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MdfPsbFile"/> class.
        /// </summary>
        /// <param name="psbFilePath">path to the PSB file.</param>
        /// <param name="verbose">whether to provide verbose output.</param>
        public MdfPsbFile(string psbFilePath, bool verbose = false)
        {
            this.verbose = verbose;
            Console.WriteLine("Decompressing PSB file...");

            this.path = psbFilePath;
            this.decompressedPath = this.path + ".extract";

            // Remove the temp file if it exists
            if (File.Exists(this.decompressedPath))
            {
                if (verbose)
                {
                    Console.WriteLine("File exists at {0}, deleting...", this.decompressedPath);
                }

                File.Delete(this.decompressedPath);
            }

            this.mdfHeader = new MdfHeader(this.path);

            if (verbose)
            {
                Console.WriteLine("MDF Header content:\n{0}", this.mdfHeader.ToString());
            }

            if (verbose)
            {
                Console.WriteLine("Generating XOR key for MDF decryption...");
            }

            this.xorKey = this.GenerateXorKey(this.path);

            if (verbose)
            {
                Console.WriteLine("Reading bytes from {0}...", this.path);
            }

            this.mdfData = File.ReadAllBytes(this.path);

            this.DecryptMdfData();

            this.DecompressMdfData();
        }

        /// <summary>
        /// Gets path to the decompressed MDF file.
        /// </summary>
        public string DecompressedPath
        {
            get { return this.decompressedPath; }
        }

        /// <summary>
        /// Whether a given path is a valid MDF PSB file.
        /// </summary>
        /// <param name="psbFilePath">path to the PSB file to test.</param>
        /// <returns>true if valid, false otherwise.</returns>
        public static bool IsMdfPsb(string psbFilePath)
        {
            MdfHeader header = new MdfHeader(psbFilePath);
            return header.IsValid();
        }

        /// <summary>
        /// Dispose PsbFile.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose MdfPsbFile.
        /// </summary>
        /// <param name="disposing">whether disposing the file.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                if (File.Exists(this.decompressedPath))
                {
                    Console.WriteLine("Deleting file {0}", this.decompressedPath);
                    File.Delete(this.decompressedPath);
                }

                // set large fields to null (if any)
                this.disposedValue = true;
            }
        }

        private byte[] GenerateXorKey(string fileName)
        {
            byte[] fixedSeed = Encoding.ASCII.GetBytes(FixedSeed);
            byte[] fileNameAsBytes = Encoding.ASCII.GetBytes(Path.GetFileName(fileName));
            int hashSeedLength = fixedSeed.Length + fileNameAsBytes.Length;
            byte[] hashSeed = new byte[hashSeedLength];

            Array.Copy(fixedSeed, hashSeed, fixedSeed.Length);
            Array.Copy(fileNameAsBytes, 0, hashSeed, fixedSeed.Length, fileNameAsBytes.Length);

            byte[] hashAsBytes = { };

            using (MD5 md5Hash = MD5.Create())
            {
                hashAsBytes = md5Hash.ComputeHash(hashSeed);
            }

            uint[] hashAsUint32 = new uint[4];

            for (int i = 0; i < 4; i++)
            {
                hashAsUint32[i] = BitConverter.ToUInt32(hashAsBytes, i * 4);
            }

            MersenneTwister mt19937 = new MersenneTwister(hashAsUint32);

            byte[] keyBuffer = new byte[XorKeyLength];

            for (int i = 0; i < XorKeyLength / 4; i++)
            {
                uint buffer = mt19937.Genrand_Int32();

                Array.Copy(BitConverter.GetBytes(buffer), 0, keyBuffer, i * 4, 4);
            }

            return keyBuffer;
        }

        private void DecryptMdfData()
        {
            if (this.verbose)
            {
                Console.WriteLine("Decrypting MDF data...");
            }

            // Copy the PSB data into decryptedData
            byte[] decryptedData = new byte[this.mdfData.Length];
            Array.Copy(this.mdfData, decryptedData, this.mdfData.Length);

            // Iterate through the data and XOR the key to decrypt it
            for (int i = 0; i < decryptedData.Length - MdfHeader.MDFHeaderLength; i++)
            {
                decryptedData[i + MdfHeader.MDFHeaderLength] ^= this.xorKey[i % XorKeyLength];
            }

            this.mdfData = decryptedData;
            if (this.verbose)
            {
                Console.WriteLine("Decryption complete");
            }
        }

        private void DecompressMdfData()
        {
            if (this.verbose)
            {
                Console.WriteLine("Decompressing MDF data...");
            }

            byte[] compressedData = new byte[this.mdfData.Length - MdfHeader.MDFHeaderLength];
            byte[] decompressedData;

            // Copy the current mdf data to compressed data without the MDF header
            Array.Copy(this.mdfData, MdfHeader.MDFHeaderLength, compressedData, 0, this.mdfData.Length - MdfHeader.MDFHeaderLength);

            using (MemoryStream compressedStream = new MemoryStream(compressedData))
            {
                using ZlibStream deflateStream = new ZlibStream(compressedStream, CompressionMode.Decompress);
                using MemoryStream decompressedStream = new MemoryStream();
                deflateStream.CopyTo(decompressedStream);
                decompressedData = decompressedStream.ToArray();
            }

            // Write all of the decompressed data to the decompressedPath
            File.WriteAllBytes(this.decompressedPath, decompressedData);

            Console.WriteLine("Decompression to {0} completed", this.decompressedPath);
        }
    }
}
