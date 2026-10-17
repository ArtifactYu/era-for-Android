using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Config.JSON;
using MinorShift.Emuera.Runtime.Script;
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using MinorShift.Emuera.Runtime.Utils.PluginSystem;
using MinorShift.Emuera.UI.Game;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using static MinorShift.Emuera.Runtime.Utils.EvilMask.Utils;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;
using trmb = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.MessageBox;

namespace MinorShift.Emuera.GameProc.Function;

internal sealed partial class FunctionIdentifier
{
	#region Emuera.NET VAR命令
	private sealed class VARI_Instruction : AInstruction
	{
		//統合_netEmuera: METHOD_SAFE が無いと #FUNCTION 中で使えない
		public VARI_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			var arg = (IntAsignArgument)func.Argument;
			var varName = arg.ConstStr;

			var privateVar = func.ParentLabelLine.GetPrivateVariable(varName);
			privateVar.ScopeIn();
			if (privateVar.GetLength(0) == 1)
			{
				privateVar.SetValue(arg.Exp.GetIntValue(exm), [0]);
			}
			else
			{

			}
		}

		//統合_netEmuera: 右辺の式解析はここで行う。
		//LogicalLineParser の時点ではユーザ定義関数がまだ登録されておらず、
		//VARI X = ユーザ定義関数(...) を解釈できないため。
		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			SplitVarStatement(line, out string varName, out List<int> lengths, out ReadOnlySpan<char> right);

