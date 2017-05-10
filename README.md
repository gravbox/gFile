# gFile
This is an  encrypted fie system. The engine implements **Envelope Encryption**. This is a multi-tenant file storage system. Each tenant stores files in a container. A container is essentially a virtual file system. Each file is stored a unique data key. The data key is prepended to the file encrypted with a tenant key. Every file for a tenant has its data key encrypted with the tenant key. This allows the re-keying of the whole tenant very quickly, as there is no need to re-encrypt the whole file. Only the dta key header is re-encryptred.

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

    //Clean up the files we just created
    FileUtilities.WipeFile(cryptFile);
    FileUtilities.WipeFile(plainFile2);
}
```

The engine is very simple and does not provide the necessary key management facilities. There is an additional FileManager component that provide the key and file management abilities for a working file storage system. It uses SQL Server to manage keys, tenants, containers, and files. There is a database installer included that will create a SQL database. The FileManager library can be included in your project to save and retieve encrytped files.

```csharp
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
````
