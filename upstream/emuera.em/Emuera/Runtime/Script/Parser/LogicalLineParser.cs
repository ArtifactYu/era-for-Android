using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Script.Parser;

internal static class LogicalLineParser
{
	public static bool ParseSharpLine(FunctionLabelLine label, CharStream st, ScriptPosition? position, List<string> OnlyLabel)
	{
		st.ShiftNext();//'#'を飛ばす
		var token = LexicalAnalyzer.ReadSingleIdentifier(st);//#～自体にはマクロ非適用
															 //#行として不正な行でもAnalyzeに行って引っかかることがあるので、先に存在しない#～は弾いてしまう
		if (string.IsNullOrEmpty(token))
		{
			ParserMediator.Warn(trerror.CanNotInterpretSharpLine.Text, position, 1);
			return false;
		}
		try
		{
			switch (token)
			{
				case var s when s.Equals("SINGLE", Config.Config.StringComparison):
					if (label.IsMethod)
					{
						ParserMediator.Warn(trerror.UseSingleUserFunc.Text, position, 1);
						break;
					}
					else if (!label.IsEvent)
					{
						ParserMediator.Warn(trerror.UsableSingleEventFunc.Text, position, 1);
						break;
					}
					else if (label.IsSingle)
					{
						ParserMediator.Warn(trerror.DuplicateSingle.Text, position, 1);
						break;
					}
					else if (label.IsOnly)
					{
						ParserMediator.Warn(trerror.OnlyWithSingle.Text, position, 1);
						break;
					}
					label.IsSingle = true;
					break;
				case var s when s.Equals("LATER", Config.Config.StringComparison):
					if (label.IsMethod)
					{
						ParserMediator.Warn(trerror.UseLaterUserFunc.Text, position, 1);
						break;
					}
					else if (!label.IsEvent)
					{
						ParserMediator.Warn(trerror.UsableLaterEventFunc.Text, position, 1);
						break;
					}
					else if (label.IsLater)
					{
						ParserMediator.Warn(trerror.DuplicateLater.Text, position, 1);
						break;
					}
					else if (label.IsOnly)
					{
						ParserMediator.Warn(trerror.OnlyWithLater.Text, position, 1);
						break;
					}
					else if (label.IsPri)
						ParserMediator.Warn(trerror.PriWithLater.Text, position, 1);
					label.IsLater = true;
					break;
				case var s when s.Equals("PRI", Config.Config.StringComparison):
					if (label.IsMethod)
					{
						ParserMediator.Warn(trerror.UsePriUserFunc.Text, position, 1);
						break;
					}
					else if (!label.IsEvent)
					{
						ParserMediator.Warn(trerror.UsablePriEventFunc.Text, position, 1);
						break;
					}
					else if (label.IsPri)
					{
						ParserMediator.Warn(trerror.DuplicatePri.Text, position, 1);
						break;
					}
					else if (label.IsOnly)
					{
						ParserMediator.Warn(trerror.OnlyWithPri.Text, position, 1);
						break;
					}
					else if (label.IsLater)
						ParserMediator.Warn(trerror.PriWithLater.Text, position, 1);
					label.IsPri = true;
					break;
				case var s when s.Equals("ONLY", Config.Config.StringComparison):
					if (label.IsMethod)
					{
						ParserMediator.Warn(trerror.UseOnlyUserFunc.Text, position, 1);
						break;
					}
					else if (!label.IsEvent)
					{
						ParserMediator.Warn(trerror.UsableOnlyEventFunc.Text, position, 1);
						break;
					}
					else if (label.IsOnly)
					{
						ParserMediator.Warn(trerror.DuplicateOnly.Text, position, 1);
						break;
					}
					else if (OnlyLabel.Contains(label.LabelName))
						ParserMediator.Warn(string.Format(trerror.AlreadyDeclaredOnly.Text, label.LabelName), position, 1);
					OnlyLabel.Add(label.LabelName);
					label.IsOnly = true;
					if (label.IsPri)
					{
						ParserMediator.Warn(trerror.BeIgnorePri.Text, position, 1);
						label.IsPri = false;
					}
					if (label.IsLater)
					{
						ParserMediator.Warn(trerror.BeIgnoreLater.Text, position, 1);
						label.IsLater = false;
					}
					if (label.IsSingle)
					{
						ParserMediator.Warn(trerror.BeIgnoreSingle.Text, position, 1);
						label.IsSingle = false;
					}
					break;
				case var s when s.Equals("FUNCTION", Config.Config.StringComparison) ||
								s.Equals("FUNCTIONS", Config.Config.StringComparison):
					if (!string.IsNullOrEmpty(label.LabelName) && char.IsDigit(label.LabelName[0]))
					{
						ParserMediator.Warn(string.Format(trerror.CanNotDeclaredBeginNumberFunction.Text, token.ToString()), position, 1);
						label.IsError = true;
						label.ErrMes = trerror.FuncNameBeginNumber.Text;
						break;
					}
					if (label.IsMethod)
					{
						if (label.MethodType == typeof(long) && token.SequenceEqual("FUNCTION") || label.MethodType == typeof(string) && token.SequenceEqual("FUNCTIONS"))
						{
							ParserMediator.Warn(string.Format(trerror.AlreadySharpDeclared.Text, label.LabelName, token.ToString()), position, 1);
							return false;
						}
						if (label.MethodType == typeof(long) && token.SequenceEqual("FUNCTIONS"))
							ParserMediator.Warn(string.Format(trerror.AlreadyDeclaredSharpFunction.Text, label.LabelName), position, 2);
						else if (label.MethodType == typeof(string) && token.SequenceEqual("FUNCTION"))
							ParserMediator.Warn(string.Format(trerror.AlreadyDeclaredSharpFunctions.Text, label.LabelName), position, 2);
						return false;
					}
					if (label.Depth == 0)
					{
						ParserMediator.Warn(string.Format(trerror.UseSharpInSystemFunc.Text, token.ToString()), position, 2);
						return false;
					}
					label.IsMethod = true;
					label.Depth = 0;
					if (token.Equals("FUNCTIONS", Config.Config.StringComparison))
						label.MethodType = typeof(string);
					else
						label.MethodType = typeof(long);
					if (label.IsPri)
					{
						ParserMediator.Warn(trerror.UsePriUserFunc.Text, position, 1);
						label.IsPri = false;
					}
					if (label.IsLater)
					{
						ParserMediator.Warn(trerror.UseLaterUserFunc.Text, position, 1);
						label.IsLater = false;
					}
					if (label.IsSingle)
					{
						ParserMediator.Warn(trerror.UseSingleUserFunc.Text, position, 1);
						label.IsSingle = false;
					}
					if (label.IsOnly)
					{
						ParserMediator.Warn(trerror.UseOnlyUserFunc.Text, position, 1);
						label.IsOnly = false;
					}
					break;
				case var s when s.Equals("LOCALSIZE", Config.Config.StringComparison) ||
								s.Equals("LOCALSSIZE", Config.Config.StringComparison):
					{
						WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
						if (wc.EOL)
						{
							ParserMediator.Warn(string.Format(trerror.SharpHasNotValidValue.Text, token.ToString()), position, 2);
							break;
						}
						//イベント関数では指定しても無視される
						if (label.IsEvent)
						{
							ParserMediator.Warn(string.Format(trerror.EventFuncIgnoreSpecified.Text, token.ToString(), token[..^4].ToString()), position, 1);
							break;
						}
						AExpression arg = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL);
						if (arg.Restructure(null) is not SingleLongTerm sizeTerm || sizeTerm.GetOperandType() != typeof(long))
						{
							ParserMediator.Warn(string.Format(trerror.SharpHasNotValidValue.Text, token.ToString()), position, 2);
							break;
						}
						if (sizeTerm.Int <= 0)
						{
							ParserMediator.Warn(string.Format(trerror.LocalsizeLessThan1.Text, token.ToString(), sizeTerm.Int.ToString()), position, 1);
							break;
						}
						if (sizeTerm.Int >= int.MaxValue)
						{
							ParserMediator.Warn(string.Format(trerror.TooManyLocalsize.Text, token.ToString(), sizeTerm.Int.ToString()), position, 1);
							break;
						}
						int size = (int)sizeTerm.Int;
						if (token.SequenceEqual("LOCALSIZE"))
						{
							if (GlobalStatic.IdentifierDictionary.getLocalIsForbid("LOCAL"))
							{
								ParserMediator.Warn(string.Format(trerror.LocalIsProhibited.Text, token.ToString(), "LOCAL"), position, 2);
								break;
							}
							if (label.LocalLength > 0)
								ParserMediator.Warn(trerror.DuplicateLocalsize.Text, position, 1);
							label.LocalLength = size;
						}
						else
						{
							if (GlobalStatic.IdentifierDictionary.getLocalIsForbid("LOCALS"))
							{
								ParserMediator.Warn(string.Format(trerror.LocalIsProhibited.Text, token.ToString(), "LOCALS"), position, 2);
								break;
							}
							if (label.LocalsLength > 0)
								ParserMediator.Warn(trerror.DuplicateLocalssize.Text, position, 1);
							label.LocalsLength = size;
						}
					}
					break;
				case var s when s.Equals("DIM", Config.Config.StringComparison) ||
								s.Equals("DIMS", Config.Config.StringComparison):
					{
						var wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);

						UserDefinedVariableData data = UserDefinedVariableData.Create(wc, token.SequenceEqual("DIMS"), true, position);
						if (!label.AddPrivateVariable(data))
						{
							ParserMediator.Warn(string.Format(trerror.VarNameAlreadyUsed.Text, data.Name), position, 2);
							return false;
						}
						if (!wc.EOL)
							ParserMediator.Warn(trerror.ExtraCharacterAfterSharp.Text, position, 1);
						break;
					}
				default:
					ParserMediator.Warn(trerror.CanNotInterpretSharpLine.Text, position, 1);
					break;
			}
		}
		catch (Exception e)
		{
			ParserMediator.Warn(e.Message, position, 2);
			return false;
		}
		return true;
	}

	public static LogicalLine ParseLine(string str, EmueraConsole console)
	{
		ScriptPosition? position = new();
		CharStream stream = new(str);
		return ParseLine(stream, position, console);
	}

	public static LogicalLine ParseLabelLine(CharStream stream, ScriptPosition? position, EmueraConsole console)
	{
		bool isFunction = stream.Current == '@';
		//int lineNo = Position.Value.LineNo;
		string labelName = "";
		string errMes = "";
		try
		{
			int warnLevel = -1;
			stream.ShiftNext();//@か$を除去
			var wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
			if (wc.EOL || wc.Current is not IdentifierWord iw)
			{
				return err(position, isFunction, ref labelName, trerror.InvalidFunc.Text);
			}
			labelName = iw.Code;
			wc.ShiftNext();
			GlobalStatic.IdentifierDictionary.CheckUserLabelName(out errMes, ref warnLevel, isFunction, labelName);
			if (warnLevel >= 0)
			{
				if (warnLevel >= 2)
					return err(position, isFunction, ref labelName, errMes);
				ParserMediator.Warn(errMes, position, warnLevel);
			}
			if (!isFunction)//$ならこの時点で終了
			{
				if (!wc.EOL)
					ParserMediator.Warn(trerror.LabelHasArg.Text, position, 1);
				return new GotoLabelLine(position, labelName);
			}



			//labelName = LexicalAnalyzer.ReadString(stream, StrEndWith.LeftParenthesis_Bracket_Comma_Semicolon);
			//labelName = labelName.Trim();
			//if (Config.Config.IgnoreCase)
			//    labelName = labelName.ToUpper();
			//GlobalStatic.IdentifierDictionary.CheckUserLabelName(ref errMes, ref warnLevel, isFunction, labelName);
			//if(warnLevel >= 0)
			//{
			//    if (warnLevel >= 2)
			//        goto err;
			//    ParserMediator.Warn(errMes, position, warnLevel);
			//}
			//if (!isFunction)//$ならこの時点で終了
			//{
			//    LexicalAnalyzer.SkipWhiteSpace(stream);
			//    if (!stream.EOS)
			//        ParserMediator.Warn("$で始まるラベルに引数が設定されています", position, 1);
			//    return new GotoLabelLine(position, labelName);
			//}

			////関数名部分に_renameを使えないように変更
			//if (ParserMediator.RenameDic != null && ((stream.ToString().IndexOf("[[") >= 0) && (stream.ToString().IndexOf("]]") >= 0)))
			//{
			//    string line = stream.ToString();
			//    foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
			//        line = line.Replace(pair.Key, pair.Value);
			//    stream = new StringStream(line);
			//}
			//WordCollection wc = null;
			//wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
			if (Program.AnalysisMode)
				console.PrintC("@" + labelName, false);
			FunctionLabelLine funclabelLine = new(position, labelName, wc);
			if (IdentifierDictionary.IsEventLabelName(labelName))
			{
				funclabelLine.IsEvent = true;
				funclabelLine.IsSystem = true;
				funclabelLine.Depth = 0;
			}
			else if (IdentifierDictionary.IsSystemLabelName(labelName))
			{
				funclabelLine.IsSystem = true;
				funclabelLine.Depth = 0;
			}
			return funclabelLine;
		}
		catch (CodeEE e)
		{
			errMes = e.Message;
		}
		return err(position, isFunction, ref labelName, errMes);

		static LogicalLine err(ScriptPosition? position, bool isFunction, ref string labelName, string errMes)
		{
			System.Media.SystemSounds.Hand.Play();
			if (isFunction)
			{
				if (labelName.Length == 0)
					labelName = "<Error>";
				return new InvalidLabelLine(position, labelName, errMes);
			}
			return new InvalidLine(position, errMes);
		}
	}


	public static LogicalLine ParseLine(CharStream stream, ScriptPosition? position, EmueraConsole console, FunctionLabelLine parentLine = null)
	{
		//int lineNo = Position.Value.LineNo;
		string errMes;
		LexicalAnalyzer.SkipWhiteSpace(stream);//先頭のホワイトスペースを読み飛ばす
		if (stream.EOS)
			return null;
		//コメント行かどうかはここに来る前に判定しておく
		try
		{
			#region 前置インクリメント、デクリメント行
			var op = stream.Current;
			if (op == '+' || op == '-')
			{
				WordCollection wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.None);
				if (wc.Current is not OperatorWord opWT || opWT.Code != OperatorCode.Increment && opWT.Code != OperatorCode.Decrement)
				{
					if (op == '+')
						errMes = trerror.StartedPlusButNotIncrement.Text;
					else
						errMes = trerror.StartedMinusButNotDecrement.Text;
					return new InvalidLine(position, errMes);
				}
				wc.ShiftNext();
				//token = EpressionParser.単語一個分取得(wc)
				//token非変数
				//token文字列形
				//token変更不可能
				//if (wc != EOS)
				//
				return new InstructionLine(position, FunctionIdentifier.SETFunction, opWT.Code, wc, null);
			}
			#endregion
			IdentifierWord idWT = LexicalAnalyzer.ReadFirstIdentifierWord(stream);
			if (idWT != null)
			{
				FunctionIdentifier func = GlobalStatic.IdentifierDictionary.GetFunctionIdentifier(idWT.Code);
				//命令文
				if (func != null)//関数文
				{
					if (func.Code == FunctionCode.VARI || func.Code == FunctionCode.VARS)
					{
						var line = new InstructionLine(position, func, stream)
						{
							ParentLabelLine = parentLine
						};
						//統合_netEmuera: ここでは変数の宣言だけ行い、右辺は消費しない。
						//右辺の解析は VARI/VARS_Instruction.CreateArgument に任せる
						//（この時点ではユーザ定義関数がまだ登録されていないため）
						var statementsSpan = stream.SubstringROS();
						var commentIndex = statementsSpan.IndexOf(';');
						if (commentIndex != -1)
						{
							statementsSpan = statementsSpan[..commentIndex];
						}

						//右辺は CreateArgument が読むので、ここでは左辺だけ見る
						var equalsIndex = statementsSpan.IndexOf('=');
						string left = equalsIndex == -1
							? statementsSpan.ToString()
							: statementsSpan[..equalsIndex].ToString();

						var leftSplit = left.Split(',');
						var varName = leftSplit[0].Trim();
						List<int> lengths = [1];
						if (leftSplit.Length > 1)
						{
							//配列である
							lengths.Clear();
							for (int i = 1; i < leftSplit.Length; i++)
							{
								lengths.Add(int.Parse(leftSplit[i].Trim()));
							}
						}
						var varData = new UserDefinedVariableData
						{
							Name = varName,
							Static = false,
							Lengths = [.. lengths],
							Dimension = lengths.Count,
							TypeIsStr = func.Code == FunctionCode.VARS
						};
						parentLine.AddPrivateVariable(varData);
						return line;
					}

					if (stream.EOS) //引数の無い関数
						return new InstructionLine(position, func, stream);
					var current = stream.Current;
					if (current != ';' && current != ' ' && current != '\t' && (!Config.Config.SystemAllowFullSpace || current != '　'))
					{
						if (current == '　')
							errMes = string.Format(trerror.InvalidCharacterAfterInstruction1.Text, Config.Config.GetConfigName(ConfigCode.SystemAllowFullSpace));
						else
							errMes = trerror.InvalidCharacterAfterInstruction2.Text;
						return new InvalidLine(position, errMes)
						{
							ParentLabelLine = parentLine
						};
					}
					stream.ShiftNext();
					return new InstructionLine(position, func, stream);
				}
			}
			LexicalAnalyzer.SkipWhiteSpace(stream);
			if (stream.EOS)
			{
				errMes = trerror.CanNotInterpretedLine.Text;
				return new InvalidLine(position, errMes);
			}
			//命令行ではない→代入行のはず
			stream.Seek(0, System.IO.SeekOrigin.Begin);
			OperatorCode assignOP = OperatorCode.NULL;
			WordCollection wc1 = LexicalAnalyzer.Analyse(stream, LexEndWith.Operator, LexAnalyzeFlag.None);
			//if (idWT != null)
			//	wc1.Collection.Insert(0, idWT);
			try
			{
				assignOP = LexicalAnalyzer.ReadAssignmentOperator(stream);
			}
			catch (CodeEE)
			{
				errMes = trerror.CanNotInterpretedLine.Text;
				return new InvalidLine(position, errMes);
			}
			//eramaker互換警告
			//stream.Jump(-1);
			//if ((stream.Current != ' ') && (stream.Current != '\t'))
			//{
			//	errMes = "変数で行が始まっていますが、演算子の直前に半角スペースまたはタブがありません";
			//	goto err;
			//}
			//stream.ShiftNext();


			if (assignOP == OperatorCode.Equal)
			{
				if (console != null)
					ParserMediator.Warn(trerror.Use2EqualToAssign.Text, position, 0);
				//"=="を代入文に使うのは本当はおかしいが結構使われているので仕様にする
				assignOP = OperatorCode.Assignment;
			}
			return new InstructionLine(position, FunctionIdentifier.SETFunction, assignOP, wc1, stream);
		err:
			return new InvalidLine(position, errMes);
		}
		catch (CodeEE e)
		{
			System.Media.SystemSounds.Hand.Play();
			return new InvalidLine(position, e.Message);
		}
	}

}
