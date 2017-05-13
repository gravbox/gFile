--Generated Upgrade For Version 0.0.0.0.20
--Generated on 2017-05-12 22:54:31

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
--PRIMARY KEY FOR TABLE [Container]
if not exists(select * from sysobjects where name = 'PK_CONTAINER' and xtype = 'PK')
ALTER TABLE [dbo].[Container] WITH NOCHECK ADD 
CONSTRAINT [PK_CONTAINER] PRIMARY KEY CLUSTERED
(
	[ContainerId]
)

GO
--INDEX FOR TABLE [Container] COLUMNS:[Name]
if not exists(select * from sys.indexes where name = 'IDX_CONTAINER_NAME') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'Name' and o.name = 'Container')
CREATE NONCLUSTERED INDEX [IDX_CONTAINER_NAME] ON [dbo].[Container] ([Name] ASC)
GO

--INDEX FOR TABLE [Container] COLUMNS:[TenantId]
if not exists(select * from sys.indexes where name = 'IDX_CONTAINER_TENANTID') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'TenantId' and o.name = 'Container')
CREATE NONCLUSTERED INDEX [IDX_CONTAINER_TENANTID] ON [dbo].[Container] ([TenantId] ASC)
GO


GO
--ADD COLUMN [FileStash].[ContainerId]
if exists(select * from sys.objects where name = 'FileStash' and type = 'U') AND not exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ContainerId' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] ADD [ContainerId] [BigInt] NULL 

GO

--DELETE DEFAULT
select 'ALTER TABLE [dbo].[FileStash] DROP CONSTRAINT ' + [name] as 'sql' into #t from sysobjects where id IN( select SC.cdefault FROM dbo.sysobjects SO INNER JOIN dbo.syscolumns SC ON SO.id = SC.id LEFT JOIN sys.default_constraints SM ON SC.cdefault = SM.parent_column_id WHERE SO.xtype = 'U' and SO.NAME = 'FileStash' and SC.NAME = 'ContainerName')
declare @sql [nvarchar] (1000)
SELECT @sql = MAX([sql]) from #t
exec (@sql)
drop table #t

--DELETE UNIQUE CONTRAINT
if exists(select * from sysobjects where name = 'IX_FILESTASH_CONTAINERNAME' and xtype = 'UQ')
ALTER TABLE [FileStash] DROP CONSTRAINT [IX_FILESTASH_CONTAINERNAME]

--DELETE INDEX
if exists (select * from sys.indexes where name = 'IDX_FILESTASH_CONTAINERNAME')
DROP INDEX [IDX_FILESTASH_CONTAINERNAME] ON [FileStash]

--DROP COLUMN
if exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ContainerName' and o.name = 'FileStash')
ALTER TABLE [dbo].[FileStash] DROP COLUMN [ContainerName]

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

--INDEX FOR TABLE [FileStash] COLUMNS:[ContainerId]
if not exists(select * from sys.indexes where name = 'IDX_FILESTASH_CONTAINERID') and exists (select * from syscolumns c inner join sysobjects o on c.id = o.id where c.name = 'ContainerId' and o.name = 'FileStash')
CREATE NONCLUSTERED INDEX [IDX_FILESTASH_CONTAINERID] ON [dbo].[FileStash] ([ContainerId] ASC)

GO

