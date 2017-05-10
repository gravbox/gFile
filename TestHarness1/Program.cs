using Gravitybox.gFileSystem;
using Gravitybox.gFileSystem.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvelopeEncryption
{
    class Program
    {
        static void Main(string[] args)
        {
            EngineTest1();
            EngineRekeyTest();

            Console.WriteLine("Complete...");
        }

        private static void EngineTest1()
        {
            //Create engine
            using (var fe = new FileEngine(MasterKey16, TenantKey16))
            {
                //This is the plain text file to test
                var plainFile = @"c:\temp\test.txt";

                //Encrypt the plain text file
                var cryptFile = fe.SaveFile(plainFile);

                //Decrypt the cipher text file
                var plainFile2 = fe.GetFile(cryptFile);

                //Compare the 2 plain text files
                var b = FileUtilities.FilesAreEqual(plainFile, plainFile2);
                Debug.Assert(b);

                //Clean up the files we just created
                FileUtilities.WipeFile(cryptFile);
                FileUtilities.WipeFile(plainFile2);
            }
        }

        private static void EngineRekeyTest()
        {
            //Change the tenant key of a file after it has been encrypted

            //Keys
            var tenantKeyNew = Encoding.UTF8.GetBytes("9d7%@r1%a8d?>%tw");

            string plainFile = null;
            string cryptFile = null;

            //Create engine
            using (var fe = new FileEngine(MasterKey16, TenantKey16))
            {
                //This is the plain text file to test
                plainFile = @"c:\temp\test.txt";

                //Encrypt the plain text file
                cryptFile = fe.SaveFile(plainFile);

                //Rekey file - This will change the key used to access the file
                fe.RekeyFile(cryptFile, tenantKeyNew);
            }

            //Declare new engine object with new tenant key
            using (var fe = new FileEngine(MasterKey16, tenantKeyNew))
            {
                //Decrypt the cipher text file
                var plainFile2 = fe.GetFile(cryptFile);

                //Compare the 2 plain text files to verify that both are the same
                var b = FileUtilities.FilesAreEqual(plainFile, plainFile2);
                Debug.Assert(b);

                //Wipe the files
                FileUtilities.WipeFile(cryptFile);
                FileUtilities.WipeFile(plainFile2);
            }

        }

        private static byte[] MasterKey16 => Encoding.UTF8.GetBytes("8s$w@r1%a-m>2pq9");

        private static byte[] MasterKey32 => MasterKey16.Concat(MasterKey16).ToArray();

        private static byte[] TenantKey16 => Encoding.UTF8.GetBytes("a726@r1%a-w)-p*7");

    }
}
