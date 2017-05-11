using Gravitybox.gFileSystem;
using Gravitybox.gFileSystem.Engine;
using Gravitybox.gFileSystem.Manager;
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
        const string ConnectionString = @"server=.\SQL2014;initial catalog=gFile;Integrated Security=SSPI;";

        static void Main(string[] args)
        {
            var testFolder = @"C:\Program Files (x86)\Notepad++";

            ManagerTestSimple();
            ManagerTestFolder(testFolder);
            ManagerTestRekeyTenant();
            ManagerDeleteAll();
            ManagerMultipleTenants();
            ManagerChangeMasterKey();

            Console.WriteLine("Complete...");
            Console.ReadLine();
        }

        private static void ManagerTestSimple()
        {
            //Create the manager object
            using (var fm = new FileManager(MasterKey16, ConnectionString))
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = fm.GetOrAddTenant(TenantName);

                //This is the plain text file to test
                var plainFile = @"c:\temp\test.txt";

                //Save the file
                var wasSaved = fm.SaveFile(tenantId, Container, plainFile);

                //Get the save file by name
                var newFile = fm.GetFile(tenantId, Container, plainFile);

                //Remove the file from storage
                fm.RemoveFile(tenantId, Container, plainFile);

                //Compare the 2 plain text files
                Debug.Assert(FileUtilities.FilesAreEqual(plainFile, newFile));

                //Remove the retrieved file
                FileUtilities.WipeFile(newFile);
            }
        }

        private static void ManagerTestFolder(string folderName)
        {
            //Create the manager object
            using (var fm = new FileManager(MasterKey16, ConnectionString))
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = fm.GetOrAddTenant(TenantName);

                //Encrypt all files in Notepad++ folder
                var allFiles = Directory.GetFiles(folderName, "*.*", SearchOption.AllDirectories);
                var timer = Stopwatch.StartNew();
                var index = 0;
                foreach (var file in allFiles)
                {
                    fm.SaveFile(tenantId, Container, file);
                    index++;
                    Console.WriteLine(string.Format("Saved file {0} / {1}", index, allFiles.Length));
                }
                timer.Stop();
                Console.WriteLine(string.Format("Load {0} files in {1} ms", allFiles.Length, timer.ElapsedMilliseconds));

                //Compare total count of disk vs storage
                var arr = fm.GetFileList(tenantId, folderName);
                Debug.Assert(allFiles.Length == arr.Count);
            }
        }

        private static void ManagerTestRekeyTenant()
        {
            //Create the manager object
            using (var fm = new FileManager(MasterKey16, ConnectionString))
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = fm.GetOrAddTenant(TenantName);

                //Rekey the tenant key
                var timer = Stopwatch.StartNew();
                var count = fm.RekeyTenant(tenantId);
                timer.Stop();

                Console.WriteLine("Rekey tenant: FileCount=" + count + ", Elapsed=" + timer.ElapsedMilliseconds);
            }
        }

        private static void ManagerDeleteAll()
        {
            var timer = Stopwatch.StartNew();
            using (var fm = new FileManager(MasterKey16, ConnectionString))
            {
                //Get/create tenant
                const string TenantName = "Test1";
                var tenantId = fm.GetOrAddTenant(TenantName);

                //Remove all of these files
                var count = fm.RemoveAll(tenantId, Container);
            }
            timer.Stop();
            Console.WriteLine("Delete all: Elapsed=" + timer.ElapsedMilliseconds);
        }

        private static void ManagerMultipleTenants()
        {
            //This is the plain text file to test
            var plainFile = @"c:\temp\test.txt";

            //Create multiple tenants
            using (var fm = new FileManager(MasterKey16, ConnectionString))
            {
                for (var ii = 1; ii <= 10; ii++)
                {
                    var tid = fm.GetOrAddTenant("Tenant " + ii);
                    //Save the same file to different tenants and look in the storage folder
                    //Each file is unqiue as each file uses a unqiue data key
                    //Each data key is encrypted with the the tenant key
                    //Each tenant key is encrypted with the master key
                    //NOTE: Do not loose the master key!!
                    fm.SaveFile(tid, "Default", plainFile);
                }

                //Look and find all files for each tenant and remove each
                //Note:the container is a grouping. It abstracts a virtual file system
                for (var ii = 1; ii <= 10; ii++)
                {
                    var tid = fm.GetOrAddTenant("Tenant " + ii);
                    var arr = fm.GetFileList(tid);
                    foreach(var item in arr)
                    {
                        fm.RemoveFile(tid, "Default", item);
                    }
                }
            }
        }

        private static void ManagerChangeMasterKey()
        {
            //TODO
        }

        private static byte[] MasterKey16 => Encoding.UTF8.GetBytes("8s$w@r1%a-m>2pq9");

        private static byte[] MasterKey32 => MasterKey16.Concat(MasterKey16).ToArray();

    }
}
