--DO NOT MODIFY THIS FILE. IT IS ALWAYS OVERWRITTEN ON GENERATION.

--##SECTION BEGIN [INTERNAL STORED PROCS]

--This SQL is generated for internal stored procedures for table [ConfigSetting]
if exists(select * from sys.objects where name = 'gen_ConfigSetting_Delete' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_ConfigSetting_Delete]
GO

if exists(select * from sys.objects where name = 'gen_ConfigSetting_Insert' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_ConfigSetting_Insert]
GO

if exists(select * from sys.objects where name = 'gen_ConfigSetting_Update' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_ConfigSetting_Update]
GO

--This SQL is generated for internal stored procedures for table [FileStash]
if exists(select * from sys.objects where name = 'gen_FileStash_Delete' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_FileStash_Delete]
GO

if exists(select * from sys.objects where name = 'gen_FileStash_Insert' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_FileStash_Insert]
GO

if exists(select * from sys.objects where name = 'gen_FileStash_Update' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_FileStash_Update]
GO

--This SQL is generated for internal stored procedures for table [Tenant]
if exists(select * from sys.objects where name = 'gen_Tenant_Delete' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_Tenant_Delete]
GO

if exists(select * from sys.objects where name = 'gen_Tenant_Insert' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_Tenant_Insert]
GO

if exists(select * from sys.objects where name = 'gen_Tenant_Update' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [dbo].[gen_Tenant_Update]
GO

--##SECTION END [INTERNAL STORED PROCS]

