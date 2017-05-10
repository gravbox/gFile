if exists(select * from sys.objects where name = 'AddOrUpdateTenant' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [AddOrUpdateTenant]
GO

CREATE PROCEDURE [AddOrUpdateTenant]
	@name nvarchar(50),
	@key varbinary(48)
AS
BEGIN

begin transaction

if not exists (select * from [Tenant] where [Name] = @name)
begin
insert into [Tenant] ([Name], [Key]) values (@name, @key)
select UniqueKey from [Tenant] where [TenantId] = SCOPE_IDENTITY()
end
select UniqueKey from [Tenant] where [Name] = @name

commit transaction

END
GO
