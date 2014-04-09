7zip-dll
============

This is a .NET project in C# (CLR DLL for MS SQL Server) which implements string (varchar) archiving using the open-source [SevenZipSharp][7zip] implementation. Vocabulary size 4MB, ultimate compression.

Installation
------------

To use the DLL on a server, copy the `SevenZipDB.dll` and `SevenZipSharp.dll` on disk. Run the script from `7zip.sql`, fixing the dll path or the target database if necessary.

**Important**: the MS SQL Server instance must have read rights on a folder containing the DLL.

File size:
----------
1. SevenZipSharp.dll - 161KB
2. SevenZipDB.dll - 38KB

For development open `SeveZipDB\SevenZip.sln`.

Compatibility
-------------
1. Project was created with ShrapDevelop 4.3.1, target framework: .NET 3.5
1. DLL tested on MS SQL Server 2008R2, MS SQL Server 2012.

[7zip]: http://sevenzipsharp.codeplex.com/