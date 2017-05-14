using Gravitybox.gFileSystem;
using Gravitybox.gFileSystem.Service.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    class Program
    {
        //The is a file grouping. All files in a group can be considered a virtual file system
        const string Container = "MyContainer";

        static void Main(string[] args)
        {
            System.Threading.Thread.Sleep(2000);
            var testFolder = @"C:\Program Files (x86)\Notepad++";

            Test1();
            //Test2(testFolder);
            //TestManyTenants(testFolder);
            //TestRekeyTenant();
            //TestMultipleTenants();
            //TestRemoveAll();

            Console.WriteLine("Complete...");
            Console.ReadLine();
        }

        private static void Test1()
        {
            using (var service = new SystemConnection(MasterKey))
            {
                service.FileUpload += (object sender, FileProgressEventArgs e) => Console.WriteLine("Upload " + e.ChunkIndex + " of " + e.TotalChunks);
                service.FileDownload += (object sender, FileProgressEventArgs e) => Console.WriteLine("Download " + e.ChunkIndex);

                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //This is the plain text file to test
                //var plainFile = @"c:\temp\test.txt";
                //var plainFile = @"d:\temp\bigfile.iso";
                var plainFile = @"c:\temp\qqq.xml";

                //Save the file
                var timer = Stopwatch.StartNew();
                service.SaveFile(tenantId, Container, plainFile);
                timer.Stop();
                Console.WriteLine("Write file: Elapsed=" + timer.ElapsedMilliseconds);

                //Get the saved file by name
                //Big files are processed on the server async so 
                //Loop and until the file is available
                string newFile = null;
                do
                {
                    newFile = service.GetFile(MasterKey, tenantId, Container, plainFile);
                    System.Threading.Thread.Sleep(500);
                } while (newFile == null);

                //Compare the 2 plain text files
                var isEqual = FileUtilities.FilesAreEqual(plainFile, newFile);
                if (isEqual) Console.WriteLine("Files match");
                else Console.WriteLine("ERROR: Files do not match!");
                Debug.Assert(isEqual);

                //Remove the file from storage
                service.RemoveFile(tenantId, Container, plainFile);

                //Remove the retrieved file
                FileUtilities.WipeFile(newFile);
            }
        }

        //private static void Service_FileUpload(object sender, FileProgressEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        private static void Test2(string folderName)
        {
            //Create the manager object
            using (var service = new SystemConnection(MasterKey))
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //Encrypt all files in Notepad++ folder
                var allFiles = Directory.GetFiles(folderName, "*.*", SearchOption.AllDirectories);
                var timer = Stopwatch.StartNew();
                var index = 0;
                foreach (var file in allFiles)
                {
                    service.SaveFile(tenantId, Container, file);
                    index++;
                    Console.WriteLine(string.Format("Saved file {0} / {1}", index, allFiles.Length));
                }
                timer.Stop();
                Console.WriteLine(string.Format("Load {0} files in {1} ms", allFiles.Length, timer.ElapsedMilliseconds));

                //Compare total count of disk vs storage
                var arr = service.GetFileList(MasterKey, tenantId, folderName);
                Debug.Assert(allFiles.Length == arr.Count);
            }
        }

        private static void TestRemoveAll()
        {
            var timer = Stopwatch.StartNew();
            using (var service = new SystemConnection(MasterKey))
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //Remove all of these files
                var count = service.RemoveAll(tenantId, Container);
            }
            timer.Stop();
            Console.WriteLine("Delete all: Elapsed=" + timer.ElapsedMilliseconds);
        }

        private static void TestManyTenants(string folderName)
        {
            var tenantList = new string[] {
                "Tenant 1",
                "Tenant 2",
                "Tenant 3",
            };

            //Create the manager object
            using (var service = new SystemConnection(MasterKey))
            {
                foreach(var tenantName in tenantList)
                {
                    //Get/create tenant
                    var tenantId = service.GetOrAddTenant(tenantName);

                    //Encrypt all files in Notepad++ folder
                    var allFiles = Directory.GetFiles(folderName, "*.*", SearchOption.AllDirectories);
                    var timer = Stopwatch.StartNew();
                    var index = 0;
                    foreach (var file in allFiles)
                    {
                        service.SaveFile(tenantId, Container, file);
                        index++;
                        Console.WriteLine(string.Format("Tenant: "+ tenantName + ", Saved file {0} / {1}", index, allFiles.Length));
                    }
                    timer.Stop();
                    Console.WriteLine(string.Format("Tenant: " + tenantName + ", Load {0} files in {1} ms", allFiles.Length, timer.ElapsedMilliseconds));

                    //Compare total count of disk vs storage
                    var arr = service.GetFileList(MasterKey, tenantId, folderName);
                    Debug.Assert(allFiles.Length == arr.Count);
                }
            }
        }

        private static void TestRekeyTenant()
        {
            //Create the manager object
            using (var service = new SystemConnection(MasterKey))
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //Rekey the tenant key
                var timer = Stopwatch.StartNew();
                var count = service.RekeyTenant(tenantId);
                timer.Stop();

                Console.WriteLine("Rekey tenant: FileCount=" + count + ", Elapsed=" + timer.ElapsedMilliseconds);
            }
        }

        private static void TestMultipleTenants()
        {
            //This is the plain text file to test
            var plainFile = @"c:\temp\test.txt";

            //Create multiple tenants
            using (var service = new SystemConnection(MasterKey))
            {
                for (var ii = 1; ii <= 10; ii++)
                {
                    var tid = service.GetOrAddTenant("Tenant " + ii);
                    //Save the same file to different tenants and look in the storage folder
                    //Each file is unqiue as each file uses a unqiue data key
                    //Each data key is encrypted with the the tenant key
                    //Each tenant key is encrypted with the master key
                    //NOTE: Do not loose the master key!!
                    service.SaveFile(tid, "Default", plainFile);
                }

                //Look and find all files for each tenant and remove each
                //Note:the container is a grouping. It abstracts a virtual file system
                for (var ii = 1; ii <= 10; ii++)
                {
                    var tid = service.GetOrAddTenant("Tenant " + ii);
                    var arr = service.GetFileList(MasterKey, tid);
                    foreach (var item in arr)
                    {
                        service.RemoveFile(tid, "Default", item);
                    }
                }
            }
        }

        /// <summary>
        /// DO A MUCH BETTER JOB OF STORING YOUR MASTER KEY THAN THIS EXAMPLE!!!
        /// </summary>
        private static byte[] MasterKey => System.Text.Encoding.UTF8.GetBytes("MY 32 BYTE KEY!! with padding...");
    }
}
