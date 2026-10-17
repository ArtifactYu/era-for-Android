//統合_netEmuera: .netEmuera の DICT_* と G_POLYGON_* を移植したもの。
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.UI.Game;
using MinorShift.Emuera.UI.Game.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.GameData.Function;

internal static partial class FunctionMethodCreator
{
	#region 統合_netEmuera_DICT

	/// <summary>
	/// DICT_* が使う名前付き辞書。キーは数値で、文字列キーはハッシュに潰される（.netEmuera と同じ）。
	/// セーブ対象ではないので実行中のみ有効。
	/// </summary>
	private static class ErbDictionary
	{
		static readonly Dictionary<string, Dictionary<long, object>> dictionaries = [];

		public static void Create(string name) => dictionaries[name] = [];
		public static bool Exists(string name) => dictionaries.ContainsKey(name);

		public static Dictionary<long, object> Require(string methodName, string name)
		{
			if (!dictionaries.TryGetValue(name, out var dic))
				throw new CodeEE(string.Format(trerror.DictNotFound.Text, methodName, name));
			return dic;
		}
	}

	/// <summary>キー引数を数値に揃える。文字列キーはハッシュ値を使う</summary>
	private static long ReadDictKey(string methodName, ExpressionMediator exm, List<AExpression> arguments, int index)
	{
		AExpression arg = arguments[index];
		if (arg.IsString)
			return arg.GetStrValue(exm).GetHashCode();
		if (arg.IsInteger)
			return arg.GetIntValue(exm);
		throw new CodeEE(string.Format(trerror.DictKeyType.Text, methodName));
	}

	public sealed class DictCreateMethod : FunctionMethod
	{
		public DictCreateMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			ErbDictionary.Create(arguments[0].GetStrValue(exm));
			return 1;
		}
	}

	public sealed class DictExistMethod : FunctionMethod
	{
		public DictExistMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return ErbDictionary.Exists(arguments[0].GetStrValue(exm)) ? 1 : 0;
		}
	}

	public sealed class DictContainsKeyMethod : FunctionMethod
	{
		public DictContainsKeyMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
				new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Any } },
			];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			long key = ReadDictKey(Name, exm, arguments, 1);
			return ErbDictionary.Require(Name, name).ContainsKey(key) ? 1 : 0;
		}
	}

	public sealed class DictSetValueMethod : FunctionMethod
	{
		public DictSetValueMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
				new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Any, ArgType.Any } },
			];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			long key = ReadDictKey(Name, exm, arguments, 1);
			AExpression value = arguments[2];
			if (value.IsString)
				ErbDictionary.Require(Name, name)[key] = value.GetStrValue(exm);
			else if (value.IsInteger)
				ErbDictionary.Require(Name, name)[key] = value.GetIntValue(exm);
			else
				throw new CodeEE(string.Format(trerror.DictValueType.Text, Name));
			return 1;
		}
	}

	public sealed class DictGetValueIntMethod : FunctionMethod
	{
		public DictGetValueIntMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
				new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Any } },
			];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			long key = ReadDictKey(Name, exm, arguments, 1);
			if (!ErbDictionary.Require(Name, name).TryGetValue(key, out object value))
				return 0;
			return value is long l ? l : throw new CodeEE(string.Format(trerror.DictValueIsNotInt.Text, Name));
		}
	}

	public sealed class DictGetValueStringMethod : FunctionMethod
	{
		public DictGetValueStringMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArrayEx = [
				new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Any } },
			];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			long key = ReadDictKey(Name, exm, arguments, 1);
			if (!ErbDictionary.Require(Name, name).TryGetValue(key, out object value))
				return "";
			return value is string s ? s : throw new CodeEE(string.Format(trerror.DictValueIsNotStr.Text, Name));
		}
	}
	#endregion

	#region 統合_netEmuera_G_POLYGON

	public sealed class GraphicsPolygonPointAddMethod : FunctionMethod
	{
		public GraphicsPolygonPointAddMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			Point p = ReadPoint(Name, exm, arguments, 1);
			g.GDrawPolygonAddPoint(p.X, p.Y);
			return 1;
		}
	}

	public sealed class GraphicsPolygonPointClearMethod : FunctionMethod
	{
		public GraphicsPolygonPointClearMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			g.GDrawPolygonClearPoint();
			return 1;
		}
	}

	public sealed class GraphicsDrawPolygonMethod : FunctionMethod
	{
		public GraphicsDrawPolygonMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			g.GDrawPolygon();
			return 1;
		}
	}

	public sealed class GraphicsFillPolygonMethod : FunctionMethod
	{
		public GraphicsFillPolygonMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			g.GFillPolygon();
			return 1;
		}
	}
	#endregion
}
