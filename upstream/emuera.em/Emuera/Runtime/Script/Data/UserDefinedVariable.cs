using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Script.Data;

internal sealed class UserDefinedVariableData
{
	public string Name;
	public bool TypeIsStr;
	public bool Reference;
	public int Dimension = 1;
	public int[] Lengths;
	public long[] DefaultInt;
	public string[] DefaultStr;
	public bool Global;
	public bool Save;
	public bool Static = true;
	public bool Private;
	public bool CharaData;
	public bool Const;

	//1822 Privateの方もDIMだけ遅延させようとしたけどちょっと課題がおおいのでやめとく
	public static UserDefinedVariableData Create(DimLineWC dimline)
	{
		return Create(dimline.WC, dimline.Dims, dimline.IsPrivate, dimline.SC);
	}

	public static UserDefinedVariableData Create(WordCollection wc, bool dims, bool isPrivate, ScriptPosition? sc)
	{
		string dimtype = dims ? "#DIM" : "#DIMS";
		UserDefinedVariableData ret = new()
		{
			TypeIsStr = dims
		};

		IdentifierWord idw;
		bool staticDefined = false;
		ret.Const = false;
		string keyword = dimtype;
		//List<string> keywords;
		while (!wc.EOL && (idw = wc.Current as IdentifierWord) != null)
		{
			wc.ShiftNext();
			keyword = idw.Code;
			var cmp = Config.Config.StringComparison;
			//TODO ifの数があたまわるい なんとかしたい
			switch (keyword)
			{
				case var s when s.Equals("CONST", cmp):
					if (ret.CharaData)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CHARADATA"), sc);
					if (ret.Global)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "GLOBAL"), sc);
					if (ret.Save)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "SAVEDATA"), sc);
					if (ret.Reference)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "REF"), sc);
					if (!ret.Static)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "DYNAMIC"), sc);
					if (ret.Const)
						throw new CodeEE(string.Format(trerror.DuplicateKeyword.Text, keyword), sc);
					ret.Const = true;
					break;
				case var s when s.Equals("REF", cmp):
					//throw new CodeEE("未実装の機能です", sc);
					//if (!isPrivate)
					//	throw new CodeEE("広域変数の宣言に" + keyword + "キーワードは指定できません", sc);
					if (staticDefined && ret.Static)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "STATIC"), sc);
					if (ret.CharaData)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CHARADATA"), sc);
					if (ret.Global)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "GLOBAL"), sc);
					if (ret.Save)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "SAVEDATA"), sc);
					if (ret.Const)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CONST"), sc);
					if (ret.Reference)
						throw new CodeEE(string.Format(trerror.DuplicateKeyword.Text, keyword), sc);
					ret.Reference = true;
					ret.Static = false;
					break;
				case var s when s.Equals("DYNAMIC", cmp):
					if (!isPrivate)
						throw new CodeEE(string.Format(trerror.CanNotUseKeywordGlobalVar.Text, keyword), sc);
					if (ret.CharaData)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CHARADATA"), sc);
					if (ret.Const)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CONST"), sc);
					if (staticDefined)
						if (ret.Static)
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, "STATIC", "DYNAMIC"), sc);
						else
							throw new CodeEE(string.Format(trerror.DuplicateKeyword.Text, keyword), sc);
					staticDefined = true;
					ret.Static = false;
					break;
				case var s when s.Equals("STATIC", cmp):
					if (!isPrivate)
						throw new CodeEE(string.Format(trerror.CanNotUseKeywordGlobalVar.Text, keyword), sc);
					if (ret.CharaData)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CHARADATA"), sc);
					if (staticDefined)
						if (!ret.Static)
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, "STATIC", "DYNAMIC"), sc);
						else
							throw new CodeEE(string.Format(trerror.DuplicateKeyword.Text, keyword), sc);
					if (ret.Reference)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "REF"), sc);
					staticDefined = true;
					ret.Static = true;
					break;
				case var s when s.Equals("GLOBAL", cmp):
					if (isPrivate)
						throw new CodeEE(string.Format(trerror.CanNotUseKeywordLocalVar.Text, keyword), sc);
					if (ret.CharaData)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CHARADATA"), sc);
					if (ret.Reference)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "REF"), sc);
					if (ret.Const)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CONST"), sc);
					if (staticDefined)
						if (ret.Static)
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, "STATIC", "GLOBAL"), sc);
						else
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, "DYNAMIC", "GLOBAL"), sc);
					ret.Global = true;
					break;
				case var s when s.Equals("SAVEDATA", cmp):
					if (isPrivate)
						throw new CodeEE(string.Format(trerror.CanNotUseKeywordLocalVar.Text, keyword), sc);
					if (staticDefined)
						if (ret.Static)
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, "STATIC", "SAVEDATA"), sc);
						else
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, "DYNAMIC", "SAVEDATA"), sc);
					if (ret.Reference)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "REF"), sc);
					if (ret.Const)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CONST"), sc);
					if (ret.Save)
						throw new CodeEE(string.Format(trerror.DuplicateKeyword.Text, keyword), sc);
					ret.Save = true;
					break;
				case var s when s.Equals("CHARADATA", cmp):
					if (isPrivate)
						throw new CodeEE(string.Format(trerror.CanNotUseKeywordLocalVar.Text, keyword), sc);
					if (ret.Reference)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "REF"), sc);
					if (ret.Const)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "CONST"), sc);
					if (staticDefined)
						if (ret.Static)
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "STATIC"), sc);
						else
							throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "DYNAMIC"), sc);
					if (ret.Global)
						throw new CodeEE(string.Format(trerror.CanNotSpecifiedWith.Text, keyword, "GLOBAL"), sc);
					if (ret.CharaData)
						throw new CodeEE(string.Format(trerror.DuplicateKeyword.Text, keyword), sc);
					ret.CharaData = true;
					break;
				default:
					ret.Name = keyword;
					goto whilebreak;
			}
		}
	whilebreak:
		if (ret.Name == null)
			throw new CodeEE(string.Format(trerror.NotVarAfterKeyword.Text, keyword), sc);
		if (Config.Config.UseERD && Config.Config.CheckDuplicateIdentifier)
			GlobalStatic.ConstantData.isDefinedErd(ret.Name, sc);
		string errMes = "";
		int errLevel = -1;
		if (isPrivate)
			GlobalStatic.IdentifierDictionary.CheckUserPrivateVarName(ref errMes, ref errLevel, ret.Name);
		else
			GlobalStatic.IdentifierDictionary.CheckUserVarName(ref errMes, ref errLevel, ret.Name);
		if (errLevel >= 0)
		{
			if (errLevel >= 2)
				throw new CodeEE(errMes, sc);
			ParserMediator.Warn(errMes, sc, errLevel);
		}


		List<int> sizeNum = [];
		if (wc.EOL)//サイズ省略
		{
			if (ret.Const)
				throw new CodeEE(trerror.ConstHasNotInitialValue.Text);
			sizeNum.Add(1);
		}
		else if (wc.Current.Type == ',')//サイズ指定
		{
			while (!wc.EOL)
			{
				if (wc.Current.Type == '=')//サイズ指定解読完了＆初期値指定
					break;
				if (wc.Current.Type != ',')
					throw new CodeEE(trerror.WrongFormat.Text, sc);
				wc.ShiftNext();
				if (ret.Reference)//参照型の場合は要素数不要
				{
					sizeNum.Add(0);
					if (wc.EOL)
						break;
					if (wc.Current.Type == ',')
						continue;
				}
				if (wc.EOL)
					throw new CodeEE(trerror.HasNotExpressionAfterComma.Text, sc);
				AExpression arg = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.Comma_Assignment);
				if (arg.Restructure(null) is not SingleLongTerm sizeTerm)
					throw new CodeEE(trerror.HasNotExpressionAfterComma.Text, sc);
				if (ret.Reference)//参照型には要素数指定不可(0にするか書かないかどっちか
				{
					if (sizeTerm.Int != 0)
						throw new CodeEE(trerror.CanNotSizedRef.Text, sc);

					continue;
				}
				else if (sizeTerm.Int <= 0 || sizeTerm.Int > 1000000)
					throw new CodeEE(trerror.OoRDefinable.Text, sc);
				sizeNum.Add((int)sizeTerm.Int);
			}
		}


		if (wc.Current.Type != '=')//初期値指定なし
		{
			if (ret.Const)
				throw new CodeEE(trerror.ConstHasNotInitialValue.Text);
		}
		else//初期値指定あり
		{
			if (((OperatorWord)wc.Current).Code != OperatorCode.Assignment)
				throw new CodeEE(trerror.UnexpectedOp.Text);
			if (ret.Reference)
				throw new CodeEE(string.Format(trerror.CanNotSetInitialValue.Text, trerror.RefType.Text));
			if (sizeNum.Count >= 2)
				throw new CodeEE(string.Format(trerror.CanNotSetInitialValue.Text, trerror.MultidimType.Text));
			if (ret.CharaData)
				throw new CodeEE(string.Format(trerror.CanNotSetInitialValue.Text, trerror.CharaType.Text));
			int size = 0;
			if (sizeNum.Count == 1)
				size = sizeNum[0];
			wc.ShiftNext();
			var terms = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
			if (terms.Count == 0)
				throw new CodeEE(trerror.ArrayVarCanNotOmitInitialValue.Text);
			if (size > 0)
			{
				if (terms.Count > size)
					throw new CodeEE(trerror.InitialValueMoreThanArraySize.Text);
				if (ret.Const && terms.Count != size)
					throw new CodeEE(trerror.ConstInitialValueDifferentArraySize.Text);
			}
			if (dims)
				ret.DefaultStr = new string[terms.Count];
			else
				ret.DefaultInt = new long[terms.Count];

			for (int i = 0; i < terms.Count; i++)
			{
				if (terms[i] == null)
					throw new CodeEE(trerror.ArrayVarCanNotOmitInitialValue.Text);
				terms[i] = terms[i].Restructure(GlobalStatic.EMediator);
				if (terms[i] is not SingleTerm sTerm)
					throw new CodeEE(trerror.InitialValueOnlyConst.Text);
				if (dims != sTerm.IsString)
					throw new CodeEE(trerror.NotMatchVarTypeAndInitialValue.Text);
				if (dims)
					ret.DefaultStr[i] = ((SingleStrTerm)sTerm).Str;
				else
					ret.DefaultInt[i] = ((SingleLongTerm)sTerm).Int;
			}
			if (sizeNum.Count == 0)
				sizeNum.Add(terms.Count);
		}
		if (!wc.EOL)
			throw new CodeEE(trerror.WrongFormat.Text, sc);

		if (sizeNum.Count == 0)
			sizeNum.Add(1);

		ret.Private = isPrivate;
		ret.Dimension = sizeNum.Count;
		if (ret.Const && ret.Dimension > 1)
			throw new CodeEE(trerror.CanNotDeclareConstArray.Text);
		if (ret.CharaData && ret.Dimension > 2)
			throw new CodeEE(trerror.CharaVarCanNotDeclareMoreThan3D.Text, sc);
		if (ret.Dimension > 3)
			throw new CodeEE(trerror.VarCanNotDeclareMoreThan4D.Text, sc);
		ret.Lengths = new int[sizeNum.Count];
		if (ret.Reference)
			return ret;
		long totalBytes = 1;
		for (int i = 0; i < sizeNum.Count; i++)
		{
			ret.Lengths[i] = sizeNum[i];
			totalBytes *= ret.Lengths[i];
		}
		if (totalBytes <= 0 || totalBytes > 1000000)
			throw new CodeEE(trerror.OoRDefinable.Text, sc);
		if (!isPrivate && ret.Save && !Config.Config.SystemSaveInBinary)
		{
			if (dims && ret.Dimension > 1)
				throw new CodeEE(trerror.StrVarrRequiredBinaryOption.Text, sc);
			else if (ret.CharaData)
				throw new CodeEE(trerror.CharaStrRequiredBinaryOption.Text, sc);
		}
		return ret;
	}
}
internal sealed class DimLineWC
{
	public WordCollection WC;
	public bool Dims;
	public bool IsPrivate;
	public ScriptPosition? SC;
	public DimLineWC(WordCollection wc, bool isString, bool isPrivate, ScriptPosition? position)
	{
		WC = wc;
		Dims = isString;
		IsPrivate = isPrivate;
		SC = position;
	}
}
