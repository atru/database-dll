/*  This file is part of SevenZipDB.

    SevenZipSharp is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SevenZipSharp is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with SevenZipSharp.  If not, see <http://www.gnu.org/licenses/>.
*/
/*
 * Created by SharpDevelop.
 * User: Alex Truman
 * Date: 07.04.2014
 * Time: 17:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */ 
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;

namespace SevenZip
{
	/// <summary>
	/// Wrapping SevenZipCompressor for MS SQL CLR.
	/// </summary>
	public class SevenZipDB
	{
		public static SqlBinary Compress(SqlString input)
		{
			return SevenZipCompressor.CompressBytes(Encoding.UTF8.GetBytes(input.Value));
		}
		public static SqlString Extract(SqlBinary input)
		{
			return Encoding.UTF8.GetString(SevenZipExtractor.ExtractBytes(input.Value));
		}
	}
}