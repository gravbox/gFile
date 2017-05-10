--DO NOT MODIFY THIS FILE. IT IS ALWAYS OVERWRITTEN ON GENERATION.
--Audit Triggers

--##SECTION BEGIN [AUDIT TRIGGERS]

--DROP ANY AUDIT TRIGGERS FOR [dbo].[ConfigSetting]
if exists(select * from sysobjects where name = '__TR_ConfigSetting__INSERT' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_ConfigSetting__INSERT]
GO
if exists(select * from sysobjects where name = '__TR_ConfigSetting__UPDATE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_ConfigSetting__UPDATE]
GO
if exists(select * from sysobjects where name = '__TR_ConfigSetting__DELETE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_ConfigSetting__DELETE]
GO

--DROP ANY AUDIT TRIGGERS FOR [dbo].[FileStash]
if exists(select * from sysobjects where name = '__TR_FileStash__INSERT' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_FileStash__INSERT]
GO
if exists(select * from sysobjects where name = '__TR_FileStash__UPDATE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_FileStash__UPDATE]
GO
if exists(select * from sysobjects where name = '__TR_FileStash__DELETE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_FileStash__DELETE]
GO

--DROP ANY AUDIT TRIGGERS FOR [dbo].[Tenant]
if exists(select * from sysobjects where name = '__TR_Tenant__INSERT' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_Tenant__INSERT]
GO
if exists(select * from sysobjects where name = '__TR_Tenant__UPDATE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_Tenant__UPDATE]
GO
if exists(select * from sysobjects where name = '__TR_Tenant__DELETE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_Tenant__DELETE]
GO

--DROP ANY AUDIT TRIGGERS FOR [dbo].[ThreadLock]
if exists(select * from sysobjects where name = '__TR_ThreadLock__INSERT' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_ThreadLock__INSERT]
GO
if exists(select * from sysobjects where name = '__TR_ThreadLock__UPDATE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_ThreadLock__UPDATE]
GO
if exists(select * from sysobjects where name = '__TR_ThreadLock__DELETE' AND xtype = 'TR')
DROP TRIGGER [dbo].[__TR_ThreadLock__DELETE]
GO

--##SECTION END [AUDIT TRIGGERS]

