using Gravitybox.gFileSystem;
using Gravitybox.gFileSystem.Service.Common;
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
        //The is a file grouping. All files in a group can be considered a virtual file system
        const string Container = "MyContainer";

        static void Main(string[] args)
        {
            Test1();

            Console.WriteLine("Complete...");
            Console.ReadLine();
        }

        private static void Test1()
        {
            using (var service = new SystemConnection())
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //This is the plain text file to test
                var plainFile = @"c:\temp\test.txt";

                //Save the file
                var timer = Stopwatch.StartNew();
                service.SaveFile(tenantId, Container, plainFile);
                timer.Stop();
                Console.WriteLine("Write file: Elapsed=" + timer.ElapsedMilliseconds);

                //Get the save file by name
                var newFile = service.GetFile(tenantId, Container, plainFile);

                //Remove the file from storage
                service.RemoveFile(tenantId, Container, plainFile);

                //Compare the 2 plain text files
                var isEqual = FileUtilities.FilesAreEqual(plainFile, newFile);
                Debug.Assert(isEqual);

                //Remove the retrieved file
                FileUtilities.WipeFile(newFile);
            }
        }

        private static byte[] MasterKey16 => Encoding.UTF8.GetBytes("8s$w@r1%a-m>2pq9");

        private static byte[] MasterKey32 => MasterKey16.Concat(MasterKey16).ToArray();

    }
}
