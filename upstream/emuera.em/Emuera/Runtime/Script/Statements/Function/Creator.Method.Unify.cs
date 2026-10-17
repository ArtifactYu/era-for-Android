//統合_netEmuera: .netEmuera (gitlab.com/alnatiyan/EmueraDotNet) 由来の式中関数。
//上流の Creator.Method.cs を触らずに済むよう、partial の別ファイルとして置いている。
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Text;

namespace MinorShift.Emuera.GameData.Function;

internal static partial class FunctionMethodCreator
{
	/// <summary>
	/// GETCSVNOBYNAME / GETCSVNOBYNICKNAME / GETCSVNOBYCALLNAME / GETCSVNOBYMASTERNAME
	/// 名前からキャラのCSV番号を引く。該当なしは -1。
	/// </summary>
	private sealed class GetCsvNoMethod : FunctionMethod
	{
		readonly CharacterStrData charaStr;

		public GetCsvNoMethod(CharacterStrData data)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
			charaStr = data;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			ConstantData constant = exm.VEvaluator.Constant;
			IReadOnlyDictionary<string, long> map = charaStr switch
			{
				CharacterStrData.NAME => constant.NameToTemplateMap,
				CharacterStrData.NICKNAME => constant.NicknameToTemplateMap,
				CharacterStrData.CALLNAME => constant.CallnameToTemplateMap,
				CharacterStrData.MASTERNAME => constant.MasternameToTemplateMap,
				_ => throw new ExeEE("error"),
			};
			return map.TryGetValue(str, out long no) ? no : -1;
		}
	}

	/// <summary>HASH_XXH3(文字列) — UTF-8バイト列のxxHash3を符号付き64bitで返す</summary>
	private sealed class XXH3Method : FunctionMethod
	{
		public XXH3Method()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string source = arguments[0].GetStrValue(exm);
			return (long)XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(source));
		}
	}

	/// <summary>HASH_XXH32(文字列) — UTF-8バイト列のxxHash32</summary>
	private sealed class XXH32Method : FunctionMethod
	{
		public XXH32Method()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string source = arguments[0].GetStrValue(exm);
			return XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(source));
		}
	}
}
