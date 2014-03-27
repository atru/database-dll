database-dll
============

MS SQL Server DLL for CLR is a .NET (C#) project which implements several userful functions, such as:

* aggregate concatenation
* regular expressions
* simple working with arrays
* ISNUMERIC() alternative

You can download a compiled version of the library, or the project itself to check things out.

Installation
------------

To use the DLL on a server, copy the `DatabaseDLL.dll` on disk. Run the script from `DatabaseDLL.sql`, fixing the dll path or the target database if necessary.

**Important**: the MS SQL Server instance must have read rights on a folder containing the DLL.

Compatibility
------------
1. Project was created with ShrapDevelop 4.3.1, target framework: .NET 3.5
1. DLL tested on MS SQL Server 2008R2, MS SQL Server 2012.
