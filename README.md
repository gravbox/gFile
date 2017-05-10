# gFile Envelope Encryption System
This is an  encrypted file system. The engine implements **Envelope Encryption**. This is a multi-tenant file storage system. Each tenant stores files in a container. A container is essentially a virtual file system. Each file is stored a unique data key. The data key is prepended to the file encrypted with a tenant key. Every file for a tenant has its unique data key encrypted with its tenant key. This allows the re-keying of the whole tenant very quickly, as there is no need to re-encrypt whole files. Only the data key header is re-encrypted.

```csharp
//Create engine
using (var fe = new FileEngine(MasterKey, TenantKey))
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

    //Clean up the files from temp folder
    FileUtilities.WipeFile(cryptFile);
    FileUtilities.WipeFile(plainFile2);
}
```
## File Management Library
The engine is very simple and does not provide the necessary key and file management facilities. There is an additional **FileManager** component that provides the key and file management abilities for a working file storage system. It uses SQL Server to manage keys, tenants, containers, and files. There is a database installer included that will create a SQL database. The FileManager library can be included in your project to save and retrieve encrypted files.

```csharp
//Create the manager object
using (var fm = new FileManager(MasterKey, ConnectionString))
{
    //Get/create tenant
    const string TenantName = "Test1";
    var tenantId = fm.GetOrAddTenant(TenantName);
    
    //A container is just a group for files
    var Container = "SomeName";

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

    //Remove the retrieved file from temp folder
    FileUtilities.WipeFile(newFile);
}
````

## Thread Safety
The FileManager library can be used by multiple applications across machines with file consistency. The locking is handled using the  database as the central coordinator. Multi-threaded and multi-machine usage will not interfere with file consistency.

## Storage
The storage is a disk folder that holds all files managed by the database. All files are encrypted and GUID named. The library will add/remove files from storage and you should never modify the contents of the specified storage folder. There is a working folder as well where files are handled while operations are being performed. You may use the file wipe method to clean any plain text files after use for security.

## Key Management
No keys are ever un-encrypted in storage, either in the database or on disk. All data keys are encrypted with the relevant tenant key. All tenant keys are encrypted with the master key. The master key is never stored. You supply the master key on object creation. You should manage the master key with the security you find appropriate.
