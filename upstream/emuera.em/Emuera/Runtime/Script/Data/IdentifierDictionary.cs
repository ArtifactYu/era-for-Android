using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Config.JSON;
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using treer = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera;

//1756 新設。
//また、使用されている名前を記憶し衝突を検出する。
internal partial class IdentifierDictionary
{
	private static readonly System.Buffers.SearchValues<char> badSymbolAsIdentifier = System.Buffers.SearchValues.Create(new char[]
			{
			'+', '-', '*', '/', '%', '=', '!', '<', '>', '|', '&', '^', '~',
			' ', ' ', '\t' ,
			'\"','(', ')', '{', '}', '[', ']', ',', '.', ':',
			'\\', '@', '$', '#', '?', ';', '\'',
				//'_'はOK
			});
	#region EM_私家版_辞書獲得
	public string[] VarKeys => varTokenDic.Keys.ToArray();
	public string[] MacroKeys => macroDic.Values.Select(v=>v.Keyword).ToArray();
	#endregion
	private enum DefinedNameType
	{
		None = 0,
		Reserved,
		SystemVariable,
		SystemMethod,
		SystemInstrument,
		//UserIdentifier,
		UserGlobalVariable,
		UserMacro,
		UserRefMethod,
		NameSpace,
	}
	readonly static Regex regexCom = preCompiledComRegex();
	readonly static Regex regexComAble = preCompiledComAbleRegex();
	readonly static Regex regexAblup = preCompiledAblupRegex();
	#region static

	public static bool IsEventLabelName(string labelName)
	{
		switch (labelName)
		{
			case "EVENTFIRST":
			case "EVENTTRAIN":
			case "EVENTSHOP":
			case "EVENTBUY":
			case "EVENTCOM":
			case "EVENTTURNEND":
			case "EVENTCOMEND":
			case "EVENTEND":
			case "EVENTLOAD":
				return true;
		}
		return false;
	}
	public static bool IsSystemLabelName(string labelName)
	{
		switch (labelName)
		{
			case "EVENTFIRST":
			case "EVENTTRAIN":
			case "EVENTSHOP":
			case "EVENTBUY":
			case "EVENTCOM":
			case "EVENTTURNEND":
			case "EVENTCOMEND":
			case "EVENTEND":
			case "SHOW_STATUS":
			case "SHOW_USERCOM":
			case "USERCOM":
			case "SOURCE_CHECK":
			case "CALLTRAINEND":
			case "SHOW_JUEL":
			case "SHOW_ABLUP_SELECT":
			case "USERABLUP":
			case "SHOW_SHOP":
			case "SAVEINFO":
			case "USERSHOP":

			case "EVENTLOAD":
			case "TITLE_LOADGAME":
			case "SYSTEM_AUTOSAVE":
			case "SYSTEM_TITLE":
			case "SYSTEM_LOADEND":
				return true;
		}

		if (regexCom.IsMatch(labelName))
			return true;
		if (regexComAble.IsMatch(labelName))
			return true;
		if (regexAblup.IsMatch(labelName))
			return true;
		return false;
	}
	#endregion


	readonly Dictionary<string, DefinedNameType> nameDic = new(Config.StrComper);

	List<string> privateDimList = [];
	//Dictionary<string, VariableToken> userDefinedVarDic = new Dictionary<string, VariableToken>();

	VariableData varData;
	readonly Dictionary<string, VariableToken> varTokenDic;
	readonly Dictionary<string, VariableLocal> localvarTokenDic;
	readonly Dictionary<string, FunctionIdentifier> instructionDic;
	readonly Dictionary<string, FunctionMethod> methodDic;
	readonly Dictionary<string, UserDefinedRefMethod> refmethodDic;
	public List<UserDefinedCharaVariableToken> CharaDimList = [];

