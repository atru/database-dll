USE SYSDB
ALTER DATABASE SYSDB SET TRUSTWORTHY ON
go
if OBJECT_ID('_7zEncode') is not null drop FUNCTION _7zEncode
if OBJECT_ID('_7zDecode') is not null drop FUNCTION _7zDecode
if exists(select 1 from sys.assemblies as A where A.name='DatabaseDLLzip') drop ASSEMBLY DatabaseDLLzip
go
CREATE ASSEMBLY DatabaseDLLzip FROM 'C:\DLL\SevenZipDB.dll'  WITH PERMISSION_SET=UNSAFE
GO
CREATE FUNCTION _7zEncode(
	@input	nvarchar(max)
) 
RETURNS varbinary(max)
AS EXTERNAL NAME DatabaseDLLzip.[SevenZip.SevenZipDB].Compress
go
CREATE FUNCTION _7zDecode(
	@input	varbinary(max)
) 
RETURNS nvarchar(max)
AS EXTERNAL NAME DatabaseDLLzip.[SevenZip.SevenZipDB].Extract
go


--TEST

GO
DECLARE @str varchar(max)=''
SET @str='aaaaaaaaaaaaaaaaaaaabbbbbbbbbbbbbbbbaaaaaaaaaaaaaaaaaaaaaaaaaaaacccccccccc'
DECLARE @byte varbinary(max)
SELECT @byte=SYSDB.dbo._7zEncode(@str)
SELECT LEN(@str) len_original,LEN(@byte) len_encoded,CAST(LEN(@byte) as float)/LEN(@str) as [percent], @byte as encoded,SYSDB.dbo._7zDecode(@byte) as decoded

