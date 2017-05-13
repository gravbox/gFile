--DO NOT MODIFY THIS FILE. IT IS ALWAYS OVERWRITTEN ON GENERATION.
--Data Schema

--CREATE TABLE [ConfigSetting]
if not exists(select * from sysobjects where name = 'ConfigSetting' and xtype = 'U')
CREATE TABLE [dbo].[ConfigSetting] (
	[ID] [Int] IDENTITY (1, 1) NOT NULL ,
	[Name] [VarChar] (50) NOT NULL ,
	[Value] [VarChar] (500) NULL ,
	[ModifiedBy] [NVarchar] (50) NULL,
	[ModifiedDate] [DateTime2] CONSTRAINT [DF__CONFIGSETTING_MODIFIEDDATE] DEFAULT sysdatetime() NULL,
	[CreatedBy] [NVarchar] (50) NULL,
	[CreatedDate] [DateTime2] CONSTRAINT [DF__CONFIGSETTING_CREATEDDATE] DEFAULT sysdatetime() NULL,
	[Timestamp] [ROWVERSION] NOT NULL,
	CONSTRAINT [PK_CONFIGSETTING] PRIMARY KEY CLUSTERED
	(
		[ID]
	)
)

GO

--CREATE TABLE [Container]
if not exists(select * from sysobjects where name = 'Container' and xtype = 'U')
CREATE TABLE [dbo].[Container] (
	[ContainerId] [BigInt] IDENTITY (1, 1) NOT NULL ,
	[Name] [NVarChar] (450) NOT NULL ,
	[TenantId] [BigInt] NOT NULL ,
	[ModifiedBy] [NVarchar] (50) NULL,
	[ModifiedDate] [DateTime2] CONSTRAINT [DF__CONTAINER_MODIFIEDDATE] DEFAULT sysdatetime() NULL,
	[CreatedBy] [NVarchar] (50) NULL,
	[CreatedDate] [DateTime2] CONSTRAINT [DF__CONTAINER_CREATEDDATE] DEFAULT sysdatetime() NULL,
	[Timestamp] [ROWVERSION] NOT NULL,
	CONSTRAINT [PK_CONTAINER] PRIMARY KEY CLUSTERED
	(
		[ContainerId]
	)
)

GO

--CREATE TABLE [FileStash]
if not exists(select * from sysobjects where name = 'FileStash' and xtype = 'U')
CREATE TABLE [dbo].[FileStash] (
	[FileStashID] [BigInt] IDENTITY (1, 1) NOT NULL ,
	[Path] [NVarChar] (450) NOT NULL ,
	[UniqueKey] [UniqueIdentifier] NOT NULL CONSTRAINT [DF__FILESTASH_UNIQUEKEY] DEFAULT (newid()),
	[TenantID] [BigInt] NOT NULL ,
	[Size] [BigInt] NOT NULL ,
	[CrcPlain] [VarChar] (32) NOT NULL ,
	[IsCompressed] [Bit] NOT NULL CONSTRAINT [DF__FILESTASH_ISCOMPRESSED] DEFAULT (0),
	[StorageSize] [BigInt] NOT NULL CONSTRAINT [DF__FILESTASH_STORAGESIZE] DEFAULT (0),
	[ContainerId] [BigInt] NULL ,
	[ModifiedBy] [NVarchar] (50) NULL,
	[ModifiedDate] [DateTime2] CONSTRAINT [DF__FILESTASH_MODIFIEDDATE] DEFAULT sysdatetime() NULL,
	[CreatedBy] [NVarchar] (50) NULL,
	[CreatedDate] [DateTime2] CONSTRAINT [DF__FILESTASH_CREATEDDATE] DEFAULT sysdatetime() NULL,
	[Timestamp] [ROWVERSION] NOT NULL,
	CONSTRAINT [PK_FILESTASH] PRIMARY KEY CLUSTERED
	(
		[FileStashID]
	)
)

GO

