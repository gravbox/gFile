if exists(select * from sys.objects where name = 'GetLock' and type = 'P' and type_desc = 'SQL_STORED_PROCEDURE')
	drop procedure [GetLock]
GO

CREATE PROCEDURE [GetLock]
	@key uniqueidentifier,
	@isWrite bit,
	@hash bigint
AS
BEGIN

begin transaction

--If there are any locks for this Key/hash then cannot get a write lock
if (@isWrite = 1)
begin
if exists (select * from [ThreadLock] where [Key] = @key and [Hash] = @hash)
select cast(0 as bigint)
end
else
begin
--If trying to get read lock and there is a write lock then exit
if exists (select * from [ThreadLock] where [Key] = @key and [Hash] = @hash and [IsWrite] = 1)
select cast(0 as bigint)
end

if not exists (select * from [ThreadLock] where [Key] = @key and [Hash] = @hash)
begin
insert into [ThreadLock] ([Key], [Hash], [IsWrite]) values (@key, @hash, @isWrite)
select cast(SCOPE_IDENTITY() as bigint)
end
else
begin
select cast(0 as bigint)
end

commit transaction

END
GO
