using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Script.Data;

internal sealed class StrForm
{
	private StrForm() { }
	string[] strs;//terms.Count + 1
	AExpression[] terms;

	#region static
	static FormattedStringMethod formatCurlyBrace;
	static FormattedStringMethod formatPercent;
	static FormattedStringMethod formatYenAt;
	static FunctionMethodTerm NameTarget;// "***"
	static FunctionMethodTerm CallnameMaster;// "+++"
	static FunctionMethodTerm CallnamePlayer;// "==="
	static FunctionMethodTerm NameAssi;// "///"
	static FunctionMethodTerm CallnameTarget;// "$$$"
	public static void Initialize()
	{
		formatCurlyBrace = new FormatCurlyBrace();
		formatPercent = new FormatPercent();
		formatYenAt = new FormatYenAt();
		VariableToken nameID = GlobalStatic.VariableData.GetSystemVariableToken("NAME");
		VariableToken callnameID = GlobalStatic.VariableData.GetSystemVariableToken("CALLNAME");
		AExpression[] zeroArg = [new SingleLongTerm(0)];
		VariableTerm target = new(GlobalStatic.VariableData.GetSystemVariableToken("TARGET"), zeroArg);
		VariableTerm master = new(GlobalStatic.VariableData.GetSystemVariableToken("MASTER"), zeroArg);
		VariableTerm player = new(GlobalStatic.VariableData.GetSystemVariableToken("PLAYER"), zeroArg);
		VariableTerm assi = new(GlobalStatic.VariableData.GetSystemVariableToken("ASSI"), zeroArg);

		VariableTerm nametarget = new(nameID, [target]);
		VariableTerm callnamemaster = new(callnameID, [master]);
		VariableTerm callnameplayer = new(callnameID, [player]);
		VariableTerm nameassi = new(nameID, [assi]);
		VariableTerm callnametarget = new(callnameID, [target]);
		NameTarget = new FunctionMethodTerm(formatPercent, [nametarget, null, null]);
		CallnameMaster = new FunctionMethodTerm(formatPercent, [callnamemaster, null, null]);
		CallnamePlayer = new FunctionMethodTerm(formatPercent, [callnameplayer, null, null]);
		NameAssi = new FunctionMethodTerm(formatPercent, [nameassi, null, null]);
		CallnameTarget = new FunctionMethodTerm(formatPercent, [callnametarget, null, null]);
	}

	public static StrForm FromWordToken(StrFormWord wt)
	{
		StrForm ret = new()
		{
			strs = wt.Strs
		};
		AExpression[] termArray = new AExpression[wt.SubWords.Length];
		for (int i = 0; i < wt.SubWords.Length; i++)
		{
			SubWord SWT = wt.SubWords[i];
			if (SWT is TripleSymbolSubWord tSymbol)
			{
				switch (tSymbol.Code)
				{
					case '*':
						termArray[i] = NameTarget;
						continue;
					case '+':
						termArray[i] = CallnameMaster;
						continue;
					case '=':
						termArray[i] = CallnamePlayer;
						continue;
					case '/':
						termArray[i] = NameAssi;
						continue;
					case '$':
						termArray[i] = CallnameTarget;
						continue;
				}
				throw new ExeEE("何かおかしい");
			}
			WordCollection wc;
			AExpression operand;
			YenAtSubWord yenat = SWT as YenAtSubWord;
			if (yenat != null)
			{
				wc = yenat.Words;
				if (wc != null)
				{
					operand = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL);
					if (!wc.EOL)
						throw new CodeEE(trerror.AbnormalFirstOperand.Text);
				}
				else
					operand = new SingleLongTerm(0);
				AExpression left = new StrFormTerm(FromWordToken(yenat.Left));
				AExpression right;
				if (yenat.Right == null)
					right = new SingleStrTerm("");
				else
					right = new StrFormTerm(FromWordToken(yenat.Right));
				termArray[i] = new FunctionMethodTerm(formatYenAt, [operand, left, right]);
				continue;
			}
			wc = SWT.Words;
			operand = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.Comma);
			if (operand == null)
			{
				if (SWT is CurlyBraceSubWord)
					throw new CodeEE(trerror.EmptyBrace.Text);
				else
					throw new CodeEE(trerror.EmptyPer.Text);
			}
			AExpression second = null;
			SingleTerm third = null;
			wc.ShiftNext();
			if (!wc.EOL)
			{
				second = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.Comma);

