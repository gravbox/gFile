# gFile Envelope Encryption System
This is an  encrypted file system using **Envelope Encryption**. This is a multi-tenant file storage system. Each tenant can have any number of containers. Each container stores files. A container can be thought of as a virtual file system independent of other containers. Each stored file uses a unique, encryption key. This data key is prepended to the file encrypted with a tenant key. Every file for a tenant has its unique data key encrypted with its tenant key. This allows the re-keying of the whole tenant very quickly, as there is no need to re-encrypt entire files. Only the data key header is re-encrypted.

```csharp
using (var service = new SystemConnection(MyMasterKey))
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

    //Compare the 2 plain text files
    var isEqual = FileUtilities.FilesAreEqual(plainFile, newFile);
    Debug.Assert(isEqual);

    //Remove the file from storage
    service.RemoveFile(tenantId, Container, plainFile);

    //Remove the retrieved file
    FileUtilities.WipeFile(newFile);
}
```

## Thread Safety
The system is managed by a WIndows service. The service hosts a WCF endpoint which can be used by multiple applications across machines with file consistency. Multi-threaded and multi-machine usage will not interfere with file consistency.

## Storage
The storage is a disk folder that holds all files managed by the system. All files are encrypted and GUID named. The library will add/remove files from storage and you should never modify the contents of the specified storage folder. There is also a working folder where temp files are created while operations are being performed. All temp files are automatically overwritten and wiped after use. You may use the file wipe method to clean any plain text files that you use for security.

## Key Management
No keys are ever un-encrypted in storage, either in the database or on disk. All data keys are encrypted with the relevant tenant key. All tenant keys are encrypted with the master key. The master key is never stored. You supply the master key on service creation. You should manage the master key with the security you find appropriate.

## More Reading
<http://docs.aws.amazon.com/kms/latest/developerguide/workflow.html>

<https://www.cloudreach.com/blog/aws-kms-envelope-encryption/>
