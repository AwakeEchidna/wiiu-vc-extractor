using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using Meisui.Random;
using Ionic.Zlib;

namespace WiiuVcExtractor.FileTypes
{
    public class MdfPsbFile
    {
        private const string FIXED_SEED = "MX8wgGEJ2+M47";
        private const byte XOR_KEY_LENGTH = 0x50;

        MdfHeader mdfHeader;
        byte[] mdfData;
        byte[] xorKey;

        bool verbose;
        string path;
        string decompressedPath;

        public string DecompressedPath { get { return decompressedPath; } }

        public static bool IsMdfPsb(string psbFilePath)
        {
            MdfHeader header = new MdfHeader(psbFilePath);
            return header.IsValid();
        }

        public MdfPsbFile(string psbFilePath, bool verbose = false)
        {
            this.verbose = verbose;
            Console.WriteLine("Decompressing PSB file...");

            path = psbFilePath;
            decompressedPath = path + ".extract";

            // Remove the temp file if it exists
            if (File.Exists(decompressedPath))
            {
                if (verbose)
                {
                    Console.WriteLine("File exists at {0}, deleting...", decompressedPath);
                }
                File.Delete(decompressedPath);
            }

            mdfHeader = new MdfHeader(path);

            if (verbose)
            {
                Console.WriteLine("MDF Header content:\n{0}", mdfHeader.ToString());
            }

            if (verbose)
            {
                Console.WriteLine("Generating XOR key for MDF decryption...");
            }
            xorKey = GenerateXorKey(path);


            if (verbose)
            {
                Console.WriteLine("Reading bytes from {0}...", path);
            }
            mdfData = File.ReadAllBytes(path);

            DecryptMdfData();

            DecompressMdfData();
        }

        ~MdfPsbFile()
        {
            if (File.Exists(decompressedPath))
            {
                File.Delete(decompressedPath);
            }
        }

        private byte[] GenerateXorKey(string fileName)
        {
            byte[] fixedSeed = Encoding.ASCII.GetBytes(FIXED_SEED);
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

            UInt32[] hashAsUint32 = new UInt32[4];

            for (int i = 0; i < 4; i++)
            {
                hashAsUint32[i] = BitConverter.ToUInt32(hashAsBytes, i * 4);
            }

            MersenneTwister mt19937 = new MersenneTwister(hashAsUint32);

            byte[] keyBuffer = new byte[XOR_KEY_LENGTH];

            for (int i = 0; i < XOR_KEY_LENGTH / 4; i++)
            {
                UInt32 buffer = mt19937.genrand_Int32();

                Array.Copy(BitConverter.GetBytes(buffer), 0, keyBuffer, i * 4, 4);
            }

            return keyBuffer;
        }

        private void DecryptMdfData()
        {
            if (verbose)
            {
                Console.WriteLine("Decrypting MDF data...");
            }
            // Copy the PSB data into decryptedData
            byte[] decryptedData = new byte[mdfData.Length];
            Array.Copy(mdfData, decryptedData, mdfData.Length);

            // Iterate through the data and XOR the key to decrypt it
            for (int i = 0; i < decryptedData.Length - MdfHeader.MDF_HEADER_LENGTH; i++)
            {
                decryptedData[i + MdfHeader.MDF_HEADER_LENGTH] ^= xorKey[i % XOR_KEY_LENGTH];
            }

            mdfData = decryptedData;
            if (verbose)
            {
                Console.WriteLine("Decryption complete");
            }
        }

        private void DecompressMdfData()
        {
            if (verbose)
            {
                Console.WriteLine("Decompressing MDF data...");
            }
            byte[] compressedData = new byte[mdfData.Length - MdfHeader.MDF_HEADER_LENGTH];
            byte[] decompressedData;

            // Copy the current mdf data to compressed data without the MDF header
            Array.Copy(mdfData, MdfHeader.MDF_HEADER_LENGTH, compressedData, 0, mdfData.Length - MdfHeader.MDF_HEADER_LENGTH);

            using (MemoryStream compressedStream = new MemoryStream(compressedData))
            {
                using (ZlibStream deflateStream = new ZlibStream(compressedStream, CompressionMode.Decompress))
                {
                    using (MemoryStream decompressedStream = new MemoryStream())
                    {
                        deflateStream.CopyTo(decompressedStream);
                        decompressedData = decompressedStream.ToArray();
                    }
                }
            }

            // Write all of the decompressed data to the decompressedPath
            File.WriteAllBytes(decompressedPath, decompressedData);

            Console.WriteLine("Decompression to {0} completed", decompressedPath);
        }
    }
}
