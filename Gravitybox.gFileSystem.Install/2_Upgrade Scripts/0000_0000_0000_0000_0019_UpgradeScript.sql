--Generated Upgrade For Version 0.0.0.0.19
--Generated on 2017-05-12 13:36:38

--ADD COLUMN [FileStash].[IsCompressed]
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'IsCompressed' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [IsCompressed] [Bit] NOT NULL CONSTRAINT [DF__FILESTASH_ISCOMPRESSED] DEFAULT (0)

GO

--ADD COLUMN [FileStash].[StorageSize]
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'StorageSize' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [StorageSize] [BigInt] NOT NULL CONSTRAINT [DF__FILESTASH_STORAGESIZE] DEFAULT (0)

GO

--REMOVE FOREIGN KEY
if exists(select * from sysobjects where name = 'FK__FILESTASH_TENANT' and xtype = 'F')
ALTER TABLE [dbo].[FileStash] DROP CONSTRAINT [FK__FILESTASH_TENANT]
GO

--FOREIGN KEY RELATIONSHIP [Tenant] -> [FileStash] ([Tenant].[TenantID] -> [FileStash].[TenantID])
if not exists(select * from sysobjects where name = 'FK__FILESTASH_TENANT' and xtype = 'F')
ALTER TABLE [dbo].[FileStash] ADD 
CONSTRAINT [FK__FILESTASH_TENANT] FOREIGN KEY 
(
	[TenantID]
) REFERENCES [dbo].[Tenant] (
	[TenantID]
)
GO

