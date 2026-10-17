using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script;
using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;


namespace MinorShift.Emuera.GameProc.Function;

internal abstract class ArgumentBuilder
{
	protected static void assignwarn(string mes, InstructionLine line, int level, bool isBackComp)
	{
		bool isError = level >= 2;
		if (isError)
		{
			line.IsError = true;
			line.ErrMes = mes;
		}
		ParserMediator.Warn(mes, line, level, isError, isBackComp);
	}
	protected static void warn(string mes, InstructionLine line, int level, bool isBackComp)
	{
		mes = line.Function.Name + trerror.Instruction.Text + mes;
		bool isError = level >= 2;
		if (isError)
		{
			line.IsError = true;
			line.ErrMes = mes;
		}
		ParserMediator.Warn(mes, line, level, isError, isBackComp);
	}
	/// <summary>
	/// 引数の型と数。typeof(void)で任意の型（あるいは個別にチェックするべき引数）。nullでその引数は省略可能
	/// </summary>
	protected Type[] argumentTypeArray;//
	/// <summary>
	/// 最低限必要な引数の数。設定しないと全て省略不可。
	/// </summary>
	protected int minArg = -1;
	/// <summary>
	/// 引数の数に制限なし。
	/// </summary>
	protected bool argAny;
	protected bool checkArgumentType(InstructionLine line, ExpressionMediator exm, List<AExpression> arguments)
	{
		if (arguments == null)
		{
			warn(trerror.MissingArg.Text, line, 2, false);
			return false;
		}
		if (
			arguments.Count < minArg ||
			((arguments.Count < argumentTypeArray.Length) && (minArg < 0))
			)
		{
			warn(trerror.NotEnoughArguments.Text, line, 2, false);
			return false;
		}
		int length = arguments.Count;
		if ((arguments.Count > argumentTypeArray.Length) && (!argAny))
		{
			warn(trerror.TooManyArg.Text, line, 1, false);
			length = argumentTypeArray.Length;
		}
		for (int i = 0; i < length; i++)
		{
			Type allowType;
			if ((!argAny) && (argumentTypeArray[i] == null))
				continue;
			else if (argAny && i >= argumentTypeArray.Length)
				allowType = argumentTypeArray[^1];
			else
				allowType = argumentTypeArray[i];
			if (arguments[i] == null)
			{
				if (allowType == null)
					continue;
				warn(string.Format(trerror.CanNotRecognizeArg.Text, (i + 1).ToString()), line, 2, false);
				return false;
			}
			if ((allowType != typeof(void)) && (allowType != arguments[i].GetOperandType()))
			{
				warn(string.Format(trerror.IncorrectArg.Text, (i + 1).ToString()), line, 2, false);
				return false;
			}
		}
		length = arguments.Count;
		for (int i = 0; i < length; i++)
		{
			if (arguments[i] == null)
				continue;
			arguments[i] = arguments[i].Restructure(exm);
		}
		return true;
	}

	protected static VariableTerm getChangeableVariable(List<AExpression> terms, int i, InstructionLine line)
	{
		if (!(terms[i - 1] is VariableTerm varTerm))
		{
			warn(string.Format(trerror.ArgIsNotVariable.Text, i), line, 2, false);
			return null;
		}
		else if (varTerm.Identifier.IsConst)
		{
			warn(string.Format(trerror.ArgIsConst.Text, i), line, 2, false);
			return null;
		}
		return varTerm;
	}

	protected static WordCollection popWords(InstructionLine line)
	{
		CharStream st = line.PopArgumentPrimitive();
		return LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
	}

	protected static List<AExpression> popTerms(InstructionLine line)
	{
		CharStream st = line.PopArgumentPrimitive();
		WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
		return ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
	}
	public abstract Argument CreateArgument(InstructionLine line, ExpressionMediator exm);
}


internal static partial class ArgumentParser
{
	readonly static Dictionary<FunctionArgType, ArgumentBuilder> argb = [];

	public static Dictionary<FunctionArgType, ArgumentBuilder> GetArgumentBuilderDictionary()
	{
		return argb;
	}
	public static ArgumentBuilder GetArgumentBuilder(FunctionArgType key)
	{
		return argb[key];
	}
	readonly static Dictionary<string, ArgumentBuilder> nargb = [];

	/// <summary>
	/// 一般的な引数作成器の呼び出し。数式と文字列式のいずれかのみを引数とし、特殊なチェックが必要ないもの
	/// </summary>
	/// <param name="argstr">大文字のIとSで"IIS"で(int, int, string )のように引数の数と順序を指定する。</param>
	/// <param name="minArg">引数の最低数。これ以降は省略可能</param>
	/// <returns></returns>
	public static ArgumentBuilder GetNormalArgumentBuilder(string argstr, int minArg)
	{
		if (minArg < 0)
			minArg = argstr.Length;
		string key = argstr + minArg.ToString();
		if (nargb.TryGetValue(key, out ArgumentBuilder value))
			return value;
		Type[] types = new Type[argstr.Length];
		for (int i = 0; i < argstr.Length; i++)
		{
			if (argstr[i] == 'I')
				types[i] = typeof(long);
			else if (argstr[i] == 'S')
				types[i] = typeof(string);
			else
				throw new ExeEE(trerror.AbnormalSpecification.Text);
		}
		ArgumentBuilder newarg = new Expressions_ArgumentBuilder(types, minArg);
		nargb.Add(key, newarg);
		return newarg;
	}
	static ArgumentParser()
	{
		argb[FunctionArgType.METHOD] = new METHOD_ArgumentBuilder();
		argb[FunctionArgType.VOID] = new VOID_ArgumentBuilder();
		argb[FunctionArgType.INT_EXPRESSION] = new INT_EXPRESSION_ArgumentBuilder(false);
		argb[FunctionArgType.INT_EXPRESSION_NULLABLE] = new INT_EXPRESSION_ArgumentBuilder(true);
		argb[FunctionArgType.STR_EXPRESSION] = new STR_EXPRESSION_ArgumentBuilder(false);
		argb[FunctionArgType.STR_EXPRESSION_NULLABLE] = new STR_EXPRESSION_ArgumentBuilder(true);
		argb[FunctionArgType.STR] = new STR_ArgumentBuilder(false);
		argb[FunctionArgType.STR_NULLABLE] = new STR_ArgumentBuilder(true);
		argb[FunctionArgType.FORM_STR] = new FORM_STR_ArgumentBuilder(false);
		argb[FunctionArgType.FORM_STR_NULLABLE] = new FORM_STR_ArgumentBuilder(true);
		argb[FunctionArgType.SP_PRINTV] = new SP_PRINTV_ArgumentBuilder();
		argb[FunctionArgType.SP_TIMES] = new SP_TIMES_ArgumentBuilder();
		argb[FunctionArgType.SP_BAR] = new SP_BAR_ArgumentBuilder();
		argb[FunctionArgType.SP_SET] = new SP_SET_ArgumentBuilder();
		argb[FunctionArgType.SP_SETS] = new SP_SET_ArgumentBuilder();
		argb[FunctionArgType.SP_SWAP] = new SP_SWAP_ArgumentBuilder(false);
		argb[FunctionArgType.SP_VAR] = new SP_VAR_ArgumentBuilder();
		argb[FunctionArgType.SP_SAVEDATA] = new SP_SAVEDATA_ArgumentBuilder();
		argb[FunctionArgType.SP_TINPUT] = new SP_TINPUT_ArgumentBuilder();
		argb[FunctionArgType.SP_TINPUTS] = new SP_TINPUTS_ArgumentBuilder();
		argb[FunctionArgType.SP_SORTCHARA] = new SP_SORTCHARA_ArgumentBuilder();
		argb[FunctionArgType.SP_CALL] = new SP_CALL_ArgumentBuilder(false, false);
		argb[FunctionArgType.SP_CALLF] = new SP_CALL_ArgumentBuilder(true, false);
		argb[FunctionArgType.SP_CALLFORM] = new SP_CALL_ArgumentBuilder(false, true);
		argb[FunctionArgType.SP_CALLFORMF] = new SP_CALL_ArgumentBuilder(true, true);
		argb[FunctionArgType.SP_CALLCSHARP] = new SP_CALLSHARP_ArgumentBuilder();
		argb[FunctionArgType.SP_FOR_NEXT] = new SP_FOR_NEXT_ArgumentBuilder();
		argb[FunctionArgType.SP_POWER] = new SP_POWER_ArgumentBuilder();
		argb[FunctionArgType.SP_SWAPVAR] = new SP_SWAPVAR_ArgumentBuilder();
		argb[FunctionArgType.EXPRESSION] = new EXPRESSION_ArgumentBuilder(false);
		argb[FunctionArgType.EXPRESSION_NULLABLE] = new EXPRESSION_ArgumentBuilder(true);
		argb[FunctionArgType.CASE] = new CASE_ArgumentBuilder();
		argb[FunctionArgType.VAR_INT] = new VAR_INT_ArgumentBuilder();
		argb[FunctionArgType.VAR_STR] = new VAR_STR_ArgumentBuilder();
		argb[FunctionArgType.BIT_ARG] = new BIT_ARG_ArgumentBuilder();
		argb[FunctionArgType.SP_VAR_SET] = new SP_VAR_SET_ArgumentBuilder();
		argb[FunctionArgType.SP_BUTTON] = new SP_BUTTON_ArgumentBuilder();
		argb[FunctionArgType.SP_COLOR] = new SP_COLOR_ArgumentBuilder();
		argb[FunctionArgType.SP_SPLIT] = new SP_SPLIT_ArgumentBuilder();
		argb[FunctionArgType.SP_GETINT] = new SP_GETINT_ArgumentBuilder();
		argb[FunctionArgType.SP_CVAR_SET] = new SP_CVAR_SET_ArgumentBuilder();
		argb[FunctionArgType.SP_CONTROL_ARRAY] = new SP_CONTROL_ARRAY_ArgumentBuilder();
		argb[FunctionArgType.SP_SHIFT_ARRAY] = new SP_SHIFT_ARRAY_ArgumentBuilder();
		argb[FunctionArgType.SP_SORTARRAY] = new SP_SORT_ARRAY_ArgumentBuilder();
		argb[FunctionArgType.INT_ANY] = new INT_ANY_ArgumentBuilder();
		argb[FunctionArgType.FORM_STR_ANY] = new FORM_STR_ANY_ArgumentBuilder();
		argb[FunctionArgType.SP_COPYCHARA] = new SP_SWAP_ArgumentBuilder(true);
		argb[FunctionArgType.SP_INPUT] = new SP_INPUT_ArgumentBuilder();
		argb[FunctionArgType.SP_INPUTS] = new SP_INPUTS_ArgumentBuilder();
		argb[FunctionArgType.SP_COPY_ARRAY] = new SP_COPY_ARRAY_Arguments();
		argb[FunctionArgType.SP_SAVEVAR] = new SP_SAVEVAR_ArgumentBuilder();
		argb[FunctionArgType.SP_SAVECHARA] = new SP_SAVECHARA_ArgumentBuilder();
		argb[FunctionArgType.SP_REF] = new SP_REF_ArgumentBuilder(false);
		argb[FunctionArgType.SP_REFBYNAME] = new SP_REF_ArgumentBuilder(true);
		argb[FunctionArgType.SP_HTMLSPLIT] = new SP_HTMLSPLIT_ArgumentBuilder();

		#region EM_私家版_HTMLパラメータ拡張
		argb[FunctionArgType.SP_PRINT_IMG] = new SP_PRINT_IMG_ArgumentBuilder();
		argb[FunctionArgType.SP_PRINT_RECT] = new SP_PRINT_SHAPE_ArgumentBuilder(4);
		argb[FunctionArgType.SP_PRINT_SPACE] = new SP_PRINT_SHAPE_ArgumentBuilder(1);
		#endregion

		#region EM_DT
		argb[FunctionArgType.SP_DT_COLUMN_OPTIONS] = new SP_DT_COLUMN_OPTIONS_ArgumentBuilder();
		#endregion
		#region EM_私家版_HTML_PRINT拡張
		argb[FunctionArgType.SP_HTML_PRINT] = new SP_HTML_PRINT_ArgumentBuilder();
		#endregion
		#region 統合_netEmuera
		argb[FunctionArgType.SP_MATCHALL] = new SP_MATCHALL_ArgumentBuilder();
		#endregion
		#region 統合_netEmuera_island層
		argb[FunctionArgType.SP_HTML_PRINT_ISLAND_CLEAR] = new SP_HTML_PRINT_ISLAND_CLEAR_ArgumentBuilder();
		#endregion
	}

