//統合_netEmuera: SQL_* の式中関数。実体は Runtime/Utils/ErbSql.cs。
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MinorShift.Emuera.GameData.Function;

internal static partial class FunctionMethodCreator
{
	#region 統合_netEmuera_SQL

	public sealed class SqlConnectionOpenMethod : FunctionMethod
	{
		public SqlConnectionOpenMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			ErbSql.ConnectionOpen(arguments[0].GetStrValue(exm));
			return 1;
		}
	}

	public sealed class SqlExecuteReaderMethod : FunctionMethod
	{
		public SqlExecuteReaderMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			ErbSql.ExecuteReader(Name, arguments[0].GetIntValue(exm), arguments[1].GetStrValue(exm));
			return 1;
		}
	}

	public sealed class SqlExecuteScalerLongMethod : FunctionMethod
	{
		public SqlExecuteScalerLongMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			object value = ErbSql.ExecuteScalar(Name, arguments[0].GetStrValue(exm));
			//結果なし・NULL は 0 にする
			return value == null || value is DBNull ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}
	}

	public sealed class SqlExecuteScalerStringMethod : FunctionMethod
	{
		public SqlExecuteScalerStringMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			object value = ErbSql.ExecuteScalar(Name, arguments[0].GetStrValue(exm));
			return value == null || value is DBNull ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
		}
	}

	public sealed class SqlExecuteNonQueryMethod : FunctionMethod
	{
		public SqlExecuteNonQueryMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			ErbSql.ExecuteNonQuery(Name, arguments[0].GetStrValue(exm));
			return 0;
		}
	}

	/// <summary>
	/// 次の行へ進む。.netEmuera に合わせて、行があったとき 0・尽きたとき 1 を返す
	/// （戻り値の意味が他の SQL_* と逆なので注意）
	/// </summary>
	public sealed class SqlReaderReadMethod : FunctionMethod
	{
		public SqlReaderReadMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return ErbSql.ReaderRead(Name, arguments[0].GetIntValue(exm)) ? 0 : 1;
		}
	}

	public sealed class SqlReaderGetLongMethod : FunctionMethod
	{
		public SqlReaderGetLongMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return ErbSql.ReaderGetLong(Name, arguments[0].GetIntValue(exm), (int)arguments[1].GetIntValue(exm));
		}
	}

	public sealed class SqlReaderGetStringMethod : FunctionMethod
	{
		public SqlReaderGetStringMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return ErbSql.ReaderGetString(Name, arguments[0].GetIntValue(exm), (int)arguments[1].GetIntValue(exm));
		}
	}

	public sealed class SqlReaderIsNullMethod : FunctionMethod
	{
		public SqlReaderIsNullMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return ErbSql.ReaderIsNull(Name, arguments[0].GetIntValue(exm), (int)arguments[1].GetIntValue(exm)) ? 1 : 0;
		}
	}
	#endregion
}