			AExpression exp = null;
			if (lengths.Count == 1 && !right.IsWhiteSpace())
			{
				var wc = LexicalAnalyzer.Analyse(new CharStream(right.ToString()), LexEndWith.EoL, LexAnalyzeFlag.None);
				exp = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL);
			}
			return new IntAsignArgument(varName, [.. lengths], exp ?? new SingleLongTerm(default));
		}
	}
	private sealed class VARS_Instruction : AInstruction
	{
		//統合_netEmuera: METHOD_SAFE が無いと #FUNCTION 中で使えない
		public VARS_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			var arg = (StrAsignArgument)func.Argument;
			var varName = arg.ConstStr;

			var privateVar = func.ParentLabelLine.GetPrivateVariable(varName);
			privateVar.ScopeIn();
			if (privateVar.GetLength(0) == 1)
			{
				privateVar.SetValue(arg.Value, [0]);
			}
			else
			{

			}
		}

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			SplitVarStatement(line, out string varName, out List<int> lengths, out ReadOnlySpan<char> right);

			string value = default;
			if (lengths.Count == 1 && !right.IsWhiteSpace())
			{
				var literalStart = right.IndexOf('\"');
				var literalEnd = right.LastIndexOf('\"');
				value = right[(literalStart + 1)..literalEnd].ToString();
			}
			return new StrAsignArgument(varName, [.. lengths], value);
		}
	}

	/// <summary>
	/// 統合_netEmuera: VARI/VARS の「変数名（, 要素数...）（= 右辺）」を分解する。
	/// 要素数が指定された場合は配列宣言なので右辺は空のまま返す。
	/// </summary>
	private static void SplitVarStatement(InstructionLine line, out string varName, out List<int> lengths, out ReadOnlySpan<char> right)
	{
		var statementsSpan = line.PopArgumentPrimitive().SubstringROS();
		var commentIndex = statementsSpan.IndexOf(';');
		if (commentIndex != -1)
			statementsSpan = statementsSpan[..commentIndex];

		var equalsIndex = statementsSpan.IndexOf('=');
		string left;
		right = "";
		if (equalsIndex == -1)
		{
			left = statementsSpan.ToString();
		}
		else
		{
			left = statementsSpan[..equalsIndex].ToString();
			right = statementsSpan[(equalsIndex + 1)..];
		}

		var leftSplit = left.Split(',');
		varName = leftSplit[0].Trim();
		lengths = [1];
		if (leftSplit.Length > 1)
		{
			lengths.Clear();
			for (int i = 1; i < leftSplit.Length; i++)
				lengths.Add(int.Parse(leftSplit[i].Trim()));
		}
	}
	#endregion
	#region normalFunction
	private sealed class PRINT_Instruction : AInstruction
	{
		bool isLineEnd = true;
		public PRINT_Instruction(string name)
		{
			//PRINT(|V|S|FORM|FORMS)(|K)(|D)(|L|W) コレと
			//PRINTSINGLE(|V|S|FORM|FORMS)(|K)(|D) コレと
			//PRINT(|FORM)(C|LC)(|K)(|D) コレ
			//PRINTDATA(|K)(|D)(|L|W) ←は別クラス
			flag = IS_PRINT;
			CharStream st = new(name);
			st.Jump(5);//PRINT
			if (st.CurrentEqualTo("SINGLE"))
			{
				flag |= PRINT_SINGLE | EXTENDED;
				st.Jump(6);
			}

			if (st.CurrentEqualTo("V"))
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_PRINTV);
				isPrintV = true;
				st.Jump(1);
			}
			else if (st.CurrentEqualTo("S"))
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
				st.Jump(1);
			}
			else if (st.CurrentEqualTo("FORMS"))
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
				isForms = true;
				st.Jump(5);
			}
			else if (st.CurrentEqualTo("FORM"))
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
				st.Jump(4);
			}
			else
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_NULLABLE);
			}
			if (st.CurrentEqualTo("LC"))
			{
				flag |= EXTENDED;
				isLC = true;
				st.Jump(2);
			}
			else if (st.CurrentEqualTo("C"))
			{
				if (name == "PRINTFORMC")
					flag |= EXTENDED;
				isC = true;
				st.Jump(1);
			}
			if (st.CurrentEqualTo("K"))
			{
				flag |= ISPRINTKFUNC | EXTENDED;
				st.Jump(1);
			}
			if (st.CurrentEqualTo("D"))
			{
				flag |= ISPRINTDFUNC | EXTENDED;
				st.Jump(1);
			}
			if (st.CurrentEqualTo("N"))
			{
				isLineEnd = false;
				flag |= PRINT_WAITINPUT;
				st.Jump(1);
			}
			if (st.CurrentEqualTo("L"))
			{
				flag |= PRINT_NEWLINE;
				flag |= METHOD_SAFE;
				st.Jump(1);
			}
			else if (st.CurrentEqualTo("W"))
			{
				flag |= PRINT_NEWLINE | PRINT_WAITINPUT;
				st.Jump(1);
			}
			else
			{
				flag |= METHOD_SAFE;
			}
			if ((ArgBuilder == null) || (!st.EOS))
				throw new ExeEE(trerror.AbnormalPrint.Text);
		}

		readonly bool isPrintV;
		readonly bool isLC;
		readonly bool isC;
		readonly bool isForms;
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			exm.Console.UseUserStyle = true;
			exm.Console.UseSetColorStyle = !func.Function.IsPrintDFunction();
			string str;
			if (func.Argument.IsConst)
				str = func.Argument.ConstStr;
			else if (isPrintV)
			{
				StringBuilder builder = new();
				var terms = ((SpPrintVArgument)func.Argument).Terms;
				foreach (AExpression termV in terms)
				{
					if (termV.GetOperandType() == typeof(long))
						builder.Append(termV.GetIntValue(exm));
					else
						builder.Append(termV.GetStrValue(exm));
				}
				str = builder.ToString();
			}
			else
			{
				str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
				if (isForms)
				{
					str = ExpressionMediator.CheckEscape(str);
					StrFormWord wt = LexicalAnalyzer.AnalyseFormattedString(new CharStream(str), FormStrEndWith.EoL, false);
					StrForm strForm = StrForm.FromWordToken(wt);
					str = strForm.GetString(exm);
				}
			}
			if (func.Function.IsPrintKFunction())
				str = exm.ConvertStringType(str);
			if (isC)
				exm.Console.PrintC(str, true);
			else if (isLC)
				exm.Console.PrintC(str, false);
			else
				exm.OutputToConsole(str, func.Function, isLineEnd);
			exm.Console.UseSetColorStyle = true;
		}
	}

	private sealed class PRINT_DATA_Instruction : AInstruction
	{
		public PRINT_DATA_Instruction(string name)
		{
			//PRINTDATA(|K)(|D)(|L|W)
			flag = EXTENDED | IS_PRINT | IS_PRINTDATA | PARTIAL;
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VAR_INT);
			CharStream st = new(name);
			st.Jump(9);//PRINTDATA
			if (st.CurrentEqualTo("K"))
			{
				flag |= ISPRINTKFUNC | EXTENDED;
				st.Jump(1);
			}
			if (st.CurrentEqualTo("D"))
			{
				flag |= ISPRINTDFUNC | EXTENDED;
				st.Jump(1);
			}
			if (st.CurrentEqualTo("L"))
			{
				flag |= PRINT_NEWLINE;
				flag |= METHOD_SAFE;
				st.Jump(1);
			}
			else if (st.CurrentEqualTo("W"))
			{
				flag |= PRINT_NEWLINE | PRINT_WAITINPUT;
				st.Jump(1);
			}
			else
			{
				flag |= METHOD_SAFE;
			}
			if ((ArgBuilder == null) || (!st.EOS))
				throw new ExeEE(trerror.AbnormalPrintdata.Text);
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			exm.Console.UseUserStyle = true;
			exm.Console.UseSetColorStyle = !func.Function.IsPrintDFunction();
			//表示データが空なら何もしないで飛ぶ
			if (func.dataList.Count == 0)
			{
				state.JumpTo(func.JumpTo);
				return;
			}
			int count = func.dataList.Count;
			int choice = (int)exm.VEvaluator.GetNextRand(count);
			VariableTerm iTerm = ((PrintDataArgument)func.Argument).Var;
			if (iTerm != null)
			{
				iTerm.SetValue(choice, exm);
			}
			List<InstructionLine> iList = func.dataList[choice];
			int i = 0;
			AExpression term;
			string str;
			foreach (InstructionLine selectedLine in iList)
			{
				state.CurrentLine = selectedLine;
				if (selectedLine.Argument == null)
					ArgumentParser.SetArgumentTo(selectedLine);
				term = ((ExpressionArgument)selectedLine.Argument).Term;
				str = term.GetStrValue(exm);
				if (func.Function.IsPrintKFunction())
					str = exm.ConvertStringType(str);
				exm.Console.Print(str);
				if (++i < iList.Count)
					exm.Console.NewLine();
			}
			if (func.Function.IsNewLine() || func.Function.IsWaitInput())
			{
				exm.Console.NewLine();
				if (func.Function.IsWaitInput())
					exm.Console.ReadAnyKey();
			}
			exm.Console.UseSetColorStyle = true;
			//ジャンプするが、流れが連続であることを保証。
			state.JumpTo(func.JumpTo);
			//state.RunningLine = null;
		}
	}

	private sealed class HTML_PRINT_Instruction : AInstruction
	{
		public HTML_PRINT_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
			#region EM_私家版_HTML_PRINT拡張
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTML_PRINT);
			#endregion
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			#region EM_私家版_HTML_PRINT拡張
			var arg = (SpHtmlPrint)func.Argument;
			// string str;
			if (arg.IsConst) exm.Console.PrintHtml(arg.ConstStr, arg.ConstInt != 0);
			else exm.Console.PrintHtml(arg.Str.GetStrValue(exm), arg.Opt == null ? false : arg.Opt.GetIntValue(exm) != 0);
			//if (func.Argument.IsConst)
			//	str = func.Argument.ConstStr;
			//else
			//	str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
			#endregion
		}
	}

	private sealed class HTML_TAGSPLIT_Instruction : AInstruction
	{
		public HTML_TAGSPLIT_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTMLSPLIT);
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpHtmlSplitArgument spSplitArg = (SpHtmlSplitArgument)func.Argument;
			string str = spSplitArg.TargetStr.GetStrValue(exm);
			string[] strs = HtmlManager.HtmlTagSplit(str);

			if (strs == null)
			{
				spSplitArg.Num.SetValue(-1, exm);
				return;
			}

			spSplitArg.Num.SetValue(strs.Length, exm);
			string[] output = (string[])spSplitArg.Var.GetArray();
			int outputlength = Math.Min(output.Length, strs.Length);
			Array.Copy(strs, output, outputlength);
		}
	}

	private sealed class HTML_PRINT_ISLAND_Instruction : AInstruction
	{
		public HTML_PRINT_ISLAND_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTML_PRINT);
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			string str;
			var arg = (SpHtmlPrint)func.Argument;
			if (arg.IsConst)
				str = arg.ConstStr;
			else
				str = ((SpHtmlPrint)func.Argument).Str.GetStrValue(exm);
			#region 統合_netEmuera_island層
			//第2引数は島の層。小さいほど奥に描く（省略時は 0）
			int layer = arg.Opt != null ? (int)arg.Opt.GetIntValue(exm) : (int)arg.ConstInt;
			exm.Console.PrintHTMLIsland(str, layer);
			#endregion
		}
	}

	private sealed class HTML_PRINT_ISLAND_CLEAR_Instruction : AInstruction
	{
		public HTML_PRINT_ISLAND_CLEAR_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
			#region 統合_netEmuera_island層
			//層を省略したら全部、指定したらその層だけ消す
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTML_PRINT_ISLAND_CLEAR);
			#endregion
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			#region 統合_netEmuera_island層
			var arg = (SpIslandClear)func.Argument;
			if (arg.Layer == null)
				exm.Console.ClearHTMLIsland();
			else
				exm.Console.ClearHTMLIsland((int)arg.Layer.GetIntValue(exm));
			#endregion
		}
	}

	private sealed class PRINT_IMG_Instruction : AInstruction
	{
		public PRINT_IMG_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
			#region EM_私家版_HTMLパラメータ拡張
			// ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_PRINT_IMG);
			#endregion
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			#region EM_私家版_HTMLパラメータ拡張
			//string str;
			//if (func.Argument.IsConst)
			//	str = func.Argument.ConstStr;
			//else
			//	str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
			//exm.Console.PrintImg(str);
			var arg = (SpPrintImgArgument)func.Argument;
			if (arg == null)
				throw new CodeEE(trerror.InvalidArg.Text);
			var strb = arg.Nameb != null ? arg.Nameb.GetStrValue(exm) : null;
			var strm = arg.Namem != null ? arg.Namem.GetStrValue(exm) : null;
			if (strb == string.Empty) strb = null;
			exm.Console.PrintImg(
				arg.Name.GetStrValue(exm),
				strb,
				strm,
				arg.Param != null && arg.Param.Length > 1 ? new MixedNum { num = (int)arg.Param[1].num.GetIntValue(exm), isPx = arg.Param[1].isPx } : null,
				arg.Param != null && arg.Param.Length > 0 ? new MixedNum { num = (int)arg.Param[0].num.GetIntValue(exm), isPx = arg.Param[0].isPx } : null,
				arg.Param != null && arg.Param.Length > 2 ? new MixedNum { num = (int)arg.Param[2].num.GetIntValue(exm), isPx = arg.Param[2].isPx } : null);
			#endregion
		}
	}

	private sealed class PRINT_RECT_Instruction : AInstruction
	{
		public PRINT_RECT_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
			#region EM_私家版_HTMLパラメータ拡張
			// ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_PRINT_RECT);
			#endregion
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			#region EM_私家版_HTMLパラメータ拡張
			//ExpressionArrayArgument intExpArg = (ExpressionArrayArgument)func.Argument;
			//int[] param = new int[intExpArg.TermList.Length];
			//for (int i = 0; i < intExpArg.TermList.Length; i++)
			//	param[i] = FunctionIdentifier.toUInt32inArg(intExpArg.TermList[i].GetIntValue(exm), "PRINT_RECT", i + 1);

			//exm.Console.PrintShape("rect", param);
			var arg = (SpPrintShapeArgument)func.Argument;
			if (arg == null)
				throw new CodeEE(trerror.InvalidArg.Text);
			var param = new MixedNum[arg.Param.Length];
			for (int i = 0; i < param.Length; i++)
				param[i] = new MixedNum { num = (int)arg.Param[i].num.GetIntValue(exm), isPx = arg.Param[i].isPx };
			exm.Console.PrintShape("rect", param);
			#endregion
		}
	}

	private sealed class PRINT_SPACE_Instruction : AInstruction
	{
		public PRINT_SPACE_Instruction()
		{
			flag = EXTENDED | METHOD_SAFE;
			#region EM_私家版_HTMLパラメータ拡張
			// ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_PRINT_SPACE);
			#endregion
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			#region EM_私家版_HTMLパラメータ拡張
			//Int64 param;
			//if (func.Argument.IsConst)
			//	param = func.Argument.ConstInt;
			//else
			//	param = ((ExpressionArgument)func.Argument).Term.GetIntValue(exm);
			//int param32 = FunctionIdentifier.toUInt32inArg(param, "PRINT_SPACE", 1);
			// exm.Console.PrintShape("space", new int[] { param32 });
			var arg = (SpPrintShapeArgument)func.Argument;
			if (arg == null)
				throw new CodeEE(trerror.InvalidArg.Text);
			var param = new MixedNum[arg.Param.Length];
			for (int i = 0; i < param.Length; i++)
				param[i] = new MixedNum { num = (int)arg.Param[i].num.GetIntValue(exm), isPx = arg.Param[i].isPx };
			exm.Console.PrintShape("space", param);
			#endregion
		}
	}
	private sealed class CUSTOMDRAWLINE_Instruction : AInstruction
	{
		public CUSTOMDRAWLINE_Instruction()
		{
			ArgBuilder = null;
			flag = METHOD_SAFE | EXTENDED;
		}

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			string rowStr;
			if (st.EOS)
				throw new CodeEE(trerror.MissingArg.Text);
			else
				rowStr = st.Substring();
			rowStr = GlobalStatic.Console.getStBar(rowStr);
			Argument ret = new ExpressionArgument(new SingleStrTerm(rowStr))
			{
				ConstStr = rowStr,
				IsConst = true
			};
			return ret;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (GlobalStatic.Process.SkipPrint)
				return;
			GlobalStatic.Console.printCustomBar(func.Argument.ConstStr, true);
			exm.Console.NewLine();
		}
	}

	private sealed class DEBUGPRINT_Instruction : AInstruction
	{
		public DEBUGPRINT_Instruction(bool form, bool newline)
		{
			if (form)
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
			else
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_NULLABLE);
			flag = METHOD_SAFE | EXTENDED | DEBUG_FUNC;
			if (newline)
				flag |= PRINT_NEWLINE;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			string str;
			if (func.Argument.IsConst)
				str = func.Argument.ConstStr;
			else
				str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
			exm.Console.DebugPrint(str);
			if (func.Function.IsNewLine())
				exm.Console.DebugNewLine();
		}
	}

	private sealed class DEBUGCLEAR_Instruction : AInstruction
	{
		public DEBUGCLEAR_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED | DEBUG_FUNC;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.DebugClear();
		}
	}

	private sealed class METHOD_Instruction : AInstruction
	{
		public METHOD_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.METHOD);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			AExpression term = ((MethodArgument)func.Argument).MethodTerm;
			//Type type = term.GetOperandType();
			if (term.GetOperandType() == typeof(long))
				exm.VEvaluator.RESULT = term.GetIntValue(exm);
			else// if (func.Argument.MethodTerm.GetOperandType() == typeof(string))
				exm.VEvaluator.RESULTS = term.GetStrValue(exm);
			//これら以外の型は現状ない
			//else
			//	throw new ExeEE(func.Function.Name + "命令の型が不明");
		}
	}

	/// <summary>
	/// 代入文
	/// </summary>
	private sealed class SET_Instruction : AInstruction
	{
		public SET_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SET);
			flag = METHOD_SAFE;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (func.Argument is SpSetArrayArgument arg)
			{
				if (arg.VariableDest.IsInteger)
				{
					if (arg.IsConst)
						arg.VariableDest.SetValue(arg.ConstIntList, exm);
					else
					{
						long[] values = new long[arg.TermList.Count];
						for (int i = 0; i < values.Length; i++)
						{
							values[i] = arg.TermList[i].GetIntValue(exm);
						}
						arg.VariableDest.SetValue(values, exm);
					}
				}
				else
				{
					if (arg.IsConst)
						arg.VariableDest.SetValue(arg.ConstStrList, exm);
					else
					{
						string[] values = new string[arg.TermList.Count];
						for (int i = 0; i < values.Length; i++)
						{
							values[i] = arg.TermList[i].GetStrValue(exm);
						}
						arg.VariableDest.SetValue(values, exm);
					}
				}
				return;
			}
			SpSetArgument spsetarg = (SpSetArgument)func.Argument;
			if (spsetarg.VariableDest.IsInteger)
			{
				long src = spsetarg.IsConst ? spsetarg.ConstInt : spsetarg.Term.GetIntValue(exm);
				if (spsetarg.AddConst)
					spsetarg.VariableDest.ChangeValue(src, exm);
				else
					spsetarg.VariableDest.SetValue(src, exm);
			}
			else
			{
				string src = spsetarg.IsConst ? spsetarg.ConstStr : spsetarg.Term.GetStrValue(exm);
				spsetarg.VariableDest.SetValue(src, exm);
			}
		}
	}

	private sealed class REUSELASTLINE_Instruction : AInstruction
	{
		public REUSELASTLINE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
			flag = METHOD_SAFE | EXTENDED | IS_PRINT;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			AExpression term = ((ExpressionArgument)func.Argument).Term;
			string str = term.GetStrValue(exm);
			exm.Console.PrintTemporaryLine(str);
		}
	}

	private sealed class CLEARLINE_Instruction : AInstruction
	{
		public CLEARLINE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED | IS_PRINT;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument intExpArg = (ExpressionArgument)func.Argument;
			int delNum = (int)intExpArg.Term.GetIntValue(exm);
			exm.Console.deleteLine(delNum);
			exm.Console.RefreshStrings(false);
		}
	}

	private sealed class STRLEN_Instruction : AInstruction
	{
		public STRLEN_Instruction(bool argisform, bool unicode)
		{
			if (argisform)
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
			else
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_NULLABLE);
			flag = METHOD_SAFE | EXTENDED;
			this.unicode = unicode;
		}
		bool unicode;
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			string str;
			if (func.Argument.IsConst)
				str = func.Argument.ConstStr;
			else
				str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
			if (unicode)
				exm.VEvaluator.RESULT = str.Length;
			else
				exm.VEvaluator.RESULT = LangManager.GetStrlenLang(str);
		}
	}

	private sealed class SETBIT_Instruction : AInstruction
	{
		public SETBIT_Instruction(int op)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.BIT_ARG);
			flag = METHOD_SAFE | EXTENDED;
			this.op = op;
		}
		int op;
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			BitArgument spsetarg = (BitArgument)func.Argument;
			VariableTerm varTerm = spsetarg.VariableDest;
			var terms = spsetarg.Term;
			for (int i = 0; i < terms.Length; i++)
			{
				long x = terms[i].GetIntValue(exm);
				if ((x < 0) || (x > 63))
					throw new CodeEE(string.Format(trerror.ArgIsOoRBit.Text, "2"));
				long baseValue = varTerm.GetIntValue(exm);
				long shift = 1L << (int)x;
				if (op == 1)
					baseValue |= shift;
				else if (op == 0)
					baseValue &= ~shift;
				else
					baseValue ^= shift;
				varTerm.SetValue(baseValue, exm);
			}
		}
	}

	private sealed class WAIT_Instruction : AInstruction
	{
		public WAIT_Instruction(bool force)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = IS_PRINT;
			isForce = force;
		}
		bool isForce;
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (isForce)
				exm.Console.ReadAnyKey(false, true);
			else
				exm.Console.ReadAnyKey();
		}
	}

	private sealed class WAITANYKEY_Instruction : AInstruction
	{
		public WAITANYKEY_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = IS_PRINT;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.ReadAnyKey(true, false);
		}
	}

	private sealed class TWAIT_Instruction : AInstruction
	{
		public TWAIT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
			flag = IS_PRINT | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.ReadAnyKey();
			SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
			long time = arg.X.GetIntValue(exm);
			long flag = arg.Y.GetIntValue(exm);
			InputRequest req = new()
			{
				InputType = InputType.EnterKey
			};
			if (flag != 0)
				req.InputType = InputType.Void;
			req.Timelimit = time;
			exm.Console.WaitInput(req);
		}
	}

	private sealed class INPUT_Instruction : AInstruction
	{
		public INPUT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
			flag = IS_PRINT | IS_INPUT;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			#region EM_私家版_INPUT系機能拡張
			//ExpressionArgument arg = (ExpressionArgument)func.Argument;
			//InputRequest req = new InputRequest();
			//req.InputType = InputType.IntValue;
			//if (arg.Term != null)
			//{
			//	Int64 def;
			//	if (arg.IsConst)
			//		def = arg.ConstInt;
			//	else
			//		def = arg.Term.GetIntValue(exm);
			//	req.HasDefValue = true;
			//	req.DefIntValue = def;
			//}
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.IntValue
			};

			if (arg.Def != null)
			{
				long def;
				def = arg.Def.GetIntValue(exm);
				req.HasDefValue = true;
				req.DefIntValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			exm.Console.Window.ApplyTextBoxChanges();
			#endregion
			#region EE_INPUT機能拡張
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
				else
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = arg.Def.GetIntValue(exm);
			}
			else
				exm.Console.WaitInput(req);
			#endregion
		}
	}
	private sealed class INPUTS_Instruction : AInstruction
	{
		public INPUTS_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
			flag = IS_PRINT | IS_INPUT;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			#region EM_私家版_INPUT系機能拡張
			//ExpressionArgument arg = (ExpressionArgument)func.Argument;
			//InputRequest req = new InputRequest();
			//req.InputType = InputType.StrValue;
			//if (arg.Term != null)
			//{
			//	string def;
			//	if (arg.IsConst)
			//		def = arg.ConstStr;
			//	else
			//		def = arg.Term.GetStrValue(exm);
			//	req.HasDefValue = true;
			//	req.DefStrValue = def;
			//}
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.StrValue
			};
			if (arg.Def != null)
			{
				string def;
				def = arg.Def.GetStrValue(exm);
				req.HasDefValue = true;
				req.DefStrValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			exm.Console.Window.ApplyTextBoxChanges();
			#endregion
			#region EE_INPUT機能拡張
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
				else
					GlobalStatic.VEvaluator.RESULTS_ARRAY[1] = arg.Def.GetStrValue(exm);
			}
			else
				exm.Console.WaitInput(req);
			#endregion
		}
	}

	private sealed class ONEINPUT_Instruction : AInstruction
	{
		public ONEINPUT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
			flag = IS_PRINT | IS_INPUT | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			#region EM_私家版_INPUT系機能拡張＆ONEINPUT系制限解除
			//ExpressionArgument arg = (ExpressionArgument)func.Argument;
			//InputRequest req = new InputRequest();
			//req.InputType = InputType.IntValue;
			//req.OneInput = true;
			//if (arg.Term != null)
			//{
			//	//TODO:二文字以上セットできるようにするかエラー停止するか
			//	//少なくともONETINPUTとの仕様を統一すべき
			//	Int64 def;
			//	if (arg.IsConst)
			//		def = arg.ConstInt;
			//	else
			//		def = arg.Term.GetIntValue(exm);
			//	if (def > 9)
			//		def = Int64.Parse(def.ToString().Remove(1));
			//	if (def >= 0)
			//	{
			//		req.HasDefValue = true;
			//		req.DefIntValue = def;
			//	}
			//}
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.IntValue,
				OneInput = true
			};
			if (arg.Def != null)
			{
				long def;
				def = arg.Def.GetIntValue(exm);
				req.HasDefValue = true;
				req.DefIntValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			//GlobalStatic.Process.InputInteger(1, 0);
			#endregion
			#region EE_INPUT機能拡張
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
				else
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = arg.Def.GetIntValue(exm);
			}
			else
				exm.Console.WaitInput(req);
			#endregion
		}
	}

	private sealed class ONEINPUTS_Instruction : AInstruction
	{
		public ONEINPUTS_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
			flag = IS_PRINT | IS_INPUT | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			#region EM_私家版_INPUT系機能拡張＆ONEINPUT系制限解除
			//ExpressionArgument arg = (ExpressionArgument)func.Argument;
			//InputRequest req = new InputRequest();
			//req.InputType = InputType.StrValue;
			//req.OneInput = true;
			//if (arg.Term != null)
			//{
			//	string def;
			//	if (arg.IsConst)
			//		def = arg.ConstStr;
			//	else
			//		def = arg.Term.GetStrValue(exm);
			//	if (def.Length > 1)
			//		def = def.Remove(1);
			//	if (def.Length > 0)
			//	{
			//		req.HasDefValue = true;
			//		req.DefStrValue = def;
			//	}
			//}
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.StrValue,
				OneInput = true
			};
			if (arg.Def != null)
			{
				string def;
				def = arg.Def.GetStrValue(exm);
				req.HasDefValue = true;
				req.DefStrValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			//GlobalStatic.Process.InputInteger(1, 0);
			#endregion
			#region EE_INPUT機能拡張
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
				else
					GlobalStatic.VEvaluator.RESULTS_ARRAY[1] = arg.Def.GetStrValue(exm);
			}
			else
				exm.Console.WaitInput(req);
			#endregion
		}
	}

	private sealed class TINPUT_Instruction : AInstruction
	{
		public TINPUT_Instruction(bool oneInput)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_TINPUT);
			flag = IS_PRINT | IS_INPUT | EXTENDED;
			isOne = oneInput;
		}
		bool isOne;
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpTInputsArgument tinputarg = (SpTInputsArgument)func.Argument;

			InputRequest req = new()
			{
				InputType = InputType.IntValue,
				HasDefValue = true,
				OneInput = isOne
			};
			long x = tinputarg.Time.GetIntValue(exm);
			long y = tinputarg.Def.GetIntValue(exm);
			//TODO:ONEINPUTと標準の値を統一
			#region EM_私家版_INPUT系機能拡張
			//if (isOne)
			//{
			//	if (y < 0)
			//		y = Math.Abs(y);
			//	if (y >= 10)
			//		y = y / (long)(Math.Pow(10.0, Math.Log10((double)y)));
			//}
			if (tinputarg.Mouse != null)
			{
				req.MouseInput = tinputarg.Mouse.GetIntValue(exm) == 1;
			}
			#endregion
			long z = (tinputarg.Disp != null) ? tinputarg.Disp.GetIntValue(exm) : 1;
			req.Timelimit = x;
			req.DefIntValue = y;
			req.DisplayTime = z != 0;
			req.TimeUpMes = (tinputarg.Timeout != null) ? tinputarg.Timeout.GetStrValue(exm) : Config.TimeupLabel;
			#region EM_私家版_INPUT系機能拡張
			//GlobalStatic.Process.InputInteger(1, 0);
			#endregion
			#region EE_INPUT機能拡張
			if (tinputarg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (tinputarg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULT = tinputarg.Def.GetIntValue(exm);
				else
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = tinputarg.Def.GetIntValue(exm);
			}
			else
				exm.Console.WaitInput(req);
			#endregion
		}
	}

	private sealed class TINPUTS_Instruction : AInstruction
	{
		public TINPUTS_Instruction(bool oneInput)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_TINPUTS);
			flag = IS_PRINT | IS_INPUT | EXTENDED;
			isOne = oneInput;
		}
		bool isOne;
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpTInputsArgument tinputarg = (SpTInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.StrValue,
				HasDefValue = true,
				OneInput = isOne
			};
			long x = tinputarg.Time.GetIntValue(exm);
			string strs = tinputarg.Def.GetStrValue(exm);
			#region EM_私家版_INPUT系機能拡張
			//if (isOne && strs.Length > 1)
			//	strs = strs.Remove(1);
			if (tinputarg.Mouse != null)
			{
				req.MouseInput = tinputarg.Mouse.GetIntValue(exm) == 1;
			}
			#endregion
			long z = (tinputarg.Disp != null) ? tinputarg.Disp.GetIntValue(exm) : 1;
			req.Timelimit = x;
			req.DefStrValue = strs;
			req.DisplayTime = z != 0;
			req.TimeUpMes = (tinputarg.Timeout != null) ? tinputarg.Timeout.GetStrValue(exm) : Config.TimeupLabel;
			#region EM_私家版_INPUT系機能拡張
			//GlobalStatic.Process.InputInteger(1, 0);
			#endregion
			#region EE_INPUT機能拡張
			if (tinputarg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (tinputarg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULTS = tinputarg.Def.GetStrValue(exm);
				else
					GlobalStatic.VEvaluator.RESULTS_ARRAY[1] = tinputarg.Def.GetStrValue(exm);
			}
			else
				exm.Console.WaitInput(req);
			#endregion
		}
	}

	private sealed class CALLF_Instruction : AInstruction
	{
		public CALLF_Instruction(bool form)
		{
			if (form)
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORMF);
			else
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLF);
			flag = EXTENDED | METHOD_SAFE | FORCE_SETARG;
		}

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			if (!func.Argument.IsConst)
			{
				useCallForm = true;
				return;
			}
			SpCallFArgment callfArg = (SpCallFArgment)func.Argument;
			try
			{
				callfArg.FuncTerm = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, callfArg.ConstStr, callfArg.RowArgs, true);
			}
			catch (CodeEE e)
			{
				ParserMediator.Warn(e.Message, func, 2, true, false);
				return;
			}
			if (callfArg.FuncTerm == null)
			{
				if (!Program.AnalysisMode)
					ParserMediator.Warn(string.Format(trerror.NotDefinedFunc.Text, callfArg.ConstStr), func, 2, true, false);
				else
					ParserMediator.Warn(callfArg.ConstStr, func, 2, true, false);
				return;
			}
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			AExpression mToken;
			string labelName;
			if ((!func.Argument.IsConst) || exm.Console.RunERBFromMemory)
			{
				SpCallFArgment spCallformArg = (SpCallFArgment)func.Argument;
				labelName = spCallformArg.FuncnameTerm.GetStrValue(exm);
				mToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, labelName, spCallformArg.RowArgs, true);
			}
			else
			{
				labelName = func.Argument.ConstStr;
				mToken = ((SpCallFArgment)func.Argument).FuncTerm;
			}
			if (mToken == null)
				throw new CodeEE(string.Format(trerror.NotDefinedUserFunc.Text, labelName));
			mToken.GetValue(exm);
		}
	}

	private sealed class CALLSHARP_Instruction : AInstruction
	{
		public CALLSHARP_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLCSHARP);
			flag = EXTENDED | METHOD_SAFE | FORCE_SETARG;
		}

		public override Argument CreateArgument(InstructionLine line, ExpressionMediator exm)
		{
			CharStream st = line.PopArgumentPrimitive();
			string rowStr;
			if (st.EOS)
				throw new CodeEE(trerror.MissingArg.Text);
			else
				rowStr = st.Substring();
			rowStr = GlobalStatic.Console.getStBar(rowStr);
			Argument ret = new ExpressionArgument(new SingleStrTerm(rowStr))
			{
				ConstStr = rowStr,
				IsConst = true
			};
			return ret;
		}

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			if (!func.Argument.IsConst)
			{
				useCallForm = true;
				return;
			}

			SpCallSharpArgment arg = (SpCallSharpArgment)func.Argument;
			var manager = PluginManager.GetInstance();
			if (!manager.HasMethod(arg.ConstStr))
			{
				ParserMediator.Warn(string.Format(trerror.MethodNotFound.Text, arg.ConstStr), func, 2, true, false);
				return;
			}

			arg.CallFunc = manager.GetMethod(func.Argument.ConstStr);
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{

			SpCallSharpArgment arg = (SpCallSharpArgment)func.Argument;
			var manager = PluginManager.GetInstance();

			var pluginArgs = arg.RowArgs.Select((term) => PluginMethodParameterBuilder.ConvertTerm(term, exm)).ToArray();
			arg.CallFunc.Execute(pluginArgs);
			for (var i = 0; i < pluginArgs.Count(); ++i)
			{
				var rowArg = arg.RowArgs[i];
				if (rowArg is VariableTerm)
				{
					var varTerm = (VariableTerm)rowArg;
					if (varTerm.IsString)
					{
						varTerm.SetValue(pluginArgs[i].strValue, exm);
					}
					else
					{
						varTerm.SetValue(pluginArgs[i].intValue, exm);
					}
				}
			}
		}
	}

	#region EE_TRYCALLF
	private sealed class TRYCALLF_Instruction : AInstruction
	{
		public TRYCALLF_Instruction(bool form)
		{
			if (form)
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORMF);
			else
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLF);
			flag = EXTENDED | METHOD_SAFE | FORCE_SETARG;
		}

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			if (!func.Argument.IsConst)
			{
				useCallForm = true;
				return;
			}
			SpCallFArgment callfArg = (SpCallFArgment)func.Argument;
			//if (Config.Config.IgnoreCase)
			//	callfArg.ConstStr = callfArg.ConstStr.ToUpper();
			try
			{
				callfArg.FuncTerm = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, callfArg.ConstStr, callfArg.RowArgs, true);
			}
			catch
			{
				return;
			}
			if (callfArg.FuncTerm == null)
			{
				return;
			}
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			AExpression mToken;
			string labelName;
			if ((!func.Argument.IsConst) || exm.Console.RunERBFromMemory)
			{
				SpCallFArgment spCallformArg = (SpCallFArgment)func.Argument;
				labelName = spCallformArg.FuncnameTerm.GetStrValue(exm);
				mToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, labelName, spCallformArg.RowArgs, true);
			}
			else
			{
				labelName = func.Argument.ConstStr;
				mToken = ((SpCallFArgment)func.Argument).FuncTerm;
			}
			if (mToken == null)
				return;
			mToken.GetValue(exm);
		}
	}
	#endregion

	private sealed class BAR_Instruction : AInstruction
	{
		public BAR_Instruction(bool newline)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_BAR);
			flag = IS_PRINT | METHOD_SAFE | EXTENDED;
			this.newline = newline;
		}
		bool newline;

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpBarArgument barArg = (SpBarArgument)func.Argument;
			long var = barArg.Terms[0].GetIntValue(exm);
			long max = barArg.Terms[1].GetIntValue(exm);
			long length = barArg.Terms[2].GetIntValue(exm);
			exm.Console.Print(ExpressionMediator.CreateBar(var, max, length));
			if (newline)
				exm.Console.NewLine();
		}
	}

	private sealed class TIMES_Instruction : AInstruction
	{
		public TIMES_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_TIMES);
			flag = METHOD_SAFE;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpTimesArgument timesArg = (SpTimesArgument)func.Argument;
			VariableTerm var = timesArg.VariableDest;
			if (Config.TimesNotRigorousCalculation)
			{
				double d = var.GetIntValue(exm) * timesArg.DoubleValue;
				unchecked
				{
					var.SetValue((long)d, exm);
				}
			}
			else
			{
				decimal d = var.GetIntValue(exm) * (decimal)timesArg.DoubleValue;
				unchecked
				{
					//decimal型は強制的にOverFlowExceptionを投げるので対策が必要
					//OverFlowの場合は昔の挙動に近づけてみる
					if (d <= long.MaxValue && d >= long.MinValue)
						var.SetValue((long)d, exm);
					else
						var.SetValue((long)(double)d, exm);
				}
			}
		}
	}


	private sealed class ADDCHARA_Instruction : AInstruction
	{
		public ADDCHARA_Instruction(bool flagSp, bool flagDel)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
			flag = METHOD_SAFE;
			isDel = flagDel;
			isSp = flagSp;
		}
		bool isDel;
		bool isSp;

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!Config.CompatiSPChara && isSp)
				throw new CodeEE(trerror.SPCharaConfigIsOff.Text);
			ExpressionArrayArgument intExpArg = (ExpressionArrayArgument)func.Argument;
			long integer;
			long[] charaNoList = new long[intExpArg.TermList.Length];
			int i = 0;
			foreach (AExpression int64Term in intExpArg.TermList)
			{
				integer = int64Term.GetIntValue(exm);
				if (isDel)
				{
					charaNoList[i] = integer;
					i++;
				}
				else
				{
					if (Config.CompatiSPChara)
						exm.VEvaluator.AddCharacter_UseSp(integer, isSp);
					else
						exm.VEvaluator.AddCharacter(integer);
				}
			}
			if (isDel)
			{
				if (charaNoList.Length == 1)
					exm.VEvaluator.DelCharacter(charaNoList[0]);
				else
					exm.VEvaluator.DelCharacter(charaNoList);
			}
		}
	}

	private sealed class ADDVOIDCHARA_Instruction : AInstruction
	{
		public ADDVOIDCHARA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.VEvaluator.AddPseudoCharacter();
		}
	}

	private sealed class SWAPCHARA_Instruction : AInstruction
	{
		public SWAPCHARA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
			long x = arg.X.GetIntValue(exm);
			long y = arg.Y.GetIntValue(exm);
			exm.VEvaluator.SwapChara(x, y);
		}
	}
	private sealed class COPYCHARA_Instruction : AInstruction
	{
		public COPYCHARA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
			long x = arg.X.GetIntValue(exm);
			long y = arg.Y.GetIntValue(exm);
			exm.VEvaluator.CopyChara(x, y);
		}
	}

	private sealed class ADDCOPYCHARA_Instruction : AInstruction
	{
		public ADDCOPYCHARA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
			flag = METHOD_SAFE;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArrayArgument intExpArg = (ExpressionArrayArgument)func.Argument;
			foreach (AExpression int64Term in intExpArg.TermList)
				exm.VEvaluator.AddCopyChara(int64Term.GetIntValue(exm));
		}
	}

	private sealed class SORTCHARA_Instruction : AInstruction
	{
		public SORTCHARA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SORTCHARA);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpSortcharaArgument spSortArg = (SpSortcharaArgument)func.Argument;
			long elem = 0;
			VariableTerm sortKey = spSortArg.SortKey;
			if (sortKey.Identifier.IsArray1D)
				elem = sortKey.GetElementInt(1, exm);
			else if (sortKey.Identifier.IsArray2D)
			{
				elem = sortKey.GetElementInt(1, exm) << 32;
				elem += sortKey.GetElementInt(2, exm);
			}

			exm.VEvaluator.SortChara(sortKey.Identifier, elem, spSortArg.SortOrder, true);
		}
	}

	private sealed class RESETCOLOR_Instruction : AInstruction
	{
		public RESETCOLOR_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.SetStringStyle(Config.ForeColor);
		}
	}

	private sealed class RESETBGCOLOR_Instruction : AInstruction
	{
		public RESETBGCOLOR_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.SetBgColor(Config.BackColor);
		}
	}

	private sealed class SETBGIMAGE_Instruction : AInstruction
	{
		public SETBGIMAGE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_ANY);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArrayArgument arg = (ExpressionArrayArgument)func.Argument;
			string bgName;
			long bgDepth = 0;
			bgName = arg.TermList[0].GetStrValue(exm);
			float opacity = 1.0f;
			if (arg.TermList.Count() >= 2)
			{
				bgDepth = long.Parse(arg.TermList[1].GetStrValue(exm));
			}
			if (arg.TermList.Count() >= 3)
			{
				opacity = long.Parse(arg.TermList[2].GetStrValue(exm)) / 255.0f;
			}
			exm.Console.AddBackgroundImage(bgName, bgDepth, opacity);
		}
	}
	private sealed class REMOVEBGIMAGE_Instruction : AInstruction
	{
		public REMOVEBGIMAGE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_ANY);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArrayArgument arg = (ExpressionArrayArgument)func.Argument;
			string bgName;
			bgName = arg.TermList[0].GetStrValue(exm);
			exm.Console.RemoveBackground(bgName);
		}
	}
	private sealed class CLEARBGIMAGE_Instruction : AInstruction
	{
		public CLEARBGIMAGE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.ClearBackgroundImage();
		}
	}

	private sealed class FONTBOLD_Instruction : AInstruction
	{
		public FONTBOLD_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!OperatingSystem.IsWindows())
				return;
			exm.Console.SetStringStyle(exm.Console.StringStyle.FontStyle | FontStyle.Bold);
		}
	}
	private sealed class FONTITALIC_Instruction : AInstruction
	{
		public FONTITALIC_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!OperatingSystem.IsWindows())
				return;
			exm.Console.SetStringStyle(exm.Console.StringStyle.FontStyle | FontStyle.Italic);
		}
	}
	private sealed class FONTREGULAR_Instruction : AInstruction
	{
		public FONTREGULAR_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (!OperatingSystem.IsWindows())
				return;
			exm.Console.SetStringStyle(FontStyle.Regular);
		}
	}

	private sealed class VARSET_Instruction : AInstruction
	{
		public VARSET_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_VAR_SET);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{

			SpVarSetArgument spvarsetarg = (SpVarSetArgument)func.Argument;
			VariableTerm var = spvarsetarg.VariableDest;
			FixedVariableTerm p = var.GetFixedVariableTerm(exm);
			int start = 0;
			int end = 0;
			//endを先に取って判定の処理変更
			if (spvarsetarg.End != null)
				end = (int)spvarsetarg.End.GetIntValue(exm);
			else if (var.Identifier.IsArray1D)
				end = var.GetLength();
			if (spvarsetarg.Start != null)
			{
				start = (int)spvarsetarg.Start.GetIntValue(exm);
				if (start > end)
				{
					(end, start) = (start, end);
				}
			}
			if (var.IsString)
			{
				string src = spvarsetarg.Term.GetStrValue(exm);
				VariableEvaluator.SetValueAll(p, src, start, end);
			}
			else
			{
				long src = spvarsetarg.Term.GetIntValue(exm);
				VariableEvaluator.SetValueAll(p, src, start, end);
			}
		}
	}

	private sealed class CVARSET_Instruction : AInstruction
	{
		public CVARSET_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CVAR_SET);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpCVarSetArgument spvarsetarg = (SpCVarSetArgument)func.Argument;
			FixedVariableTerm p = spvarsetarg.VariableDest.GetFixedVariableTerm(exm);
			SingleTerm index = spvarsetarg.Index.GetValue(exm);
			int charaNum = (int)exm.VEvaluator.CHARANUM;
			int start = 0;
			if (spvarsetarg.Start != null)
			{
				start = (int)spvarsetarg.Start.GetIntValue(exm);
				if (start < 0 || start >= charaNum)
					throw new CodeEE(string.Format(trerror.OoRCvarsetArg.Text, "4", start.ToString()));
			}
			int end;
			if (spvarsetarg.End != null)
			{
				end = (int)spvarsetarg.End.GetIntValue(exm);
				if (end < 0 || end > charaNum)
					throw new CodeEE(string.Format(trerror.OoRCvarsetArg.Text, "5", start.ToString()));
			}
			else
				end = charaNum;
			if (start > end)
			{
				int temp = start;
				start = end;
				end = temp;
			}
			if (!p.Identifier.IsCharacterData)
				throw new CodeEE(string.Format(trerror.CvarsetArgIsNotCharaVar.Text, p.Identifier.Name));
			if (index is SingleStrTerm singleStrTerm && p.Identifier.IsArray1D)
			{
				if (!GlobalStatic.ConstantData.isDefined(p.Identifier.Code, singleStrTerm.Str))
					throw new CodeEE(string.Format(trerror.NotDefinedKey.Text, p.Identifier.Name, singleStrTerm.Str));
			}
			if (p.Identifier.IsString)
			{
				string src = spvarsetarg.Term.GetStrValue(exm);
				exm.VEvaluator.SetValueAllEachChara(p, index, src, start, end);
			}
			else
			{
				long src = spvarsetarg.Term.GetIntValue(exm);
				exm.VEvaluator.SetValueAllEachChara(p, index, src, start, end);
			}
		}
	}

	private sealed class RANDOMIZE_Instruction : AInstruction
	{
		public RANDOMIZE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION_NULLABLE);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			long iValue;
			if (func.Argument.IsConst)
				iValue = func.Argument.ConstInt;
			else
				iValue = ((ExpressionArgument)func.Argument).Term.GetIntValue(exm);
			if (JSONConfig.Data.UseNewRandom)
			{
				ParserMediator.Warn(trerror.IgnoreRandomize.Text, null, 0);
				ParserMediator.FlushWarningList();
			}
			else
			{
				exm.VEvaluator.Randomize(iValue);
			}
		}
	}
	private sealed class INITRAND_Instruction : AInstruction
	{
		public INITRAND_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (JSONConfig.Data.UseNewRandom)
			{
				ParserMediator.Warn(trerror.CanNotUseInitrand.Text, null, 0);
				ParserMediator.FlushWarningList();
			}
			else
			{
				exm.VEvaluator.InitRanddata();
			}
		}
	}

	private sealed class DUMPRAND_Instruction : AInstruction
	{
		public DUMPRAND_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (JSONConfig.Data.UseNewRandom)
			{
				ParserMediator.Warn(trerror.CanNotUseDumprand.Text, null, 0);
				ParserMediator.FlushWarningList();
			}
			else
			{
				exm.VEvaluator.DumpRanddata();
			}
		}
	}


	private sealed class SAVEGLOBAL_Instruction : AInstruction
	{
		public SAVEGLOBAL_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.VEvaluator.SaveGlobal();
		}
	}

	private sealed class LOADGLOBAL_Instruction : AInstruction
	{
		public LOADGLOBAL_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (exm.VEvaluator.LoadGlobal())
				exm.VEvaluator.RESULT = 1;
			else
				exm.VEvaluator.RESULT = 0;
		}
	}

	private sealed class RESETDATA_Instruction : AInstruction
	{
		public RESETDATA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.VEvaluator.ResetData();
			exm.Console.ResetStyle();
		}
	}

	private sealed class RESETGLOBAL_Instruction : AInstruction
	{
		public RESETGLOBAL_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.VEvaluator.ResetGlobalData();
		}
	}

	private static int toUInt32inArg(long value, string funcName, int argnum)
	{
		if (value < 0)
			throw new CodeEE(string.Format(trerror.ArgIsNegative.Text, funcName, argnum.ToString(), value.ToString()));
		else if (value > int.MaxValue)
			throw new CodeEE(string.Format(trerror.ArgIsTooLarge.Text, funcName, argnum.ToString(), value.ToString()));

		return (int)value;
	}

	private sealed class SAVECHARA_Instruction : AInstruction
	{
		public SAVECHARA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SAVECHARA);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArrayArgument arg = (ExpressionArrayArgument)func.Argument;
			var terms = arg.TermList;
			string datFilename = terms[0].GetStrValue(exm);
			string savMes = terms[1].GetStrValue(exm);
			int[] savCharaList = new int[terms.Length - 2];
			int charanum = (int)exm.VEvaluator.CHARANUM;
			for (int i = 0; i < savCharaList.Length; i++)
			{
				long v = terms[i + 2].GetIntValue(exm);
				savCharaList[i] = toUInt32inArg(v, "SAVECHARA", i + 3);
				if (savCharaList[i] >= charanum)
					throw new CodeEE(string.Format(trerror.OoRSavecharaArg.Text, (i + 3).ToString()));
				for (int j = 0; j < i; j++)
				{
					if (savCharaList[i] == savCharaList[j])
						throw new CodeEE(string.Format(trerror.DuplicateCharaNo.Text, savCharaList[i].ToString()));
				}
			}
			exm.VEvaluator.SaveChara(datFilename, savMes, savCharaList);
		}
	}

	private sealed class LOADCHARA_Instruction : AInstruction
	{
		public LOADCHARA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument arg = (ExpressionArgument)func.Argument;
			string datFilename;
			if (arg.IsConst)
				datFilename = arg.ConstStr;
			else
				datFilename = arg.Term.GetStrValue(exm);
			exm.VEvaluator.LoadChara(datFilename);
		}
	}


	private sealed class SAVEVAR_Instruction : AInstruction
	{
		public SAVEVAR_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SAVEVAR);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			throw new NotImplCodeEE();
			//SpSaveVarArgument arg = (SpSaveVarArgument)func.Argument;
			//VariableToken[] vars = arg.VarTokens;
			//string datFilename = arg.Term.GetStrValue(exm);
			//string savMes = arg.SavMes.GetStrValue(exm);
			//exm.VEvaluator.SaveVariable(datFilename, savMes, vars);
		}
	}
	private sealed class LOADVAR_Instruction : AInstruction
	{
		public LOADVAR_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			throw new NotImplCodeEE();
			//ExpressionArgument arg = (ExpressionArgument)func.Argument;
			//string datFilename = null;
			//if (arg.IsConst)
			//    datFilename = arg.ConstStr;
			//else
			//    datFilename = arg.Term.GetStrValue(exm);
			//exm.VEvaluator.LoadVariable(datFilename);

		}
	}

	private sealed class DELDATA_Instruction : AInstruction
	{
		public DELDATA_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			long target;
			if (func.Argument.IsConst)
				target = func.Argument.ConstInt;
			else
				target = ((ExpressionArgument)func.Argument).Term.GetIntValue(exm);

			int target32 = toUInt32inArg(target, "DELDATA", 1);
			VariableEvaluator.DelData(target32);
		}
	}

	private sealed class DO_NOTHING_Instruction : AInstruction
	{
		public DO_NOTHING_Instruction()
		{
			//事実上ENDIFの非フローコントロール版
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED | PARTIAL;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//何もしない
		}
	}

	private sealed class REF_Instruction : AInstruction
	{
		public REF_Instruction(bool byname)
		{
			this.byname = byname;
			if (byname)
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_REFBYNAME);
			else
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_REF);

			flag = METHOD_SAFE | EXTENDED;
		}
		bool byname;

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			throw new NotImplCodeEE();

