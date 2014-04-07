/*
 * Created by SharpDevelop.
 * User: Alex Truman
 * Date: 20.03.2014
 * Time: 10:02
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

using System.IO;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;


[Serializable]
[SqlUserDefinedAggregate(
    Format.UserDefined, //use clr serialization to serialize the intermediate result
    IsInvariantToNulls = true, //optimizer property
    IsInvariantToDuplicates = false, //optimizer property
    IsInvariantToOrder = false, //optimizer property
    MaxByteSize = -1) //maximum size in bytes of persisted value
]
/// <summary>
/// Aggregate concatenation function
/// </summary>
public class Concatenate : IBinarySerialize
{
    /// <summary>
    /// The variable that holds the intermediate result of the concatenation
    /// </summary>
    public StringBuilder intermediateResult;
    /// <summary>
    /// Delimiter between values in the result
    /// </summary>
    public String delim;

    /// <summary>
    /// Initialize the internal data structures
    /// </summary>
    public void Init()
    {
        intermediateResult = new StringBuilder();
        delim = String.Empty;
    }

    /// <summary>
    /// Accumulate the next value, not if the value is null
    /// </summary>
    /// <param name="value">single value</param>
    /// <param name="delimiter">delimiter between values in the result</param>
    public void Accumulate(SqlString value, SqlString delimiter)
    {
    	if (value.IsNull) return;
        delim = delimiter.IsNull ? "," : delimiter.Value;
        intermediateResult.Append(value.Value).Append(delim);
    }

    /// <summary>
    /// Merge the partially computed aggregate with this aggregate.
    /// </summary>
    /// <param name="other">partially computed aggregate</param>
    public void Merge(Concatenate other)
    {
        intermediateResult.Append(other.intermediateResult);
        delim = other.delim;
    }

    /// <summary>
    /// Called at the end of aggregation, to return the results of the aggregation.
    /// </summary>
    /// <returns>Returns result of aggregation</returns>
    public SqlString Terminate()
    {
        string output = string.Empty;
        //delete the trailing delimiter, if any
        if (this.intermediateResult != null
            && this.intermediateResult.Length > 0)
        {
        	output = this.intermediateResult.ToString(0, this.intermediateResult.Length-this.delim.Length);
        }
        return new SqlString(output);
    }
    
    /// <summary>
    /// Standard routine for aggregate functions. Reads two values subsequently.
    /// </summary>
    public void Read(BinaryReader r)
    {
        intermediateResult = new StringBuilder(r.ReadString());
		delim = r.ReadString();        
    }

    /// <summary>
    /// Standard routine for aggregate functions. Writes two values subsequently.
    /// </summary>
    public void Write(BinaryWriter w)
    {
    	w.Write(intermediateResult.ToString());
    	//write deliminator in case of multithreading ??
    	w.Write(delim.ToString());
    }
}

/// <summary>
/// Working with "arrays" (varchar strings)
/// </summary>
public class ArrayFunction
{
    /// <summary>
    /// Counts the number of elements in a string, divided by the delimiter
    /// Eg: arrayCnt("a;bc;d",";") returns 3
    /// </summary>
    /// <param name="array">input string</param>
    /// <param name="delim">delimiter</param>
    /// <returns>The counted number of elements</returns>
    public static SqlInt32 arrayCnt(SqlString array, SqlString delim)
    {
        if(array.IsNull) return -1;
        string[] ss = new string[1];
        ss[0]=delim.Value;
        return array.Value.Split(ss, StringSplitOptions.None).Length;
    }
    /// <summary>
    /// Splits the string by the delimiter and returns and element at specified position.
    /// Zero-based.
    /// </summary>
    /// <param name="array">Input string</param>
    /// <param name="num">Position</param>
    /// <param name="delim">Delimiter</param>
    /// <returns>The element at specified position</returns>
    public static SqlString arrayAt(SqlString array, SqlInt32 num, SqlString delim)
    {
        if(array.IsNull) return null;
        string[] ss = new string[1];
        ss[0]=delim.Value;
        try{
        	return array.Value.Split(ss, StringSplitOptions.None)[num.Value];
        }
        catch(Exception ex){
        	return null;
        }
        
    }
    /// <summary>
    /// Builds a union-query to select all elements of string divided by delimiter.
    /// Eg: arraySelect("a;b;c",";") returns "select 'a' as Value UNION ALL
    /// select 'b' UNION ALL select 'c'"
    /// </summary>
    /// <param name="array">Input string</param>
    /// <param name="delim">Delimiter</param>
    /// <returns>Returns a query</returns>
    public static SqlString arraySelect(SqlString array, SqlString delim)
    {
        if(array.IsNull) return null;
        string[] ss = new string[1];
        ss[0]=delim.Value;
        string[] items = array.Value.Split(ss, StringSplitOptions.None);
        string selectAll =string.Format("select '{0}' as Value ",items[0]);;
        if(items.Length>1)
            selectAll += " union all ";
        for(int i=1;i<items.Length-1;i++)
            selectAll+=string.Format("select '{0}' union all ",items[i]);
        if(items.Length>1)
            selectAll+=string.Format("select '{0}'",items[items.Length-1]);
        return selectAll;
    }
    /// <summary>
    /// Gets a varchar representation of a parameter in a string of "key=value" pairs.
    /// </summary>
    /// <param name="list">Input string of format "a=1.0;b=2.4;c=text value;d=2014-03-20 10:00"</param>
    /// <param name="param">The "key" to look for ("a","b" etc. in the example)</param>
    /// <param name="delim">The delimiter between pairs (";" in the example)</param>
    /// <param name="comp">The comparator ("=" in the example)</param>
    /// <returns>Returns a varchar string of the found value.</returns>
    public static SqlString getParam(SqlString list, SqlString param, SqlString delim, SqlString comp)
    {
        if(list.IsNull) return null;
        string[] ss = new string[1], cc = new string[1], pair;
        ss[0]=delim.Value;
        cc[0]=comp.Value;
        string[] items = list.Value.Split(ss, StringSplitOptions.None);
        
        foreach(string item in items)
        {
            pair = item.Split(cc, StringSplitOptions.None);            
            if(pair[0]==param)
            	return item.Substring(pair[0].Length+cc.Length);
        }
        return null;
    }
}

/// <summary>
/// Working with query schema
/// </summary>
public class TableDefinition
{
	[SqlFunction(DataAccess= DataAccessKind.Read)]
	/// <summary>
	/// Builds the "guessed" schema for the provided query.
	/// Uses SqlDataReader.GetSchemaTable() to run through columns.
	/// </summary>
	/// <param name="query">Input query</param>
	/// <returns>Returns varchar string of "name type, name type" pairs for columns.</returns>
	public static SqlString getSchemaByQuery(SqlString query)
	{
		try
		{
			string schema="";
			string sql=query.ToString();
			
			SqlConnection conn = new SqlConnection("context connection=true");
			conn.Open();
			SqlCommand command = null;
			SqlDataReader reader = null;
			
			try
			{
				command = new SqlCommand(String.Format(sql),conn);
				reader = command.ExecuteReader();
			}
			catch
			{
				conn.Close();
				return null;	
			}
			
			DataTable td = reader.GetSchemaTable();
			foreach (DataRow myField in td.Rows)
			{
				string ColumnName="";
				string ColumnSize="";
				string NumericPrecision="";
				string NumericScale="";
				string DataTypeName="";
				
			    foreach (DataColumn myProperty in td.Columns)
			    {
					switch (myProperty.ColumnName.ToString())
					{
							case "ColumnName":			{ColumnName = myField[myProperty].ToString();break;}
							case "ColumnSize":			{ColumnSize = myField[myProperty].ToString();break;}
							case "NumericPrecision":	{NumericPrecision = myField[myProperty].ToString();break;}
							case "NumericScale":		{NumericScale = myField[myProperty].ToString();break;}
							case "DataTypeName":		{DataTypeName = myField[myProperty].ToString();break;}
					}
			    }
			    
				schema += ColumnName + " " + DataTypeName;
				if(	DataTypeName == "binary" 
				   || DataTypeName == "char"
				   || DataTypeName == "nchar"
				   || DataTypeName == "nvarchar" 
				   || DataTypeName == "varchar" 
				   || DataTypeName == "varbinary"
				  )
					schema += "(" + (ColumnSize=="2147483647"?"max":ColumnSize) + ")";
				else if(DataTypeName == "datetime2" 
				   || DataTypeName == "datetimeoffset"
				   || DataTypeName == "time"
				  )
					schema += "(" + NumericScale + ")";
				else if(DataTypeName == "decimal" 
				   || DataTypeName == "numeric"
				  )
					schema += "(" + NumericPrecision + "," + NumericScale + ")";
				
				schema += ",";				
			    
			}
			
			if(schema.Length > 1)
				schema = schema.Substring(0, schema.Length - 1);		
			
			return schema;
		}
		catch
		{
			return null;
		}
	}
}

/// <summary>
/// Checks whether given string is a certain type of "numeric"
/// </summary>
public class IsNumeric
{
    [Microsoft.SqlServer.Server.SqlFunction]
    /// <summary>
    ///  Checks whether given string is a certain type of "numeric"
    /// </summary>
    /// <param name="field">Varchar string</param>
    /// <param name="sqltype">A type to compare</param>
    /// <returns></returns>
    public static SqlBoolean fnIsNumeric(SqlString field, string sqltype)
    {
        var result = new SqlBoolean(0);  // default False
        string errorMessage = string.Empty;

        // Determine base type and any decimal precision parameters
        if(sqltype==null)
        	return result;
        var type = sqltype.ToString().Trim().ToLower();
        var typePrecision = string.Empty;
        if(type.Contains("(")) {
            typePrecision = type.Substring(type.IndexOf("(") + 1).Replace(")", "").Trim();
            type = type.Substring(0, type.IndexOf("(") );
        }

        try
        {
            switch (type)
            {
                case "bigint":
                    var sqlBigInt = new SqlInt64();
                    sqlBigInt = field.ToSqlInt64();
                    if (sqlBigInt.IsNull == false) result = true;
                    break;

                case "bit":
                    if(field.Value.Contains("+") || field.Value.Contains("-")) {
                        result = false;
                    } else {
                        var sqlBit = new SqlByte();
                        sqlBit = field.ToSqlByte();
                        if (sqlBit.IsNull == false && (sqlBit == 0 || sqlBit == 1)) result = true;
                    }
                    break;

                case "decimal":
                case "numeric":
                    // Support format decimal(x,0) or decimal(x,y) where true only if number fits in precision x,y
                    // Precision = maximum number of digits to the left of the decimal point
                    // If decimal(x,y) supplied, maximum precision = x - y
                    var sqlDecimal = new SqlDecimal();
                    sqlDecimal = field.ToSqlDecimal();
                    if(sqlDecimal.IsNull == false)
                    {
                        result = true;
                        if (typePrecision.Length > 0)
                        {
                            var parameters = typePrecision.Split(",".ToCharArray());
                            if (parameters.Length > 0)
                            {
                                int precision = 0;
                                int.TryParse(parameters[0], out precision);
                                if (precision > 0)
                                {
                                    if (parameters.Length > 1)
                                    {
                                        int scale = 0;
                                        int.TryParse(parameters[1], out scale);
                                        precision = precision - scale;
                                    }
                                    var x = " " + sqlDecimal.Value.ToString().Replace("-","") + ".";
                                    string decPrecisionDigitCount = x.Substring(0,x.IndexOf(".")).Trim();
                                    if(decPrecisionDigitCount.Length > precision) result = false;
                                }
                            }
                        }
                    }
                    break;

                case "float":
                    var sqlFloat = new SqlDouble();
                    sqlFloat = field.ToSqlDouble();
                    if (sqlFloat.IsNull == false) result = true;
                    break;

                case "int":
                    var sqlInt = new SqlInt32();
                    sqlInt = field.ToSqlInt32();
                    if (sqlInt.IsNull == false) result = true;
                    break;

                case "money":
                    var sqlMoney = new SqlMoney();
                    sqlMoney = field.ToSqlMoney();
                    if (sqlMoney.IsNull == false) result = true;
                    break;

                case "real":
                    var SqlSingle = new SqlSingle();
                    SqlSingle = field.ToSqlSingle();
                    if (SqlSingle.IsNull == false) result = true;
                    break;

                case "smallint":
                    var sqlSmallInt = new SqlInt16();
                    sqlSmallInt = field.ToSqlInt16();
                    if (sqlSmallInt.IsNull == false) result = true;
                    break;

                case "smallmoney":
                    var sqlSmallMoney = new SqlMoney();
                    sqlSmallMoney = field.ToSqlMoney();
                    if (sqlSmallMoney.IsNull == false) {
                        // Ensure that it will fit in a 4-byte small money
                        if (sqlSmallMoney.Value >= -214748.3648m && sqlSmallMoney.Value <= 214748.3647m) {
                            result = true;
                        }
                    }
                    break;

                case "tinyint":
                    var sqlTinyInt = new SqlByte();
                    sqlTinyInt = field.ToSqlByte();
                    if (sqlTinyInt.IsNull == false) result = true;
                    break;

                default:
                    errorMessage = "Unrecognized format";
                    break;
            }
        }
        catch (Exception)
        {
            if (string.IsNullOrEmpty(errorMessage) == false)
            {
                result = SqlBoolean.Null;
            }
        }
       return result;
    }
};

/// <summary>
/// Regular expresssions
/// </summary>
namespace SQLRegularExpression
{
    public static class RegExFunctions
    {
 
        [SqlFunction(IsDeterministic=true, IsPrecise=true)]
        public static SqlInt32 RegExOptions(SqlBoolean IgnoreCase, SqlBoolean Multiline, SqlBoolean ExplicitCapture, SqlBoolean Compiled,
            SqlBoolean Singleline, SqlBoolean IgnorePatternWhitespace, SqlBoolean RightToLeft, SqlBoolean ECMAScript, SqlBoolean CultureInvariant)
        {
            RegexOptions options;
 
            options = (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None)
                | (Multiline ? RegexOptions.Multiline : RegexOptions.None)
                | (ExplicitCapture ? RegexOptions.ExplicitCapture : RegexOptions.None)
                | (Compiled ? RegexOptions.Compiled : RegexOptions.None)
                | (Singleline ? RegexOptions.Singleline : RegexOptions.None)
                | (IgnorePatternWhitespace ? RegexOptions.IgnorePatternWhitespace : RegexOptions.None)
                | (RightToLeft ? RegexOptions.RightToLeft : RegexOptions.None)
                | (ECMAScript ? RegexOptions.ECMAScript : RegexOptions.None)
                | (CultureInvariant ? RegexOptions.CultureInvariant : RegexOptions.None);
 
            return (Int32) options;
        }
 
        [SqlFunction(IsDeterministic=true, IsPrecise=true)]
        public static SqlString RegExMatch(SqlString input, SqlString pattern, SqlInt32 options)
        {
            if (input.IsNull || pattern.IsNull) return null;
            return Regex.Match(input.Value, pattern.Value, (RegexOptions)options.Value).Value;
        }
 
        [SqlFunction(IsDeterministic=true, IsPrecise=true)]
        public static SqlInt32 RegExIndex(SqlString input, SqlString pattern, SqlInt32 options)
        {
            if (input.IsNull || pattern.IsNull) return 0;
            return Regex.Match(input.Value, pattern.Value, (RegexOptions)options.Value).Index;
        }
 
        [SqlFunction(IsDeterministic=true, IsPrecise=true)]
        public static SqlBoolean RegExIsMatch(SqlString input, SqlString pattern, SqlInt32 options)
        {
            if (input.IsNull || pattern.IsNull) return false;
            return Regex.IsMatch(input.Value, pattern.Value, (RegexOptions)options.Value);
        }
 
        [SqlFunction(IsDeterministic = true, IsPrecise = true)]
        public static SqlString RegExReplace(SqlString input, SqlString pattern, SqlString replacement, SqlInt32 options)
        {
            if (input.IsNull || pattern.IsNull) return null;
            return Regex.Replace(input.Value, pattern.Value, replacement.Value, (RegexOptions)options.Value);
        }
 
        [SqlFunction(IsDeterministic = true, IsPrecise = true)]
        public static SqlString RegExSqlReplace(SqlString input, SqlString pattern, SqlString replacement)
        {
            if (input.IsNull || pattern.IsNull) return null;
            return Regex.Replace(input.Value, pattern.Value, replacement.Value, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
 
        [SqlFunction(IsDeterministic=true, IsPrecise=true)]
        public static SqlString RegExEscape(SqlString input)
        {
            if (input.IsNull) return null;
            return Regex.Escape(input.Value);
        }
 
        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true, IsPrecise = true, Name = "RegExSplit",
            SystemDataAccess = SystemDataAccessKind.None, FillRowMethodName = "RegExSplitRow")]
        public static IEnumerable RegExSplit(SqlString input, SqlString pattern, SqlInt32 options)
        {
            if (input.IsNull || pattern.IsNull) return null;
            return Regex.Split(input.Value, pattern.Value, (RegexOptions)options.Value);
        }
 
        public static void RegExSplitRow(Object input, ref SqlString match)
        {
            match = new SqlString(input.ToString());
        }
 
        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true, IsPrecise = true, Name = "RegExMatches",
            SystemDataAccess = SystemDataAccessKind.None, FillRowMethodName = "RegExMatchesRow")]
        public static IEnumerable RegExMatches(SqlString input, SqlString pattern, SqlInt32 options)
        {
            if (input.IsNull || pattern.IsNull) return null;
            return Regex.Matches(input.Value, pattern.Value, (RegexOptions)options.Value);
        }
 
        public static void RegExMatchesRow(Object input, ref SqlString match, ref SqlInt32 matchIndex, ref SqlInt32 matchLength)
        {
            Match m = (Match)input;
            match = new SqlString(m.Value);
            matchIndex = m.Index;
            matchLength = m.Length;
        }
    }
};
