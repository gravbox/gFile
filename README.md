# gFile Envelope Encryption System
This is an  encrypted file system using **Envelope Encryption**. This is a multi-tenant file storage system. Each tenant can have any number of containers. Each container stores files. A container can be thought of as a virtual file system independent of other containers. Each stored file uses a unique, encryption key. This data key is prepended to the file encrypted with a tenant key. Every file for a tenant has its unique data key encrypted with its tenant key. This allows the re-keying of the whole tenant very quickly, as there is no need to re-encrypt entire files. Only the data key header is re-encrypted.

```csharp
const string TenantName = "Test1";
const string ContainerName = "MyBox";
using (var service = new TenantConnection(TenantName, Container))
{
    //This is the plain text file to test
    var plainFile = @"c:\temp\test.txt";

    //Save the file (the file name is the key)
    service.SaveFile(plainFile);

    //Retrieve the file from storage (file name is just the key)
    var newFile = service.GetFile(plainFile);

    //Write to text file
    var tempFile = newFile.ToFile();

    //Compare the 2 plain text files
    var isEqual = FileUtilities.FilesAreEqual(plainFile, tempFile);
    if (isEqual) Console.WriteLine("Files match");
    else Console.WriteLine("ERROR: Files do not match!");
    Debug.Assert(isEqual);

    //Remove the file from storage (the file name is the key)
    service.RemoveFile(plainFile);

    //Remove the plaintext temp file
    FileUtilities.WipeFile(tempFile);
}
```

## Thread Safety
The system is managed by a WIndows service. The service hosts a WCF endpoint which can be used by multiple applications across machines with file consistency. Multi-threaded and multi-machine usage will not interfere with file consistency.

## Storage
The storage is a disk folder that holds all files managed by the system. All files are encrypted and GUID named. The library will add/remove files from storage and you should never modify the contents of the storage folder. There is also a working folder where temp files are created while operations are being performed. All temp files are encrypted and automatically overwritten and wiped after use. You may use the file wipe method ("FileUtilities.WipeFile") to clean any files that you use for security.

## Disk Usage
No plaintext file is ever written to disk on the server. The server only processes unencrypted, plaintext in memory. If the server machine looses power, the service crashes, or any other action that kills the process occurs while temp files are on disk, the files are encrypted.

## Key Management
No keys are ever un-encrypted in storage, either in the database or on disk. All data keys are encrypted with the relevant tenant key. All tenant keys are encrypted with the master key. The master key is never stored. You supply the master key on service creation. You should manage the master key with the security you find appropriate.

## More Reading
<http://docs.aws.amazon.com/kms/latest/developerguide/workflow.html>

<https://www.cloudreach.com/blog/aws-kms-envelope-encryption/>
