using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service.Common
{
    public static class FileUtilities
    {
        private static Random _rnd = new Random();

        /// <summary>
        /// CRC hash a file
        /// </summary>
        public static string FileCRC(string filename)
        {
            var sb = new StringBuilder();
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    foreach (byte bt in hashBytes)
                    {
                        sb.Append(bt.ToString("x2"));
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// CRC hash a file
        /// </summary>
        public static string FileCRC(byte[] data)
        {
            var sb = new StringBuilder();
            using (var md5 = MD5.Create())
            {
                using (var stream = new MemoryStream(data))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    foreach (byte bt in hashBytes)
                    {
                        sb.Append(bt.ToString("x2"));
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Deletes a file in a secure way by overwriting it with
        /// random garbage data n times.
        /// </summary>
        /// <param name="filename">Full path of the file to be deleted</param>
        /// <param name="timesToWrite">Specifies the number of times the file should be overwritten</param>
        public static bool WipeFile(string filename, int timesToWrite = 1)
        {
            if (timesToWrite < 1) timesToWrite = 1;
            try
            {
                if (File.Exists(filename))
                {
                    // Set the files attributes to normal in case it's read-only.
                    File.SetAttributes(filename, FileAttributes.Normal);

                    // Calculate the total number of sectors in the file.
                    var sectors = Math.Ceiling(new FileInfo(filename).Length / 512.0);

                    // Create a dummy-buffer the size of a sector.
                    var dummyBuffer = new byte[512];

                    // Create a cryptographic Random Number Generator.
                    // This is what I use to create the garbage data.
                    var rng = new RNGCryptoServiceProvider();

                    // Open a FileStream to the file.
                    var inputStream = new FileStream(filename, FileMode.Open);
                    for (int currentPass = 0; currentPass < timesToWrite; currentPass++)
                    {
                        // Go to the beginning of the stream
                        inputStream.Position = 0;

                        // Loop all sectors
                        for (var sectorsWritten = 0; sectorsWritten < sectors; sectorsWritten++)
                        {
                            // Fill the dummy-buffer with random data
                            rng.GetBytes(dummyBuffer);
                            // Write it to the stream
                            inputStream.Write(dummyBuffer, 0, dummyBuffer.Length);
                        }
                    }
                    // Truncate the file to 0 bytes.
                    // This will hide the original file-length if you try to recover the file.
                    inputStream.SetLength(0);
                    // Close the stream.
                    inputStream.Close();

                    // As an extra precaution I change the dates of the file so the
                    // original dates are hidden if you try to recover the file.
                    var dt = new DateTime(_rnd.Next(2000, 2050), _rnd.Next(1, 13), _rnd.Next(1, 28), _rnd.Next(0, 24), _rnd.Next(0, 60), _rnd.Next(0, 60));
                    File.SetCreationTime(filename, dt);
                    File.SetLastAccessTime(filename, dt);
                    File.SetLastWriteTime(filename, dt);

                    File.SetCreationTimeUtc(filename, dt);
                    File.SetLastAccessTimeUtc(filename, dt);
                    File.SetLastWriteTimeUtc(filename, dt);

                    // Finally, delete the file
                    File.Delete(filename);
                    return true;
                }
            }
            catch (Exception ex)
            {
                //Logger.LogError(ex);
            }
            return false;
        }

        /// <summary>
        /// Compares 2 files to verify that they are the identical
        /// </summary>
        public static bool FilesAreEqual(string firstName, string secondName)
        {
            var first = new FileInfo(firstName);
            var second = new FileInfo(secondName);

            const int BYTES_TO_READ = 1024 * 1024;
            if (first.Length != second.Length)
                return false;

            if (first.FullName == second.FullName)
                return true;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Create a new encryption key with the specified key size
        /// </summary>
        public static byte[] GetNewKey(int keyByteSize)
        {
            if (keyByteSize != 16 && keyByteSize != 32)
                throw new Exception("The key size must be 16 or 32.");

            var aes = new System.Security.Cryptography.AesManaged();
            aes.KeySize = keyByteSize * 8;
            aes.GenerateKey();
            return aes.Key;
        }

        /// <summary>
        /// Given a byte array generate a Int64 hash
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static long Hash(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            UInt64 hashedValue = 3074457345618258791ul;
            for (int i = 0; i < data.Length; i++)
            {
                hashedValue += data[i];
                hashedValue *= 3074457345618258799ul;
            }

            //Convert to long as it is just a hash.
            //We do not care what the actual value is as long as it is unique
            return (long)hashedValue;
        }

        public static bool ZipFile(string fileName, string outFile)
        {
            try
            {
                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    using (var cs = File.Create(outFile))
                    {
                        using (var compressionStream = new System.IO.Compression.GZipStream(cs, System.IO.Compression.CompressionMode.Compress))
                        {
                            fs.CopyTo(compressionStream);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool UnzipFile(string fileName, string outFile)
        {
            try
            {
                using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
                {
                    using (var decompressedFileStream = File.Create(outFile))
                    {
                        using (var decompressionStream = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress))
                        {
                            decompressionStream.CopyTo(decompressedFileStream);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static byte[] ZipArray(byte[] data)
        {
            try
            {
                var outStream = new System.IO.MemoryStream();
                using (var fs = new MemoryStream(data))
                {
                    using (var compressionStream = new System.IO.Compression.GZipStream(outStream, System.IO.Compression.CompressionMode.Compress))
                    {
                        fs.CopyTo(compressionStream);
                    }
                }
                return outStream.ToArray();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static byte[] UnzipArray(byte[] data)
        {
            try
            {
                using (var bigStream = new System.IO.Compression.GZipStream(new MemoryStream(data), System.IO.Compression.CompressionMode.Decompress))
                {
                    using (var bigStreamOut = new System.IO.MemoryStream())
                    {
                        bigStream.CopyTo(bigStreamOut);
                        return bigStreamOut.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
