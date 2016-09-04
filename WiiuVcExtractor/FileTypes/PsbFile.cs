using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Meisui.Random;

namespace WiiuVcExtractor
{
    class PsbFile
    {
        private const int MAX_READ_SIZE = 1024 * 1024 * 1024;
        private const string FIXED_SEED = "MX8wgGEJ2+M47";
        private const byte XOR_KEY_LENGTH = 0x50;

        private byte[] rawPsbData;
        private byte[] xorKey;
        private byte[] decryptedData;

        private string baseFileName;
        private string fullPath;

        private CompressedPsbHeader compressedHeader;
        private PsbHeader header;

        private string[] names;
        private string[] strings;
        private string[] chunkData;
        private string[] chunkNames;
        private string entries;

        public PsbFile(string path)
        {
            this.fullPath = path;

            // Verify the file exists
            if (!File.Exists(path))
            {
                Console.WriteLine("Failed to find psb.m file at " + path);
                return;
            }

            this.baseFileName = Path.GetFileName(path);
            Console.WriteLine("The baseFileName is " + baseFileName);

            // Read in the PSB's data from the path
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs, new ASCIIEncoding()))
                {
                    Console.WriteLine("Getting Compressed PSB data from " + path + "...");

                    rawPsbData = br.ReadBytes(MAX_READ_SIZE);
                }
            }

            // Get the header from the raw PSB data
            compressedHeader = new CompressedPsbHeader(rawPsbData);

            Console.WriteLine("Compressed PSB Header Info: " + Environment.NewLine + compressedHeader.ToString());

            if (!compressedHeader.Valid())
            {
                Console.WriteLine("The Compressed PSB Header is not valid.");
                return;
            }
            else
            {
                Console.WriteLine("The Compressed PSB Header is valid.");
            }

            xorKey = GenerateXorKey(this.baseFileName);

            DecryptData();

            Console.WriteLine("Data has been decrypted");

            byte[] uncompressedData = UncompressData();

            Console.WriteLine("Data is uncompressed");

            header = new PsbHeader(uncompressedData);
            Console.WriteLine("PSB Header Info: " + Environment.NewLine + header.ToString());

            if (!header.Valid())
            {
                Console.WriteLine("The PSB Header is not valid.");
                return;
            }
            else
            {
                Console.WriteLine("The PSB Header is valid.");
            }

            
        }

        private byte[] GenerateXorKey(string baseFilename)
        {
            byte[] fixedSeed = Encoding.ASCII.GetBytes(FIXED_SEED);

            byte[] fileNameAsBytes = Encoding.ASCII.GetBytes(baseFilename);

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
             //= BitConverter.ToUInt32(hashAsBytes, 0);

            MersenneTwister mt19937 = new MersenneTwister(hashAsUint32);

            byte[] keyBuffer = new byte[XOR_KEY_LENGTH];

            for (int i = 0; i < XOR_KEY_LENGTH / 4; i++)
            {
                UInt32 buffer = mt19937.genrand_Int32();

                Array.Copy(BitConverter.GetBytes(buffer), 0, keyBuffer, i * 4, 4);
            }

            Console.WriteLine("Using key: " + BitConverter.ToString(keyBuffer));
            return keyBuffer;
        }

        private void DecryptData()
        {
            // Copy the PSB data into decryptedData
            decryptedData = new byte[rawPsbData.Length];
            Array.Copy(rawPsbData, decryptedData, rawPsbData.Length);

            // Iterate through the data and XOR the key to decrypt it
            for (int i = 0; i < decryptedData.Length - CompressedPsbHeader.HEADER_SIZE; i++)
            {
                decryptedData[i + CompressedPsbHeader.HEADER_SIZE] ^= xorKey[i % XOR_KEY_LENGTH];
            }
        }

        private byte[] UncompressData()
        {
            byte[] data = new byte[decryptedData.Length - CompressedPsbHeader.HEADER_SIZE];
            Array.Copy(decryptedData, CompressedPsbHeader.HEADER_SIZE, data, 0, decryptedData.Length - CompressedPsbHeader.HEADER_SIZE);

            using (MemoryStream compressedStream = new MemoryStream(data))
            {
                compressedStream.ReadByte();
                compressedStream.ReadByte();

                using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    using (MemoryStream resultStream = new MemoryStream())
                    {
                        deflateStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }
    }
}