	#region initialize
	public IdentifierDictionary(VariableData varData)
	{
		this.varData = varData;
		nameDic.Clear();
		//予約語を登録。式中に登場すると構文解析が崩壊する名前群。
		//ただしeramaker用スクリプトなら特に気にすることはない。式中に出てこない単語も同様。
		nameDic.Add("IS", DefinedNameType.Reserved);
		nameDic.Add("TO", DefinedNameType.Reserved);
		nameDic.Add("INT", DefinedNameType.Reserved);
		nameDic.Add("STR", DefinedNameType.Reserved);
		nameDic.Add("REFFUNC", DefinedNameType.Reserved);
		nameDic.Add("STATIC", DefinedNameType.Reserved);
		nameDic.Add("DYNAMIC", DefinedNameType.Reserved);
		nameDic.Add("GLOBAL", DefinedNameType.Reserved);
		nameDic.Add("PRIVATE", DefinedNameType.Reserved);
		nameDic.Add("SAVEDATA", DefinedNameType.Reserved);
		nameDic.Add("CHARADATA", DefinedNameType.Reserved);//CHARDATAから変更
		nameDic.Add("REF", DefinedNameType.Reserved);
		nameDic.Add("__DEBUG__", DefinedNameType.Reserved);
		nameDic.Add("__SKIP__", DefinedNameType.Reserved);
		nameDic.Add("_", DefinedNameType.Reserved);
		try
		{
			instructionDic = FunctionIdentifier.GetInstructionNameDic();
		}
		catch
		{
			throw new CodeEE(treer.DoNotInstallWMP.Text);
		}


		varTokenDic = varData.GetVarTokenDicClone();
		localvarTokenDic = varData.GetLocalvarTokenDic();
		methodDic = FunctionMethodCreator.GetMethodList();
		refmethodDic = new(Config.StrComper);

		foreach (KeyValuePair<string, FunctionMethod> pair in methodDic)
		{
			nameDic.Add(pair.Key, DefinedNameType.SystemMethod);
		}

		foreach (KeyValuePair<string, VariableToken> pair in varTokenDic)
		{
			//RANDが衝突している
			//1808a3 GLOBAL、PRIVATEも
			//1808beta009 REFも
			nameDic.TryAdd(pair.Key, DefinedNameType.SystemVariable);
		}

		foreach (KeyValuePair<string, VariableLocal> pair in localvarTokenDic)
		{
			nameDic.Add(pair.Key, DefinedNameType.SystemVariable);
		}

		foreach (KeyValuePair<string, FunctionIdentifier> pair in instructionDic)
		{
			//Methodと被る
			//1808a3 SAVEDATAも
			nameDic.TryAdd(pair.Key, DefinedNameType.SystemInstrument);
		}
	}

	//public void SetSystemInstrumentName(List<string> names)
	//{
	//}

