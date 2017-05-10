# gFile
This is an  encrypted fie system. The engine implements envelope encryption. This is a multi-tenant file storage system. Each tenant stores files in a container. A container is essentially a virtual file system. Each file is stored a unique data key. The data key is prepended to the file encrypted with a tenant key. Every file for a tenant has its data key encrypted with the tenant key. This allows the re-keying of the whole tenant very quickly, as there is no need to re-encrypt the whole file. Only the dta key header is re-encryptred.

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