	#region 統合_netEmuera_island層
	/// <summary>
	/// HTML_PRINT_ISLAND_CLEAR（<層>）。層を省略したら島を全部消す
	/// </summary>
	private sealed class SP_HTML_PRINT_ISLAND_CLEAR_ArgumentBuilder : ArgumentBuilder
	{
		public SP_HTML_PRINT_ISLAND_CLEAR_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long)];
			minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			List<AExpression> terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			if (terms.Count == 0 || terms[0] == null)
				return new SpIslandClear(null);
			return new SpIslandClear(terms[0]);
		}
	}
	#endregion

	#region 統合_netEmuera
	private sealed class SP_MATCHALL_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			List<AExpression> terms = popTerms(line);
			int count = 0;
			if (terms.Count <= count)
			{
				warn("引数がありません", line, 2, false);
				return null;
			}
			++count;
			VariableTerm varTerm = getChangeableVariable(terms, 1, line);
			VariableToken token = varTerm?.Identifier;
			if (token == null)
			{
				warn("第一引数が無効な変数指定です", line, 2, false);
				return null;
			}
			Type targetType = token.VariableType;
			if (token.IsArray3D || token.IsArray2D)
			{
				warn("変数の次元が大きすぎます", line, 2, false);
				return null;
			}
			if (!token.IsCharacterData && !token.IsArray1D)
			{
				warn("非キャラクタ変数が対象の場合は1次元である必要があります", line, 2, false);
				return null;
			}

			//1次元キャラクタ変数はキャラを走査するので、変数側の添字を定数で受け取る
			long? indexValue = null;
			if (token.IsCharacterData && token.IsArray1D)
			{
				if (terms.Count <= count)
				{
					warn("1次元キャラクタ変数に対するインデックス指定がありません", line, 2, false);
					return null;
				}
				if (terms[count] is not SingleLongTerm indexTerm)
				{
					warn("1次元キャラクタ変数に対するインデックス指定は整数定数である必要があります", line, 2, false);
					return null;
				}
				++count;
				indexValue = indexTerm.GetIntValue(exm);
				if (indexValue < 0)
				{
					warn("インデックスが負の値です", line, 2, false);
					return null;
				}
			}

			if (terms.Count <= count)
			{
				warn("探す値の指定がありません", line, 2, false);
				return null;
			}
			AExpression value = terms[count];
			++count;
			if (value.GetOperandType() != targetType)
			{
				warn("検索する値と対象変数の型が一致しません", line, 2, false);
				return null;
			}

			AExpression beg = null;
			AExpression end = null;
			if (terms.Count > count)
			{
				beg = terms[count];
				if (!beg.IsInteger)
				{
					warn("範囲指定は整数値である必要があります", line, 2, false);
					return null;
				}
				++count;
			}
			if (terms.Count > count)
			{
				end = terms[count];
				if (!end.IsInteger)
				{
					warn("範囲指定は整数値である必要があります", line, 2, false);
					return null;
				}
				++count;
			}
			if (terms.Count > count)
			{
				warn("引数が多すぎます", line, 2, false);
				return null;
			}
			return new SpMatchAllArgument(token, value, beg, end, indexValue);
		}
	}
	#endregion

	#region EM_私家版_HTMLパラメータ拡張
	private sealed class SP_PRINT_IMG_ArgumentBuilder : ArgumentBuilder
	{
		public SP_PRINT_IMG_ArgumentBuilder()
		{
			argumentTypeArray = null;// new Type[] { typeof(string), typeof(string), typeof(Int64), typeof(Int64), typeof(Int64) };
			minArg = 1;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var wc = popWords(line);
			AExpression name, nameb = null, namem = null;
			List<MixedIntegerExprTerm> param = [];
			if (!wc.EOL)
			{
				name = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
				if (name == null)
				{
					warn(string.Format(trerror.CanNotOmitArg.Text, "1"), line, 2, false);
					return null;
				}
				if (Config.NeedReduceArgumentOnLoad) name = name.Restructure(exm);
				wc.ShiftNext();
			}
			else
			{
				warn(string.Format(trerror.CanNotOmitArg.Text, "1"), line, 2, false);
				return null;
			}
			int argCount = 2;
			while (!wc.EOL)
			{
				if (param.Count == 3)
				{
					warn(trerror.TooManyArg.Text, line, 2, false);
					return null;
				}
				var arg = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma | TermEndWith.KeyWordPx);
				if (Config.NeedReduceArgumentOnLoad && arg != null) arg = arg.Restructure(exm);
				if (arg.GetOperandType() == typeof(string))
				{
					if (param.Count > 0 || argCount > 3)
					{
						warn(string.Format(trerror.IncorrectArg.Text, argCount), line, 2, false);
						return null;
					}
					switch (argCount)
					{
						case 2: nameb = arg; break;
						case 3: namem = arg; break;
					}
				}
				else
					param.Add(new MixedIntegerExprTerm { num = arg, isPx = wc.Current.Type != '\0' && wc.Current.Type != ',' });
				if (wc.Current.Type != '\0' && wc.Current.Type != ',') wc.ShiftNext();
				wc.ShiftNext();
				argCount++;
			}

			return new SpPrintImgArgument(name, nameb, namem, param.Count > 0 ? param.ToArray() : null);
		}
	}
	private sealed class SP_PRINT_SHAPE_ArgumentBuilder : ArgumentBuilder
	{
		public SP_PRINT_SHAPE_ArgumentBuilder(int max)
		{
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(long)];
			minArg = 1;
			maxArg = max;
		}
		int maxArg;
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var wc = popWords(line);
			List<MixedIntegerExprTerm> param = [];

			while (!wc.EOL)
			{
				var arg = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma | TermEndWith.KeyWordPx);
				if (Config.NeedReduceArgumentOnLoad) arg = arg.Restructure(exm);
				param.Add(new MixedIntegerExprTerm { num = arg, isPx = wc.Current.Type != '\0' && wc.Current.Type != ',' });
				if (wc.Current.Type != '\0' && wc.Current.Type != ',') wc.ShiftNext();
				wc.ShiftNext();

				if (param.Count > maxArg)
				{
					warn(trerror.TooManyArg.Text, line, 1, false);
				}
			}

			if (param.Count != 1 && param.Count != 4)
			{
				warn(trerror.DifferentArgsCount.Text, line, 2, false);
				return null;
			}

			var terms = new AExpression[param.Count];
			for (int i = 0; i < param.Count; i++) terms[i] = param[i].num;
			//if (!checkArgumentType(line, exm, terms)) return null;

			return new SpPrintShapeArgument(param.ToArray());
		}
	}
	#endregion
	#region EM_私家版_HTML_PRINT拡張
	private sealed class SP_HTML_PRINT_ArgumentBuilder : ArgumentBuilder
	{
		public SP_HTML_PRINT_ArgumentBuilder()
		{
			argumentTypeArray = null;// new Type[] { typeof(string), typeof(string), typeof(Int64), typeof(Int64), typeof(Int64) };
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AnalyzePrintV);
			List<AExpression> args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
			if (args.Count < 1)
			{
				warn(trerror.NotEnoughArguments.Text, line, 2, false);
				return null;
			}
			if (args.Count > 2)
			{
				warn(trerror.TooManyArg.Text, line, 2, false);
				return null;
			}
			bool constStr = false, constInt = true;
			for (int i = 0; i < args.Count; i++)
			{
				if (i == 0 && args[i] == null)
				{ warn(string.Format(trerror.CanNotOmitArg.Text, i + 1), line, 2, false); return null; }

				if (i == 0 && args[i].GetOperandType() != typeof(string))
				{ warn(string.Format(trerror.IncorrectArg.Text, i + 1), line, 2, false); return null; }
				if (i == 1 && args[i].GetOperandType() != typeof(long))
				{ warn(string.Format(trerror.IncorrectArg.Text, i + 1), line, 2, false); return null; }

				args[i] = args[i].Restructure(exm);
				if (i == 0 && args[i] is SingleTerm) constStr = true;
				if (i == 1 && !(args[i] is SingleTerm)) constInt = false;
			}
			var ret = new SpHtmlPrint(args[0], args.Count > 1 ? args[1] : null);
			if (constStr && constInt)
			{
				ret.ConstInt = args.Count > 1 ? args[1].GetIntValue(exm) : 0;
				ret.ConstStr = args[0].GetStrValue(exm);
			}
			return ret;
		}
	}
	#endregion
	#region EM_DT
	private sealed class SP_DT_COLUMN_OPTIONS_ArgumentBuilder : ArgumentBuilder
	{
		public SP_DT_COLUMN_OPTIONS_ArgumentBuilder()
		{
			argumentTypeArray = null;// new Type[] { typeof(string), typeof(string), typeof(Int64), typeof(Int64), typeof(Int64) };
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var wc = popWords(line);
			AExpression dt, colum;
			if (!wc.EOL)
			{
				dt = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
				if (dt == null)
				{
					warn(string.Format(trerror.CanNotOmitArg.Text, "1"), line, 2, false);
					return null;
				}
				if (Config.NeedReduceArgumentOnLoad) dt = dt.Restructure(exm);
				wc.ShiftNext();
			}
			else
			{
				warn(string.Format(trerror.CanNotOmitArg.Text, "1"), line, 2, false);
				return null;
			}
			if (!wc.EOL)
			{
				colum = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
				if (colum == null)
				{
					warn(string.Format(trerror.CanNotOmitArg.Text, "1"), line, 2, false);
					return null;
				}
				if (Config.NeedReduceArgumentOnLoad) colum = colum.Restructure(exm);
				wc.ShiftNext();
			}
			else
			{
				warn(string.Format(trerror.CanNotOmitArg.Text, "1"), line, 2, false);
				return null;
			}
			List<SpDtColumnOptions.DTOptions> opts = [];
			List<AExpression> values = [];
			int argCount = 3;
			while (!wc.EOL)
			{
				AExpression v = null;
				string keyword = wc.Current.ToString().ToLower();
				wc.ShiftNext(); // keyword
				wc.ShiftNext(); // ,
				if (wc.EOL)
				{
					warn(string.Format(trerror.NotEnoughArguments.Text), line, 2, false);
					return null;
				}
				argCount++;
				switch (keyword)
				{
					case "default":
						opts.Add(SpDtColumnOptions.DTOptions.Default);
						v = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
						wc.ShiftNext();
						break;
					default:
						warn(string.Format(trerror.CanNotInterpreted.Text), line, 2, false);
						return null;
				}
				if (v == null)
				{
					warn(string.Format(trerror.CanNotOmitArg.Text, argCount), line, 2, false);
					return null;
				}
				Type type = null;
				//switch (keyword)
				//{
				//	case "default": type = typeof(Int64);  break;
				//}
				if (type != null && type != v.GetOperandType())
				{
					warn(string.Format(trerror.IncorrectArg.Text, argCount), line, 2, false);
					continue;
				}
				if (Config.NeedReduceArgumentOnLoad) v = v.Restructure(exm);
				values.Add(v);
				argCount++;
			}
			if (opts.Count == 0)
			{
				warn(string.Format(trerror.NotEnoughArguments.Text), line, 2, false);
				return null;
			}
			return new SpDtColumnOptions(dt, colum, opts.ToArray(), values.ToArray());
		}
	}
	#endregion

	private sealed class SP_PRINTV_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AnalyzePrintV);
			var args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
			for (int i = 0; i < args.Count; i++)
			{
				if (args[i] == null)
				{
					warn(string.Format(trerror.CanNotOmitArg.Text, i + 1), line, 2, false);
					return null;
				}
				else
					args[i] = args[i].Restructure(exm);
			}
			return new SpPrintVArgument(args);
		}
	}

	private sealed class SP_TIMES_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.Comma, LexAnalyzeFlag.None);
			st.ShiftNext();
			if (st.EOS)
			{
				warn(trerror.NotEnoughArguments.Text, line, 2, false);
				return null;
			}
			double d;
			try
			{
				LexicalAnalyzer.SkipWhiteSpace(st);
				d = LexicalAnalyzer.ReadDouble(st);
				LexicalAnalyzer.SkipWhiteSpace(st);
				if (!st.EOS)
					warn(trerror.TooManyArg.Text, line, 1, false);
			}
			catch
			{
				warn(string.Format(trerror.ArgIsNotRealNumber.Text, "2"), line, 1, false);
				d = 0.0;
			}
			AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
			if (term == null)
			{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
			if (!(term.Restructure(exm) is VariableTerm varTerm))
			{ warn(string.Format(trerror.ArgIsNotVariable.Text, "1"), line, 2, false); return null; }
			else if (varTerm.IsString)
			{ warn(string.Format(trerror.ArgIsStrVar.Text, "1"), line, 2, false); return null; }
			else if (varTerm.Identifier.IsConst)
			{ warn(string.Format(trerror.ArgIsConst.Text, "1"), line, 2, false); return null; }
			return new SpTimesArgument(varTerm, d);
		}
	}

	private sealed class FORM_STR_ANY_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			Argument ret;
			CharStream st = line.PopArgumentPrimitive();
			List<AExpression> termList = [];
			LexicalAnalyzer.SkipHalfSpace(st);
			if (st.EOS)
			{
				if (line.FunctionCode == FunctionCode.RETURNFORM)
				{
					termList.Add(new SingleStrTerm("0"));
					ret = new ExpressionArrayArgument(termList)
					{
						IsConst = true,
						ConstInt = 0
					};
					return ret;
				}
				warn(trerror.MissingArg.Text, line, 2, false);
				return null;
			}
			while (true)
			{
				StrFormWord sfwt = LexicalAnalyzer.AnalyseFormattedString(st, FormStrEndWith.Comma, false);
				AExpression term = ExpressionParser.ToStrFormTerm(sfwt);
				term = term.Restructure(exm);
				termList.Add(term);
				st.ShiftNext();
				if (st.EOS)
					break;
				LexicalAnalyzer.SkipHalfSpace(st);
				if (st.EOS)
				{
					warn(trerror.MissingArgAfterComma.Text, line, 1, false);
					break;
				}
			}
			return new ExpressionArrayArgument(termList);
		}
	}

	private sealed class VOID_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			LexicalAnalyzer.SkipWhiteSpace(st);
			if (!st.EOS)
				warn(trerror.ArgIsNotRequired.Text, line, 1, false);
			return new VoidArgument();
		}
	}

	private sealed class STR_ArgumentBuilder : ArgumentBuilder
	{
		public STR_ArgumentBuilder(bool nullable)
		{
			this.nullable = nullable;
		}

		readonly bool nullable;
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			string rowStr;
			if (st.EOS)
			{
				if (!nullable)
				{
					warn(trerror.MissingArg.Text, line, 2, false);
					return null;
				}
				rowStr = "";
				//1756 処理変更のために完全に見分けが付かなくなってしまった
				//if (line.FunctionCode == FunctionCode.PRINTL)
				//	warn("PRINTLの後ろに空白がありません(eramaker：\'PRINTL\'を表示)", line, 0, true);
			}
			else
				rowStr = st.Substring();
			if (line.FunctionCode == FunctionCode.SETCOLORBYNAME || line.FunctionCode == FunctionCode.SETBGCOLORBYNAME)
			{
				Color c = Color.FromName(rowStr);
				if (c.A == 0)
				{
					if (rowStr.Equals("transparent", StringComparison.OrdinalIgnoreCase))
						throw new CodeEE(trerror.TransparentUnsupported.Text);
					throw new CodeEE(string.Format(trerror.InvalidColorName.Text, rowStr));
				}

			}
			Argument ret = new ExpressionArgument(new SingleStrTerm(rowStr))
			{
				ConstStr = rowStr,
				IsConst = true
			};
			return ret;
		}
	}

	private sealed class FORM_STR_ArgumentBuilder : ArgumentBuilder
	{
		public FORM_STR_ArgumentBuilder(bool nullable)
		{
			this.nullable = nullable;
		}

		readonly bool nullable;

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			Argument ret;
			if (st.EOS)
			{
				if (!nullable)
				{
					warn(trerror.MissingArg.Text, line, 2, false);
					return null;
				}
				//if (line.FunctionCode == FunctionCode.PRINTFORML)
				//	warn("PRINTFORMLの後ろに空白がありません(eramaker：\'PRINTFORML\'を表示)", line, 0, true);
				ret = new ExpressionArgument(new SingleStrTerm(""))
				{
					ConstStr = "",
					IsConst = true
				};
				return ret;
			}
			StrFormWord sfwt = LexicalAnalyzer.AnalyseFormattedString(st, FormStrEndWith.EoL, false);
			AExpression term = ExpressionParser.ToStrFormTerm(sfwt);
			term = term.Restructure(exm);
			ret = new ExpressionArgument(term);
			if (term is SingleTerm)
			{
				ret.ConstStr = term.GetStrValue(exm);
				ret.IsConst = true;
			}
			return ret;
		}
	}

	private sealed class SP_VAR_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			IdentifierWord iw = LexicalAnalyzer.ReadSingleIdentifierWord(st);
			if (iw == null)
			{ warn(string.Format(trerror.CanNotRecognizeArg.Text, "1"), line, 2, false); return null; }
			string idStr = iw.Code;
			VariableToken id = GlobalStatic.IdentifierDictionary.GetVariableToken(idStr, null, true);
			if (id == null)
			{ warn(string.Format(trerror.ArgIsNotVariable.Text, "1"), line, 2, false); return null; }
			else if ((!id.IsArray1D && !id.IsArray2D && !id.IsArray3D) || (id.Code == VariableCode.RAND))
			{ warn(string.Format(trerror.ArgIsNotArrayVar.Text, "1"), line, 2, false); return null; }
			LexicalAnalyzer.SkipWhiteSpace(st);
			if (!st.EOS)
			{
				warn(trerror.ExtraCharacterAfterArg.Text, line, 1, false);
			}
			return new SpVarsizeArgument(id);
		}
	}

	private sealed class SP_SORTCHARA_ArgumentBuilder : ArgumentBuilder
	{

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			VariableTerm varTerm = new(GlobalStatic.VariableData.GetSystemVariableToken("NO"), [new SingleLongTerm(0)]);
			SortOrder order = SortOrder.ASCENDING;
			WordCollection wc = popWords(line);
			if (wc.EOL)
			{
				return new SpSortcharaArgument(varTerm, order);
			}
			if ((wc.Current is IdentifierWord id) && (id.Code.Equals("FORWARD", Config.StringComparison)
				|| id.Code.Equals("BACK", Config.StringComparison)))
			{
				if (id.Code.Equals("BACK", Config.StringComparison))
					order = SortOrder.DESENDING;
				wc.ShiftNext();
				if (!wc.EOL)
					warn(trerror.TooManyArg.Text, line, 1, false);
			}
			else
			{
				AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
				if (term == null)
				{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
				varTerm = term.Restructure(exm) as VariableTerm;
				if (varTerm == null)
				{ warn(string.Format(trerror.ArgIsNotVariable.Text, "1"), line, 2, false); return null; }
				else if (!varTerm.Identifier.IsCharacterData)
				{ warn(string.Format(trerror.ArgIsNotCharaVar.Text, "1"), line, 2, false); return null; }
				wc.ShiftNext();
				if (!wc.EOL)
				{
					id = wc.Current as IdentifierWord;
					if ((id != null) && (id.Code.Equals("FORWARD", Config.StringComparison)
						|| id.Code.Equals("BACK", Config.StringComparison)))
					{
						if (id.Code.Equals("BACK", Config.StringComparison))
							order = SortOrder.DESENDING;
						wc.ShiftNext();
						if (!wc.EOL)
							warn(trerror.TooManyArg.Text, line, 1, false);
					}
					else
					{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
				}
			}
			return new SpSortcharaArgument(varTerm, order);
		}
	}

	private sealed class SP_SORT_ARRAY_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			SortOrder order = SortOrder.ASCENDING;
			WordCollection wc = popWords(line);
			AExpression term3 = new SingleLongTerm(0);
			AExpression term4 = null;

			if (wc.EOL)
			{
				warn(trerror.WrongFormat.Text, line, 2, false); return null;
			}

			VariableTerm varTerm;
			AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
			if (term == null)
			{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
			varTerm = term.Restructure(exm) as VariableTerm;
			if (varTerm == null)
			{ warn(string.Format(trerror.ArgIsNotVariable.Text, "1"), line, 2, false); return null; }
			else if (varTerm.Identifier.IsConst)
			{ warn(string.Format(trerror.ArgIsConst.Text, "1"), line, 2, false); return null; }
			if (!varTerm.Identifier.IsArray1D)
			{ warn(string.Format(trerror.ArgIsNot1DVar.Text, "1"), line, 2, false); return null; }

			wc.ShiftNext();
			IdentifierWord id = wc.Current as IdentifierWord;

			if ((id != null) && (id.Code.Equals("FORWARD", Config.StringComparison) || id.Code.Equals("BACK", Config.StringComparison)))
			{
				if (id.Code.Equals("BACK", Config.StringComparison))
					order = SortOrder.DESENDING;
				wc.ShiftNext();
			}
			else if (id != null)
			{ warn(string.Format(trerror.IsNotForwardBack.Text, "2"), line, 2, false); return null; }

			if (id != null)
			{
				wc.ShiftNext();
				if (!wc.EOL)
				{
					term3 = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
					if (term3 == null)
					{ warn(string.Format(trerror.CanNotRecognizeArg.Text, "3"), line, 2, false); return null; }
					if (!term3.IsInteger)
					{ warn(string.Format(trerror.ArgIsNotNumber.Text, "3"), line, 2, false); return null; }
					wc.ShiftNext();
					if (!wc.EOL)
					{
						term4 = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
						if (term4 == null)
						{ warn(string.Format(trerror.CanNotRecognizeArg.Text, "4"), line, 2, false); return null; }
						if (!term4.IsInteger)
						{ warn(string.Format(trerror.ArgIsNotNumber.Text, "4"), line, 2, false); return null; }
						wc.ShiftNext();
						if (!wc.EOL)
							warn(trerror.TooManyArg.Text, line, 1, false);
					}
				}
			}
			return new SpArraySortArgument(varTerm, order, term3, term4);
		}
	}

	private sealed class SP_CALL_ArgumentBuilder : ArgumentBuilder
	{
		public SP_CALL_ArgumentBuilder(bool callf, bool form)
		{
			this.form = form;
			this.callf = callf;
		}

		readonly bool form;
		readonly bool callf;
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			AExpression funcname;
			if (form)
			{
				StrFormWord sfw = LexicalAnalyzer.AnalyseFormattedString(st, FormStrEndWith.LeftParenthesis_Bracket_Comma_Semicolon, true);
				funcname = ExpressionParser.ToStrFormTerm(sfw);
				funcname = funcname.Restructure(exm);
			}
			else
			{
				string str = LexicalAnalyzer.ReadString(st, StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon);
				str = str.Trim([' ', '\t']);
				funcname = new SingleStrTerm(str);
			}
			char cur = st.Current;
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
			wc.ShiftNext();

			List<AExpression> subNames = null;
			List<AExpression> args = null;
			if (cur == '[')
			{
				subNames = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightBracket, false);
				if (!wc.EOL)
				{
					if (wc.Current.Type != '(')
						wc.ShiftNext();
					args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightParenthesis, false);
				}
			}
			if ((cur == '(') || (cur == ','))
			{
				if (cur == '(')
					args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightParenthesis, false);
				else
					args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
				if (!wc.EOL)
				{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
			}
			if (subNames == null)
				subNames = [];
			if (args == null)
				args = [];
			for (int i = 0; i < subNames.Count; i++)
				if (subNames != null)
					subNames[i] = subNames[i].Restructure(exm);
			for (int i = 0; i < args.Count; i++)
				if (args[i] != null)
					args[i] = args[i].Restructure(exm);
			Argument ret;
			if (callf)
				ret = new SpCallFArgment(funcname, subNames, args);
			else
				ret = new SpCallArgment(funcname, subNames, args);
			if (funcname is SingleTerm)
			{
				ret.IsConst = true;
				ret.ConstStr = funcname.GetStrValue(null);
				if (string.IsNullOrEmpty(ret.ConstStr))
				{
					warn(trerror.NotSpecifiedFuncName.Text, line, 2, false);
					return null;
				}
			}
			return ret;
		}
	}
	private sealed class SP_CALLSHARP_ArgumentBuilder : ArgumentBuilder
	{
		public SP_CALLSHARP_ArgumentBuilder()
		{

		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			AExpression funcname;
			string str = LexicalAnalyzer.ReadString(st, StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon);
			str = str.Trim([' ', '\t']);
			funcname = new SingleStrTerm(str);
			char cur = st.Current;
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
			wc.ShiftNext();

			List<AExpression> subNames = null;
			List<AExpression> args = null;
			if (cur == '[')
			{
				subNames = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightBracket, false);
				if (!wc.EOL)
				{
					if (wc.Current.Type != '(')
						wc.ShiftNext();
					args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightParenthesis, false);
				}
			}
			if ((cur == '(') || (cur == ','))
			{
				if (cur == '(')
					args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightParenthesis, false);
				else
					args = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
				if (!wc.EOL)
				{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
			}
			if (subNames == null)
				subNames = [];
			if (args == null)
				args = [];
			for (int i = 0; i < subNames.Count; i++)
				if (subNames != null)
					subNames[i] = subNames[i].Restructure(exm);
			for (int i = 0; i < args.Count; i++)
				if (args[i] != null)
					args[i] = args[i].Restructure(exm);
			Argument ret;
			ret = new SpCallSharpArgment(funcname, subNames, args);
			if (funcname is SingleTerm)
			{
				ret.IsConst = true;
				ret.ConstStr = funcname.GetStrValue(null);
				if (ret.ConstStr == "")
				{
					warn(trerror.NotSpecifiedFuncName.Text, line, 2, false);
					return null;
				}
			}
			return ret;
		}
	}
	private sealed class CASE_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			WordCollection wc = popWords(line);
			CaseExpression[] args = ExpressionParser.ReduceCaseExpressions(wc);
			if ((!wc.EOL) || (args.Length == 0))
			{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
			for (int i = 0; i < args.Length; i++)
				args[i].Reduce(exm);
			return new CaseArgument(args);
		}
	}

	private sealed class SP_SET_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			WordCollection destWc = line.PopAssignmentDestStr();
			var destTerms = ExpressionParser.ReduceArguments(destWc, ArgsEndWith.EoL, false);
			SpSetArgument ret;
			if ((destTerms.Count == 0) || (destTerms[0] == null))
			{
				assignwarn(trerror.CanNotReadLeft.Text, line, 2, false);
				return null;
			}
			if (destTerms.Count != 1)
			{
				assignwarn(trerror.LeftHasExtraComma.Text, line, 2, false);
				return null;
			}
			if (!(destTerms[0] is VariableTerm varTerm))
			{
				assignwarn(trerror.LeftIsNotVar.Text, line, 2, false);
				return null;
			}
			else if (varTerm.Identifier.IsConst)
			{
				assignwarn(trerror.LeftIsConst.Text, line, 2, false);
				return null;
			}
			varTerm.Restructure(exm);
			CharStream st = line.PopArgumentPrimitive();
			if (st == null)
				st = new CharStream("");
			OperatorCode op = line.AssignOperator;
			AExpression src;
			if (varTerm.IsInteger)
			{
				if (op == OperatorCode.AssignmentStr)
				{
					assignwarn(string.Format(trerror.InvalidOpWithInt.Text, OperatorManager.ToOperatorString(op)), line, 2, false);
					return null;
				}
				if ((op == OperatorCode.Increment) || (op == OperatorCode.Decrement))
				{
					LexicalAnalyzer.SkipWhiteSpace(st);
					if (!st.EOS)
					{
						if (op == OperatorCode.Increment)
						{
							assignwarn(trerror.InvalidOpWithIncrement.Text, line, 2, false);
							return null;
						}
						else
						{
							assignwarn(trerror.InvalidOpWithDecrement.Text, line, 2, false);
							return null;
						}
					}
					ret = new SpSetArgument(varTerm, null)
					{
						IsConst = true,
						ConstInt = op == OperatorCode.Increment ? 1 : -1,
						AddConst = true
					};
					return ret;
				}
				WordCollection srcWc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
				var srcTerms = ExpressionParser.ReduceArguments(srcWc, ArgsEndWith.EoL, false);

				if ((srcTerms.Count == 0) || (srcTerms[0] == null))
				{
					assignwarn(trerror.CanNotReadRight.Text, line, 2, false);
					return null;
				}
				if (srcTerms.Count != 1)
				{
					if (op != OperatorCode.Assignment)
					{
						assignwarn(trerror.CanNotContainMultipleValue.Text, line, 2, false);
						return null;
					}
					bool allConst = true;
					long[] constValues = new long[srcTerms.Count];
					for (int i = 0; i < srcTerms.Count; i++)
					{
						if (srcTerms[i] == null)
						{
							assignwarn(trerror.CanNotOmitRight.Text, line, 2, false);
							return null;
						}
						if (!srcTerms[i].IsInteger)
						{
							assignwarn(trerror.CanNotAssignStrToInt.Text, line, 2, false);
							return null;
						}
						srcTerms[i] = srcTerms[i].Restructure(exm);
						if (allConst && (srcTerms[i] is SingleTerm))
							constValues[i] = srcTerms[i].GetIntValue(null);
						else
							allConst = false;
					}
					var arrayarg = new SpSetArrayArgument(varTerm, srcTerms, constValues)
					{
						IsConst = allConst
					};
					return arrayarg;
				}
				if (!srcTerms[0].IsInteger)
				{
					assignwarn(trerror.CanNotAssignStrToInt.Text, line, 2, false);
					return null;
				}
				src = srcTerms[0].Restructure(exm);
				if (op == OperatorCode.Assignment)
				{
					ret = new SpSetArgument(varTerm, src);
					if (src is SingleTerm)
					{
						ret.IsConst = true;
						ret.AddConst = false;
						ret.ConstInt = src.GetIntValue(null);
					}
					return ret;
				}
				if ((op == OperatorCode.Plus) || (op == OperatorCode.Minus))
				{
					if (src is SingleTerm)
					{
						ret = new SpSetArgument(varTerm, null)
						{
							IsConst = true,
							ConstInt = op == OperatorCode.Plus ? src.GetIntValue(null) : -src.GetIntValue(null),
							AddConst = true
						};
						return ret;
					}
				}
				src = OperatorMethodManager.ReduceBinaryTerm(op, varTerm, src);
				return new SpSetArgument(varTerm, src);

			}
			else
			{
				if (op == OperatorCode.Assignment)
				{
					if (Config.SystemIgnoreStringSet)
					{
						assignwarn(trerror.StrAssignIsPrihibited.Text, line, 2, false);
						return null;
					}
					LexicalAnalyzer.SkipHalfSpace(st);//文字列の代入なら半角スペースだけを読み飛ばす
													  //eramakerは代入文では妙なTrim()をする。半端にしか再現できないがとりあえずtrim = true
					StrFormWord sfwt = LexicalAnalyzer.AnalyseFormattedString(st, FormStrEndWith.EoL, true);
					AExpression term = ExpressionParser.ToStrFormTerm(sfwt);
					src = term.Restructure(exm);
					ret = new SpSetArgument(varTerm, src);
					if (src is SingleTerm)
					{
						ret.IsConst = true;
						ret.AddConst = false;
						ret.ConstStr = src.GetStrValue(null);
					}
					return ret;
				}
				else if ((op == OperatorCode.Mult) || (op == OperatorCode.Plus) || (op == OperatorCode.AssignmentStr))
				{
					WordCollection srcWc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
					var srcTerms = ExpressionParser.ReduceArguments(srcWc, ArgsEndWith.EoL, false);

					if ((srcTerms.Count == 0) || (srcTerms[0] == null))
					{
						assignwarn(trerror.CanNotReadRight.Text, line, 2, false);
						return null;
					}
					if (op == OperatorCode.AssignmentStr)
					{
						if (srcTerms.Count == 1)
						{
							if (srcTerms[0].IsInteger)
							{
								assignwarn(trerror.CanNotAssignIntToStr.Text, line, 2, false);
								return null;
							}
							src = srcTerms[0].Restructure(exm);
							ret = new SpSetArgument(varTerm, src);
							if (src is SingleTerm)
							{
								ret.IsConst = true;
								ret.AddConst = false;
								ret.ConstStr = src.GetStrValue(null);
							}
							return ret;
						}
						bool allConst = true;
						string[] constValues = new string[srcTerms.Count];
						for (int i = 0; i < srcTerms.Count; i++)
						{
							if (srcTerms[i] == null)
							{
								assignwarn(trerror.CanNotOmitRight.Text, line, 2, false);
								return null;
							}
							if (srcTerms[i].IsInteger)
							{
								assignwarn(trerror.CanNotAssignIntToStr.Text, line, 2, false);
								return null;
							}
							srcTerms[i] = srcTerms[i].Restructure(exm);
							if (allConst && (srcTerms[i] is SingleTerm))
								constValues[i] = srcTerms[i].GetStrValue(null);
							else
								allConst = false;
						}
						var arrayarg = new SpSetArrayArgument(varTerm, srcTerms, constValues)
						{
							IsConst = allConst
						};
						return arrayarg;
					}
					if (srcTerms.Count != 1)
					{
						assignwarn(trerror.RightHasExtraComma.Text, line, 2, false);
						return null;
					}

					src = srcTerms[0].Restructure(exm);
					src = OperatorMethodManager.ReduceBinaryTerm(op, varTerm, src);
					return new SpSetArgument(varTerm, src);
				}
				assignwarn(trerror.InvalidAssignmentOp.Text, line, 2, false);
				return null;
			}
		}
	}

	private sealed class METHOD_ArgumentBuilder : ArgumentBuilder
	{
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var args = popTerms(line);
			string errmes = line.Function.Method.CheckArgumentType(line.Function.Name, args);
			if (errmes != null)
				throw new CodeEE(errmes);
			var mTerm = new FunctionMethodTerm(line.Function.Method, args);
			return new MethodArgument(mTerm.Restructure(exm));
		}
	}

	private sealed class SP_INPUTS_ArgumentBuilder : ArgumentBuilder
	{
		public SP_INPUTS_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string)];
			//if (nullable)妥協
			minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			#region EM_私家版_INPUT系機能拡張＆ONEINPUT系制限解除
			//Argument ret = null;
			//if (st.EOS)
			//{
			//	ret = new ExpressionArgument(null);
			//	return ret;
			//}
			//StrFormWord sfwt = LexicalAnalyzer.AnalyseFormattedString(st, FormStrEndWith.EoL, false);
			//if (!st.EOS)
			//{
			//	warn("引数が多すぎます", line, 1, false);
			//}
			//IOperandTerm term = ExpressionParser.ToStrFormTerm(sfwt);
			//term = term.Restructure(exm);
			//ret = new ExpressionArgument(term);
			//if (term is SingleTerm)
			//{
			//	ret.ConstStr = term.GetStrValue(exm);
			//	if (line.FunctionCode == FunctionCode.ONEINPUTS)
			//	{
			//		if (string.IsNullOrEmpty(ret.ConstStr))
			//		{
			//			warn("引数が空文字列なため、引数は無視されます", line, 1, false);
			//			return new ExpressionArgument(null);
			//		}
			//		else if (ret.ConstStr.Length > 1)
			//		{
			//			warn("ONEINPUTSの引数に２文字以上の文字列が渡されています（２文字目以降は無視されます）", line, 1, false);
			//			ret.ConstStr = ret.ConstStr.Remove(1);
			//		}
			//	}
			//	ret.IsConst = true;
			//}
			SpInputsArgument ret = null;
			if (st.EOS)
			{
				ret = new SpInputsArgument(null, null, null);
				return ret;
			}
			StrFormWord sfwt = LexicalAnalyzer.AnalyseFormattedString(st, FormStrEndWith.Comma, false);
			AExpression term = ExpressionParser.ToStrFormTerm(sfwt);
			term = term.Restructure(exm);
			if (st.EOS)
			{
				ret = new SpInputsArgument(term, null, null);
				return ret;
			}
			st.ShiftNext();
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);

			var terms = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
			if (!st.EOS || terms.Count > 1)
			{
				warn(trerror.TooManyArg.Text, line, 1, false);
			}
			if (terms.Count > 0)
			{
				if (terms[0] == null || !terms[0].IsInteger)
				{
					warn(trerror.IgnoreArgBecauseNotInt.Text, line, 1, false);
					ret = new SpInputsArgument(term, null, null);
				}
				else if (terms.Count == 1) ret = new SpInputsArgument(term, terms[0], null);
				else ret = new SpInputsArgument(term, terms[0], terms[1]);
			}
			#endregion
			return ret;
		}
	}

	#region 正規型 popTerms()とcheckArgumentType()を両方行うもの。考えることは最低限でよい。

	private sealed class INT_EXPRESSION_ArgumentBuilder : ArgumentBuilder
	{
		public INT_EXPRESSION_ArgumentBuilder(bool nullable)
		{
			argumentTypeArray = [typeof(long)];
			//if (nullable)妥協
			minArg = 0;
			this.nullable = nullable;
		}

		readonly bool nullable;

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			AExpression term;
			if (terms.Count == 0)
			{
				term = new SingleLongTerm(0);
				if (!nullable)
				{
					if (line.Function.IsExtended())
						warn(trerror.OmittedArg1.Text, line, 1, false);
					else
						warn(trerror.OmittedArg2.Text, line, 1, false);
				}
			}
			else
			{
				term = terms[0];
			}

			if (line.FunctionCode == FunctionCode.REPEAT)
			{
				if (GlobalStatic.IdentifierDictionary.getVarTokenIsForbid("COUNT"))
				{
					throw new CodeEE(trerror.CanNotUseRepeat.Text);
				}
				if ((term is SingleTerm) && (term.GetIntValue(null) <= 0L))
				{
					warn(trerror.RepeatCountLessthan0.Text, line, 0, true);
				}
				VariableToken count = GlobalStatic.VariableData.GetSystemVariableToken("COUNT");
				VariableTerm repCount = new(count, [new SingleLongTerm(0)]);
				repCount.Restructure(exm);
				return new SpForNextArgment(repCount, new SingleLongTerm(0), term, new SingleLongTerm(1));
			}
			ExpressionArgument ret = new(term);
			if (term is SingleTerm)
			{
				long i = term.GetIntValue(null);
				ret.ConstInt = i;
				ret.IsConst = true;
				if (line.FunctionCode == FunctionCode.CLEARLINE)
				{
					if (i <= 0L)
						warn(trerror.ArgLessThan0.Text, line, 1, false);
				}
				else if (line.FunctionCode == FunctionCode.FONTSTYLE)
				{
					if (i < 0L)
						warn(trerror.ArgIsNegativeValue.Text, line, 1, false);
				}
			}
			return ret;
		}
	}

	private sealed class INT_ANY_ArgumentBuilder : ArgumentBuilder
	{
		public INT_ANY_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long)];
			minArg = 0;
			argAny = true;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;

			List<AExpression> termList = [.. terms];
			ExpressionArrayArgument ret = new(termList);
			if (terms.Count == 0)
			{
				if (line.FunctionCode == FunctionCode.RETURN)
				{
					termList.Add(new SingleLongTerm(0));
					ret.IsConst = true;
					ret.ConstInt = 0;
					return ret;
				}
				warn(trerror.MissingArg.Text, line, 2, false);
				return null;
			}
			else if (terms.Count == 1)
			{
				if (terms[0] is SingleLongTerm s)
				{
					ret.IsConst = true;
					ret.ConstInt = s.Int;
					return ret;
				}
				else if (line.FunctionCode == FunctionCode.RETURN)
				{
					//定数式は定数化してしまうので現行システムでは見つけられない
					if (terms[0] is VariableTerm)
						warn(trerror.ReturnArgIsVar.Text, line, 0, true);
					else
						warn(trerror.ReturnArgIsFormula.Text, line, 0, true);
				}
			}
			else
			{
				warn(string.Format(trerror.ArgIsFormula.Text, line.Function.Name), line, 0, true);
			}
			return ret;
		}
	}

	private sealed class STR_EXPRESSION_ArgumentBuilder : ArgumentBuilder
	{
		public STR_EXPRESSION_ArgumentBuilder(bool nullable)
		{
			argumentTypeArray = [typeof(string)];
			if (nullable)
				minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			ExpressionArgument ret;
			if (terms.Count == 0)
			{
				ret = new ExpressionArgument(new SingleStrTerm(""))
				{
					ConstStr = "",
					IsConst = true
				};
				return ret;
			}
			return new ExpressionArgument(terms[0]);
		}
	}

	private sealed class EXPRESSION_ArgumentBuilder : ArgumentBuilder
	{
		public EXPRESSION_ArgumentBuilder(bool nullable)
		{
			argumentTypeArray = [typeof(void)];
			if (nullable)
				minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			if (terms.Count == 0)
			{
				ExpressionArgument ret = new(null)
				{
					ConstStr = "",
					ConstInt = 0,
					IsConst = true
				};
				return ret;
			}
			return new ExpressionArgument(terms[0]);
		}
	}

	private sealed class SP_BAR_ArgumentBuilder : ArgumentBuilder
	{
		public SP_BAR_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			//minArg = 3;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			return new SpBarArgument(terms[0], terms[1], terms[2]);
		}
	}

	private sealed class SP_SWAP_ArgumentBuilder : ArgumentBuilder
	{
		//emuera1803beta2+v1 第2引数省略型に対応
		public SP_SWAP_ArgumentBuilder(bool nullable)
		{
			argumentTypeArray = [typeof(long), typeof(long)];
			if (nullable)
				minArg = 1;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			//上の判定で省略不可時はここに来ないので即さばける
			if (terms.Count == 1)
				terms = [terms[0], null];
			return new SpSwapCharaArgument(terms[0], terms[1]);
		}
	}

	private sealed class SP_SAVEDATA_ArgumentBuilder : ArgumentBuilder
	{
		public SP_SAVEDATA_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), typeof(string)];
		}

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			return new SpSaveDataArgument(terms[0], terms[1]);
		}
	}

	#region EM_私家版_INPUT系機能拡張
	private sealed class SP_TINPUT_ArgumentBuilder : ArgumentBuilder
	{
		public SP_TINPUT_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(string), typeof(long), typeof(long)];
			minArg = 2;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			AExpression term3 = null, term4 = null, term5 = null, term6 = null;
			if (!checkArgumentType(line, exm, terms))
				return null;
			if (terms.Count > 2)
				term3 = terms[2];
			if (terms.Count > 3)
				term4 = terms[3];
			if (terms.Count > 4)
				term5 = terms[4];
			if (terms.Count > 5)
				term6 = terms[5];

			return new SpTInputsArgument(terms[0], terms[1], term3, term4, term5, term6);
		}
	}

	private sealed class SP_TINPUTS_ArgumentBuilder : ArgumentBuilder
	{
		public SP_TINPUTS_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), typeof(string), typeof(long), typeof(string), typeof(long), typeof(long)];
			minArg = 2;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			AExpression term3 = null, term4 = null, term5 = null, term6 = null;
			if (!checkArgumentType(line, exm, terms))
				return null;
			if (terms.Count > 2)
				term3 = terms[2];
			if (terms.Count > 3)
				term4 = terms[3];
			if (terms.Count > 4)
				term5 = terms[4];
			if (terms.Count > 5)
				term6 = terms[5];
			return new SpTInputsArgument(terms[0], terms[1], term3, term4, term5, term6);
		}
	}
	//private sealed class SP_TINPUT_ArgumentBuilder : ArgumentBuilder
	//{
	//	public SP_TINPUT_ArgumentBuilder()
	//	{
	//		argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(string) };
	//		minArg = 2;
	//	}
	//	public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
	//	{
	//		IOperandTerm[] terms = popTerms(line);
	//		IOperandTerm term3 = null, term4 = null;
	//		if (!checkArgumentType(line, exm, terms))
	//			return null;
	//		if (terms.Count > 2)
	//			term3 = terms[2];
	//		if (terms.Count > 3)
	//			term4 = terms[3];

	//		return new SpTInputsArgument(terms[0], terms[1], term3, term4);
	//	}
	//}

	//private sealed class SP_TINPUTS_ArgumentBuilder : ArgumentBuilder
	//{
	//	public SP_TINPUTS_ArgumentBuilder()
	//	{
	//		argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(Int64), typeof(string) };
	//		minArg = 2;
	//	}
	//	public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
	//	{
	//		IOperandTerm[] terms = popTerms(line);
	//		IOperandTerm term3 = null, term4 = null;
	//		if (!checkArgumentType(line, exm, terms))
	//			return null;
	//		if (terms.Count > 2)
	//			term3 = terms[2];
	//		if (terms.Count > 3)
	//			term4 = terms[3];
	//		return new SpTInputsArgument(terms[0], terms[1], term3, term4);
	//	}
	//}
	#endregion

	private sealed class SP_FOR_NEXT_ArgumentBuilder : ArgumentBuilder
	{
		public SP_FOR_NEXT_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), null, typeof(long), typeof(long)];
			minArg = 3;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm varTerm = getChangeableVariable(terms, 1, line);
			if (varTerm == null)
				return null;
			if (varTerm.Identifier.IsCharacterData)
			{ warn(string.Format(trerror.CharaVarCanNotSpecifiedArg.Text, "1"), line, 2, false); return null; }

			AExpression start = terms[1];
			AExpression end = terms[2];
			AExpression step;
			if (start == null)
				start = new SingleLongTerm(0);
			if ((terms.Count > 3) && (terms[3] != null))
				step = terms[3];
			else
				step = new SingleLongTerm(1);
			if (!start.IsInteger)
			{ warn(string.Format(trerror.DifferentArgType.Text, "2"), line, 2, false); return null; }
			return new SpForNextArgment(varTerm, start, end, step);
		}
	}

	private sealed class SP_POWER_ArgumentBuilder : ArgumentBuilder
	{
		public SP_POWER_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			//minArg = 2;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm varTerm = getChangeableVariable(terms, 1, line);
			if (varTerm == null)
				return null;

			return new SpPowerArgument(varTerm, terms[1], terms[2]);
		}
	}

	private sealed class SP_SWAPVAR_ArgumentBuilder : ArgumentBuilder
	{
		public SP_SWAPVAR_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(void), typeof(void)];
			//minArg = 2;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm x = getChangeableVariable(terms, 1, line);
			if (x == null)
				return null;
			VariableTerm y = getChangeableVariable(terms, 2, line);
			if (y == null)
				return null;
			if (x.GetOperandType() != y.GetOperandType())
			{
				warn(trerror.NotMatchTwoArg.Text, line, 2, false);
				return null;
			}
			return new SpSwapVarArgument(x, y);
		}
	}

	private sealed class VAR_INT_ArgumentBuilder : ArgumentBuilder
	{
		public VAR_INT_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long)];
			minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (terms.Count == 0)
				return new PrintDataArgument(null);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm varTerm = getChangeableVariable(terms, 1, line);
			if (varTerm == null)
				return null;
			return new PrintDataArgument(varTerm);
		}
	}

	private sealed class VAR_STR_ArgumentBuilder : ArgumentBuilder
	{
		public VAR_STR_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string)];
			minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (terms.Count == 0)
			{
				VariableToken varToken = GlobalStatic.VariableData.GetSystemVariableToken("RESULTS");
				VariableTerm varTerm = new(varToken, [new SingleLongTerm(0)]);
				return new StrDataArgument(varTerm);
			}
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm x = getChangeableVariable(terms, 1, line);
			if (x == null)
				return null;
			return new StrDataArgument(x);
		}
	}

	private sealed class BIT_ARG_ArgumentBuilder : ArgumentBuilder
	{
		public BIT_ARG_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), typeof(long)];
			minArg = 2;
			argAny = true;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm varTerm = getChangeableVariable(terms, 1, line);
			if (varTerm == null)
				return null;
			List<AExpression> termList = [.. terms];
			//最初の項はいらない
			termList.RemoveAt(0);
			BitArgument ret = new(varTerm, termList.ToArray());
			for (int i = 0; i < termList.Count; i++)
			{
				if (termList[i] is SingleLongTerm term)
				{
					long bit = term.Int;
					if ((bit < 0) || (bit > 63))
					{
						warn(string.Format(trerror.ArgIsOoRBit.Text, i + 2, bit.ToString()), line, 2, false);
						return null;
					}
				}
			}
			return ret;
		}
	}

	private sealed class SP_VAR_SET_ArgumentBuilder : ArgumentBuilder
	{
		public SP_VAR_SET_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(void), typeof(void), typeof(long), typeof(long)];
			minArg = 1;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm varTerm = getChangeableVariable(terms, 1, line);
			if (varTerm == null)
				return null;
			if (varTerm.Identifier.IsConst)
			{
				warn(string.Format(trerror.SpecifiedConst.Text, varTerm.Identifier.Name), line, 2, false);
				return null;
			}

			AExpression term, term3 = null, term4 = null;
			if (terms.Count > 1)
				term = terms[1];
			else
			{
				if (varTerm.IsString)
					term = new SingleStrTerm("");
				else
					term = new SingleLongTerm(0);
			}
			if (varTerm is VariableNoArgTerm)
			{
				if (terms.Count > 2)
				{
					warn(string.Format(trerror.CanNotSetthirdLaterArg.Text, varTerm.Identifier.Name), line, 2, false);
					return null;
				}
				return new SpVarSetArgument(new FixedVariableTerm(varTerm.Identifier), term, null, null);
			}
			if (terms.Count > 2)
				term3 = terms[2];
			if (terms.Count > 3)
				term4 = terms[3];
			if (terms.Count >= 3 && !varTerm.Identifier.IsArray1D)
				warn(trerror.IgnoreThirdLaterArg.Text, line, 1, false);
			if (term.GetOperandType() != varTerm.GetOperandType())
			{
				warn(trerror.NotMatchTwoArg.Text, line, 2, false);
				return null;
			}
			return new SpVarSetArgument(varTerm, term, term3, term4);
		}
	}

	private sealed class SP_CVAR_SET_ArgumentBuilder : ArgumentBuilder
	{
		public SP_CVAR_SET_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(void), typeof(void), typeof(void), typeof(long), typeof(long)];
			minArg = 1;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;

			VariableTerm varTerm = getChangeableVariable(terms, 1, line);
			if (varTerm == null)
				return null;
			if (!varTerm.Identifier.IsCharacterData)
			{ warn(trerror.ArgIsNotCharaVar.Text, line, 2, false); return null; }
			//1803beta004 暫定CDFLAGを弾く
			if (varTerm.Identifier.IsArray2D)
			{ warn(trerror.ArgIs2DVar.Text, line, 2, false); return null; }
			AExpression index, term, term4 = null, term5 = null;
			if (terms.Count > 1)
				index = terms[1];
			else
				index = new SingleLongTerm(0);
			if (terms.Count > 2)
				term = terms[2];
			else
			{
				if (varTerm.IsString)
					term = new SingleStrTerm("");
				else
					term = new SingleLongTerm(0);
			}
			if (terms.Count > 3)
				term4 = terms[3];
			if (terms.Count > 4)
				term5 = terms[4];
			if (index is SingleStrTerm term1 && index.GetOperandType() == typeof(string) && varTerm.Identifier.IsArray1D)
			{
				if (!GlobalStatic.ConstantData.isDefined(varTerm.Identifier.Code, term1.Str))
				{ warn(string.Format(trerror.NotDefinedKey.Text, varTerm.Identifier.Name, index.GetStrValue(null)), line, 2, false); return null; }
			}
			if (terms.Count > 3 && !varTerm.Identifier.IsArray1D)
				warn(trerror.IgnoreFourthLaterArg.Text, line, 1, false);
			if (term.GetOperandType() != varTerm.GetOperandType())
			{
				warn(trerror.NotMatchTwoArg.Text, line, 2, false);
				return null;
			}
			return new SpCVarSetArgument(varTerm, index, term, term4, term5);
		}
	}

	private sealed class SP_BUTTON_ArgumentBuilder : ArgumentBuilder
	{
		public SP_BUTTON_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string), typeof(void)];
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			return new SpButtonArgument(terms[0], terms[1]);
		}
	}

	private sealed class SP_COLOR_ArgumentBuilder : ArgumentBuilder
	{
		public SP_COLOR_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			minArg = 1;
		}

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			if (terms.Count == 2)
			{ warn(trerror.InvalidSetcolorArgCount.Text, line, 2, false); return null; }
			SpColorArgument arg;
			if (terms.Count == 1)
			{
				arg = new SpColorArgument(terms[0]);
				if (terms[0] is SingleTerm)
				{
					arg.ConstInt = terms[0].GetIntValue(exm);
					arg.IsConst = true;
				}
			}
			else
			{
				arg = new SpColorArgument(terms[0], terms[1], terms[2]);
				if ((terms[0] is SingleTerm) && (terms[1] is SingleTerm) && (terms[2] is SingleTerm))
				{
					arg.ConstInt = (terms[0].GetIntValue(exm) << 16) + (terms[1].GetIntValue(exm) << 8) + terms[2].GetIntValue(exm);
					arg.IsConst = true;
				}
			}
			return arg;
		}
	}

	private sealed class SP_SPLIT_ArgumentBuilder : ArgumentBuilder
	{
		public SP_SPLIT_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string), typeof(string), typeof(string), typeof(long)];
			minArg = 3;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm x = getChangeableVariable(terms, 3, line);
			if (x == null)
				return null;
			if (!x.Identifier.IsArray1D && !x.Identifier.IsArray2D && !x.Identifier.IsArray3D)
			{ warn(string.Format(trerror.ArgIsNotArrayVar.Text, "3"), line, 2, false); return null; }
			VariableTerm term = (terms.Count >= 4) ? getChangeableVariable(terms, 4, line) : new VariableTerm(GlobalStatic.VariableData.GetSystemVariableToken("RESULT"), [new SingleLongTerm(0)]);
			return new SpSplitArgument(terms[0], terms[1], x.Identifier, term);
		}
	}

	private sealed class SP_HTMLSPLIT_ArgumentBuilder : ArgumentBuilder
	{
		public SP_HTMLSPLIT_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string), typeof(string), typeof(long)];
			minArg = 1;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableToken destVar;
			VariableTerm destVarTerm = null;
			VariableTerm term = null;
			if (terms.Count >= 2)
				destVarTerm = getChangeableVariable(terms, 2, line);
			if (destVarTerm != null)
				destVar = destVarTerm.Identifier;
			else
				destVar = GlobalStatic.VariableData.GetSystemVariableToken("RESULTS");
			if (!destVar.IsArray1D || destVar.IsCharacterData)
			{ warn(string.Format(trerror.ArgIsRequiredNonCharaArrayVar.Text, "2"), line, 2, false); return null; }
			if (terms.Count >= 3)
				term = getChangeableVariable(terms, 3, line);
			if (term == null)
			{
				VariableToken varToken = GlobalStatic.VariableData.GetSystemVariableToken("RESULT");
				term = new VariableTerm(varToken, [new SingleLongTerm(0)]);
			}
			return new SpHtmlSplitArgument(terms[0], destVar, term);
		}
	}

	private sealed class SP_GETINT_ArgumentBuilder : ArgumentBuilder
	{
		public SP_GETINT_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(long)];
			minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (terms.Count == 0)
			{
				VariableToken varToken = GlobalStatic.VariableData.GetSystemVariableToken("RESULT");
				return new SpGetIntArgument(new VariableTerm(varToken, [new SingleLongTerm(0)]));
			}
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm x = getChangeableVariable(terms, 1, line);
			if (x == null)
				return null;
			return new SpGetIntArgument(x);
		}
	}

	private sealed class SP_CONTROL_ARRAY_ArgumentBuilder : ArgumentBuilder
	{
		public SP_CONTROL_ARRAY_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(void), typeof(long), typeof(long)];
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableTerm x = getChangeableVariable(terms, 1, line);
			if (x == null)
				return null;
			return new SpArrayControlArgument(x, terms[1], terms[2]);
		}
	}

	private sealed class SP_SHIFT_ARRAY_ArgumentBuilder : ArgumentBuilder
	{
		public SP_SHIFT_ARRAY_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(void), typeof(long), typeof(void), typeof(long), typeof(long)];
			minArg = 3;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;

			VariableTerm x = getChangeableVariable(terms, 1, line);
			if (x == null)
				return null;
			if (!x.Identifier.IsArray1D)
			{ warn(string.Format(trerror.ArgIsNot1DVar.Text, "1"), line, 2, false); return null; }

			if (line.FunctionCode == FunctionCode.ARRAYSHIFT)
			{
				if (terms[0].GetOperandType() != terms[2].GetOperandType())
				{ warn(trerror.NotMatchFirstAndThirdVar.Text, line, 2, false); return null; }
			}
			AExpression term4 = terms.Count >= 4 ? terms[3] : new SingleLongTerm(0);
			AExpression term5 = terms.Count >= 5 ? terms[4] : null;
			return new SpArrayShiftArgument(x, terms[1], terms[2], term4, term5);
		}
	}

	private sealed class SP_SAVEVAR_ArgumentBuilder : ArgumentBuilder
	{
		public SP_SAVEVAR_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string), typeof(string), typeof(void)];
			argAny = true;
			minArg = 3;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			List<VariableToken> varTokens = [];
			for (int i = 2; i < terms.Count; i++)
			{
				if (terms[i] == null)
				{ warn(string.Format(trerror.CanNotOmitArg.Text, i + 1), line, 2, false); return null; }
				VariableTerm vTerm = getChangeableVariable(terms, i + 1, line);
				if (vTerm == null)
					return null;
				VariableToken vToken = vTerm.Identifier;
				if (vToken.IsCharacterData)
				{ warn(string.Format(trerror.CanNotSaveCharaVar.Text, vToken.Name), line, 2, false); return null; }
				if (vToken.IsPrivate)
				{ warn(string.Format(trerror.CanNotSavePrivVar.Text, vToken.Name), line, 2, false); return null; }
				if (vToken.IsLocal)
				{ warn(string.Format(trerror.CanNotSaveLocalVar.Text, vToken.Name), line, 2, false); return null; }
				if (vToken.IsConst)
				{ warn(trerror.CanNotSaveConstVar.Text, line, 2, false); return null; }
				if (vToken.IsCalc)
				{ warn(trerror.CanNotSavePseudoVar.Text, line, 2, false); return null; }
				if (vToken.IsReference)
				{ warn(trerror.CanNotSaveRefVar.Text, line, 2, false); return null; }
				varTokens.Add(vToken);
			}
			for (int i = 0; i < varTokens.Count; i++)
			{
				for (int j = i + 1; j < varTokens.Count; j++)
					if (varTokens[i] == varTokens[j])
					{
						warn(string.Format(trerror.DuplicateVarSave.Text, varTokens[i].Name), line, 1, false);
						return null;
					}
			}
			VariableToken[] arg3 = new VariableToken[varTokens.Count];
			varTokens.CopyTo(arg3);
			return new SpSaveVarArgument(terms[0], terms[1], arg3);
		}
	}

	private sealed class SP_SAVECHARA_ArgumentBuilder : ArgumentBuilder
	{
		public SP_SAVECHARA_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string), typeof(string), typeof(long)];
			minArg = 3;
			argAny = true;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;

			List<AExpression> termList = [.. terms];
			ExpressionArrayArgument ret = new(termList);

			for (int i = 2; i < termList.Count; i++)
			{
				if (!(termList[i] is SingleTerm))
					continue;
				long iValue = termList[i].GetIntValue(null);
				if (iValue < 0)
				{ warn(trerror.NotPositiveCharaNo.Text, line, 2, false); return null; }
				if (iValue > int.MaxValue)
				{ warn(trerror.CharaNoOverInt32.Text, line, 2, false); return null; }
				for (int j = i + 1; j < termList.Count; j++)
				{
					if (!(termList[j] is SingleTerm))
						continue;
					if (iValue == termList[j].GetIntValue(null))
					{
						warn(string.Format(trerror.DuplicateVarSave.Text, iValue.ToString()), line, 1, false);
						return null;
					}
				}
			}
			return ret;
		}
	}

	private sealed class SP_REF_ArgumentBuilder : ArgumentBuilder
	{
		public SP_REF_ArgumentBuilder(bool byname)
		{
			argumentTypeArray = [typeof(void), typeof(void)];
			minArg = 2;
			this.byname = byname;
		}

		readonly bool byname;

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			WordCollection wc = popWords(line);
			wc.ShiftNext();
			if (!(wc.Current is IdentifierWord id) || wc.Current.Type != ',')
			{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
			wc.ShiftNext();
			AExpression name = null;
			string srcCode = null;
			if (byname)
			{
				name = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
				if (name == null || name.IsInteger || !wc.EOL)
				{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
				name = name.Restructure(exm);
				if (name is SingleTerm)
					srcCode = name.GetStrValue(exm);
			}
			else
			{
				wc.ShiftNext();
				if (!(wc.Current is IdentifierWord id2) || !wc.EOL)
				{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
				srcCode = id2.Code;
			}
			UserDefinedRefMethod refm = GlobalStatic.IdentifierDictionary.GetRefMethod(id.Code);
			ReferenceToken refVar = null;
			if (refm == null)
			{
				VariableToken token = GlobalStatic.IdentifierDictionary.GetVariableToken(id.Code, null, true);
				if (token == null || !token.IsReference)
				{ warn(string.Format(trerror.ArgIsNotRef.Text, "1"), line, 2, false); return null; }
				refVar = (ReferenceToken)token;
			}

			if (refm != null)
			{
				if (srcCode == null)
					return new RefArgument(refm, name);
				UserDefinedRefMethod srcRef = GlobalStatic.IdentifierDictionary.GetRefMethod(srcCode);
				if (srcRef != null)
				{
					return new RefArgument(refm, srcRef);
				}
				FunctionLabelLine label = GlobalStatic.LabelDictionary.GetNonEventLabel(srcCode);
				if (label == null)
				{ warn(string.Format(trerror.NotDefinedUserFunc.Text, srcCode), line, 2, false); return null; }
				if (!label.IsMethod)
				{ warn(string.Format(trerror.CanNotRefFunc.Text, srcCode), line, 2, false); return null; }
				CalledFunction called = CalledFunction.CreateCalledFunctionMethod(label, label.LabelName);
				return new RefArgument(refm, called);
			}
			else
			{
				if (srcCode == null)
					return new RefArgument(refVar, name);
				VariableToken srcVar = GlobalStatic.IdentifierDictionary.GetVariableToken(srcCode, null, true);
				if (srcVar == null)
				{ warn(string.Format(trerror.NotDefinedVar.Text, srcCode), line, 2, false); return null; }
				return new RefArgument(refVar, srcVar);
			}
		}
	}

	private sealed class SP_INPUT_ArgumentBuilder : ArgumentBuilder
	{
		public SP_INPUT_ArgumentBuilder()
		{
			#region EM_私家版_INPUT系機能拡張
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(long)];
			#endregion
			//if (nullable)妥協
			minArg = 0;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			#region EM_私家版_INPUT系機能拡張＆ONEINPUT系制限解除
			//IOperandTerm[] terms = popTerms(line);
			//if (!checkArgumentType(line, exm, terms))
			//	return null;
			//IOperandTerm term;

			//ExpressionArgument ret;
			//if (terms.Count == 0)
			//{
			//	ret = new ExpressionArgument(term);
			//	return ret;
			//}
			//else
			//{
			//	term = terms[0];
			//	ret = new ExpressionArgument(term);
			//}

			//if (term is SingleTerm)
			//{
			//	Int64 i = term.GetIntValue(null);
			//	if (line.FunctionCode == FunctionCode.ONEINPUT)
			//	{
			//		if (i < 0)
			//		{
			//			warn("ONEINPUTの引数にONEINPUTが受け取れない負の数数が指定されています（引数を無効とします）", line, 1, false);
			//			ret = new ExpressionArgument(null);
			//			return ret;
			//		}
			//		else if (i > 9)
			//		{
			//			warn("ONEINPUTの引数にONEINPUTが受け取れない2桁以上の数数が指定されています（最初の桁を引数と見なします）", line, 1, false);
			//			i = Int64.Parse(i.ToString().Remove(1));
			//		}
			//	}
			//	ret.ConstInt = i;
			//	ret.IsConst = true;
			//}
			var terms = popTerms(line);
			for (int i = 0; i < terms.Count; i++)
			{
				if (terms[i] != null && terms[i].GetOperandType() != typeof(long))
				{
					warn(string.Format(trerror.DifferentArgType.Text, i + 1), line, 2, false);
					return null;
				}
			}
			SpInputsArgument ret;
			if (terms.Count == 0)
			{
				ret = new SpInputsArgument(null, null, null);
				return ret;
			}
			else if (terms.Count == 1)
			{
				ret = new SpInputsArgument(terms[0], null, null);
			}
			else if (terms.Count == 2)
			{
				ret = new SpInputsArgument(terms[0], terms[1], null);
			}
			else
			{
				ret = new SpInputsArgument(terms[0], terms[1], terms[2]);
			}
			#endregion
			return ret;
		}
	}

	private sealed class SP_COPY_ARRAY_Arguments : ArgumentBuilder
	{
		public SP_COPY_ARRAY_Arguments()
		{
			argumentTypeArray = [typeof(string), typeof(string)];
			minArg = 2;
		}

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			VariableToken[] vars = [null, null];
			if (terms[0] is SingleStrTerm term)
			{
				if ((vars[0] = GlobalStatic.IdentifierDictionary.GetVariableToken(term.Str, null, true)) == null)
				{
					warn(string.Format(trerror.ArraycopyArgIsNotDefined.Text, "1", term.Str), line, 2, false);
					return null;
				}
				if (!vars[0].IsArray1D && !vars[0].IsArray2D && !vars[0].IsArray3D)
				{
					warn(string.Format(trerror.ArraycopyArgIsNotArray.Text, "1", term.Str), line, 2, false);
					return null;
				}
				if (vars[0].IsCharacterData)
				{
					warn(string.Format(trerror.ArraycopyArgIsCharaVar.Text, "1", term.Str), line, 2, false);
					return null;
				}
			}
			if (terms[1] is SingleStrTerm term1)
			{
				if ((vars[1] = GlobalStatic.IdentifierDictionary.GetVariableToken(term1.Str, null, true)) == null)
				{
					warn(string.Format(trerror.ArraycopyArgIsNotDefined.Text, "2", term1.Str), line, 2, false);
					return null;
				}
				if (!vars[1].IsArray1D && !vars[1].IsArray2D && !vars[1].IsArray3D)
				{
					warn(string.Format(trerror.ArraycopyArgIsNotArray.Text, "2", term1.Str), line, 2, false);
				}
				if (vars[1].IsCharacterData)
				{
					warn(string.Format(trerror.ArraycopyArgIsCharaVar.Text, "2", term1.Str), line, 2, false);
					return null;
				}
				if (vars[1].IsConst)
				{
					warn(string.Format(trerror.ArraycopyArgIsConst.Text, "2", term1.Str), line, 2, false);
					return null;
				}
			}
			if ((vars[0] != null) && (vars[1] != null))
			{
				if ((vars[0].IsArray1D && !vars[1].IsArray1D) || (vars[0].IsArray2D && !vars[1].IsArray2D) || (vars[0].IsArray3D && !vars[1].IsArray3D))
				{
					warn(trerror.DifferentArraycopyArgsDim.Text, line, 2, false);
					return null;
				}
				if ((vars[0].IsInteger && vars[1].IsString) || (vars[0].IsString && vars[1].IsInteger))
				{
					warn(trerror.DifferentArraycopyArgsType.Text, line, 2, false);
					return null;
				}
			}
			return new SpCopyArrayArgument(terms[0], terms[1]);
		}
	}
	#endregion

	/// <summary>
	/// 一般型。数式と文字列式の組み合わせのみを引数とし、特殊なチェックが必要ないもの
	/// </summary>
	private sealed class Expressions_ArgumentBuilder : ArgumentBuilder
	{
		public Expressions_ArgumentBuilder(Type[] types, int minArgs = -1)
		{
			argumentTypeArray = types;
			minArg = minArgs;
		}

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			var terms = popTerms(line);
			if (!checkArgumentType(line, exm, terms))
				return null;
			return new ExpressionsArgument(argumentTypeArray, terms);
		}
	}

	#region EE版
	private sealed class STR_DOUBLE_ArgumentBuilder : ArgumentBuilder
	{
		public STR_DOUBLE_ArgumentBuilder()
		{
			argumentTypeArray = [typeof(string), typeof(double)];
			minArg = 1;
		}
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			//TIMES_Argからの引用 きたない
			CharStream st = line.PopArgumentPrimitive();
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.Comma, LexAnalyzeFlag.None);
			st.ShiftNext();

			double d = 0.0;
			if (st != null)
			{
				try
				{
					LexicalAnalyzer.SkipWhiteSpace(st);
					d = LexicalAnalyzer.ReadDouble(st);
					LexicalAnalyzer.SkipWhiteSpace(st);
					if (!st.EOS)
						warn(trerror.TooManyArg.Text, line, 1, false);
				}
				catch
				{
					d = 0.0;
				}
			}
			AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
			if (term == null)
			{ warn(trerror.WrongFormat.Text, line, 2, false); return null; }
			return new StrDoubleArgument(term, d);
		}

	}
	#endregion
}