	public void CheckUserLabelName(out string errMes, ref int warnLevel, bool isFunction, string labelName)
	{
		errMes = "";
		if (labelName.Length == 0)
		{
			errMes = treer.LabelNameMissing.Text;
			warnLevel = 2;
			return;
		}
		//1.721 記号をサポートしない方向に変更
		if (labelName.AsSpan().ContainsAny(badSymbolAsIdentifier))
		{
			errMes = string.Format(treer.LabelContainsOtherThanUnderline.Text, labelName);
			warnLevel = 1;
			return;
		}
		if (char.IsDigit(labelName[0]) && labelName[0].ToString().Length == LangManager.GetStrlenLang(labelName[0].ToString()))
		{
			errMes = string.Format(treer.LabelStartedHalfDigit.Text, labelName);
			warnLevel = 0;
			return;
		}
		if (!isFunction || !Config.WarnFunctionOverloading)
			return;
		if (nameDic.TryGetValue(labelName, out DefinedNameType value))
		{
			switch (value)
			{
				case DefinedNameType.Reserved:
					if (Config.AllowFunctionOverloading)
					{
						errMes = string.Format(treer.LabelConflictReservedWord1.Text, labelName);
						warnLevel = 1;
					}
					else
					{
						errMes = string.Format(treer.LabelConflictReservedWord2.Text, labelName);
						warnLevel = 2;
					}
					break;
				case DefinedNameType.SystemMethod:
					if (Config.AllowFunctionOverloading)
					{
						errMes = string.Format(treer.LabelOverwriteInternalExpression.Text, labelName);
						warnLevel = 1;
					}
					else
					{
						errMes = string.Format(treer.LabelNameAlreadyUsedInternalExpression.Text, labelName);
						warnLevel = 2;
					}
					break;
				case DefinedNameType.SystemVariable:
					errMes = string.Format(treer.LabelNameAlreadyUsedInternalVariable.Text, labelName);
					warnLevel = 1;
					break;
				case DefinedNameType.SystemInstrument:
					errMes = string.Format(treer.LabelNameAlreadyUsedInternalInstruction.Text, labelName);
					warnLevel = 1;
					break;
				case DefinedNameType.UserMacro:
					//字句解析がうまくいっていれば本来あり得ないはず
					errMes = string.Format(treer.LabelNameAlreadyUsedMacro.Text, labelName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserRefMethod:
					errMes = string.Format(treer.LabelNameAlreadyUsedRefFunction.Text, labelName);
					warnLevel = 2;
					break;
			}
		}
		return;
	}

	public void CheckUserVarName(ref string errMes, ref int warnLevel, string varName)
	{
		//if (varName.Length == 0)
		//{
		//    errMes = "変数名がありません";
		//    warnLevel = 2;
		//    return;
		//}
		//1.721 記号をサポートしない方向に変更
		if (varName.AsSpan().IndexOfAny(badSymbolAsIdentifier) != -1)
		{
			errMes = string.Format(treer.VarContainsOtherThanUnderline.Text, varName);
			warnLevel = 2;
			return;
		}
		//if (char.IsDigit(varName[0]))
		//{
		//    errMes = "変数名" + varName + "が半角数字から始まっています";
		//    warnLevel = 2;
		//    return;
		//}

		if (nameDic.TryGetValue(varName, out DefinedNameType value))
		{
			switch (value)
			{
				case DefinedNameType.Reserved:
					errMes = string.Format(treer.VarConflictReservedWord.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.SystemInstrument:
				case DefinedNameType.SystemMethod:
					//代入文が使えなくなるために命令名との衝突は致命的。
					errMes = string.Format(treer.VarNameAlreadyUsedInternalInstruction.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.SystemVariable:
					errMes = string.Format(treer.VarNameAlreadyUsedInternalVariable.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserMacro:
					errMes = string.Format(treer.VarNameAlreadyUsedMacro.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserGlobalVariable:
					errMes = string.Format(treer.VarNameAlreadyUsedGlobalVariable.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserRefMethod:
					errMes = string.Format(treer.VarNameAlreadyUsedRefFunction.Text, varName);
					warnLevel = 2;
					break;
			}
		}
	}

	public void CheckUserMacroName(ref string errMes, ref int warnLevel, string macroName)
	{
		if (macroName.AsSpan().IndexOfAny(badSymbolAsIdentifier) != -1)
		{
			errMes = string.Format(treer.MacroContainsOtherThanUnderline.Text, macroName);
			warnLevel = 2;
			return;
		}
		if (nameDic.TryGetValue(macroName, out DefinedNameType value))
		{
			switch (value)
			{
				case DefinedNameType.Reserved:
					errMes = string.Format(treer.MacroConflictReservedWord.Text, macroName);
					warnLevel = 2;
					break;
				case DefinedNameType.SystemInstrument:
				case DefinedNameType.SystemMethod:
					//命令名を上書きした時が面倒なのでとりあえず許可しない
					errMes = string.Format(treer.MacroNameAlreadyUsedInternalInstruction.Text, macroName);
					warnLevel = 2;
					break;
				case DefinedNameType.SystemVariable:
					//別に上書きしてもいいがとりあえず許可しないでおく。いずれ解放するかもしれない
					errMes = string.Format(treer.MacroNameAlreadyUsedInternalVariable.Text, macroName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserMacro:
					errMes = string.Format(treer.MacroNameAlreadyUsedMacro.Text, macroName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserGlobalVariable:
					errMes = string.Format(treer.MacroNameAlreadyUsedGlobalVariable.Text, macroName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserRefMethod:
					errMes = string.Format(treer.MacroNameAlreadyUsedRefFunction.Text, macroName);
					warnLevel = 2;
					break;
			}
		}
	}

	public void CheckUserPrivateVarName(ref string errMes, ref int warnLevel, string varName)
	{
		if (varName.Length == 0)
		{
			errMes = treer.LabelNameMissing.Text;
			warnLevel = 2;
			return;
		}
		//1.721 記号をサポートしない方向に変更
		if (varName.AsSpan().IndexOfAny(badSymbolAsIdentifier) != -1)
		{
			errMes = string.Format(treer.VarContainsOtherThanUnderline.Text, varName);
			warnLevel = 2;
			return;
		}
		if (char.IsDigit(varName[0]))
		{
			errMes = string.Format(treer.VarStartedHalfDigit.Text, varName);
			warnLevel = 2;
			return;
		}
		if (nameDic.TryGetValue(varName, out DefinedNameType value))
		{
			switch (value)
			{
				case DefinedNameType.Reserved:
					errMes = string.Format(treer.VarConflictReservedWord.Text, varName);
					warnLevel = 2;
					return;
				case DefinedNameType.SystemInstrument:
				case DefinedNameType.SystemMethod:
					//代入文が使えなくなるために命令名との衝突は致命的。
					errMes = string.Format(treer.VarNameAlreadyUsedInternalInstruction.Text, varName);
					warnLevel = 2;
					return;
				case DefinedNameType.SystemVariable:
					//システム変数の上書きは不可
					errMes = string.Format(treer.VarNameAlreadyUsedInternalVariable.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserMacro:
					//字句解析がうまくいっていれば本来あり得ないはず
					errMes = string.Format(treer.VarNameAlreadyUsedMacro.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserGlobalVariable:
					//広域変数の上書きは禁止しておく
					errMes = string.Format(treer.VarNameAlreadyUsedGlobalVariable.Text, varName);
					warnLevel = 2;
					break;
				case DefinedNameType.UserRefMethod:
					errMes = string.Format(treer.VarNameAlreadyUsedRefFunction.Text, varName);
					warnLevel = 2;
					break;
			}
		}
		privateDimList.Add(varName);
	}
	#endregion

	#region header.erb
	//1807 ErbLoaderに移動
	Dictionary<int, DefineMacro> macroDic = [];

	internal void AddUseDefinedVariable(VariableToken var)
	{
		varTokenDic.Add(var.Name, var);
		if (var.IsCharacterData)
		{

		}
		nameDic.Add(var.Name, DefinedNameType.UserGlobalVariable);
	}
	internal void AddMacro(DefineMacro mac)
	{
		nameDic.Add(mac.Keyword, DefinedNameType.UserMacro);
		int key;
		if (Config.IgnoreCase)
		{
			key = mac.Keyword.GetHashCode(StringComparison.OrdinalIgnoreCase);
		}
		else
		{
			key = mac.Keyword.GetHashCode(StringComparison.Ordinal);
		}
		macroDic.Add(key, mac);
	}
	internal void AddRefMethod(UserDefinedRefMethod refm)
	{
		refmethodDic.Add(refm.Name, refm);
		nameDic.Add(refm.Name, DefinedNameType.UserRefMethod);
	}
	#endregion

	#region get

	public bool UseMacro()
	{
		return macroDic.Count > 0;
	}

	public DefineMacro GetMacro(string key)
	{
		int hash;
		if (Config.IgnoreCase)
		{
			hash = key.GetHashCode(StringComparison.OrdinalIgnoreCase);
		}
		else
		{
			hash = key.GetHashCode(StringComparison.Ordinal);
		}
		if (macroDic.TryGetValue(hash, out var value))
			return value;
		return null;
	}

	public VariableToken GetVariableToken(string key, string subKey, bool allowPrivate)
	{
		VariableToken ret;
		//if (Config.Config.IgnoreCase)
		//	key = key.ToUpper();
		if (allowPrivate)
		{
			LogicalLine line = GlobalStatic.Process.GetScaningLine();
			if ((line != null) && (line.ParentLabelLine != null))
			{
				ret = line.ParentLabelLine.GetPrivateVariable(key);
				if (ret != null)
				{
					if (subKey != null)
						throw new CodeEE(string.Format(treer.UsedAtForPrivVar.Text, key));
					return ret;
				}
			}
		}
		if (localvarTokenDic.TryGetValue(key, out VariableLocal value))
		{
			if (value.IsForbid)
			{
				throw new CodeEE(string.Format(treer.UsedProhibitedVar.Text, key));
			}
			LogicalLine line = GlobalStatic.Process.GetScaningLine();
			if (string.IsNullOrEmpty(subKey))
			{
				//システムの入力待ち中にデバッグコマンドからLOCALを呼んだとき。
				if ((line == null) || (line.ParentLabelLine == null))
					throw new CodeEE(string.Format(treer.CannotGetKeyNotExistRunningFunction.Text, key));
				subKey = line.ParentLabelLine.LabelName;
			}
			else
			{
				ParserMediator.Warn(treer.CannotRecommendCallLocalVar.Text, line, 1, false, false);
			}
			LocalVariableToken retLocal = value.GetExistLocalVariableToken(subKey);
			retLocal ??= value.GetNewLocalVariableToken(subKey, line.ParentLabelLine);
			return retLocal;
		}
		if (varTokenDic.TryGetValue(key, out ret))
		{
			//一文字変数の禁止オプションを考えた名残
			//if (Config.ForbidOneCodeVariable && ret.CanForbid)
			//    throw new CodeEE("設定によりシステム一文字数値変数の使用が禁止されています(呼び出された変数：" + ret.Name +")");
			if (ret.IsForbid)
			{
				if (!ret.CanForbid)
					throw new ExeEE(string.Format(treer.InvalidProhibitedVar.Text, ret.Name));
				throw new CodeEE(string.Format(treer.UsedProhibitedVar.Text, ret.Name));
			}
			if (subKey != null)
				throw new CodeEE(string.Format(treer.UsedAtForGlobalVar.Text, key));
			return ret;
		}
		if (subKey != null)
			throw new CodeEE(treer.InvalidAt.Text);
		return null;
	}

	public FunctionIdentifier GetFunctionIdentifier(string str)
	{
		string key = str;
		if (string.IsNullOrEmpty(key))
			return null;
		if (Config.IgnoreCase)
			key = key.ToUpper();
		if (instructionDic.TryGetValue(key, out FunctionIdentifier ret))
			return ret;
		else
			return null;
	}

	public List<string> GetOverloadedList(LabelDictionary labelDic)
	{
		List<string> list = [];
		foreach (KeyValuePair<string, FunctionMethod> pair in methodDic)
		{
			FunctionLabelLine func = labelDic.GetNonEventLabel(pair.Key);
			if (func == null)
				continue;
			if (!func.IsMethod)
				continue;
			list.Add(pair.Key);
		}
		return list;
	}

	public UserDefinedRefMethod GetRefMethod(string codeStr)
	{
		if (refmethodDic.TryGetValue(codeStr, out UserDefinedRefMethod value))
			return value;
		return null;
	}

	public AExpression GetFunctionMethod(LabelDictionary labelDic, string codeStr, List<AExpression> arguments, bool userDefinedOnly)
	{
		//if (Config.Config.IgnoreCase)
		//	codeStr = codeStr.ToUpper();
		if (arguments == null)//引数なし、名前のみの探索
		{
			if (refmethodDic.TryGetValue(codeStr, out UserDefinedRefMethod value))
				return new UserDefinedRefMethodNoArgTerm(value);
			return null;
		}
		if ((labelDic != null) && labelDic.Initialized)
		{
			if (refmethodDic.TryGetValue(codeStr, out UserDefinedRefMethod value))
				return new UserDefinedRefMethodTerm(value, arguments);
			FunctionLabelLine func = labelDic.GetNonEventLabel(codeStr);
			if (func != null)
			{
				if (userDefinedOnly && !func.IsMethod)
				{
					throw new CodeEE(string.Format(treer.CallfNonMethodFunc.Text, func.LabelName));
				}
				if (func.IsMethod)
				{
					string errMes;
					AExpression ret = UserDefinedMethodTerm.Create(func, arguments, out errMes);
					if (ret == null)
						throw new CodeEE(errMes);
					return ret;
				}
				//1.721 #FUNCTIONが定義されていない関数は組み込み関数を上書きしない方向に。 PANCTION.ERBのRANDとか。
				if (!methodDic.ContainsKey(codeStr))
					throw new CodeEE(string.Format(treer.UsedNonMethodFunc.Text, func.Position.Value.Filename, func.Position.Value.LineNo));
			}
		}
		if (userDefinedOnly)
			return null;
		if (!methodDic.TryGetValue(codeStr, out FunctionMethod method))
			return null;
		string errmes = method.CheckArgumentType(codeStr, arguments);
		if (errmes != null)
			throw new CodeEE(errmes);
		return new FunctionMethodTerm(method, arguments);
	}

	//1756 作成中途
	//名前リストを元に何がやりたかったのかを推定してCodeEEを投げる
	//1822 DIMリストの解決中にIdentifierNotFoundCodeEEが飛んだ場合にはやり直しの可能性がある
	public void ThrowException(string str, bool isFunc)
	{
		string idStr = str;
		//if (Config.Config.IgnoreCase || Config.Config.IgnoreCase) //片方だけなのは互換性用オプションなのでレアケースのはず。対応しない。
		//	idStr = idStr.ToUpper();
		if (!isFunc && privateDimList.Contains(idStr))
			throw new IdentifierNotFoundCodeEE(string.Format(treer.VarNotDefinedThisFunc.Text, str));
		if (nameDic.TryGetValue(idStr, out DefinedNameType value))
		{
			DefinedNameType type = value;
			switch (type)
			{
				case DefinedNameType.Reserved:
					throw new CodeEE(string.Format(treer.IllegalUseReservedWord.Text, str));
				case DefinedNameType.SystemVariable:
				case DefinedNameType.UserGlobalVariable:
					if (isFunc)
						throw new CodeEE(string.Format(treer.UseVarLikeFunc.Text, str));
					break;
				case DefinedNameType.SystemMethod:
				case DefinedNameType.UserRefMethod:
					if (!isFunc)
						throw new CodeEE(string.Format(treer.UseFuncLikeVar.Text, str));
					break;
				case DefinedNameType.UserMacro:
					throw new CodeEE(string.Format(treer.UnexpectedMacro.Text, str));
				case DefinedNameType.SystemInstrument:
					if (isFunc)
						throw new CodeEE(string.Format(treer.UseInstructionLikeFunc.Text, str));
					else
						throw new CodeEE(string.Format(treer.UseInstructionLikeVar.Text, str));

			}
		}
		if (!JSONConfig.Data.UseScopedVariableInstruction &&
			(idStr == "VARS" || idStr == "VARI"))
		{
			throw new CodeEE(string.Format(treer.CanNotUseVAR.Text, idStr));
		}

		throw new IdentifierNotFoundCodeEE(string.Format(treer.CanNotInterpreted.Text, idStr));
	}
	[GeneratedRegex("^COM[0-9]+$")]
	private static partial Regex preCompiledComRegex();
	[GeneratedRegex("^COM_ABLE[0-9]+$")]
	private static partial Regex preCompiledComAbleRegex();
	[GeneratedRegex("^ABLUP[0-9]+$")]
	private static partial Regex preCompiledAblupRegex();

	#endregion

	#region util
	public void resizeLocalVars(string key, string subKey, int newSize)
	{
		localvarTokenDic[key].ResizeLocalVariableToken(subKey, newSize);
	}

	public int getLocalDefaultSize(string key)
	{
		return localvarTokenDic[key].GetDefaultSize();
	}

	public bool getLocalIsForbid(string key)
	{
		return localvarTokenDic[key].IsForbid;
	}
	public bool getVarTokenIsForbid(string key)
	{
		if (localvarTokenDic.TryGetValue(key, out VariableLocal value))
			return value.IsForbid;
		varTokenDic.TryGetValue(key, out VariableToken var);
		if (var != null)
			return var.IsForbid;
		return true;
	}
	#endregion


}