				wc.ShiftNext();
				if (!wc.EOL)
				{
					if (wc.Current is not IdentifierWord id)
						throw new CodeEE(trerror.NotSpecifiedLR.Text);
					if (string.Equals(id.Code, "LEFT", Config.Config.StringComparison))//標準RIGHT
						third = new SingleLongTerm(1);
					else if (!string.Equals(id.Code, "RIGHT", Config.Config.StringComparison))
						throw new CodeEE(trerror.OtherThanLR.Text);
					wc.ShiftNext();
				}
				if (!wc.EOL)
					throw new CodeEE(trerror.ExtraCharacterLR.Text);
			}
			if (SWT is CurlyBraceSubWord)
			{
				if (operand.GetOperandType() != typeof(long))
					throw new CodeEE(trerror.IsNotNumericBrace.Text);
				termArray[i] = new FunctionMethodTerm(formatCurlyBrace, [operand, second, third]);
				continue;
			}
			if (operand.GetOperandType() != typeof(string))
				throw new CodeEE(trerror.IsNotStringPer.Text);
			termArray[i] = new FunctionMethodTerm(formatPercent, [operand, second, third]);
		}
		ret.terms = termArray;
		return ret;
	}
	#endregion

	public bool IsConst
	{
		get
		{
			return strs.Length == 1;
		}
	}

	public AExpression GetAExpression()
	{
		if (strs.Length == 2 && strs[0].Length == 0 && strs[1].Length == 0)
			return terms[0];
		return null;
	}

	public void Restructure(ExpressionMediator exm)
	{
		if (strs.Length == 1)
			return;
		bool canRestructure = false;
		for (int i = 0; i < terms.Length; i++)
		{
			terms[i] = terms[i].Restructure(exm);
			if (terms[i] is SingleTerm)
			{
				canRestructure = true;
			}
		}
		if (!canRestructure)
			return;
		List<string> strList = [.. strs];
		List<AExpression> termList = [.. terms];
		for (int i = 0; i < termList.Count; i++)
		{
			if (termList[i] is SingleTerm)
			{
				string str = termList[i].GetStrValue(exm);
				strList[i] = strList[i] + str + strList[i + 1];
				termList.RemoveAt(i);
				strList.RemoveAt(i + 1);
				i--;
			}
		}
		strs = [.. strList];
		terms = [.. termList];
		return;
	}
	public string GetString(ExpressionMediator exm)
	{
		var handler = new DefaultInterpolatedStringHandler(strs.Length + terms.Length, 0);
		if (strs.Length == 1)
			return strs[0];
		for (int i = 0; i < strs.Length - 1; i++)
		{
			handler.AppendLiteral(strs[i]);
			handler.AppendLiteral(terms[i].GetStrValue(exm));
		}
		handler.AppendLiteral(strs[^1]);
		return handler.ToString();
	}

	#region FormattedStringMethod 書式付文字列の内部
	private abstract class FormattedStringMethod : FunctionMethod
	{
		public FormattedStringMethod()
		{
			CanRestructure = true;
			ReturnType = typeof(string);
			argumentTypeArray = null;
		}
		public override string CheckArgumentType(string name, List<AExpression> arguments) { throw new ExeEE("型チェックは呼び出し元が行うこと"); }
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments) { throw new ExeEE("戻り値の型が違う"); }
		public override SingleTerm GetReturnValue(ExpressionMediator exm, List<AExpression> arguments) { return new SingleStrTerm(GetStrValue(exm, arguments)); }
	}

	private sealed class FormatCurlyBrace : FormattedStringMethod
	{
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string ret = arguments[0].GetIntValue(exm).ToString();
			if (arguments[1] == null)
				return ret;
			if (arguments[2] != null)
				ret = ret.PadRight((int)arguments[1].GetIntValue(exm), ' ');//LEFT
			else
				ret = ret.PadLeft((int)arguments[1].GetIntValue(exm), ' ');//RIGHT
			return ret;
		}
	}

	private sealed class FormatPercent : FormattedStringMethod
	{
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string ret = arguments[0].GetStrValue(exm);
			if (arguments[1] == null)
				return ret;
			int totalLength = (int)arguments[1].GetIntValue(exm);
			int currentLength = LangManager.GetStrlenLang(ret);
			totalLength -= currentLength - ret.Length;//全角文字の数だけマイナス。タブ文字？ゼロ幅文字？知るか！
			if (totalLength < ret.Length)
				return ret;//PadLeftは0未満を送ると例外を投げる
			if (arguments[2] != null)
				ret = ret.PadRight(totalLength, ' ');//LEFT
			else
				ret = ret.PadLeft(totalLength, ' ');//RIGHT
			return ret;
		}
	}

	private sealed class FormatYenAt : FormattedStringMethod
	{//Operator のTernaryIntStrStrとやってることは同じ
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return arguments[0].GetIntValue(exm) != 0 ? arguments[1].GetStrValue(exm) : arguments[2].GetStrValue(exm);
		}
	}

	#endregion
}
