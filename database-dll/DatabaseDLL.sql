
sp_configure 'clr enabled', 1
RECONFIGURE WITH OVERRIDE

-- ************************************************************************************************
-- Remove Function Entry Points
-- ************************************************************************************************
  
use SYSDB

go
if OBJECT_ID('list') is not null drop AGGREGATE list
go
if OBJECT_ID('arrayCnt') is not null drop function arrayCnt
go
if OBJECT_ID('arrayAt') is not null drop function arrayAt
go
if OBJECT_ID('arraySelect') is not null drop function arraySelect
go
if OBJECT_ID('getParam') is not null drop function getParam
go
if OBJECT_ID('IsNumeric2') is not null drop function IsNumeric2
go
if OBJECT_ID('IsNumeric') is not null drop function IsNumeric
go
if OBJECT_ID('_getSchemaByQuery') is not null drop FUNCTION _getSchemaByQuery
GO
IF OBJECT_ID(N'dbo.RegExOptions') IS NOT NULL DROP FUNCTION dbo.RegExOptions
GO
IF OBJECT_ID(N'dbo.RegExReplace') IS NOT NULL DROP FUNCTION dbo.RegExReplace
GO
IF OBJECT_ID(N'dbo.RegExSplit') IS NOT NULL DROP FUNCTION dbo.RegExSplit
GO
IF OBJECT_ID(N'dbo.RegExEscape') IS NOT NULL DROP FUNCTION dbo.RegExEscape
GO
IF OBJECT_ID(N'dbo.RegExIndex') IS NOT NULL DROP FUNCTION dbo.RegExIndex
GO
IF OBJECT_ID(N'dbo.RegExIsMatch') IS NOT NULL DROP FUNCTION dbo.RegExIsMatch
GO
IF OBJECT_ID(N'dbo.RegExMatch') IS NOT NULL DROP FUNCTION dbo.RegExMatch
GO
IF OBJECT_ID(N'dbo.RegExMatches') IS NOT NULL DROP FUNCTION dbo.RegExMatches
GO
    
-- ************************************************************************************************
-- Remove the assembly from SQL Server
-- ************************************************************************************************
 
if exists(select 1 from sys.assemblies as A where A.name='DatabaseDLL') drop ASSEMBLY DatabaseDLL
go

-- ************************************************************************************************
-- Create Assembly reference
-- ************************************************************************************************
 
CREATE ASSEMBLY DatabaseDLL FROM 'C:\DLL\DatabaseDLL.dll'
GO 
-- ************************************************************************************************
-- Create Function Entry Points
-- ************************************************************************************************
  
CREATE AGGREGATE list(
	@input	nvarchar(max),
	@delim	nvarchar(20)
) 
RETURNS nvarchar(max)
EXTERNAL NAME DatabaseDLL.Concatenate
go
CREATE FUNCTION dbo.arrayCnt (
    @array	nvarchar(max),
    @sep	nvarchar(10)
)
RETURNS int
AS EXTERNAL NAME DatabaseDLL.ArrayFunction.arrayCnt
go
CREATE FUNCTION dbo.arrayAt (
    @array	nvarchar(max),
	@num	int,
    @sep	nvarchar(10)
)
RETURNS nvarchar(max)
AS EXTERNAL NAME DatabaseDLL.ArrayFunction.arrayAt
go
CREATE FUNCTION dbo.arraySelect (
    @array	nvarchar(max),
    @sep	nvarchar(10)
)
RETURNS nvarchar(max)
AS EXTERNAL NAME DatabaseDLL.ArrayFunction.arraySelect
go
CREATE FUNCTION dbo.getParam (
    @list	nvarchar(max),
    @param	nvarchar(100),
    @sep	nvarchar(10)=N';',
    @comp	nvarchar(10)=N'='
)
RETURNS nvarchar(max)
AS EXTERNAL NAME DatabaseDLL.ArrayFunction.getParam
go
CREATE FUNCTION dbo.IsNumeric2 (
    @field		nvarchar(max),
	@sqltype	nvarchar(32)='decimal'
)
RETURNS BIT
AS EXTERNAL NAME DatabaseDLL.IsNumeric.fnIsNumeric
go
CREATE FUNCTION dbo.IsNumeric (
    @field		nvarchar(max)
)
RETURNS BIT
AS BEGIN
	RETURN dbo.IsNumeric2(@field,DEFAULT)
END
GO
CREATE FUNCTION _getSchemaByQuery(
	@query	nvarchar(max)
) 
RETURNS nvarchar(max)
AS EXTERNAL NAME DatabaseDLL.TableDefinition.getSchemaByQuery
GO

-- ************************************************************************************************
-- Regular expressions
-- ************************************************************************************************
  
CREATE FUNCTION [dbo].[RegExOptions]
      (
      @IgnoreCase bit,
        @MultiLine bit,
        @ExplicitCapture bit,
        @Compiled  bit,
        @SingleLine  bit,
        @IgnorePatternWhitespace  bit,
        @RightToLeft  bit,
        @ECMAScript  bit,
        @CultureInvariant  bit
        )
returns int
AS EXTERNAL NAME
   [DatabaseDLL].[SQLRegularExpression.RegExFunctions].[RegExOptions]
  
go
  
CREATE FUNCTION [dbo].[RegExIsMatch]
   (
    @Input NVARCHAR(MAX),
    @Pattern NVARCHAR(4000),
    @Options int
   )
RETURNS BIT
AS EXTERNAL NAME
   DatabaseDLL.[SQLRegularExpression.RegExFunctions].RegExIsMatch
GO
  
CREATE FUNCTION [dbo].[RegExIndex]
   (
    @Input NVARCHAR(MAX),
    @Pattern NVARCHAR(4000),
    @Options int
   )
RETURNS int
AS EXTERNAL NAME
   DatabaseDLL.[SQLRegularExpression.RegExFunctions].RegExIndex
GO
  
CREATE FUNCTION [dbo].[RegExMatch]
   (
    @Input NVARCHAR(MAX),
    @Pattern NVARCHAR(4000),
    @Options int
 )
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME
   DatabaseDLL.[SQLRegularExpression.RegExFunctions].RegExMatch
GO
  
CREATE FUNCTION [dbo].[RegExEscape]
   (
    @Input NVARCHAR(MAX)
   )
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME
   DatabaseDLL.[SQLRegularExpression.RegExFunctions].RegExEscape
GO
 
CREATE FUNCTION [dbo].[RegExSplit]
   (
    @Input NVARCHAR(MAX),
    @Pattern NVARCHAR(4000),
    @Options int
   )
RETURNS TABLE (Match NVARCHAR(MAX))
AS EXTERNAL NAME
   DatabaseDLL.[SQLRegularExpression.RegExFunctions].RegExSplit
GO
  
CREATE FUNCTION [dbo].[RegExReplace]
   (
    @Input NVARCHAR(MAX),
    @Pattern NVARCHAR(4000),
    @Repacement NVARCHAR(MAX),
    @Options int
   )
RETURNS  NVARCHAR(MAX)
AS EXTERNAL NAME
   DatabaseDLL.[SQLRegularExpression.RegExFunctions].RegExReplace
GO
  
CREATE FUNCTION [dbo].[RegExMatches]
   (
    @Input NVARCHAR(MAX),
    @Pattern NVARCHAR(4000),
    @Options int
   )
RETURNS TABLE (Match NVARCHAR(MAX), MatchIndex INT, MatchLength INT)
AS EXTERNAL NAME
   DatabaseDLL.[SQLRegularExpression.RegExFunctions].RegExMatches
GO