--CREATE TABLE [Tenant]
if not exists(select * from sysobjects where name = 'Tenant' and xtype = 'U')
CREATE TABLE [dbo].[Tenant] (
	[TenantID] [BigInt] IDENTITY (1, 1) NOT NULL ,
	[Name] [NVarChar] (50) NOT NULL ,
	[Key] [VarBinary] (48) NOT NULL ,
	[UniqueKey] [UniqueIdentifier] NOT NULL CONSTRAINT [DF__TENANT_UNIQUEKEY] DEFAULT (newid()),
	[ModifiedBy] [NVarchar] (50) NULL,
	[ModifiedDate] [DateTime2] CONSTRAINT [DF__TENANT_MODIFIEDDATE] DEFAULT sysdatetime() NULL,
	[CreatedBy] [NVarchar] (50) NULL,
	[CreatedDate] [DateTime2] CONSTRAINT [DF__TENANT_CREATEDDATE] DEFAULT sysdatetime() NULL,
	[Timestamp] [ROWVERSION] NOT NULL,
	CONSTRAINT [PK_TENANT] PRIMARY KEY CLUSTERED
	(
		[TenantID]
	)
)

GO

--##SECTION BEGIN [FIELD CREATE]
--TABLE [ConfigSetting] ADD FIELDS
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ID' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [ID] [Int] IDENTITY (1, 1) NOT NULL 
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Name' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [Name] [VarChar] (50) NOT NULL 
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Value' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [Value] [VarChar] (500) NULL 
GO
--TABLE [Container] ADD FIELDS
if exists(select * from sys.objects where name = 'Container' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ContainerId' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [ContainerId] [BigInt] IDENTITY (1, 1) NOT NULL 
if exists(select * from sys.objects where name = 'Container' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Name' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [Name] [NVarChar] (450) NOT NULL 
if exists(select * from sys.objects where name = 'Container' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'TenantId' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [TenantId] [BigInt] NOT NULL 
GO
--TABLE [FileStash] ADD FIELDS
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'FileStashID' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [FileStashID] [BigInt] IDENTITY (1, 1) NOT NULL 
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Path' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [Path] [NVarChar] (450) NOT NULL 
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'UniqueKey' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [UniqueKey] [UniqueIdentifier] NOT NULL CONSTRAINT [DF__FILESTASH_UNIQUEKEY] DEFAULT (newid())
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'TenantID' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [TenantID] [BigInt] NOT NULL 
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Size' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [Size] [BigInt] NOT NULL 
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CrcPlain' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [CrcPlain] [VarChar] (32) NOT NULL 
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'IsCompressed' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [IsCompressed] [Bit] NOT NULL CONSTRAINT [DF__FILESTASH_ISCOMPRESSED] DEFAULT (0)
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'StorageSize' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [StorageSize] [BigInt] NOT NULL CONSTRAINT [DF__FILESTASH_STORAGESIZE] DEFAULT (0)
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ContainerId' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [ContainerId] [BigInt] NULL 
GO
--TABLE [Tenant] ADD FIELDS
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'TenantID' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [TenantID] [BigInt] IDENTITY (1, 1) NOT NULL 
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Name' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [Name] [NVarChar] (50) NOT NULL 
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Key' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [Key] [VarBinary] (48) NOT NULL 
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'UniqueKey' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [UniqueKey] [UniqueIdentifier] NOT NULL CONSTRAINT [DF__TENANT_UNIQUEKEY] DEFAULT (newid())
GO
--##SECTION END [FIELD CREATE]

--##SECTION BEGIN [AUDIT TRAIL CREATE]

--APPEND AUDIT TRAIL CREATE FOR TABLE [ConfigSetting]
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedBy' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [CreatedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedDate' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [CreatedDate] [DateTime2] CONSTRAINT [DF__CONFIGSETTING_CREATEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL MODIFY FOR TABLE [ConfigSetting]
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedBy' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [ModifiedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedDate' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [ModifiedDate] [DateTime2] CONSTRAINT [DF__CONFIGSETTING_MODIFIEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL TIMESTAMP FOR TABLE [ConfigSetting]
if exists(select * from sys.objects where name = 'ConfigSetting' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Timestamp' and o.name = 'ConfigSetting')
ALTER TABLE [dbo].[ConfigSetting] ADD [Timestamp] [ROWVERSION] NOT NULL
GO

GO

--APPEND AUDIT TRAIL CREATE FOR TABLE [Container]
if exists(select * from sys.objects where name = 'Container' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedBy' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [CreatedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'Container' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedDate' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [CreatedDate] [DateTime2] CONSTRAINT [DF__CONTAINER_CREATEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL MODIFY FOR TABLE [Container]
if exists(select * from sys.objects where name = 'Container' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedBy' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [ModifiedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'Container' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedDate' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [ModifiedDate] [DateTime2] CONSTRAINT [DF__CONTAINER_MODIFIEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL TIMESTAMP FOR TABLE [Container]
if exists(select * from sys.objects where name = 'Container' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Timestamp' and o.name = 'Container')
ALTER TABLE [dbo].[Container] ADD [Timestamp] [ROWVERSION] NOT NULL
GO

GO

--APPEND AUDIT TRAIL CREATE FOR TABLE [FileStash]
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedBy' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [CreatedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedDate' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [CreatedDate] [DateTime2] CONSTRAINT [DF__FILESTASH_CREATEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL MODIFY FOR TABLE [FileStash]
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedBy' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [ModifiedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedDate' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [ModifiedDate] [DateTime2] CONSTRAINT [DF__FILESTASH_MODIFIEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL TIMESTAMP FOR TABLE [FileStash]
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Timestamp' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [Timestamp] [ROWVERSION] NOT NULL
GO

GO

--APPEND AUDIT TRAIL CREATE FOR TABLE [Tenant]
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedBy' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [CreatedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'CreatedDate' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [CreatedDate] [DateTime2] CONSTRAINT [DF__TENANT_CREATEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL MODIFY FOR TABLE [Tenant]
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedBy' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [ModifiedBy] [NVarchar] (50) NULL
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ModifiedDate' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [ModifiedDate] [DateTime2] CONSTRAINT [DF__TENANT_MODIFIEDDATE] DEFAULT sysdatetime() NULL
GO

--APPEND AUDIT TRAIL TIMESTAMP FOR TABLE [Tenant]
if exists(select * from sys.objects where name = 'Tenant' and type = 'U') and not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Timestamp' and o.name = 'Tenant')
ALTER TABLE [dbo].[Tenant] ADD [Timestamp] [ROWVERSION] NOT NULL
GO

GO

--##SECTION END [AUDIT TRAIL CREATE]

--##SECTION BEGIN [AUDIT TRAIL REMOVE]

--##SECTION END [AUDIT TRAIL REMOVE]

--##SECTION BEGIN [RENAME PK]

--RENAME EXISTING PRIMARY KEYS IF NECESSARY
DECLARE @pkfixConfigSetting varchar(500)
SET @pkfixConfigSetting = (SELECT top 1 i.name AS IndexName FROM sys.indexes AS i WHERE i.is_primary_key = 1 AND OBJECT_NAME(i.OBJECT_ID) = 'ConfigSetting')
if @pkfixConfigSetting <> '' and (BINARY_CHECKSUM(@pkfixConfigSetting) <> BINARY_CHECKSUM('PK_CONFIGSETTING')) exec('sp_rename '''+@pkfixConfigSetting+''', ''PK_CONFIGSETTING''')
DECLARE @pkfixContainer varchar(500)
SET @pkfixContainer = (SELECT top 1 i.name AS IndexName FROM sys.indexes AS i WHERE i.is_primary_key = 1 AND OBJECT_NAME(i.OBJECT_ID) = 'Container')
if @pkfixContainer <> '' and (BINARY_CHECKSUM(@pkfixContainer) <> BINARY_CHECKSUM('PK_CONTAINER')) exec('sp_rename '''+@pkfixContainer+''', ''PK_CONTAINER''')
DECLARE @pkfixFileStash varchar(500)
SET @pkfixFileStash = (SELECT top 1 i.name AS IndexName FROM sys.indexes AS i WHERE i.is_primary_key = 1 AND OBJECT_NAME(i.OBJECT_ID) = 'FileStash')
if @pkfixFileStash <> '' and (BINARY_CHECKSUM(@pkfixFileStash) <> BINARY_CHECKSUM('PK_FILESTASH')) exec('sp_rename '''+@pkfixFileStash+''', ''PK_FILESTASH''')
DECLARE @pkfixTenant varchar(500)
SET @pkfixTenant = (SELECT top 1 i.name AS IndexName FROM sys.indexes AS i WHERE i.is_primary_key = 1 AND OBJECT_NAME(i.OBJECT_ID) = 'Tenant')
if @pkfixTenant <> '' and (BINARY_CHECKSUM(@pkfixTenant) <> BINARY_CHECKSUM('PK_TENANT')) exec('sp_rename '''+@pkfixTenant+''', ''PK_TENANT''')
GO

--##SECTION END [RENAME PK]

--##SECTION BEGIN [DROP PK]

--##SECTION END [DROP PK]

--##SECTION BEGIN [CREATE PK]

--PRIMARY KEY FOR TABLE [ConfigSetting]
if not exists(select * from sysobjects where name = 'PK_CONFIGSETTING' and xtype = 'PK')
ALTER TABLE [dbo].[ConfigSetting] WITH NOCHECK ADD 
CONSTRAINT [PK_CONFIGSETTING] PRIMARY KEY CLUSTERED
(
	[ID]
)
GO
--PRIMARY KEY FOR TABLE [Container]
if not exists(select * from sysobjects where name = 'PK_CONTAINER' and xtype = 'PK')
ALTER TABLE [dbo].[Container] WITH NOCHECK ADD 
CONSTRAINT [PK_CONTAINER] PRIMARY KEY CLUSTERED
(
	[ContainerId]
)
GO
--PRIMARY KEY FOR TABLE [FileStash]
if not exists(select * from sysobjects where name = 'PK_FILESTASH' and xtype = 'PK')
ALTER TABLE [dbo].[FileStash] WITH NOCHECK ADD 
CONSTRAINT [PK_FILESTASH] PRIMARY KEY CLUSTERED
(
	[FileStashID]
)
GO
--PRIMARY KEY FOR TABLE [Tenant]
if not exists(select * from sysobjects where name = 'PK_TENANT' and xtype = 'PK')
ALTER TABLE [dbo].[Tenant] WITH NOCHECK ADD 
CONSTRAINT [PK_TENANT] PRIMARY KEY CLUSTERED
(
	[TenantID]
)
GO
--##SECTION END [CREATE PK]

--##SECTION BEGIN [AUDIT TABLES PK]

--DROP PRIMARY KEY FOR TABLE [__AUDIT__CONFIGSETTING]
if exists(select * from sys.objects where name = 'PK___AUDIT__CONFIGSETTING' and type = 'PK' and type_desc = 'PRIMARY_KEY_CONSTRAINT')
ALTER TABLE [dbo].[__AUDIT__CONFIGSETTING] DROP CONSTRAINT [PK___AUDIT__CONFIGSETTING]
GO

--DROP PRIMARY KEY FOR TABLE [__AUDIT__CONTAINER]
if exists(select * from sys.objects where name = 'PK___AUDIT__CONTAINER' and type = 'PK' and type_desc = 'PRIMARY_KEY_CONSTRAINT')
ALTER TABLE [dbo].[__AUDIT__CONTAINER] DROP CONSTRAINT [PK___AUDIT__CONTAINER]
GO

--DROP PRIMARY KEY FOR TABLE [__AUDIT__FILESTASH]
if exists(select * from sys.objects where name = 'PK___AUDIT__FILESTASH' and type = 'PK' and type_desc = 'PRIMARY_KEY_CONSTRAINT')
ALTER TABLE [dbo].[__AUDIT__FILESTASH] DROP CONSTRAINT [PK___AUDIT__FILESTASH]
GO

--DROP PRIMARY KEY FOR TABLE [__AUDIT__TENANT]
if exists(select * from sys.objects where name = 'PK___AUDIT__TENANT' and type = 'PK' and type_desc = 'PRIMARY_KEY_CONSTRAINT')
ALTER TABLE [dbo].[__AUDIT__TENANT] DROP CONSTRAINT [PK___AUDIT__TENANT]
GO

--##SECTION END [AUDIT TABLES PK]

--FOREIGN KEY RELATIONSHIP [Tenant] -> [Container] ([Tenant].[TenantID] -> [Container].[TenantId])
if not exists(select * from sysobjects where name = 'FK__CONTAINER_TENANT' and xtype = 'F')
ALTER TABLE [dbo].[Container] ADD 
CONSTRAINT [FK__CONTAINER_TENANT] FOREIGN KEY 
(
	[TenantId]
) REFERENCES [dbo].[Tenant] (
	[TenantID]
)
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

--FOREIGN KEY RELATIONSHIP [Container] -> [FileStash] ([Container].[ContainerId] -> [FileStash].[ContainerId])
if not exists(select * from sysobjects where name = 'FK__FILESTASH_CONTAINER' and xtype = 'F')
ALTER TABLE [dbo].[FileStash] ADD 
CONSTRAINT [FK__FILESTASH_CONTAINER] FOREIGN KEY 
(
	[ContainerId]
) REFERENCES [dbo].[Container] (
	[ContainerId]
)
GO

--##SECTION BEGIN [CREATE INDEXES]

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_CONTAINER_NAME' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_CONTAINER_NAME] ON [dbo].[Container]
GO

--INDEX FOR TABLE [Container] COLUMNS:[Name]
if not exists(select * from sys.indexes where name = 'IDX_CONTAINER_NAME') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Name' and o.name = 'Container')
CREATE NONCLUSTERED INDEX [IDX_CONTAINER_NAME] ON [dbo].[Container] ([Name] ASC)
GO

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_CONTAINER_TENANTID' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_CONTAINER_TENANTID] ON [dbo].[Container]
GO

--INDEX FOR TABLE [Container] COLUMNS:[TenantId]
if not exists(select * from sys.indexes where name = 'IDX_CONTAINER_TENANTID') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'TenantId' and o.name = 'Container')
CREATE NONCLUSTERED INDEX [IDX_CONTAINER_TENANTID] ON [dbo].[Container] ([TenantId] ASC)
GO

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_FILESTASH_UNIQUEKEY' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_FILESTASH_UNIQUEKEY] ON [dbo].[FileStash]
GO

--INDEX FOR TABLE [FileStash] COLUMNS:[UniqueKey]
if not exists(select * from sys.indexes where name = 'IDX_FILESTASH_UNIQUEKEY') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'UniqueKey' and o.name = 'FileStash')
CREATE NONCLUSTERED INDEX [IDX_FILESTASH_UNIQUEKEY] ON [dbo].[FileStash] ([UniqueKey] ASC)
GO

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_FILESTASH_TENANTID' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_FILESTASH_TENANTID] ON [dbo].[FileStash]
GO

--INDEX FOR TABLE [FileStash] COLUMNS:[TenantID]
if not exists(select * from sys.indexes where name = 'IDX_FILESTASH_TENANTID') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'TenantID' and o.name = 'FileStash')
CREATE NONCLUSTERED INDEX [IDX_FILESTASH_TENANTID] ON [dbo].[FileStash] ([TenantID] ASC)
GO

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_FILESTASH_PATH' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_FILESTASH_PATH] ON [dbo].[FileStash]
GO

--INDEX FOR TABLE [FileStash] COLUMNS:[Path]
if not exists(select * from sys.indexes where name = 'IDX_FILESTASH_PATH') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Path' and o.name = 'FileStash')
CREATE NONCLUSTERED INDEX [IDX_FILESTASH_PATH] ON [dbo].[FileStash] ([Path] ASC)
GO

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_FILESTASH_CONTAINERID' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_FILESTASH_CONTAINERID] ON [dbo].[FileStash]
GO

--INDEX FOR TABLE [FileStash] COLUMNS:[ContainerId]
if not exists(select * from sys.indexes where name = 'IDX_FILESTASH_CONTAINERID') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ContainerId' and o.name = 'FileStash')
CREATE NONCLUSTERED INDEX [IDX_FILESTASH_CONTAINERID] ON [dbo].[FileStash] ([ContainerId] ASC)
GO

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_TENANT_UNIQUEKEY' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_TENANT_UNIQUEKEY] ON [dbo].[Tenant]
GO

--INDEX FOR TABLE [Tenant] COLUMNS:[UniqueKey]
if not exists(select * from sys.indexes where name = 'IDX_TENANT_UNIQUEKEY') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'UniqueKey' and o.name = 'Tenant')
CREATE NONCLUSTERED INDEX [IDX_TENANT_UNIQUEKEY] ON [dbo].[Tenant] ([UniqueKey] ASC)
GO

--DELETE INDEX
if exists(select * from sys.indexes where name = 'IDX_TENANT_NAME' and type_desc = 'CLUSTERED')
DROP INDEX [IDX_TENANT_NAME] ON [dbo].[Tenant]
GO

--INDEX FOR TABLE [Tenant] COLUMNS:[Name]
if not exists(select * from sys.indexes where name = 'IDX_TENANT_NAME') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Name' and o.name = 'Tenant')
CREATE NONCLUSTERED INDEX [IDX_TENANT_NAME] ON [dbo].[Tenant] ([Name] ASC)
GO

--##SECTION END [CREATE INDEXES]

--##SECTION BEGIN [TENANT INDEXES]

--##SECTION END [TENANT INDEXES]

--##SECTION BEGIN [REMOVE DEFAULTS]

--BEGIN DEFAULTS FOR TABLE [ConfigSetting]
DECLARE @defaultName varchar(max)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'ConfigSetting' and c.name = 'ID')
if @defaultName IS NOT NULL
exec('ALTER TABLE [ConfigSetting] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'ConfigSetting' and c.name = 'Name')
if @defaultName IS NOT NULL
exec('ALTER TABLE [ConfigSetting] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'ConfigSetting' and c.name = 'Value')
if @defaultName IS NOT NULL
exec('ALTER TABLE [ConfigSetting] DROP CONSTRAINT ' + @defaultName)
--END DEFAULTS FOR TABLE [ConfigSetting]
GO

--BEGIN DEFAULTS FOR TABLE [Container]
DECLARE @defaultName varchar(max)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'Container' and c.name = 'ContainerId')
if @defaultName IS NOT NULL
exec('ALTER TABLE [Container] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'Container' and c.name = 'Name')
if @defaultName IS NOT NULL
exec('ALTER TABLE [Container] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'Container' and c.name = 'TenantId')
if @defaultName IS NOT NULL
exec('ALTER TABLE [Container] DROP CONSTRAINT ' + @defaultName)
--END DEFAULTS FOR TABLE [Container]
GO

--BEGIN DEFAULTS FOR TABLE [FileStash]
DECLARE @defaultName varchar(max)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'ContainerId')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'CrcPlain')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'FileStashID')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'IsCompressed')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'Path')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'Size')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'StorageSize')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'TenantID')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'FileStash' and c.name = 'UniqueKey')
if @defaultName IS NOT NULL
exec('ALTER TABLE [FileStash] DROP CONSTRAINT ' + @defaultName)
--END DEFAULTS FOR TABLE [FileStash]
GO

--BEGIN DEFAULTS FOR TABLE [Tenant]
DECLARE @defaultName varchar(max)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'Tenant' and c.name = 'Key')
if @defaultName IS NOT NULL
exec('ALTER TABLE [Tenant] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'Tenant' and c.name = 'Name')
if @defaultName IS NOT NULL
exec('ALTER TABLE [Tenant] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'Tenant' and c.name = 'TenantID')
if @defaultName IS NOT NULL
exec('ALTER TABLE [Tenant] DROP CONSTRAINT ' + @defaultName)
SET @defaultName = (SELECT d.name FROM sys.columns c inner join sys.default_constraints d on c.column_id = d.parent_column_id and c.object_id = d.parent_object_id inner join sys.objects o on d.parent_object_id = o.object_id where o.name = 'Tenant' and c.name = 'UniqueKey')
if @defaultName IS NOT NULL
exec('ALTER TABLE [Tenant] DROP CONSTRAINT ' + @defaultName)
--END DEFAULTS FOR TABLE [Tenant]
GO

--##SECTION END [REMOVE DEFAULTS]

--##SECTION BEGIN [CREATE DEFAULTS]

--BEGIN DEFAULTS FOR TABLE [FileStash]
if not exists(select * from sys.objects where name = 'DF__FILESTASH_ISCOMPRESSED' and type = 'D' and type_desc = 'DEFAULT_CONSTRAINT')
ALTER TABLE [dbo].[FileStash] ADD CONSTRAINT [DF__FILESTASH_ISCOMPRESSED] DEFAULT (0) FOR [IsCompressed]

if not exists(select * from sys.objects where name = 'DF__FILESTASH_STORAGESIZE' and type = 'D' and type_desc = 'DEFAULT_CONSTRAINT')
ALTER TABLE [dbo].[FileStash] ADD CONSTRAINT [DF__FILESTASH_STORAGESIZE] DEFAULT (0) FOR [StorageSize]

if not exists(select * from sys.objects where name = 'DF__FILESTASH_UNIQUEKEY' and type = 'D' and type_desc = 'DEFAULT_CONSTRAINT')
ALTER TABLE [dbo].[FileStash] ADD CONSTRAINT [DF__FILESTASH_UNIQUEKEY] DEFAULT (newid()) FOR [UniqueKey]

--END DEFAULTS FOR TABLE [FileStash]
GO

--BEGIN DEFAULTS FOR TABLE [Tenant]
if not exists(select * from sys.objects where name = 'DF__TENANT_UNIQUEKEY' and type = 'D' and type_desc = 'DEFAULT_CONSTRAINT')
ALTER TABLE [dbo].[Tenant] ADD CONSTRAINT [DF__TENANT_UNIQUEKEY] DEFAULT (newid()) FOR [UniqueKey]

--END DEFAULTS FOR TABLE [Tenant]
GO

--##SECTION END [CREATE DEFAULTS]

if not exists(select * from sys.objects where [name] = '__nhydrateschema' and [type] = 'U')
BEGIN
CREATE TABLE [__nhydrateschema] (
[dbVersion] [varchar] (50) NOT NULL,
[LastUpdate] [datetime] NOT NULL,
[ModelKey] [uniqueidentifier] NOT NULL,
[History] [nvarchar](max) NOT NULL
)
if not exists(select * from sys.objects where [name] = '__pk__nhydrateschema' and [type] = 'PK')
ALTER TABLE [__nhydrateschema] WITH NOCHECK ADD CONSTRAINT [__pk__nhydrateschema] PRIMARY KEY CLUSTERED ([ModelKey])
END
GO

if not exists(select * from sys.objects where name = '__nhydrateobjects' and [type] = 'U')
CREATE TABLE [dbo].[__nhydrateobjects]
(
	[rowid] [bigint] IDENTITY(1,1) NOT NULL,
	[id] [uniqueidentifier] NULL,
	[name] [nvarchar](450) NOT NULL,
	[type] [varchar](10) NOT NULL,
	[schema] [nvarchar](450) NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ModifiedDate] [datetime] NOT NULL,
	[Hash] [varchar](32) NULL,
	[ModelKey] [uniqueidentifier] NOT NULL,
)

if not exists(select * from sys.indexes where name = '__ix__nhydrateobjects_name')
CREATE NONCLUSTERED INDEX [__ix__nhydrateobjects_name] ON [dbo].[__nhydrateobjects]
(
	[name] ASC
)

if not exists(select * from sys.indexes where name = '__ix__nhydrateobjects_schema')
CREATE NONCLUSTERED INDEX [__ix__nhydrateobjects_schema] ON [dbo].[__nhydrateobjects] 
(
	[schema] ASC
)

if not exists(select * from sys.indexes where name = '__ix__nhydrateobjects_type')
CREATE NONCLUSTERED INDEX [__ix__nhydrateobjects_type] ON [dbo].[__nhydrateobjects] 
(
	[type] ASC
)

if not exists(select * from sys.indexes where name = '__ix__nhydrateobjects_modelkey')
CREATE NONCLUSTERED INDEX [__ix__nhydrateobjects_modelkey] ON [dbo].[__nhydrateobjects] 
(
	[ModelKey] ASC
)

if not exists(select * from sys.indexes where name = '__pk__nhydrateobjects')
ALTER TABLE [dbo].[__nhydrateobjects] ADD CONSTRAINT [__pk__nhydrateobjects] PRIMARY KEY CLUSTERED 
(
	[rowid] ASC
)
GO

