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
            Test2();
            Test3(testFolder);
            TestAsyncUpload(testFolder);
            TestAsyncDownload(testFolder);
            TestManyTenants(testFolder);
            TestRekeyTenant();
            TestMultipleTenants();
            TestRemoveAll();

            Console.WriteLine("Complete...");
            Console.ReadLine();
        }

        /// <summary>
        /// Use a system connection to save a file and then load it from storage
        /// </summary>
        private static void Test1()
        {
            using (var service = new SystemConnection())
            {
                service.FileUpload += (object sender, FileProgressEventArgs e) => Console.WriteLine("Upload " + e.ChunkIndex + " of " + e.TotalChunks);
                service.FileDownload += (object sender, FileProgressEventArgs e) => Console.WriteLine("Download " + e.ChunkIndex);

                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //This is the plain text file to test
                var plainFile = @"c:\temp\test.txt";

                //Save the file (the name is the key)
                service.SaveFile(tenantId, Container, plainFile);

                //Retrieve the file from storage (file name is just the key)
                var newFile = service.GetFile(tenantId, Container, plainFile);

                //Write to decrypted file
                //In the real world you could work with the stream in memory
                //such that the plaintext fiel never touches disk
                var tempFile = newFile.ToFile();

                //Compare the original and download file
                var isEqual = FileUtilities.FilesAreEqual(plainFile, tempFile);
                if (isEqual) Console.WriteLine("Files match");
                else Console.WriteLine("ERROR: Files do not match!");
                Debug.Assert(isEqual);

                //Remove the file from storage (the file name is the key)
                service.RemoveFile(tenantId, Container, plainFile);

                //Remove the plaintext temp file
                FileUtilities.WipeFile(tempFile);
            }
        }

        /// <summary>
        /// This saves and re-loads a file just like Test1 but it used a TenantConnection for simpler syntax
        /// </summary>
        private static void Test2()
        {
            const string TenantName = "Test1";
            using (var service = new TenantConnection(TenantName, Container))
            {
                service.FileUpload += (object sender, FileProgressEventArgs e) => Console.WriteLine("Upload " + e.ChunkIndex + " of " + e.TotalChunks);
                service.FileDownload += (object sender, FileProgressEventArgs e) => Console.WriteLine("Download " + e.ChunkIndex);

                //This is the plain text file to test
                var plainFile = @"c:\temp\test.txt";

                //Save the file (the file name is the key)
                service.SaveFile(plainFile);

                //Retrieve the file from storage (file name is just the key)
                var newFile = service.GetFile(plainFile);

                //Write to decrypted file
                //In the real world you could work with the stream in memory
                //such that the plaintext fiel never touches disk
                var tempFile = newFile.ToFile();

                //Compare the original and download file
                var isEqual = FileUtilities.FilesAreEqual(plainFile, tempFile);
                if (isEqual) Console.WriteLine("Files match");
                else Console.WriteLine("ERROR: Files do not match!");
                Debug.Assert(isEqual);

                //Remove the file from storage (the file name is the key)
                service.RemoveFile(plainFile);

                //Remove the plaintext temp file
                FileUtilities.WipeFile(tempFile);
            }
        }

        private static void Test3(string folderName)
        {
            //Create the manager object
            using (var service = new SystemConnection())
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

                //Wait for files to post process on server
                System.Threading.Thread.Sleep(2000);

                //Compare total count of disk vs storage
                var arr = service.GetFileList(tenantId, folderName);
                Debug.Assert(allFiles.Length == arr.Count);
            }
        }

        private static void TestAsyncUpload(string folderName)
        {
            //Create the manager object
            using (var service = new SystemConnection())
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //Encrypt all files in Notepad++ folder
                var allFiles = Directory.GetFiles(folderName, "*.*", SearchOption.AllDirectories);
                var timer = Stopwatch.StartNew();
                var index = 0;
                Parallel.ForEach(allFiles, (file) =>
                {
                    service.SaveFile(tenantId, Container, file);
                    index++;
                    Console.WriteLine(string.Format("Saved file {0} / {1}", index, allFiles.Length));
                });
                timer.Stop();
                Console.WriteLine(string.Format("Load {0} files in {1} ms", allFiles.Length, timer.ElapsedMilliseconds));
            }
        }

        private static void TestAsyncDownload(string folderName)
        {
            //Wait for all files to post process on server
            System.Threading.Thread.Sleep(2000);

            //Create the manager object
            using (var service = new SystemConnection())
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = service.GetOrAddTenant(TenantName);

                //Encrypt all files in Notepad++ folder
                var allFiles = Directory.GetFiles(folderName, "*.*", SearchOption.AllDirectories);
                var timer = Stopwatch.StartNew();
                var index = 0;
                Parallel.ForEach(allFiles, (file) =>
                {
                    service.SaveFile(tenantId, Container, file);
                    index++;
                    Console.WriteLine(string.Format("Saved file {0} / {1}", index, allFiles.Length));
                });
                timer.Stop();
                Console.WriteLine(string.Format("Load {0} files in {1} ms", allFiles.Length, timer.ElapsedMilliseconds));
            }
        }

        private static void TestRemoveAll()
        {
            var timer = Stopwatch.StartNew();
            using (var service = new SystemConnection())
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
            using (var service = new SystemConnection())
            {
                foreach (var tenantName in tenantList)
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
                        Console.WriteLine(string.Format("Tenant: " + tenantName + ", Saved file {0} / {1}", index, allFiles.Length));
                    }
                    timer.Stop();
                    Console.WriteLine(string.Format("Tenant: " + tenantName + ", Load {0} files in {1} ms", allFiles.Length, timer.ElapsedMilliseconds));

                    //Compare total count of disk vs storage
                    var arr = service.GetFileList(tenantId, folderName);
                    Debug.Assert(allFiles.Length == arr.Count);
                }
            }
        }

        private static void TestRekeyTenant()
        {
            //Create the manager object
            using (var service = new SystemConnection())
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
            using (var service = new SystemConnection())
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
                    var arr = service.GetFileList(tid);
                    foreach (var item in arr)
                    {
                        service.RemoveFile(tid, "Default", item);
                    }
                }
            }
        }

    }
}