#pragma warning disable CS0162 // 到達できないコードが検出されました
			RefArgument arg = (RefArgument)func.Argument;
#pragma warning restore CS0162 // 到達できないコードが検出されました
			string str = null;
			if (arg.SrcTerm != null)
				str = arg.SrcTerm.GetStrValue(exm);
			if (arg.RefMethodToken != null)
			{
				UserDefinedRefMethod srcRef = arg.SrcRefMethodToken;
				CalledFunction call = arg.SrcCalledFunction;
				if (str != null)//REFBYNAMEかつ第二引数が定数でない
				{
					srcRef = GlobalStatic.IdentifierDictionary.GetRefMethod(str);
					if (srcRef == null)
					{
						FunctionLabelLine label = GlobalStatic.LabelDictionary.GetNonEventLabel(str);
						//if (label == null)
						//    throw new CodeEE("式中関数" + str + "が見つかりません");
						//if (!label.IsMethod)
						//    throw new CodeEE("#FUNCTION(S)属性を持たない関数" + str + "は参照できません");
						if (label != null && label.IsMethod)
							call = CalledFunction.CreateCalledFunctionMethod(label, str);
					}
				}
				else if (srcRef != null)
					call = srcRef.CalledFunction;//第二引数が関数参照。callがnullならエラー
				if (call == null || !arg.RefMethodToken.MatchType(call))
				{
					arg.RefMethodToken.SetReference(null);
					exm.VEvaluator.RESULT = 0;
				}
				else
				{
					arg.RefMethodToken.SetReference(call);
					exm.VEvaluator.RESULT = 1;
				}
				return;
			}

			ReferenceToken refVar = arg.RefVarToken;
			VariableToken srcVar = arg.SrcVarToken;
			string errmes;
			if (str != null)
			{
				srcVar = GlobalStatic.IdentifierDictionary.GetVariableToken(str, null, true);

				//if (srcVar == null)
				//    throw new CodeEE("変数" + str + "が見つかりません");
			}
			if (srcVar == null || !refVar.MatchType(srcVar, false, out errmes))
			{
				refVar.SetRef(null);
				exm.VEvaluator.RESULT = 0;
			}
			else
			{
				refVar.SetRef((Array)srcVar.GetArray());
				exm.VEvaluator.RESULT = 1;
			}
			return;
		}
	}

	private sealed class TOOLTIP_SETCOLOR_Instruction : AInstruction
	{
		public TOOLTIP_SETCOLOR_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
			long foreColor = arg.X.GetIntValue(exm);
			long backColor = arg.Y.GetIntValue(exm);
			if (foreColor < 0 || foreColor > 0xFFFFFF)
				throw new CodeEE(string.Format(trerror.ArgIsOoRColorCode.Text, "1"));
			if (backColor < 0 || backColor > 0xFFFFFF)
				throw new CodeEE(string.Format(trerror.ArgIsOoRColorCode.Text, "2"));
			Color fc = Color.FromArgb((int)foreColor >> 16, (int)foreColor >> 8 & 0xFF, (int)foreColor & 0xFF);
			Color bc = Color.FromArgb((int)backColor >> 16, (int)backColor >> 8 & 0xFF, (int)backColor & 0xFF);
			exm.Console.SetToolTipColor(fc, bc);
			return;
		}
	}

	private sealed class TOOLTIP_SETDELAY_Instruction : AInstruction
	{
		public TOOLTIP_SETDELAY_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument arg = (ExpressionArgument)func.Argument;
			long delay;
			if (arg.IsConst)
				delay = arg.ConstInt;
			else
				delay = arg.Term.GetIntValue(exm);
			if (delay < 0 || delay > int.MaxValue)
				throw new CodeEE(trerror.ArgIsOoR.Text);
			exm.Console.SetToolTipDelay((int)delay);
			return;
		}
	}

	private sealed class TOOLTIP_SETDURATION_Instruction : AInstruction
	{
		public TOOLTIP_SETDURATION_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument arg = (ExpressionArgument)func.Argument;
			long duration;
			if (arg.IsConst)
				duration = arg.ConstInt;
			else
				duration = arg.Term.GetIntValue(exm);
			if (duration < 0 || duration > int.MaxValue)
				throw new CodeEE(trerror.ArgIsOoR.Text);
			if (duration > short.MaxValue)
				duration = short.MaxValue;
			exm.Console.SetToolTipDuration((int)duration);
			return;
		}
	}

	private sealed class INPUTMOUSEKEY_Instruction : AInstruction
	{
		public INPUTMOUSEKEY_Instruction()
		{
			ArgBuilder = ArgumentParser.GetNormalArgumentBuilder("I", 0);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionsArgument arg = (ExpressionsArgument)func.Argument;
			long time = 0;
			if (arg.ArgumentArray.Count > 0)
				time = arg.ArgumentArray[0].GetIntValue(exm);
			InputRequest req = new()
			{
				InputType = InputType.PrimitiveMouseKey
			};
			if (time > 0)
				req.Timelimit = (int)time;
			exm.Console.WaitInput(req);
		}
	}
	#region EE_INPUTANY
	private sealed class INPUTANY_Instruction : AInstruction
	{
		public INPUTANY_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			InputRequest req = new()
			{
				InputType = InputType.AnyValue
			};
			exm.Console.WaitInput(req);
		}
	}
	#endregion
	#region EE_BINPUT
	private sealed class BINPUT_Instruction : AInstruction
	{
		public BINPUT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
			flag = IS_PRINT | IS_INPUT;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//実行時点で描画されてないときがあるのでやっておく
			if (!exm.Console.PrintBuffer.IsEmpty)
				exm.Console.NewLine();
			exm.Console.RefreshStrings(true);
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.IntButton
			};
			if (arg.Def != null)
			{
				long def;
				def = arg.Def.GetIntValue(exm);
				req.HasDefValue = true;
				req.DefIntValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			exm.Console.Window.ApplyTextBoxChanges();
			int count = 0;
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
				else
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = arg.Def.GetIntValue(exm);
			}
			else
			{
				foreach (ConsoleDisplayLine line in Enumerable.Reverse(exm.Console.DisplayLineList).ToList())
				{
					foreach (ConsoleButtonString button in line.Buttons)
					{
						if (button.Generation != 0 && button.Generation != exm.Console.LastButtonGeneration)
							goto loopep;
						else if (button.IsButton && button.IsInteger)
							count++;
					}
				}
			loopep:
				List<AConsoleDisplayNode> ep;
				foreach (var value in exm.Console.EscapedParts)
				{
					ep = value.Value;
					foreach (var part in ep)
					{
						if (part is ConsoleDivPart div)
						{
							foreach (ConsoleDisplayLine line in Enumerable.Reverse(div.Children).ToList())
							{
								foreach (ConsoleButtonString button in line.Buttons)
								{
									if (button.IsButton && button.IsInteger)
									{
										count++;
										goto loopend;
									}
								}
							}
						}
					}
				}

			}
		loopend:
			if (count == 0)
			{
				if (arg.Def == null)
					throw new CodeEE(string.Format(trerror.NothingButtonBinput.Text, "BINPUT"));
				else
				{
					GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
					return;
				}
			}
			exm.Console.WaitInput(req);
		}
	}
	private sealed class BINPUTS_Instruction : AInstruction
	{
		public BINPUTS_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
			flag = IS_PRINT | IS_INPUT;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//ExpressionArgument arg = (ExpressionArgument)func.Argument;
			//InputRequest req = new InputRequest();
			//req.InputType = InputType.StrValue;
			//if (arg.Term != null)
			//{
			//	string def;
			//	if (arg.IsConst)
			//		def = arg.ConstStr;
			//	else
			//		def = arg.Term.GetStrValue(exm);
			//	req.HasDefValue = true;
			//	req.DefStrValue = def;
			//}
			//実行時点で描画されてないときがあるのでやっておく
			if (!exm.Console.PrintBuffer.IsEmpty)
				exm.Console.NewLine();
			exm.Console.RefreshStrings(true);
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.StrButton
			};
			if (arg.Def != null)
			{
				string def;
				def = arg.Def.GetStrValue(exm);
				req.HasDefValue = true;
				req.DefStrValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			exm.Console.Window.ApplyTextBoxChanges();
			int count = 0;
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
				else
					GlobalStatic.VEvaluator.RESULTS_ARRAY[1] = arg.Def.GetStrValue(exm);
			}
			else
			{
				foreach (ConsoleDisplayLine line in Enumerable.Reverse(exm.Console.DisplayLineList).ToList())
				{
					foreach (ConsoleButtonString button in line.Buttons)
					{
						if (button.Generation != 0 && button.Generation != exm.Console.LastButtonGeneration)
							goto loopep;
						else if (button.IsButton)
							count++;
					}
				}
			loopep:
				List<AConsoleDisplayNode> ep;
				foreach (var value in exm.Console.EscapedParts)
				{
					ep = value.Value;
					foreach (var part in ep)
					{
						if (part is ConsoleDivPart div)
						{
							foreach (ConsoleDisplayLine line in Enumerable.Reverse(div.Children).ToList())
							{
								foreach (ConsoleButtonString button in line.Buttons)
								{
									if (button.IsButton)
									{
										count++;
										goto loopend;
									}
								}
							}
						}
					}
				}

			}
		loopend:
			if (count == 0)
			{
				if (arg.Def == null)
					throw new CodeEE(string.Format(trerror.NothingButtonBinput.Text, "BINPUTS"));
				else
				{
					GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
					return;
				}
			}
			exm.Console.WaitInput(req);
		}
	}
	#endregion

	#region EE_ONEBINPUT
	private sealed class ONEBINPUT_Instruction : AInstruction
	{
		public ONEBINPUT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
			flag = IS_PRINT | IS_INPUT;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//実行時点で描画されてないときがあるのでやっておく
			if (!exm.Console.PrintBuffer.IsEmpty)
				exm.Console.NewLine();
			exm.Console.RefreshStrings(true);
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.IntButton,
				OneInput = true
			};
			if (arg.Def != null)
			{
				long def;
				def = arg.Def.GetIntValue(exm);
				req.HasDefValue = true;
				req.DefIntValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			exm.Console.Window.ApplyTextBoxChanges();
			int count = 0;
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
				else
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = arg.Def.GetIntValue(exm);
			}
			else
			{
				foreach (ConsoleDisplayLine line in Enumerable.Reverse(exm.Console.DisplayLineList).ToList())
				{
					foreach (ConsoleButtonString button in line.Buttons)
					{
						if (button.Generation != 0 && button.Generation != exm.Console.LastButtonGeneration)
							goto loopep;
						else if (button.IsButton && button.IsInteger)
							count++;
					}
				}
			loopep:
				List<AConsoleDisplayNode> ep;
				foreach (var value in exm.Console.EscapedParts)
				{
					ep = value.Value;
					foreach (var part in ep)
					{
						if (part is ConsoleDivPart div)
						{
							foreach (ConsoleDisplayLine line in Enumerable.Reverse(div.Children).ToList())
							{
								foreach (ConsoleButtonString button in line.Buttons)
								{
									if (button.IsButton && button.IsInteger)
									{
										count++;
										goto loopend;
									}
								}
							}
						}
					}
				}
			}
		loopend:
			if (count == 0)
			{
				if (arg.Def == null)
					throw new CodeEE(string.Format(trerror.NothingButtonBinput.Text, "ONEBINPUT"));
				else
				{
					GlobalStatic.VEvaluator.RESULT = arg.Def.GetIntValue(exm);
					return;
				}
			}
			exm.Console.WaitInput(req);
		}
	}
	private sealed class ONEBINPUTS_Instruction : AInstruction
	{
		public ONEBINPUTS_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
			flag = IS_PRINT | IS_INPUT;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//ExpressionArgument arg = (ExpressionArgument)func.Argument;
			//InputRequest req = new InputRequest();
			//req.InputType = InputType.StrValue;
			//if (arg.Term != null)
			//{
			//	string def;
			//	if (arg.IsConst)
			//		def = arg.ConstStr;
			//	else
			//		def = arg.Term.GetStrValue(exm);
			//	req.HasDefValue = true;
			//	req.DefStrValue = def;
			//}
			//実行時点で描画されてないときがあるのでやっておく
			if (!exm.Console.PrintBuffer.IsEmpty)
				exm.Console.NewLine();
			exm.Console.RefreshStrings(true);
			SpInputsArgument arg = (SpInputsArgument)func.Argument;
			InputRequest req = new()
			{
				InputType = InputType.StrButton,
				OneInput = true
			};
			if (arg.Def != null)
			{
				string def;
				def = arg.Def.GetStrValue(exm);
				req.HasDefValue = true;
				req.DefStrValue = def;
			}
			if (arg.Mouse != null)
			{
				req.MouseInput = arg.Mouse.GetIntValue(exm) != 0;
			}
			exm.Console.Window.ApplyTextBoxChanges();
			int count = 0;
			if (arg.CanSkip != null && GlobalStatic.Console.MesSkip)
			{
				if (arg.Mouse.GetIntValue(exm) == 0)
					GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
				else
					GlobalStatic.VEvaluator.RESULTS_ARRAY[1] = arg.Def.GetStrValue(exm);
			}
			else
			{
				foreach (ConsoleDisplayLine line in Enumerable.Reverse(exm.Console.DisplayLineList).ToList())
				{
					foreach (ConsoleButtonString button in line.Buttons)
					{
						if (button.Generation != 0 && button.Generation != exm.Console.LastButtonGeneration)
							goto loopep;
						else if (button.IsButton)
							count++;
					}
				}
			loopep:
				List<AConsoleDisplayNode> ep;
				foreach (var value in exm.Console.EscapedParts)
				{
					ep = value.Value;
					foreach (var part in ep)
					{
						if (part is ConsoleDivPart div)
						{
							foreach (ConsoleDisplayLine line in Enumerable.Reverse(div.Children).ToList())
							{
								foreach (ConsoleButtonString button in line.Buttons)
								{
									if (button.IsButton)
									{
										count++;
										goto loopend;
									}
								}
							}
						}
					}
				}

			}
		loopend:
			if (count == 0)
			{
				if (arg.Def == null)
					throw new CodeEE(string.Format(trerror.NothingButtonBinput.Text, "ONEBINPUTS"));
				else
				{
					GlobalStatic.VEvaluator.RESULTS = arg.Def.GetStrValue(exm);
					return;
				}
			}
			exm.Console.WaitInput(req);
		}
	}
	#endregion
	#region EM_DT
	private sealed class DT_COLUMN_OPTIONS_Instruction : AInstruction
	{
		public DT_COLUMN_OPTIONS_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_DT_COLUMN_OPTIONS);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED | METHOD_SAFE;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			var arg = (SpDtColumnOptions)func.Argument;
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			var cName = arg.Column.GetStrValue(exm);
			var key = arg.DT.GetStrValue(exm);
			if (!dict.ContainsKey(key)) exm.VEvaluator.RESULT = -1;
			var dt = dict[key];
			if (!dt.Columns.Contains(cName)) exm.VEvaluator.RESULT = 0;
			var column = dt.Columns[cName];
			bool isString = column.DataType == typeof(string);
			int idx = 0;
			foreach (var opt in arg.Options)
			{
				var v = arg.Values[idx];
				switch (opt)
				{
					case SpDtColumnOptions.DTOptions.Default:
						if (v.GetOperandType() != (isString ? typeof(string) : typeof(long)))
							throw new CodeEE(string.Format(trerror.DTInvalidDataType.Text, "DT_COLUMN_OPTIONS", key, cName));
						if (isString)
							column.DefaultValue = v.GetStrValue(exm);
						else
							column.DefaultValue = DataTable.ConvertInt(v.GetIntValue(exm), column.DataType);
						break;
				}
				idx++;
			}
		}
	}
	#endregion

	private sealed class AWAIT_Instruction : AInstruction
	{
		public AWAIT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.EXPRESSION_NULLABLE);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			long waittime = -1;
			ExpressionArgument arg = func.Argument as ExpressionArgument;
			if (arg != null && arg.Term != null)
			{
				waittime = arg.Term.GetIntValue(exm);
				if (waittime < 0)
					throw new CodeEE(string.Format(trerror.AwaitArgIsNegative.Text, waittime.ToString()));
				if (waittime > 10000)
					throw new CodeEE(string.Format(trerror.AwaitArgIsOver10Seconds.Text, waittime.ToString()));
			}

			exm.Console.Await((int)waittime);
		}
	}
	//ここからEnter版
	#region EE
	public static Sound[] sound = new Sound[10];
	public static Sound bgm = new();
	private sealed class PLAYSOUND_Instruction : AInstruction
	{

		public PLAYSOUND_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTML_PRINT);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			var soundArg = (SpHtmlPrint)func.Argument;
			string datFilename = null;
			if (soundArg.IsConst)
				datFilename = soundArg.ConstStr;
			else
				datFilename = soundArg.Str.GetStrValue(exm);
			int repeat = soundArg.Opt != null ? (int)Math.Max(soundArg.Opt.GetIntValue(exm), 1) : 1;
			//andEmuera: ERB が '\' 区切りで書いていても Android で解決できるよう区切りを揃える
			string filepath = Path.GetFullPath(Program.SoundDir + PortablePath.Normalize(datFilename));
			try
			{
				if (File.Exists(filepath))
				{
					int i;
					for (i = 0; i < sound.Length; i++)
					{
						if (sound[i] == null)
							sound[i] = new Sound();
						//未使用もしくは再生完了してる要素を使う
						if (!sound[i].isPlaying())
							break;
					}
					// if no available sounds were found use sound 0
					if (i >= sound.Length)
						i = 0;

					sound[i].play(filepath, repeat);
				}
			}
			catch
			{
				throw new CodeEE(trerror.ImcompatibleSoundFile.Text);
			}
		}
	}

	public sealed class STOPSOUND_Instruction : AInstruction
	{
		public STOPSOUND_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			for (int i = 0; i < sound.Length; i++)
			{
				if (sound[i] == null)
					sound[i] = new Sound();
				if (sound[i].isPlaying())
					sound[i].stop();
			}
		}
	}

	private sealed class PLAYBGM_Instruction : AInstruction
	{

		public PLAYBGM_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument arg = (ExpressionArgument)func.Argument;
			string datFilename = null;
			if (arg.IsConst)
				datFilename = arg.ConstStr;
			else
				datFilename = arg.Term.GetStrValue(exm);
			//andEmuera: ERB が '\' 区切りで書いていても Android で解決できるよう区切りを揃える
			string filepath = Path.GetFullPath(Program.SoundDir + PortablePath.Normalize(datFilename));

			try
			{
				if (File.Exists(filepath))
					bgm.play(filepath, -1); // -1 means repeat indefinitely
			}
			catch
			{
				throw new CodeEE(trerror.ImcompatibleSoundFile.Text);
			}
		}
	}

	public sealed class STOPBGM_Instruction : AInstruction
	{
		public STOPBGM_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			bgm.stop();
		}
	}

	public sealed class SETSOUNDVOLUME_Instruction : AInstruction
	{
		public SETSOUNDVOLUME_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument intExpArg = (ExpressionArgument)func.Argument;
			int vol = (int)intExpArg.Term.GetIntValue(exm);
			for (int i = 0; i < sound.Length; i++)
			{
				if (sound[i] == null)
					sound[i] = new Sound();
				sound[i].setVolume(vol);
			}
		}
	}
	public sealed class SETBGMVOLUME_Instruction : AInstruction
	{
		public SETBGMVOLUME_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument intExpArg = (ExpressionArgument)func.Argument;
			int vol = (int)intExpArg.Term.GetIntValue(exm);
			bgm.setVolume(vol);
		}
	}

	public sealed class UPDATECHECK_Instruction : AInstruction
	{
		public UPDATECHECK_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if (Config.ForbidUpdateCheck == true)
			{
				exm.VEvaluator.RESULT = 4;
				return;
			}

			if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() == false)
			{
				exm.VEvaluator.RESULT = 5;
				return;
			}

			string url = GlobalStatic.GameBaseData.UpdateCheckURL;
			WebClient wc = new();
			if (url == null || url == "")
			{
				exm.VEvaluator.RESULT = 3;
				return;
			}
			try
			{
				Stream st = wc.OpenRead(url);
				StreamReader sr = new(st);
				try
				{
					var version = sr.ReadLine();
					var link = sr.ReadLine();
					if (version == null || version == "")
					{
						exm.VEvaluator.RESULT = 3;
						return;
					}
					if (link == null || link == "")
					{
						exm.VEvaluator.RESULT = 3;
						return;
					}
					if (version != GlobalStatic.GameBaseData.VersionName)
					{
						DialogResult result = MessageBox.Show(string.Format(trmb.NewVersionAvailable.Text, version, link),
							trmb.UpdateCheck.Text,
							MessageBoxButtons.YesNo,
							MessageBoxIcon.None,
							MessageBoxDefaultButton.Button2
							);
						if (result == DialogResult.Yes)
						{
							exm.VEvaluator.RESULT = 2;
							//andEmuera: Android では Process.Start(UseShellExecute) が使えないため、
							//プラットフォーム側の URL オープン処理に委譲する
							MinorShift.Emuera.Runtime.Utils.PluginSystem.BuiltinPlugins.OpenUrl(link);
							//System.Diagnostics.Process.Start(link);
							st.Close();
							wc.Dispose();
							return;
						}
						else
						{
							exm.VEvaluator.RESULT = 1;
							st.Close();
							wc.Dispose();
							return;
						}
					}
					else
					{
						exm.VEvaluator.RESULT = 0;
						st.Close();
						wc.Dispose();
						return;
					}
				}
				catch
				{
					exm.VEvaluator.RESULT = 3;
					st.Close();
					wc.Dispose();
					return;
				}
			}
			catch
			{
				exm.VEvaluator.RESULT = 3;
				return;
			}
		}
	}
	#endregion
	#region EE_TOOLTIP拡張
	private sealed class TOOLTIP_SETFONT_Instruction : AInstruction
	{
		public TOOLTIP_SETFONT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument fn = (ExpressionArgument)func.Argument;
			exm.Console.SetToolTipFontName(fn.Term.GetStrValue(exm));
		}
	}
	private sealed class TOOLTIP_SETFONTSIZE_Instruction : AInstruction
	{
		public TOOLTIP_SETFONTSIZE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument fs = (ExpressionArgument)func.Argument;
			exm.Console.SetToolTipFontSize(fs.Term.GetIntValue(exm));
		}
	}
	private sealed class TOOLTIP_CUSTOM_Instruction : AInstruction
	{
		public TOOLTIP_CUSTOM_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument b = (ExpressionArgument)func.Argument;
			if (b.Term.GetIntValue(exm) == 0)
				exm.Console.CustomToolTip(false);
			else
				exm.Console.CustomToolTip(true);
		}
	}
	private sealed class TOOLTIP_FORMAT_Instruction : AInstruction
	{
		public TOOLTIP_FORMAT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument i = (ExpressionArgument)func.Argument;
			exm.Console.SetToolTipFormat(i.Term.GetIntValue(exm));
		}
	}
	private sealed class TOOLTIP_IMG_Instruction : AInstruction
	{
		public TOOLTIP_IMG_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			//スキップ不可
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
			flag = EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument i = (ExpressionArgument)func.Argument;
			exm.Console.SetToolTipImg(i.Term.GetIntValue(exm) != 0);
		}
	}
	#endregion
	#region BREAKBUTTON
	private sealed class BREAKBUTTON_Instruction : AInstruction
	{
		public BREAKBUTTON_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.EXPRESSION_NULLABLE);
			flag = METHOD_SAFE | EXTENDED;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			exm.Console.forceUpdateGeneration();
		}
	}
	#endregion

	#endregion

	#region flowControlFunction

	private sealed class BEGIN_Instruction : AInstruction
	{
		public BEGIN_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR);
			flag = FLOW_CONTROL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			string keyword = func.Argument.ConstStr;
			#region EE
			// state.SetBegin(keyword);
			state.SetBegin(keyword, true);
			#endregion
			state.Return(0);
			exm.Console.ResetStyle();
		}
	}
	#region EE
	private sealed class FORCE_BEGIN_Instruction : AInstruction
	{
		public FORCE_BEGIN_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR);
			flag = FLOW_CONTROL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			string keyword = func.Argument.ConstStr;
			//if (Config.Config.IgnoreCase)//1756 BEGINのキーワードは関数扱いらしい
			//	keyword = keyword.ToUpper();
			state.SetBegin(keyword, true);
			state.Return(0);
			exm.Console.ResetStyle();
		}
	}
	#endregion
	private sealed class SAVELOADGAME_Instruction : AInstruction
	{
		public SAVELOADGAME_Instruction(bool isSave)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = FLOW_CONTROL;
			this.isSave = isSave;
		}
		readonly bool isSave;
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			if ((state.SystemState & SystemStateCode.__CAN_SAVE__) != SystemStateCode.__CAN_SAVE__)
			{
				string funcName = state.Scope;
				if (funcName == null)
					funcName = "";
				throw new CodeEE(string.Format(trerror.CanNotUseInstruction.Text, funcName, "SAVEGAME/LOADGAME"));
			}
			GlobalStatic.Process.saveCurrentState(true);
			//バックアップに入れた旧ProcessStateの方を参照するため、ここでstateは使えない
			GlobalStatic.Process.getCurrentState.SaveLoadData(isSave);
		}
	}

	private sealed class REPEAT_Instruction : AInstruction
	{
		public REPEAT_Instruction(bool fornext)
		{
			flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL;
			if (fornext)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_FOR_NEXT);
				flag |= EXTENDED;
			}
			else
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			}
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpForNextArgment forArg = (SpForNextArgment)func.Argument;
			func.LoopCounter = forArg.Cnt;
			//1.725 順序変更。REPEATにならう。
			func.LoopCounter.SetValue(forArg.Start.GetIntValue(exm), exm);
			func.LoopEnd = forArg.End.GetIntValue(exm);
			func.LoopStep = forArg.Step.GetIntValue(exm);
			if ((func.LoopStep > 0) && (func.LoopEnd > func.LoopCounter.GetIntValue(exm)))//まだ回数が残っているなら、
				return;//そのまま次の行へ
			else if ((func.LoopStep < 0) && (func.LoopEnd < func.LoopCounter.GetIntValue(exm)))//まだ回数が残っているなら、
				return;//そのまま次の行へ
			state.JumpTo(func.JumpTo);
		}
	}

	private sealed class WHILE_Instruction : AInstruction
	{
		public WHILE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument expArg = (ExpressionArgument)func.Argument;
			if (expArg.Term.GetIntValue(exm) != 0)//式が真
				return;//そのまま中の処理へ
			state.JumpTo(func.JumpTo);
		}
	}

	private sealed class SIF_Instruction : AInstruction
	{
		public SIF_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
		}

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			LogicalLine jumpto = func.NextLine;
			if ((jumpto == null) || (jumpto.NextLine == null) ||
				(jumpto is FunctionLabelLine) || (jumpto is NullLine))
			{
				ParserMediator.Warn(trerror.NothingAfterSif.Text, func, 2, true, false);
				return;
			}
			else if (jumpto is InstructionLine)
			{
				InstructionLine sifFunc = (InstructionLine)jumpto;
				if (sifFunc.Function.IsPartial())
					ParserMediator.Warn(string.Format(trerror.FuncCanNotAfterSif.Text, sifFunc.Function.Name), func, 2, true, false);
				else
					func.JumpTo = func.NextLine.NextLine;
			}
			else if (jumpto is GotoLabelLine)
				ParserMediator.Warn(trerror.LabelCanNotAfterSif.Text, func, 2, true, false);
			else
				func.JumpTo = func.NextLine.NextLine;

			if ((func.JumpTo != null) && (func.Position.Value.LineNo + 1 != func.NextLine.Position.Value.LineNo))
				ParserMediator.Warn(trerror.EmptyAfterSif.Text, func, 0, false, true);
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument expArg = (ExpressionArgument)func.Argument;
			if (expArg.Term.GetIntValue(exm) == 0)//評価式が真ならそのまま流れ落ちる
				state.ShiftNextLine();//偽なら一行とばす。順に来たときと同じ扱いにする
		}
	}

	private sealed class ELSEIF_Instruction : AInstruction
	{
		public ELSEIF_Instruction(FunctionArgType argtype)
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(argtype);
			flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//if (iFuncCode == FunctionCode.ELSE || iFuncCode == FunctionCode.ELSEIF
			//	|| iFuncCode == FunctionCode.CASE || iFuncCode == FunctionCode.CASEELSE)
			//チェック済み
			//if (func.JumpTo == null)
			//	throw new ExeEE(func.Function.Name + "のジャンプ先が設定されていない");
			state.JumpTo(func.JumpTo);
		}
	}
	private sealed class ENDIF_Instruction : AInstruction
	{
		public ENDIF_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = FLOW_CONTROL | PARTIAL | FORCE_SETARG;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
		}
	}

	private sealed class IF_Instruction : AInstruction
	{
		public IF_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			LogicalLine ifJumpto = func.JumpTo;//ENDIF
											   //チェック済み
											   //if (func.IfCaseList == null)
											   //	throw new ExeEE("IFのIF-ELSEIFリストが適正に作成されていない");
											   //if (func.JumpTo == null)
											   //	throw new ExeEE("IFに対応するENDIFが設定されていない");

			foreach (var line in func.IfCaseList)
			{
				if (line.IsError)
					continue;
				if (line.FunctionCode == FunctionCode.ELSE)
				{
					ifJumpto = line;
					break;
				}

				//ExpressionArgument expArg = (ExpressionArgument)(line.Argument);
				//チェック済み
				//if (expArg == null)
				//	throw new ExeEE("IFチェック中。引数が解析されていない。", func.IfCaseList[i].Position);

				//1730 ELSEIFが出したエラーがIFのエラーとして検出されていた
				state.CurrentLine = line;
				long value = 0;
				if (line.Argument.IsConst)
				{
					value = line.Argument.ConstInt;
				}
				else
				{
					value = ((ExpressionArgument)line.Argument).Term.GetIntValue(exm);
				}
				if (value != 0)//式が真
				{
					ifJumpto = line;
					break;
				}
			}
			if (ifJumpto != func)//自分自身がジャンプ先ならそのまま
				state.JumpTo(ifJumpto);
			//state.RunningLine = null;
		}
	}


	private sealed class SELECTCASE_Instruction : AInstruction
	{
		public SELECTCASE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.EXPRESSION);
			flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			LogicalLine caseJumpto = func.JumpTo;//ENDSELECT
			AExpression selectValue = ((ExpressionArgument)func.Argument).Term;
			string sValue = null;
			long iValue = 0;
			if (selectValue.IsInteger)
				iValue = selectValue.GetIntValue(exm);
			else
				sValue = selectValue.GetStrValue(exm);
			//チェック済み
			//if (func.IfCaseList == null)
			//	throw new ExeEE("SELECTCASEのCASEリストが適正に作成されていない");
			//if (func.JumpTo == null)
			//	throw new ExeEE("SELECTCASEに対応するENDSELECTが設定されていない");
			foreach (var line in func.IfCaseList)
			{
				if (line.IsError)
					continue;
				if (line.FunctionCode == FunctionCode.CASEELSE)
				{
					caseJumpto = line;
					break;
				}
				CaseArgument caseArg = (CaseArgument)line.Argument;
				//チェック済み
				//if (caseArg == null)
				//	throw new ExeEE("CASEチェック中。引数が解析されていない。", func.IfCaseList[i].Position);

				state.CurrentLine = line;
				if (selectValue.IsInteger)
				{
					long Is = iValue;
					foreach (CaseExpression caseExp in caseArg.CaseExps)
					{
						if (caseExp.GetBool(Is, exm))
						{
							caseJumpto = line;
							goto casefound;
						}
					}
				}
				else
				{
					string Is = sValue;
					foreach (CaseExpression caseExp in caseArg.CaseExps)
					{
						if (caseExp.GetBool(Is, exm))
						{
							caseJumpto = line;
							goto casefound;
						}
					}
				}

			}
		casefound:
			state.JumpTo(caseJumpto);
			//state.RunningLine = null;
		}
	}

	private sealed class RETURNFORM_Instruction : AInstruction
	{
		public RETURNFORM_Instruction()
		{
			//ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_ANY);
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR);
			flag = EXTENDED | FLOW_CONTROL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//int termnum = 0;
			//foreach (IOperandTerm term in ((ExpressionArrayArgument)func.Argument).TermList)
			//{
			//    string arg = term.GetStrValue(exm);
			//    StringStream aSt = new StringStream(arg);
			//    WordCollection wc = LexicalAnalyzer.Analyse(aSt, LexEndWith.EoL, LexAnalyzeFlag.None);
			//    exm.VEvaluator.SetResultX((ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL).GetIntValue(exm)), termnum);
			//    termnum++;
			//}
			//state.Return(exm.VEvaluator.RESULT);
			//if (state.ScriptEnd)
			//    return;
			//int termnum = 0;
			CharStream aSt = new(((ExpressionArgument)func.Argument).Term.GetStrValue(exm));
			List<long> termList = [];
			while (!aSt.EOS)
			{
				WordCollection wc = LexicalAnalyzer.Analyse(aSt, LexEndWith.Comma, LexAnalyzeFlag.None);
				//exm.VEvaluator.SetResultX(ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL).GetIntValue(exm), termnum++);
				termList.Add(ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL).GetIntValue(exm));
				aSt.ShiftNext();
				LexicalAnalyzer.SkipHalfSpace(aSt);
				//termnum++;
			}
			if (termList.Count == 0)
				termList.Add(0);
			exm.VEvaluator.SetResultX(termList);
			state.Return(exm.VEvaluator.RESULT);
			if (state.ScriptEnd)
				return;
		}
	}

	private sealed class RETURN_Instruction : AInstruction
	{
		public RETURN_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
			flag = FLOW_CONTROL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//int termnum = 0;
			ExpressionArrayArgument expArrayArg = (ExpressionArrayArgument)func.Argument;
			if (expArrayArg.TermList.Length == 0)
			{
				exm.VEvaluator.RESULT = 0;
				state.Return(0);
				return;
			}
			List<long> termList = [];
			foreach (AExpression term in expArrayArg.TermList)
			{
				termList.Add(term.GetIntValue(exm));
				//exm.VEvaluator.SetResultX(term.GetIntValue(exm), termnum++);
			}
			if (termList.Count == 0)
				termList.Add(0);
			exm.VEvaluator.SetResultX(termList);
			state.Return(exm.VEvaluator.RESULT);
		}
	}

	private sealed class CATCH_Instruction : AInstruction
	{
		public CATCH_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			//if (sequential)//上から流れてきたなら何もしないでENDCATCHに飛ぶ
			state.JumpTo(func.JumpToEndCatch);
		}
	}

	private sealed class RESTART_Instruction : AInstruction
	{
		public RESTART_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | FLOW_CONTROL | EXTENDED;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			state.JumpTo(func.ParentLabelLine);
		}
	}

	private sealed class BREAK_Instruction : AInstruction
	{
		public BREAK_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | FLOW_CONTROL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			////BREAKのJUMP先はRENDまたはNEXT。そのジャンプ先であるREPEATかFORをiLineに代入。
			//1.723 仕様変更。BREAKのJUMP先にはREPEAT、FOR、WHILEを記憶する。そのJUMP先が本当のJUMP先。
			InstructionLine jumpTo = (InstructionLine)func.JumpTo;
			InstructionLine iLine = (InstructionLine)jumpTo.JumpTo;
			//WHILEとDOはカウンタがないので、即ジャンプ
			if (jumpTo.FunctionCode != FunctionCode.WHILE && jumpTo.FunctionCode != FunctionCode.DO)
			{
				unchecked
				{//eramakerではBREAK時にCOUNTが回る
					jumpTo.LoopCounter.ChangeValue(jumpTo.LoopStep, exm);
				}
			}
			state.JumpTo(iLine);
		}
	}

	private sealed class CONTINUE_Instruction : AInstruction
	{
		public CONTINUE_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | FLOW_CONTROL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			InstructionLine jumpTo = (InstructionLine)func.JumpTo;
			if ((jumpTo.FunctionCode == FunctionCode.REPEAT) || (jumpTo.FunctionCode == FunctionCode.FOR))
			{
				//ループ変数が不明(REPEAT、FORを経由せずにループしようとした場合は無視してループを抜ける(eramakerがこういう仕様だったりする))
				if (jumpTo.LoopCounter == null)
				{
					state.JumpTo(jumpTo.JumpTo);
					return;
				}
				unchecked
				{
					jumpTo.LoopCounter.ChangeValue(jumpTo.LoopStep, exm);
				}
				long counter = jumpTo.LoopCounter.GetIntValue(exm);
				//まだ回数が残っているなら、
				if (((jumpTo.LoopStep > 0) && (jumpTo.LoopEnd > counter))
					|| ((jumpTo.LoopStep < 0) && (jumpTo.LoopEnd < counter)))
					state.JumpTo(func.JumpTo);
				else
					state.JumpTo(jumpTo.JumpTo);
				return;
			}
			if (jumpTo.FunctionCode == FunctionCode.WHILE)
			{
				if (((ExpressionArgument)jumpTo.Argument).Term.GetIntValue(exm) != 0)
					state.JumpTo(func.JumpTo);
				else
					state.JumpTo(jumpTo.JumpTo);
				return;
			}
			if (jumpTo.FunctionCode == FunctionCode.DO)
			{
				//こいつだけはCONTINUEよりも後ろに判定行があるため、判定行にエラーがあった場合に問題がある
				InstructionLine tFunc = (InstructionLine)((InstructionLine)func.JumpTo).JumpTo;//LOOP
				if (tFunc.IsError)
					throw new CodeEE(tFunc.ErrMes, tFunc.Position);
				ExpressionArgument expArg = (ExpressionArgument)tFunc.Argument;
				if (expArg.Term.GetIntValue(exm) != 0)//式が真
					state.JumpTo(jumpTo);//DO
				else
					state.JumpTo(tFunc);//LOOP
				return;
			}
			throw new ExeEE(trerror.AbnormalContinue.Text);
		}
	}

	private sealed class REND_Instruction : AInstruction
	{
		public REND_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			InstructionLine jumpTo = (InstructionLine)func.JumpTo;
			//ループ変数が不明(REPEAT、FORを経由せずにループしようとした場合は無視してループを抜ける(eramakerがこういう仕様だったりする))
			if (jumpTo.LoopCounter == null)
			{
				state.JumpTo(jumpTo.JumpTo);
				return;
			}
			unchecked
			{
				jumpTo.LoopCounter.ChangeValue(jumpTo.LoopStep, exm);
			}
			long counter = jumpTo.LoopCounter.GetIntValue(exm);
			//まだ回数が残っているなら、
			if (((jumpTo.LoopStep > 0) && (jumpTo.LoopEnd > counter))
				|| ((jumpTo.LoopStep < 0) && (jumpTo.LoopEnd < counter)))
				state.JumpTo(func.JumpTo);
		}
	}

	private sealed class WEND_Instruction : AInstruction
	{
		public WEND_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			InstructionLine jumpTo = (InstructionLine)func.JumpTo;
			if (((ExpressionArgument)jumpTo.Argument).Term.GetIntValue(exm) != 0)
				state.JumpTo(func.JumpTo);
		}
	}

	private sealed class LOOP_Instruction : AInstruction
	{
		public LOOP_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			ExpressionArgument expArg = (ExpressionArgument)func.Argument;
			if (expArg.Term.GetIntValue(exm) != 0)//式が真
				state.JumpTo(func.JumpTo);
		}
	}


	private sealed class RETURNF_Instruction : AInstruction
	{
		public RETURNF_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.EXPRESSION_NULLABLE);
			flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL;
		}

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			FunctionLabelLine label = func.ParentLabelLine;
			if (!label.IsMethod)
			{
				ParserMediator.Warn(trerror.CanNotUseReturnf.Text, func, 2, true, false);
			}
			if (func.Argument != null)
			{
				AExpression term = ((ExpressionArgument)func.Argument).Term;
				if (term != null)
				{
					if (label.MethodType != term.GetOperandType())
					{
						if (label.MethodType == typeof(long))
							ParserMediator.Warn(trerror.ReturnfStrInIntFunc.Text, func, 2, true, false);
						else if (label.MethodType == typeof(string))
							ParserMediator.Warn(trerror.ReturnfIntInStrFunc.Text, func, 2, true, false);
					}
				}
			}
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			AExpression term = ((ExpressionArgument)func.Argument).Term;
			SingleTerm ret = null;
			if (term != null)
			{
				ret = term.GetValue(exm);
			}
			state.ReturnF(ret);
		}
	}

	private sealed class CALL_Instruction : AInstruction
	{
		public CALL_Instruction(bool form, bool isJump, bool isTry, bool isTryCatch)
		{
			if (form)
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORM);
			else
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALL);
			flag = FLOW_CONTROL | FORCE_SETARG;
			if (isJump)
				flag |= IS_JUMP;
			if (isTry)
				flag |= IS_TRY;
			if (isTryCatch)
				flag |= IS_TRYC | PARTIAL;
			this.isJump = isJump;
			this.isTry = isTry;
		}
		readonly bool isJump;
		readonly bool isTry;

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			if (!func.Argument.IsConst)
			{
				useCallForm = true;
				return;
			}
			SpCallArgment callArg = (SpCallArgment)func.Argument;
			string labelName = callArg.ConstStr;
			CalledFunction call = CalledFunction.CallFunction(GlobalStatic.Process, labelName, func);
			if ((call == null) && (!func.Function.IsTry()))
			{
				FunctionoNotFoundName = labelName;
				return;
			}
			if (call != null)
			{
				func.JumpTo = call.TopLabel;
				if (call.TopLabel.Depth < 0)
					call.TopLabel.Depth = currentDepth + 1;
				if (call.TopLabel.IsError)
				{
					func.IsError = true;
					func.ErrMes = call.TopLabel.ErrMes;
					return;
				}
				string errMes;
				callArg.UDFArgument = call.ConvertArg(callArg.RowArgs, out errMes);
				if (callArg.UDFArgument == null)
				{
					ParserMediator.Warn(errMes, func, 2, true, false);
					return;
				}
			}
			callArg.CallFunc = call;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpCallArgment spCallArg = (SpCallArgment)func.Argument;
			CalledFunction call;
			string labelName;
			UserDefinedFunctionArgument arg = null;
			if (spCallArg.IsConst)
			{
				call = spCallArg.CallFunc;
				labelName = spCallArg.ConstStr;
				arg = spCallArg.UDFArgument;
			}
			else
			{
				labelName = spCallArg.FuncnameTerm.GetStrValue(exm);
				call = CalledFunction.CallFunction(GlobalStatic.Process, labelName, func);
			}
			if (call == null)
			{
				if (!isTry)
					throw new CodeEE(string.Format(trerror.NotDefinedFunc.Text, labelName));
				if (func.JumpToEndCatch != null)
					state.JumpTo(func.JumpToEndCatch);
				return;
			}
			call.IsJump = isJump;
			if (arg == null)
			{
				string errMes;
				arg = call.ConvertArg(spCallArg.RowArgs, out errMes);
				if (arg == null)
					throw new CodeEE(errMes);
			}
			state.IntoFunction(call, arg, exm);
		}
	}

	private sealed class CALLEVENT_Instruction : AInstruction
	{
		public CALLEVENT_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR);
			flag = FLOW_CONTROL | EXTENDED;
		}

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			//EVENT関数からCALLされた先でCALLEVENTされるようなパターンはIntoFunctionで捕まえる
			FunctionLabelLine label = func.ParentLabelLine;
			if (label.IsEvent)
			{
				ParserMediator.Warn(trerror.CanNotUseCallevent.Text, func, 2, true, false);
			}
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			string labelName = func.Argument.ConstStr;
			CalledFunction call = CalledFunction.CallEventFunction(GlobalStatic.Process, labelName, func);
			if (call == null)
				return;
			state.IntoFunction(call, null, null);
		}
	}

	private sealed class GOTO_Instruction : AInstruction
	{
		public GOTO_Instruction(bool form, bool isTry, bool isTryCatch)
		{
			if (form)
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORM);
			else
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALL);
			this.isTry = isTry;
			flag = METHOD_SAFE | FLOW_CONTROL | FORCE_SETARG;
			if (isTry)
				flag |= IS_TRY;
			if (isTryCatch)
				flag |= IS_TRYC | PARTIAL;
		}
		readonly bool isTry;

		public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
		{
			GotoLabelLine jumpto;
			func.JumpTo = null;
			if (func.Argument.IsConst)
			{
				string labelName = func.Argument.ConstStr;
				jumpto = GlobalStatic.LabelDictionary.GetLabelDollar(labelName, func.ParentLabelLine);
				if (jumpto == null)
				{
					if (!func.Function.IsTry())
						ParserMediator.Warn(string.Format(trerror.NotDefinedLabelName.Text, labelName), func, 2, true, false);
					else
						return;
				}
				else if (jumpto.IsError)
					ParserMediator.Warn(string.Format(trerror.InvalidLabelName.Text, labelName), func, 2, true, false);
				else if (jumpto != null)
				{
					func.JumpTo = jumpto;
				}
			}
		}
		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			string label;
			LogicalLine jumpto;
			if (func.Argument.IsConst)
			{
				label = func.Argument.ConstStr;
				if (func.JumpTo != null)
					jumpto = func.JumpTo;
				else
					return;
			}
			else
			{
				label = ((SpCallArgment)func.Argument).FuncnameTerm.GetStrValue(exm);
				jumpto = state.CurrentCalled.CallLabel(GlobalStatic.Process, label);
			}
			if (jumpto == null)
			{
				if (!func.Function.IsTry())
					throw new CodeEE(string.Format(trerror.NotDefinedLabelName.Text, label));
				if (func.JumpToEndCatch != null)
					state.JumpTo(func.JumpToEndCatch);
				return;
			}
			else if (jumpto.IsError)
				throw new CodeEE(string.Format(trerror.InvalidLabelName.Text, label));
			state.JumpTo(jumpto);
		}
	}
	#endregion
}
