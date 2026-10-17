using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Function;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using MinorShift.Emuera.UI.Game;
using MinorShift.Emuera.UI.Game.Image;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.GameData.Function;

internal static partial class FunctionMethodCreator
{
	#region EM_私家版_追加関数
	private sealed class HtmlStringLenMethod : FunctionMethod
	{
		public HtmlStringLenMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int}, OmitStart = 1 }
				];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			int len = HtmlManager.HtmlLength(arguments[0].GetStrValue(exm));
			if (arguments.Count == 1 || arguments[1].GetIntValue(exm) == 0)
			{
				if (len >= 0)
					return 2 * len / Config.FontSize + ((2 * len % Config.FontSize != 0) ? 1 : 0);
				else
					return 2 * len / Config.FontSize - ((2 * len % Config.FontSize != 0) ? 1 : 0);
			}
			return len;
		}
	}
	private sealed class XmlGetMethod : FunctionMethod
	{
		public XmlGetMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.String, ArgType.Int, ArgType.Int}, OmitStart = 2 },
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.String, ArgType.RefString1D, ArgType.Int}, OmitStart = 3 },
				];
			CanRestructure = false;
		}
		public XmlGetMethod(bool byname) : this()
		{
			byName = byname;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.String, ArgType.Int, ArgType.Int}, OmitStart = 2 },
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.String, ArgType.RefString1D, ArgType.Int}, OmitStart = 3 },
				];
		}
		private bool byName;
		private static void OutPutNode(XmlNode node, string[] array, int i, long style)
		{
			switch (style)
			{
				case 1: array[i] = node.InnerText; break;
				case 2: array[i] = node.InnerXml; break;
				case 3: array[i] = node.OuterXml; break;
				case 4: array[i] = node.Name; break;
				default: array[i] = node.Value; break;
			}
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			XmlDocument doc = null;
			XmlNodeList nodes = null;
			if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
			{
				var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
				var dict = exm.VEvaluator.VariableData.DataXmlDocument;
				if (dict.ContainsKey(idx)) doc = dict[idx];
				else return -1;
			}
			else
			{
				doc = new XmlDocument();
				var xml = arguments[0].GetStrValue(exm);
				try
				{
					doc.LoadXml(xml);
				}
				catch (XmlException e)
				{
					throw new CodeEE(string.Format(trerror.XmlGetError.Text, xml, e.Message));
				}
			}
			string path = arguments[1].GetStrValue(exm);
			try
			{
				nodes = doc.SelectNodes(path);
			}
			catch (System.Xml.XPath.XPathException e)
			{
				throw new CodeEE(string.Format(trerror.XmlGetPathError.Text, path, e.Message));
			}
			long outputStyle = arguments.Count == 4 ? arguments[3].GetIntValue(exm) : 0;

			if (arguments.Count >= 3)
			{
				if (arguments[2].GetOperandType() == typeof(long) && arguments[2].GetIntValue(exm) != 0)
				{
					for (int i = 0; i < Math.Min(nodes.Count, exm.VEvaluator.RESULTS_ARRAY.Length); i++)
						OutPutNode(nodes[i], exm.VEvaluator.RESULTS_ARRAY, i, outputStyle);
				}
				else
				{
					var arr = (arguments[2] as VariableTerm).Identifier.GetArray() as string[];
					for (int i = 0; i < Math.Min(nodes.Count, arr.Length); i++)
						OutPutNode(nodes[i], arr, i, outputStyle);
				}
			}
			return nodes.Count;
		}
	}
	private sealed class IsDefinedMethod : FunctionMethod
	{
		public IsDefinedMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return (GlobalStatic.IdentifierDictionary.GetMacro(arguments[0].GetStrValue(exm)) != null) ? 1 : 0;
		}
	}
	private sealed class EnumNameMethod : FunctionMethod
	{
		public enum EType
		{
			Function,
			Variable,
			Macro
		}
		public enum EAction
		{
			BeginsWith,
			EndsWith,
			With
		}
		private EType type;
		private EAction action;
		public EnumNameMethod(EType type, EAction act)
		{
			ReturnType = typeof(long);
			CanRestructure = false;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.RefString1D }, OmitStart = 1 },
				];
			this.type = type;
			action = act;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string arg = arguments[0].GetStrValue(exm).ToUpper();
			string[] array = null;
			switch (type)
			{
				case EType.Function:
					array = GlobalStatic.Process.LabelDictionary.NoneventKeys;
					break;
				case EType.Variable:
					array = GlobalStatic.IdentifierDictionary.VarKeys;
					break;
				case EType.Macro:
					array = GlobalStatic.IdentifierDictionary.MacroKeys;
					break;
			}
			List<string> strs = [];
			if (arg.Length > 0)
				foreach (string item in array)
				{
					if (item.Length < arg.Length) continue;
					// andEmuera: 元は候補 1 本ごとに item.ToUpper() で新しい文字列を作っていた。
					// 候補は「登録されている全関数名 / 全変数名」なので、1 回の呼び出しで
					// 数千〜数万本ぶんの確保とカルチャ依存の大文字化 (ICU) が走る。
					// 大文字にしてから序数比較するのは大小無視の比較そのものなので、
					// 比較方法を変えて確保をなくす (辞書側も Config.StrComper = OrdinalIgnoreCase)
					switch (action)
					{
						case EAction.BeginsWith:
							if (item.StartsWith(arg, StringComparison.OrdinalIgnoreCase)) strs.Add(item);
							break;
						case EAction.EndsWith:
							if (item.EndsWith(arg, StringComparison.OrdinalIgnoreCase)) strs.Add(item);
							break;
						case EAction.With:
							if (item.Contains(arg, StringComparison.OrdinalIgnoreCase)) strs.Add(item);
							break;
					}
				}
			// strs.Sort();
			string[] output;
			if (arguments.Count == 2)
				output = (arguments[1] as VariableTerm).Identifier.GetArray() as string[];
			else
				output = exm.VEvaluator.RESULTS_ARRAY;
			string[] ret = strs.ToArray();
			int outputlength = Math.Min(output.Length, ret.Length);
			Array.Copy(ret, output, outputlength);
			return outputlength;
		}
	}
	private sealed class EnumFilesMethod : FunctionMethod
	{
		public EnumFilesMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.Int, ArgType.RefString1D }, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var dir = Utils.GetValidPath(arguments[0].GetStrValue(exm));
			// andEmuera: 元は Directory.Exists で事前確認していた。フォルダが無ければ下の
			// EnumerateFiles が投げ、catch が同じ -1 を返すので結果は変わらない。
			// ENUMFILES は実データのロードで 4,177 回呼ばれ、その 1 回ごとに
			// 1 回 stat が増えていた (端末は FUSE 越しなので 1 回が高い)
			if (dir == null) return -1;
			var pattern = arguments.Count > 1 ? arguments[1].GetStrValue(exm) : "*";
			//andEmuera: Android(ext4) は検索パターンの大文字小文字を区別する。Windows と同じ結果になるよう非依存で列挙する
			var option = arguments.Count > 2 && arguments[2].GetIntValue(exm) != 0
				? Config.CaseInsensitiveAllDirectories
				: Config.CaseInsensitiveTopDirectory;
			string[] files;
			try
			{
				files = Directory.EnumerateFiles(dir, pattern, option).ToArray();
				//andEmuera: ext4 の readdir 順は不定。NTFS は名前順に返すため、
				//「n 番目の差分」を添字で拾う ERB が Windows と同じ結果になるよう揃える
				Array.Sort(files, StringComparer.OrdinalIgnoreCase);
				// andEmuera: 列挙結果は必ずゲームフォルダ配下なので、
				// Path.GetRelativePath (正規化を通す) まで持ち出さずに切り落とせる。
				// 念のため前提が崩れたときだけ従来の経路へ落とす
				for (int i = 0; i < files.Length; i++)
				{
					files[i] = files[i].StartsWith(Program.ExeDir, StringComparison.Ordinal)
						? files[i][Program.ExeDir.Length..]
						: Path.GetRelativePath(Program.ExeDir, files[i]);
				}

			}
			catch
			{
				return -1;
			}
			string[] output;
			if (arguments.Count == 4)
				output = (arguments[3] as VariableTerm).Identifier.GetArray() as string[];
			else
				output = exm.VEvaluator.RESULTS_ARRAY;
			var ret = Math.Min(files.Length, output.Length);
			Array.Copy(files, output, ret);
			return ret;
		}
	}
	private sealed class GetVarMethod : FunctionMethod
	{
		public GetVarMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			WordCollection wc = LexicalAnalyzer.Analyse(new CharStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
			AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);

			if (term is VariableTerm)
			{
				VariableTerm var = (VariableTerm)term;

				if (var.Identifier == null)
					throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));
				if (!var.IsInteger)
					throw new CodeEE(string.Format(trerror.IsNotInt.Text, name));
				return var.GetIntValue(exm);
			}
			else
				throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));
		}
	}
	private sealed class GetVarsMethod : FunctionMethod
	{
		public GetVarsMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			WordCollection wc = LexicalAnalyzer.Analyse(new CharStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
			AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);

			if (term is VariableTerm)
			{
				VariableTerm var = (VariableTerm)term;

				if (var.Identifier == null)
					throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));
				if (!var.IsString)
					throw new CodeEE(string.Format(trerror.IsNotStr.Text, name));
				return var.GetStrValue(exm);
			}
			else
				throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));
		}
	}
	private sealed class ExistVarMethod : FunctionMethod
	{
		public ExistVarMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableToken token = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
			if (token != null)
			{
				long res = 0;
				if (token.IsInteger) res |= 1;
				if (token.IsString) res |= 2;
				if (token.IsConst) res |= 4;
				if (token.IsArray2D) res |= 8;
				if (token.IsArray3D) res |= 16;
				return res;
			}
			return 0;
		}
	}
	private sealed class ArrayMultiSortExMethod : FunctionMethod
	{
		public ArrayMultiSortExMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.RefString1D, ArgType.Int, ArgType.Int | ArgType.DisallowVoid }, OmitStart = 2 },
					new ArgTypeList{ ArgTypes = { ArgType.RefInt1D, ArgType.RefString1D, ArgType.Int, ArgType.Int | ArgType.DisallowVoid }, OmitStart = 2 },
				];
			CanRestructure = false;
		}
		private string CheckVariableTerm(AExpression arg, string v)
		{
			var vname = v == null ? trerror.FirstArg.Text : v;
			if (!(arg is VariableTerm varTerm) || varTerm.Identifier.IsCalc || varTerm.Identifier.IsConst)
				return string.Format(trerror.NotVarFunc.Text, Name, vname);
			if (v == null && !varTerm.Identifier.IsArray1D)
				return string.Format(trerror.Not1DFuncArg.Text, Name, "1");
			if (varTerm.Identifier.IsCharacterData)
				return string.Format(trerror.IsCharaVarFunc.Text, Name, vname);
			if (!varTerm.Identifier.IsArray1D && !varTerm.Identifier.IsArray2D && !varTerm.Identifier.IsArray3D)
				return string.Format(trerror.NotDimVarFunc.Text, Name, vname);
			return null;
		}
		private VariableTerm GetConvertedTerm(ExpressionMediator exm, string name)
		{
			WordCollection wc = LexicalAnalyzer.Analyse(new CharStream(name), LexEndWith.EoL, LexAnalyzeFlag.None);
			var term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
			var err = CheckVariableTerm(term, name);
			if (err != null)
				throw new CodeEE(err);
			return term as VariableTerm;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			bool isAscending = arguments.Count < 3 || arguments[2] == null || arguments[2].GetIntValue(exm) != 0;
			long fixedLength = arguments.Count < 4 ? -1 : arguments[3].GetIntValue(exm);
			if (fixedLength == 0) return 0;
			VariableTerm varTerm = arguments[0] is VariableTerm ? arguments[0] as VariableTerm : GetConvertedTerm(exm, arguments[0].GetStrValue(exm));
			int[] sortedArray;
			if (varTerm.Identifier.IsInteger)
			{
				List<KeyValuePair<long, int>> sortList = [];
				long[] array = (long[])varTerm.Identifier.GetArray();
				var length = fixedLength > 0 ? Math.Min(fixedLength, array.Length) : array.Length;
				for (int i = 0; i < length; i++)
				{
					if (fixedLength == -1 && array[i] == 0)
						break;
					if (array[i] < long.MinValue || array[i] > long.MaxValue)
						return 0;
					sortList.Add(new KeyValuePair<long, int>(array[i], i));
				}
				//素ではintの範囲しか扱えないので一工夫
				sortList.Sort((a, b) => { return (isAscending ? 1 : -1) * Math.Sign(a.Key - b.Key); });
				sortedArray = sortList.Select(p => p.Value).ToArray();
			}
			else
			{
				List<KeyValuePair<string, int>> sortList = [];
				string[] array = (string[])varTerm.Identifier.GetArray();
				var length = fixedLength > 0 ? Math.Min(fixedLength, array.Length) : array.Length;
				for (int i = 0; i < length; i++)
				{
					if (fixedLength == -1 && string.IsNullOrEmpty(array[i]))
						return 0;
					sortList.Add(new KeyValuePair<string, int>(array[i], i));
				}
				sortList.Sort((a, b) => { return (isAscending ? 1 : -1) * a.Key.CompareTo(b.Key); });
				sortedArray = sortList.Select(p => p.Value).ToArray();
			}
			List<VariableTerm> varTerms = [];
			foreach (var nTerm in (string[])(arguments[1] as VariableTerm).Identifier.GetArray())
				varTerms.Add(GetConvertedTerm(exm, nTerm));
			foreach (var term in varTerms)
			{
				if (term.Identifier.IsArray1D)
				{
					if (term.IsInteger)
					{
						var array = (long[])term.Identifier.GetArray();
						var clone = (long[])array.Clone();
						if (array.Length < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							array[i] = clone[sortedArray[i]];
					}
					else
					{
						var array = (string[])term.Identifier.GetArray();
						var clone = (string[])array.Clone();
						if (array.Length < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							array[i] = clone[sortedArray[i]];
					}
				}
				else if (term.Identifier.IsArray2D)
				{
					if (term.IsInteger)
					{
						var array = (long[,])term.Identifier.GetArray();
						var clone = (long[,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								array[i, x] = clone[sortedArray[i], x];
					}
					else
					{
						var array = (string[,])term.Identifier.GetArray();
						var clone = (string[,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								array[i, x] = clone[sortedArray[i], x];
					}
				}
				else if (term.Identifier.IsArray3D)
				{
					if (term.IsInteger)
					{
						var array = (long[,,])term.Identifier.GetArray();
						var clone = (long[,,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								for (int y = 0; y < array.GetLength(2); y++)
									array[i, x, y] = clone[sortedArray[i], x, y];
					}
					else
					{
						var array = (string[,,])term.Identifier.GetArray();
						var clone = (string[,,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								for (int y = 0; y < array.GetLength(2); y++)
									array[i, x, y] = clone[sortedArray[i], x, y];
					}
				}
				else { throw new ExeEE(trerror.AbnormalArray.Text); }
			}
			return 1;
		}
		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			for (int i = 0; i < arguments.Count; i++)
				arguments[i] = arguments[i].Restructure(exm);
			return false;
		}
	}
	private sealed class SetVarMethod : FunctionMethod
	{
		public SetVarMethod()
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
			WordCollection wc = LexicalAnalyzer.Analyse(new CharStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
			AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);

			if (term is VariableTerm var)
			{
				if (var.Identifier == null || var.Identifier.IsConst)
					throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));
				if (var.IsString)
				{
					if (arguments[1].GetOperandType() != typeof(string))
						throw new CodeEE(string.Format(trerror.IsNotInt.Text, name));
					var.SetValue(arguments[1].GetStrValue(exm), exm);
				}
				else
				{
					if (arguments[1].GetOperandType() != typeof(long))
						throw new CodeEE(string.Format(trerror.IsNotStr.Text, name));
					var.SetValue(arguments[1].GetIntValue(exm), exm);
				}
				return 1;
			}
			else
				throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));
		}
	}
	private sealed class VarSetExMethod : FunctionMethod
	{
		public VarSetExMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Any, ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			WordCollection wc = LexicalAnalyzer.Analyse(new CharStream(arguments[0].GetStrValue(exm)), LexEndWith.EoL, LexAnalyzeFlag.None);
			AExpression term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);

			if (term is VariableTerm var)
			{
				if (var.Identifier == null || var.Identifier.IsConst)
					throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));

				int start = (int)(arguments.Count >= 4 ? arguments[3].GetIntValue(exm) : 0);
				int end = (int)(arguments.Count == 5 ? arguments[4].GetIntValue(exm)
					: (var.Identifier.IsArray1D ? var.Identifier.GetLength()
					: (var.Identifier.IsArray2D ? var.Identifier.GetLength(1)
					: (var.Identifier.IsArray2D ? var.Identifier.GetLength(2) : 0))));
				bool setAllDims = arguments.Count >= 3 ? arguments[2].GetIntValue(exm) != 0 : true;
				if (var.IsString)
				{
					var val = string.Empty;
					if (arguments.Count > 1 && arguments[1].GetOperandType() != typeof(string))
						throw new CodeEE(string.Format(trerror.SetStrToInt.Text, name));
					if (arguments.Count > 1)
						val = arguments[1].GetStrValue(exm);
					if (var.Identifier.IsArray1D)
						var.Identifier.SetValueAll(val, start, end, 0);
					else if (var.Identifier.IsArray2D)
					{
						var array = var.Identifier.GetArray() as string[,];
						var idx1 = var.GetElementInt(0, exm);
						var idx2 = var.GetElementInt(1, exm);
						for (int i = Math.Max(start, (int)idx2); i < end; i++)
							array[idx1, i] = val;
					}
					if (var.Identifier.IsArray3D)
					{
						var idx1 = var.GetElementInt(0, exm);
						var idx2 = var.GetElementInt(1, exm);
						var idx3 = var.GetElementInt(2, exm);
						var array = var.Identifier.GetArray() as string[,,];
						for (int i = Math.Max(start, (int)idx3); i < end; i++)
							array[idx2, idx1, i] = val;
					}
				}
				else
				{
					long val = 0;
					if (arguments.Count > 1 && arguments[1].GetOperandType() != typeof(long))
						throw new CodeEE(string.Format(trerror.SetIntToStr.Text, name));
					if (arguments.Count > 1)
						val = arguments[1].GetIntValue(exm);
					if (var.Identifier.IsArray1D)
						var.Identifier.SetValueAll(val, start, end, 0);
					else if (var.Identifier.IsArray2D)
					{
						var array = var.Identifier.GetArray() as long[,];
						var idx1 = var.GetElementInt(0, exm);
						var idx2 = var.GetElementInt(1, exm);
						if (setAllDims)
						{
							for (int j = 0; j < array.GetLength(0); j++)
								for (int i = Math.Max(start, (int)idx2); i < end; i++)
									array[j, i] = val;
						}
						else
						{
							for (int i = Math.Max(start, (int)idx2); i < end; i++)
								array[idx1, i] = val;
						}
					}
					if (var.Identifier.IsArray3D)
					{
						var idx1 = var.GetElementInt(0, exm);
						var idx2 = var.GetElementInt(1, exm);
						var idx3 = var.GetElementInt(2, exm);
						var array = var.Identifier.GetArray() as long[,,];
						if (setAllDims)
						{
							for (int k = 0; k < array.GetLength(0); k++)
								for (int j = 0; j < array.GetLength(1); j++)
									for (int i = Math.Max(start, (int)idx3); i < end; i++)
										array[k, j, i] = val;
						}
						else
						{
							for (int i = Math.Max(start, (int)idx3); i < end; i++)
								array[idx2, idx1, i] = val;
						}
					}
				}
				return 1;
			}
			else
				throw new CodeEE(string.Format(trerror.IsNotVar.Text, name));
		}
	}
	private sealed class HtmlSubStringMethod : FunctionMethod
	{
		public HtmlSubStringMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string), typeof(long)];
			CanRestructure = false;
		}

		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			string[] strs = HtmlManager.HtmlSubString(str, (int)arguments[1].GetIntValue(exm));
			string[] output = GlobalStatic.Process.VEvaluator.RESULTS_ARRAY;
			int outputlength = Math.Min(output.Length, strs.Length);
			Array.Copy(strs, output, outputlength);
			return output[0];
		}
	}
	private sealed class HtmlStringLinesMethod : FunctionMethod
	{
		public HtmlStringLinesMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(long)];
			CanRestructure = false;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(str)) return 0;
			var ret = 0;
			do
			{
				string[] strs = HtmlManager.HtmlSubString(str, (int)arguments[1].GetIntValue(exm));
				str = strs[1];
				ret++;
			} while (!string.IsNullOrEmpty(str));
			return ret;
		}
	}
	private sealed class RegexpMatchMethod : FunctionMethod
	{
		public RegexpMatchMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.Int }, OmitStart = 2 },
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.RefInt, ArgType.RefString1D } },
				];
			CanRestructure = false;
		}

		static void Output(MatchCollection matches, Regex reg, string[] values)
		{
			var idx = 0;
			foreach (Match match in matches)
				foreach (var name in reg.GetGroupNames())
				{
					if (idx >= values.Length) return;
					values[idx] = match.Groups[name].Value;
					idx++;
				}
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string baseString = arguments[0].GetStrValue(exm);
			Regex reg;
			try
			{
				reg = RegexFactory.GetRegex(arguments[1].GetStrValue(exm));
			}
			catch (ArgumentException e)
			{
				throw new CodeEE(string.Format(trerror.InvalidRegexArg.Text, Name, 2, e.Message));
			}
			var matches = reg.Matches(baseString);
			var ret = matches.Count;
			if (arguments.Count == 3 && arguments[2].GetIntValue(exm) != 0)
			{
				exm.VEvaluator.RESULT_ARRAY[1] = reg.GetGroupNumbers().Length;
				if (ret > 0) Output(matches, reg, exm.VEvaluator.RESULTS_ARRAY);
			}
			if (arguments.Count == 4)
			{
				(arguments[2] as VariableTerm).SetValue(reg.GetGroupNumbers().Length, exm);
				if (ret > 0) Output(matches, reg, (arguments[3] as VariableTerm).Identifier.GetArray() as string[]);
			}
			return ret;
		}
	}
	private sealed class XmlDocumentMethod : FunctionMethod
	{
		public enum Operation { Create, Check, Release };
		public XmlDocumentMethod(Operation type)
		{
			op = type;
			ReturnType = typeof(long);
			if (op == Operation.Create)
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.String } },
					];
			else
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.Any } },
					];
			CanRestructure = false;
		}
		private Operation op;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
			var xmlDict = exm.VEvaluator.VariableData.DataXmlDocument;
			if (op == Operation.Create)
			{
				string xml = arguments[1].GetStrValue(exm);
				if (xmlDict.ContainsKey(idx))
				{
					return 0;
				}
				XmlDocument doc = new();
				try
				{
					doc.LoadXml(xml);
				}
				catch (XmlException e)
				{
					throw new CodeEE(string.Format(trerror.XmlGetError.Text, xml, e.Message));
				}
				xmlDict.Add(idx, doc);
			}
			else
			{
				if (xmlDict.ContainsKey(idx))
				{
					if (op == Operation.Check) return 1;
					xmlDict.Remove(idx);
				}
				else return 0;
			}
			return 1;
		}
	}
	private sealed class XmlSetMethod : FunctionMethod
	{
		public XmlSetMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 },
					new ArgTypeList{ ArgTypes = { ArgType.RefString, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 },
				];
			CanRestructure = false;
		}
		public XmlSetMethod(bool byname) : this()
		{
			byName = byname;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 },
				];
		}
		private bool byName;
		private static void SetNode(XmlNode node, string val, long style)
		{
			switch (style)
			{
				case 1: node.InnerText = val; break;
				case 2: node.InnerXml = val; break;
				default: node.Value = val; break;
			}
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			XmlDocument doc;
			bool saveToArg0 = true;
			if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
			{
				saveToArg0 = false;
				var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
				var dict = exm.VEvaluator.VariableData.DataXmlDocument;
				if (dict.ContainsKey(idx)) doc = dict[idx];
				else return -1;
			}
			else
			{
				string xml = arguments[0].GetStrValue(exm);
				doc = new XmlDocument();
				try
				{
					doc.LoadXml(xml);
				}
				catch (XmlException e)
				{
					throw new CodeEE(string.Format(trerror.XmlParseError.Text, Name, xml, e.Message));
				}
			}

			string path = arguments[1].GetStrValue(exm);
			XmlNodeList nodes = null;
			try
			{
				nodes = doc.SelectNodes(path);
			}
			catch (System.Xml.XPath.XPathException e)
			{
				throw new CodeEE(string.Format(trerror.XmlXPathParseError.Text, Name, path, e.Message));
			}
			bool setAllNodes = arguments.Count >= 4 ? arguments[3].GetIntValue(exm) != 0 : false;
			var style = arguments.Count == 5 ? arguments[4].GetIntValue(exm) : 0;
			if (style > 2 || style < 0) style = 0;
			var val = arguments[2].GetStrValue(exm);
			if (nodes.Count > 0)
			{
				if (nodes.Count != 1)
				{
					if (setAllNodes)
						for (int i = 0; i < nodes.Count; i++) SetNode(nodes[i], val, style);
				}
				else SetNode(nodes[0], val, style);
				if (saveToArg0)
				{
					(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
				}
			}
			return nodes.Count;
		}
	}
	private sealed class XmlToStrMethod : FunctionMethod
	{
		public XmlToStrMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Any } },
				];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
			var xmlDict = exm.VEvaluator.VariableData.DataXmlDocument;
			if (!xmlDict.ContainsKey(idx)) return string.Empty;
			return xmlDict[idx].OuterXml;
		}
	}
	private sealed class XmlAddNodeMethod : FunctionMethod
	{
		public enum Operation { Node, Attribute };
		public XmlAddNodeMethod(Operation op)
		{
			ReturnType = typeof(long);
			if (op == Operation.Node)
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 },
						new ArgTypeList{ ArgTypes = { ArgType.RefString, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 }
					];
			else
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 },
						new ArgTypeList{ ArgTypes = { ArgType.RefString, ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 }
					];
			CanRestructure = false;
			this.op = op;
		}
		public XmlAddNodeMethod(Operation op, bool byname) : this(op)
		{
			byName = byname;
			if (op == Operation.Node)
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 },
					];
			else
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 },
					];
		}
		private bool byName;
		Operation op;
		bool Insert(XmlNode node, XmlNode child, int method)
		{
			if (op == Operation.Node)
			{
				switch (method)
				{
					case 0: node.AppendChild(child); break;
					case 1:
						if (node.ParentNode == null) return false;
						node.ParentNode.InsertBefore(child, node);
						break;
					case 2:
						if (node.ParentNode == null) return false;
						node.ParentNode.InsertAfter(child, node);
						break;
				}
				return true;
			}
			else
			{
				if (child is XmlAttribute newAttr)
				{
					XmlAttribute attr;
					if (method > 0 && !(node is XmlAttribute)) return false;
					attr = method == 0 ? null : node as XmlAttribute;
					switch (method)
					{
						case 0: node.Attributes.Append(newAttr); break;
						case 1: attr.OwnerElement.Attributes.InsertBefore(newAttr, attr); break;
						case 2: attr.OwnerElement.Attributes.InsertAfter(newAttr, attr); break;
					}
					return true;
				}
			}
			return false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			XmlDocument doc;
			int methodPos = op == Operation.Node ? 4 : 5;
			int method = arguments.Count >= methodPos ? (int)arguments[methodPos - 1].GetIntValue(exm) : 0;
			if (method > 2 || method < 0) method = 0;
			bool saveToArg0 = true;
			if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
			{
				saveToArg0 = false;
				var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
				var dict = exm.VEvaluator.VariableData.DataXmlDocument;
				if (dict.ContainsKey(idx)) doc = dict[idx];
				else return -1;
			}
			else
			{
				string xml = arguments[0].GetStrValue(exm);
				doc = new XmlDocument();
				try
				{
					doc.LoadXml(xml);
				}
				catch (XmlException e)
				{
					throw new CodeEE(string.Format(trerror.XmlParseError.Text, Name, xml, e.Message));
				}
			}

			string path = arguments[1].GetStrValue(exm);
			XmlNodeList nodes;
			try
			{
				nodes = doc.SelectNodes(path);
			}
			catch (System.Xml.XPath.XPathException e)
			{
				throw new CodeEE(string.Format(trerror.XmlXPathParseError.Text, Name, path, e.Message));
			}
			if (nodes.Count > 0)
			{
				int setAllPos = op == Operation.Node ? 5 : 6;
				bool setAllNodes = arguments.Count == setAllPos ? arguments[setAllPos - 1].GetIntValue(exm) != 0 : false;
				XmlNode child;
				if (op == Operation.Node)
				{
					var childNode = new XmlDocument();
					var xml = arguments[2].GetStrValue(exm);
					try
					{
						childNode.LoadXml(xml);
					}
					catch (XmlException e)
					{
						throw new CodeEE(string.Format(trerror.XmlParseError.Text, Name, xml, e.Message));
					}
					var newNode = childNode.DocumentElement;
					child = doc.CreateNode(newNode.NodeType, newNode.Name, newNode.NamespaceURI);
					for (int i = 0; i < newNode.Attributes.Count; i++)
					{
						var xattr = newNode.Attributes[i];
						var attr = doc.CreateAttribute(xattr.Name);
						attr.Value = xattr.Value;
						child.Attributes.Append(attr);
					}
					child.InnerXml = newNode.InnerXml;
				}
				else
				{
					child = doc.CreateAttribute(arguments[2].GetStrValue(exm));
					if (arguments.Count >= 4) child.Value = arguments[3].GetStrValue(exm);
				}
				if (nodes.Count != 1)
				{
					if (setAllNodes)
						for (int i = 0; i < nodes.Count; i++) Insert(nodes[i], child, method);
				}
				else if (!Insert(nodes[0], child, method) && method > 0) return 0;
				if (saveToArg0)
				{
					(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
				}
			}
			return nodes.Count;
		}
	}
	private sealed class XmlRemoveNodeMethod : FunctionMethod
	{
		public enum Operation { Node, Attribute };
		public XmlRemoveNodeMethod(Operation op)
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.Int }, OmitStart = 2 },
						new ArgTypeList{ ArgTypes = { ArgType.RefString, ArgType.String, ArgType.Int }, OmitStart = 2 }
					];
			CanRestructure = false;
			this.op = op;
		}
		public XmlRemoveNodeMethod(Operation op, bool byname) : this(op)
		{
			byName = byname;
			argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.Int }, OmitStart = 2 },
					];
		}
		private bool byName;
		Operation op;
		bool Remove(XmlNode node)
		{
			if (op == Operation.Attribute)
			{
				if (node is XmlAttribute attr)
				{
					attr.OwnerElement.Attributes.Remove(attr);
					return true;
				}
			}
			else
			{
				if (node.ParentNode != null)
				{
					var parent = node.ParentNode;
					node.ParentNode.RemoveChild(node);
					return true;
				}
			}
			return false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			XmlDocument doc;
			int method = arguments.Count >= 4 ? (int)arguments[3].GetIntValue(exm) : 0;
			if (method > 2 || method < 0) method = 0;
			bool saveToArg0 = true;
			if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)))
			{
				saveToArg0 = false;
				var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
				var dict = exm.VEvaluator.VariableData.DataXmlDocument;
				if (dict.ContainsKey(idx)) doc = dict[idx];
				else return -1;
			}
			else
			{
				string xml = arguments[0].GetStrValue(exm);
				doc = new XmlDocument();
				try
				{
					doc.LoadXml(xml);
				}
				catch (XmlException e)
				{
					throw new CodeEE(string.Format(trerror.XmlParseError.Text, Name, xml, e.Message));
				}
			}

			string path = arguments[1].GetStrValue(exm);
			XmlNodeList nodes;
			try
			{
				nodes = doc.SelectNodes(path);
			}
			catch (System.Xml.XPath.XPathException e)
			{
				throw new CodeEE(string.Format(trerror.XmlXPathParseError.Text, Name, path, e.Message));
			}
			if (nodes.Count > 0)
			{
				bool setAllNodes = arguments.Count == 3 ? arguments[2].GetIntValue(exm) != 0 : false;
				if (nodes.Count != 1)
				{
					if (setAllNodes)
						for (int i = 0; i < nodes.Count; i++) Remove(nodes[i]);
				}
				else if (!Remove(nodes[0])) return 0;
				if (saveToArg0)
				{
					(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
				}
			}
			return nodes.Count;
		}
	}
	private sealed class XmlReplaceMethod : FunctionMethod
	{
		public XmlReplaceMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.String } },
						new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.String, ArgType.Int }, OmitStart = 3 },
						new ArgTypeList{ ArgTypes = { ArgType.RefString, ArgType.String, ArgType.String, ArgType.Int }, OmitStart = 3 },
					];
			CanRestructure = false;
		}
		public XmlReplaceMethod(bool byname) : this()
		{
			byName = byname;
			argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.String, ArgType.Int }, OmitStart = 3 },
					];
		}
		private bool byName;

		static bool Replace(XmlNode node, XmlNode newNode)
		{
			if (node.ParentNode != null)
			{
				node.ParentNode.ReplaceChild(newNode, node);
				return true;
			}
			return false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			XmlDocument newXml = new();
			{
				string xml = arguments.Count > 2 ? arguments[2].GetStrValue(exm) : arguments[1].GetStrValue(exm);
				try
				{
					newXml.LoadXml(xml);
				}
				catch (XmlException e)
				{
					throw new CodeEE(string.Format(trerror.XmlParseError.Text, Name, xml, e.Message));
				}
			}
			bool saveToArg0 = true;
			XmlDocument doc = null;
			if (arguments[0].GetOperandType() == typeof(long) || (byName && arguments[0].GetOperandType() == typeof(string)) || (arguments[0].GetOperandType() == typeof(string) && arguments.Count == 2))
			{
				saveToArg0 = false;
				var idx = arguments[0].GetOperandType() == typeof(string) ? arguments[0].GetStrValue(exm) : arguments[0].GetIntValue(exm).ToString();
				var dict = exm.VEvaluator.VariableData.DataXmlDocument;
				if (!dict.ContainsKey(idx)) return -1;
				if (arguments.Count == 2)
				{
					dict[idx] = newXml;
					return 1;
				}
				doc = dict[idx];
			}
			else
			{
				string xml = arguments[0].GetStrValue(exm);
				doc = new XmlDocument();
				try
				{
					doc.LoadXml(xml);
				}
				catch (XmlException e)
				{
					throw new CodeEE(string.Format(trerror.XmlParseError.Text, Name, xml, e.Message));
				}
			}
			string path = arguments[1].GetStrValue(exm);
			XmlNodeList nodes;
			try
			{
				nodes = doc.SelectNodes(path);
			}
			catch (System.Xml.XPath.XPathException e)
			{
				throw new CodeEE(string.Format(trerror.XmlXPathParseError.Text, Name, path, e.Message));
			}
			if (nodes.Count > 0)
			{
				var newNode = newXml.DocumentElement;
				var child = doc.CreateNode(newNode.NodeType, newNode.Name, newNode.NamespaceURI);
				for (int i = 0; i < newNode.Attributes.Count; i++)
				{
					var xattr = newNode.Attributes[i];
					var attr = doc.CreateAttribute(xattr.Name);
					attr.Value = xattr.Value;
					child.Attributes.Append(attr);
				}
				child.InnerXml = newNode.InnerXml;
				bool setAllNodes = arguments.Count >= 4 ? arguments[3].GetIntValue(exm) != 0 : false;
				if (nodes.Count != 1)
				{
					if (setAllNodes)
						for (int i = 0; i < nodes.Count; i++) Replace(nodes[i], child);
				}
				else if (!Replace(nodes[0], child)) return 0;
				if (saveToArg0)
				{
					(arguments[0] as VariableTerm).SetValue(doc.OuterXml, exm);
				}
			}
			return nodes.Count;
		}
	}
	private sealed class ExistFileMethod : FunctionMethod
	{
		public ExistFileMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var filepath = Utils.GetValidPath(arguments[0].GetStrValue(exm));
			if (filepath != null && File.Exists(filepath)) return 1;
			return 0;
		}
	}
	private sealed class DataTableManagementMethod : FunctionMethod
	{
		public enum Operation { Create, Check, Release, Clear, Case };
		public DataTableManagementMethod(Operation type)
		{
			ReturnType = typeof(long);
			if (type == Operation.Case)
				argumentTypeArray = [typeof(string), typeof(long)];
			else
				argumentTypeArray = [typeof(string)];
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			bool contains = dict.ContainsKey(key);
			switch (op)
			{
				case Operation.Clear:
					{
						if (contains)
						{
							dict[key].Clear();
							return 1;
						}
						return -1;
					}
				case Operation.Case:
					{
						if (contains)
						{
							dict[key].CaseSensitive = arguments[1].GetIntValue(exm) == 0;
							return 1;
						}
						return -1;
					}
				case Operation.Check: { return contains ? 1 : 0; }
				case Operation.Release: { if (contains) dict.Remove(key); return 1; }
			}
			if (contains) return 0;
			var dt = new DataTable(key)
			{
				CaseSensitive = true
			};
			var c = dt.Columns.Add("id", typeof(long));
			c.AllowDBNull = false;
			c.Unique = true;
			dict[key] = dt;
			dt.PrimaryKey = [c];
			return 1;
		}
	}
	private sealed class DataTableColumnManagementMethod : FunctionMethod
	{
		public enum Operation { Create, Check, Remove, Names };
		public DataTableColumnManagementMethod(Operation type)
		{
			ReturnType = typeof(long);
			if (type == Operation.Create)
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.Any, ArgType.Int }, OmitStart = 2 },
					];
			else if (type == Operation.Names)
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.RefString1D }, OmitStart = 1 },
					];
			else
				argumentTypeArray = [typeof(string), typeof(string)];
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return -1;
			var dt = dict[key];
			if (op == Operation.Names)
			{
				string[] output;
				if (arguments.Count > 1 && arguments[1] is VariableTerm v) output = v.Identifier.GetArray() as string[];
				else output = exm.VEvaluator.RESULTS_ARRAY;
				for (int i = 0; i < dt.Columns.Count; i++) output[i] = dt.Columns[i].ColumnName;
				return dt.Columns.Count;
			}
			string cName = arguments[1].GetStrValue(exm);
			bool contains = dt.Columns.Contains(cName);
			switch (op)
			{
				case Operation.Check: { return contains ? Utils.DataTable.TypeToInt(dt.Columns[cName].DataType) : 0; }
				case Operation.Remove:
					{
						if (contains && cName.ToLower() != "id")
						{
							dt.Columns.Remove(cName);
							return 1;
						}
						return 0;
					}
			}
			if (contains) return 0;
			Type t = null;
			if (arguments.Count >= 3)
			{
				if (arguments[2].GetOperandType() == typeof(string)) t = Utils.DataTable.NameToType(arguments[2].GetStrValue(exm));
				else t = Utils.DataTable.IntToType(arguments[2].GetIntValue(exm));
				if (t == null)
				{
					throw new CodeEE(string.Format(trerror.UnsupportedType.Text, Name));
				}
			}
			bool nullable = arguments.Count == 4 ? arguments[3].GetIntValue(exm) != 0 : true;
			DataColumn dc;
			if (t != null) dc = dt.Columns.Add(cName, t);
			else dc = dt.Columns.Add(cName);
			dc.AllowDBNull = nullable;
			return 1;
		}
	}
	private sealed class DataTableRowSetMethod : FunctionMethod
	{
		public enum Operation { Add, Set };
		public DataTableRowSetMethod(Operation type)
		{
			ReturnType = typeof(long);
			if (type == Operation.Add)
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.VariadicString, ArgType.VariadicAny }, MatchVariadicGroup = true, OmitStart = 1 },
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.RefString1D, ArgType.RefAny1D, ArgType.Int } },
					];
			else
				argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.VariadicString, ArgType.VariadicAny }, MatchVariadicGroup = true },
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.RefString1D, ArgType.RefAny1D, ArgType.Int } },
					];
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		void CheckName(DataTable dt, string name, string key)
		{
			if (name == "id")
				throw new CodeEE(string.Format(trerror.DTCanNotEditIdColumn.Text, Name, key));
			if (!dt.Columns.Contains(name))
				throw new CodeEE(string.Format(trerror.DTLackOfNamedColumn.Text, Name, key, name));
		}
		void SetValue(DataRow row, DataTable dt, string name, string key, ExpressionMediator exm, AExpression v)
		{
			CheckName(dt, name, key);
			if (v == null)
			{
				row[name] = DBNull.Value;
				return;
			}
			bool isString = dt.Columns[name].DataType == typeof(string);
			if (v.GetOperandType() != (isString ? typeof(string) : typeof(long)))
				throw new CodeEE(string.Format(trerror.DTInvalidDataType.Text, Name, key, name));

			if (isString)
				row[name] = v.GetStrValue(exm);
			else
				row[name] = Utils.DataTable.ConvertInt(v.GetIntValue(exm), dt.Columns[name].DataType);
		}
		void SetValue(DataRow row, DataTable dt, string name, string key, string str)
		{
			CheckName(dt, name, key);
			if (dt.Columns[name].DataType != typeof(string))
				throw new CodeEE(string.Format(trerror.DTInvalidDataType.Text, Name, key, name));
			row[name] = str;
		}
		void SetValue(DataRow row, DataTable dt, string name, string key, long v)
		{
			CheckName(dt, name, key);
			if (dt.Columns[name].DataType == typeof(string))
				throw new CodeEE(string.Format(trerror.DTInvalidDataType.Text, Name, key, name));
			row[name] = Utils.DataTable.ConvertInt(v, dt.Columns[name].DataType);
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var b = op == Operation.Add ? 0 : 1;
			string key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return -1;
			var dt = dict[key];
			var cCount = 0L;
			DataRow row;
			if (op == Operation.Set)
			{
				var idx = arguments[1].GetIntValue(exm);
				if (dt.Rows.Find(idx) is DataRow r)
					row = r;
				else return -2;
			}
			else
			{
				row = dt.NewRow();
				row[0] = Utils.TimePoint();
			}
			if (arguments.Count == b + 4)
			{
				var names = (arguments[b + 1] as VariableTerm).Identifier.GetArray() as string[];
				var count = Math.Min(names.Length, arguments[b + 3].GetIntValue(exm));
				if (arguments[b + 2].GetOperandType() == typeof(string))
				{
					var vals = (arguments[b + 2] as VariableTerm).Identifier.GetArray() as string[];
					count = Math.Min(vals.Length, count);
					for (int i = 0; i < count; i++)
						SetValue(row, dt, names[i], key, vals[i]);
					cCount += count;
				}
				else
				{
					var vals = (arguments[b + 2] as VariableTerm).Identifier.GetArray() as long[];
					count = Math.Min(vals.Length, count);
					for (int i = 0; i < count; i++)
						SetValue(row, dt, names[i], key, vals[i]);
					cCount += count;
				}
			}
			else
			{
				var pos = b + 1;
				while (pos < arguments.Count)
				{
					var name = arguments[pos].GetStrValue(exm);
					SetValue(row, dt, name, key, exm, arguments[pos + 1]);
					pos += 2;
					cCount++;
				}
			}
			if (op == Operation.Add)
			{
				dt.Rows.Add(row);
				return (long)row[0];
			}
			return cCount;
		}
	}
	private sealed class DataTableLengthMethod : FunctionMethod
	{
		public enum Operation { Row, Column };
		public DataTableLengthMethod(Operation type)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return -1;
			return op == Operation.Row ? dict[key].Rows.Count : dict[key].Columns.Count;
		}
	}
	private sealed class DataTableRowRemoveMethod : FunctionMethod
	{
		public DataTableRowRemoveMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int } },
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.RefInt1D, ArgType.Int } },
					];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return -1;
			var dt = dict[key];
			DataRow[] rows;
			if (arguments.Count == 3)
			{
				StringBuilder sb = new();
				var array = (arguments[1] as VariableTerm).Identifier.GetArray() as long[];
				var count = Math.Min((int)arguments[2].GetIntValue(exm), array.Length);
				if (count <= 0) return 0;
				sb.Append('(');
				for (int i = 0; i < count; i++)
					sb.Append(i == 0 ? array[i].ToString() : "," + array[i]);
				sb.Append(')');
				rows = dt.Select("id IN " + sb.ToString());
				if (rows == null) return 0;
			}
			else if (dt.Rows.Find(arguments[1].GetIntValue(exm)) is DataRow row)
				rows = [row];
			else return 0;
			foreach (var row in rows) dt.Rows.Remove(row);
			return rows.Length;
		}
	}
	private sealed class DataTableCellGetMethod : FunctionMethod
	{
		public enum Operation { Get, IsNull, Gets };
		public DataTableCellGetMethod(Operation type)
		{
			ReturnType = type == Operation.Gets ? typeof(string) : typeof(long);
			argumentTypeArrayEx = [
						new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.String, ArgType.Int }, OmitStart = 3 },
					];
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return op == Operation.IsNull ? -1 : 0;
			bool asId = arguments.Count == 4 ? arguments[3].GetIntValue(exm) != 0 : false;
			var dt = dict[key];
			var idx = arguments[1].GetIntValue(exm);
			var name = arguments[2].GetStrValue(exm);
			if (asId)
			{
				if (dt.Rows.Find(idx) is DataRow row && dt.Columns.Contains(name))
				{
					var v = row[name];
					return op == Operation.Get ? v == DBNull.Value ? 0 : Convert.ToInt64(v) : (v == DBNull.Value ? 1 : 0);
				}
			}
			else
			{
				if (0 <= idx && idx < dt.Rows.Count && dt.Columns.Contains(name))
				{
					var v = dt.Rows[(int)idx][name];
					return op == Operation.Get ? v == DBNull.Value ? 0 : Convert.ToInt64(v) : (v == DBNull.Value ? 1 : 0);
				}
			}
			return op == Operation.IsNull ? -2 : 0;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return string.Empty;
			bool asId = arguments.Count == 4 ? arguments[3].GetIntValue(exm) != 0 : false;
			var dt = dict[key];
			var idx = arguments[1].GetIntValue(exm);
			var name = arguments[2].GetStrValue(exm);
			if (asId)
			{
				if (dt.Rows.Find(idx) is DataRow row && dt.Columns.Contains(name))
				{
					var v = row[name];
					if (v != DBNull.Value) return (string)v;
				}
			}
			else
			{
				if (0 <= idx && idx < dt.Rows.Count && dt.Columns.Contains(name))
				{
					var v = dt.Rows[(int)idx][name];
					if (v != DBNull.Value) return v.ToString();
				}
			}
			return string.Empty;
		}
	}
	private sealed class DataTableCellSetMethod : FunctionMethod
	{
		public DataTableCellSetMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.String, ArgType.Any, ArgType.Int }, OmitStart = 3 },
				];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return -1;
			bool asId = arguments.Count == 5 ? arguments[4].GetIntValue(exm) != 0 : false;
			var dt = dict[key];
			var idx = arguments[1].GetIntValue(exm);
			var name = arguments[2].GetStrValue(exm);
			if (name.ToLower() == "id") return 0;
			var v = arguments.Count > 3 ? arguments[3] : null;
			DataRow row = null;
			if (asId) row = dt.Rows.Find(idx);
			else if (idx >= 0 && idx < dt.Rows.Count) row = dt.Rows[(int)idx];
			if (row != null && dt.Columns.Contains(name))
			{
				if (v == null) row[name] = DBNull.Value;
				else
				{
					bool isString = dt.Columns[name].DataType == typeof(string);
					if (v.GetOperandType() != (isString ? typeof(string) : typeof(long))) return -2;

					if (isString)
						row[name] = v.GetStrValue(exm);
					else
						row[name] = Utils.DataTable.ConvertInt(v.GetIntValue(exm), dt.Columns[name].DataType);
				}
				return 1;
			}
			return -3;
		}
	}
	private sealed class DataTableSelectMethod : FunctionMethod
	{
		public DataTableSelectMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.String, ArgType.RefInt1D }, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return -1;
			var dt = dict[key];
			string filter = arguments.Count > 1 ? (arguments[1] != null ? arguments[1].GetStrValue(exm) : null) : null;
			string sort = arguments.Count > 2 ? (arguments[2] != null ? arguments[2].GetStrValue(exm) : null) : null;
			DataRow[] res;
			if (sort != null) res = dt.Select(filter, sort);
			else if (filter != null) res = dt.Select(filter);
			else res = dt.Select();
			bool toResult = arguments.Count != 4;
			long[] output = toResult ? GlobalStatic.VEvaluator.RESULT_ARRAY : (arguments[3] as VariableTerm).Identifier.GetArray() as long[];
			if (res != null)
			{
				int count = Math.Min(res.Length, toResult ? output.Length - 1 : output.Length);
				for (int i = 0; i < count; i++)
					output[toResult ? i + 1 : i] = (long)res[i][0];
				if (toResult) output[0] = res.Length;
				return res.Length;
			}
			if (toResult) output[0] = 0;
			return 0;
		}
	}
	private sealed class DataTableToXmlMethod : FunctionMethod
	{
		public DataTableToXmlMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.RefString }, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			if (!dict.ContainsKey(key)) return string.Empty;
			var dt = dict[key];
			var output = arguments.Count > 1 ? (arguments[1] as VariableTerm).Identifier.GetArray() as string[] : GlobalStatic.VEvaluator.RESULTS_ARRAY;
			var idx = arguments.Count > 1 ? 0 : 1;

			var sb = new StringBuilder();
			using (var sw = new StringWriter(sb))
			{
				dt.WriteXmlSchema(sw);
				output[idx] = sb.ToString();
				sb.Clear();
				dt.WriteXml(sw);
				return sb.ToString();
			}
		}
	}
	private sealed class DataTableFromXmlMethod : FunctionMethod
	{
		public DataTableFromXmlMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(string), typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataDataTables;
			DataTable dt;
			try
			{
				dt = new DataTable(key);
				using (var reader = new StringReader(arguments[1].GetStrValue(exm)))
				{
					dt.ReadXmlSchema(reader);
				}
				using (var reader = new StringReader(arguments[2].GetStrValue(exm)))
				{
					dt.ReadXml(reader);
				}
			}
			catch
			{
				return 0;
			}
			if (dict.ContainsKey(key)) dict[key] = dt;
			else dict.Add(key, dt);
			return 1;
		}
	}

	private sealed class MapManagementMethod : FunctionMethod
	{
		public enum Operation { Create, Check, Release };
		public MapManagementMethod(Operation type)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string key = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataStringMaps;
			bool contains = dict.ContainsKey(key);
			switch (op)
			{
				case Operation.Check: { return contains ? 1 : 0; }
				case Operation.Release: { if (contains) dict.Remove(key); return 1; }
			}
			if (contains) return 0;
			dict[key] = [];
			return 1;
		}
	}
	private sealed class MapDataOperationMethod : FunctionMethod
	{
		public enum Operation { Set, Has, Remove, Clear, Size };
		public MapDataOperationMethod(Operation type)
		{
			ReturnType = typeof(long);
			switch (type)
			{
				case Operation.Set:
					argumentTypeArray = [typeof(string), typeof(string), typeof(string)]; break;
				case Operation.Has:
				case Operation.Remove:
					argumentTypeArray = [typeof(string), typeof(string)]; break;
				default:
					argumentTypeArray = [typeof(string)]; break;
			}
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var map = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataStringMaps;
			if (!dict.ContainsKey(map)) return -1;
			var sMap = dict[map];
			if (op == Operation.Clear) sMap.Clear();
			else if (op == Operation.Size) return sMap.Count;
			else
			{
				var key = arguments[1].GetStrValue(exm);
				bool contains = sMap.ContainsKey(key);
				if (op == Operation.Has) return contains ? 1 : 0;
				if (op == Operation.Remove)
					sMap.Remove(key);
				else
					sMap[key] = arguments[2].GetStrValue(exm);
			}
			return 1;
		}
	}
	private sealed class MapGetStrMethod : FunctionMethod
	{
		public enum Operation { Get, ToXml, GetKeys };
		public MapGetStrMethod(Operation type)
		{
			ReturnType = typeof(string);
			switch (type)
			{
				case Operation.Get:
					argumentTypeArray = [typeof(string), typeof(string)]; break;
				case Operation.ToXml:
					argumentTypeArray = [typeof(string)]; break;
				case Operation.GetKeys:
					argumentTypeArrayEx = [
							new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int }, OmitStart = 1 },
							new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.RefString1D, ArgType.Int } },
						]; break;
			}
			CanRestructure = false;
			op = type;
		}
		private Operation op;
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var dict = exm.VEvaluator.VariableData.DataStringMaps;
			var map = arguments[0].GetStrValue(exm);
			if (!dict.ContainsKey(map)) return "";
			var sMap = dict[map];
			if (op == Operation.Get)
			{
				var key = arguments[1].GetStrValue(exm);
				if (sMap.ContainsKey(key)) return sMap[key];
				return "";
			}
			else if (op == Operation.GetKeys && arguments.Count > 1)
			{
				int count = 0;
				string[] array;
				if (arguments.Count == 3) // to array
				{
					var Term = arguments[1] as VariableTerm;
					if (arguments[2].GetIntValue(exm) == 0) return "";
					array = Term.Identifier.GetArray() as string[];
				}
				else if (arguments.Count == 2) // to RESULTS array
				{
					if (arguments[1].GetIntValue(exm) == 0) return "";
					array = exm.VEvaluator.RESULTS_ARRAY;
				}
				else return "";
				foreach (var k in sMap.Keys)
				{
					if (count >= array.Length) break;
					array[count] = k;
					count++;
				}
				exm.VEvaluator.RESULT = sMap.Keys.Count;
				return arguments.Count == 2 ? exm.VEvaluator.RESULTS : "";
			}
			StringBuilder sb = new();
			if (op == Operation.GetKeys)
			{
				bool isNotEmpty = false;
				foreach (var k in sMap.Keys)
				{
					if (isNotEmpty) sb.Append(",").Append(k);
					else
					{
						isNotEmpty = true;
						sb.Append(k);
					}
				}
			}
			else
			{
				sb.Append("<map>");
				foreach (var p in sMap)
					sb.Append(string.Format("<p><k>{0}</k><v>{1}</v></p>", p.Key, p.Value));
				sb.Append("</map>");
			}
			return sb.ToString();
		}
	}
	private sealed class MapFromXmlMethod : FunctionMethod
	{
		public MapFromXmlMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			var map = arguments[0].GetStrValue(exm);
			var dict = exm.VEvaluator.VariableData.DataStringMaps;
			if (!dict.ContainsKey(map)) return 0;
			var xml = arguments[1].GetStrValue(exm);
			var sMap = dict[map];
			XmlDocument doc = new();
			XmlNodeList nodes;
			try
			{
				doc.LoadXml(xml);
				nodes = doc.SelectNodes("/map/p");
			}
			catch (XmlException e)
			{
				throw new CodeEE(string.Format(trerror.XmlParseError.Text, Name, xml, e.Message));
			}
			for (int i = 0; i < nodes.Count; i++)
			{
				XmlNodeList key, val;
				var node = nodes[i];
				key = node.SelectNodes("./k");
				val = node.SelectNodes("./v");
				if (key.Count != 1 || val.Count != 1) continue;
				sMap[key[0].InnerText] = val[0].InnerXml;
			}
			return 1;
		}
	}

	private sealed class MoveTextBoxMethod : FunctionMethod
	{
		public MoveTextBoxMethod(bool b = false)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
			resume = b;
		}
		bool resume;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (resume) exm.Console.Window.ResetTextBoxPos();
			else exm.Console.Window.SetTextBoxPos(
				(int)arguments[0].GetIntValue(exm),
				(int)arguments[1].GetIntValue(exm),
				(int)arguments[2].GetIntValue(exm));
			return 1;
		}
	}
	#endregion

	#region CSVデータ関係
	private sealed class GetcharaMethod : FunctionMethod
	{
		public GetcharaMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			CanRestructure = false;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常２つ、１つ省略可能で１～２の引数が必要。
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";

		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (arguments[0].GetOperandType() != typeof(Int64))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	//2は省略可能
		//	if ((arguments.Count == 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long integer = arguments[0].GetIntValue(exm);
			if (!Config.CompatiSPChara)
			{
				//if ((arguments.Count > 1) && (arguments[1] != null) && (arguments[1].GetIntValue(exm) != 0))
				return exm.VEvaluator.GetChara(integer);
			}
			//以下互換性用の旧処理
			bool CheckSp = false;
			if ((arguments.Count > 1) && (arguments[1] != null) && (arguments[1].GetIntValue(exm) != 0))
				CheckSp = true;
			if (CheckSp)
			{
				long chara = exm.VEvaluator.GetChara_UseSp(integer, false);
				if (chara != -1)
					return chara;
				else
					return exm.VEvaluator.GetChara_UseSp(integer, true);
			}
			else
				return exm.VEvaluator.GetChara_UseSp(integer, false);
		}
	}

	private sealed class GetspcharaMethod : FunctionMethod
	{
		public GetspcharaMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (!Config.CompatiSPChara)
				// throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
				throw new CodeEE(trerror.SPCharacterFeatureDisabled.Text);
			long integer = arguments[0].GetIntValue(exm);
			return exm.VEvaluator.GetChara_UseSp(integer, true);
		}
	}

	private sealed class CsvStrDataMethod : FunctionMethod
	{
		readonly CharacterStrData charaStr;
		public CsvStrDataMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = null;
			charaStr = CharacterStrData.NAME;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			CanRestructure = true;
		}
		public CsvStrDataMethod(CharacterStrData cStr)
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			charaStr = cStr;
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!arguments[0].IsInteger)
		//		return name + "関数の1番目の引数が数値ではありません";
		//	if (arguments.Count == 1)
		//		return null;
		//	if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の変数が数値ではありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long x = arguments[0].GetIntValue(exm);
			long y = (arguments.Count > 1 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
			if (!Config.CompatiSPChara && y != 0)
				// throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
				throw new CodeEE(trerror.SPCharacterFeatureDisabled.Text);
			return exm.VEvaluator.GetCharacterStrfromCSVData(x, charaStr, y != 0, 0);
		}
	}

	private sealed class CsvcstrMethod : FunctionMethod
	{
		public CsvcstrMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments.Count > 3)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!arguments[0].IsInteger)
		//		return name + "関数の1番目の引数が数値ではありません";
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != typeof(Int64))
		//		return name + "関数の2番目の変数が数値ではありません";
		//	if (arguments.Count == 2)
		//		return null;
		//	if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の変数が数値ではありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long x = arguments[0].GetIntValue(exm);
			long y = arguments[1].GetIntValue(exm);
			long z = (arguments.Count == 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
			if (!Config.CompatiSPChara && z != 0)
				// throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
				throw new CodeEE(trerror.SPCharacterFeatureDisabled.Text);
			return exm.VEvaluator.GetCharacterStrfromCSVData(x, CharacterStrData.CSTR, z != 0, y);
		}
	}

	private sealed class CsvDataMethod : FunctionMethod
	{
		readonly CharacterIntData charaInt;
		public CsvDataMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			charaInt = CharacterIntData.BASE;
			CanRestructure = true;
		}
		public CsvDataMethod(CharacterIntData cInt)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			charaInt = cInt;
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments.Count > 3)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!arguments[0].IsInteger)
		//		return name + "関数の1番目の引数が数値ではありません";
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != typeof(Int64))
		//		return name + "関数の2番目の変数が数値ではありません";
		//	if (arguments.Count == 2)
		//		return null;
		//	if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の変数が数値ではありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long x = arguments[0].GetIntValue(exm);
			long y = arguments[1].GetIntValue(exm);
			long z = (arguments.Count == 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
			if (!Config.CompatiSPChara && z != 0)
				// throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
				throw new CodeEE(trerror.SPCharacterFeatureDisabled.Text);
			return exm.VEvaluator.GetCharacterIntfromCSVData(x, charaInt, z != 0, y);
		}
	}

	private sealed class FindcharaMethod : FunctionMethod
	{
		public FindcharaMethod(bool last)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.CharacterData | ArgType.Any, ArgType.SameAsFirst, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			CanRestructure = false;
			isLast = last;
		}

		readonly bool isLast;
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常3つ、1つ省略可能で2～3の引数が必要。
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments.Count > 4)
		//		return name + "関数の引数が多すぎます";

		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	if (!(((VariableTerm)arguments[0]).Identifier.IsCharacterData))
		//		return name + "関数の1番目の引数の変数がキャラクタ変数ではありません";
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != arguments[0].GetOperandType())
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	//3番目は省略可能
		//	if ((arguments.Count >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の引数の型が正しくありません";
		//	//4番目は省略可能
		//	if ((arguments.Count >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
		//		return name + "関数の4番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm vTerm = (VariableTerm)arguments[0];
			VariableToken varID = vTerm.Identifier;

			long elem = 0;
			if (vTerm.Identifier.IsArray1D)
				elem = vTerm.GetElementInt(1, exm);
			else if (vTerm.Identifier.IsArray2D)
			{
				elem = vTerm.GetElementInt(1, exm) << 32;
				elem += vTerm.GetElementInt(2, exm);
			}
			long startindex = 0;
			long lastindex = exm.VEvaluator.CHARANUM;
			if (arguments.Count >= 3 && arguments[2] != null)
				startindex = arguments[2].GetIntValue(exm);
			if (arguments.Count >= 4 && arguments[3] != null)
				lastindex = arguments[3].GetIntValue(exm);
			if (startindex < 0 || startindex >= exm.VEvaluator.CHARANUM)
				// throw new CodeEE((isLast ? "" : "") + "関数の第3引数(" + startindex.ToString() + ")はキャラクタ位置の範囲外です");
				throw new CodeEE(string.Format(trerror.CharacterIndexOutOfRange.Text, Name, 3, startindex));
			if (lastindex < 0 || lastindex > exm.VEvaluator.CHARANUM)
				// throw new CodeEE((isLast ? "" : "") + "関数の第4引数(" + lastindex.ToString() + ")はキャラクタ位置の範囲外です");
				throw new CodeEE(string.Format(trerror.CharacterIndexOutOfRange.Text, Name, 4, lastindex));
			long ret;
			if (varID.IsString)
			{
				string word = arguments[1].GetStrValue(exm);
				ret = VariableEvaluator.FindChara(varID, elem, word, startindex, lastindex, isLast);
			}
			else
			{
				long word = arguments[1].GetIntValue(exm);
				ret = VariableEvaluator.FindChara(varID, elem, word, startindex, lastindex, isLast);
			}
			return ret;
		}
	}

	private sealed class ExistCsvMethod : FunctionMethod
	{
		public ExistCsvMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!arguments[0].IsInteger)
		//		return name + "関数の1番目の引数が数値ではありません";
		//	if (arguments.Count == 1)
		//		return null;
		//	if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の変数が数値ではありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long no = arguments[0].GetIntValue(exm);
			bool isSp = (arguments.Count == 2 && arguments[1] != null) ? (arguments[1].GetIntValue(exm) != 0) : false;
			if (!Config.CompatiSPChara && isSp)
				// throw new CodeEE("SPキャラ関係の機能は標準では使用できません(互換性オプション「SPキャラを使用する」をONにしてください)");
				throw new CodeEE(trerror.SPCharacterFeatureDisabled.Text);

			return exm.VEvaluator.ExistCsv(no, isSp);
		}
	}
	#endregion

	#region 汎用処理系
	private sealed class VarsizeMethod : FunctionMethod
	{
		public VarsizeMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int }, OmitStart = 1 },
				];
			CanRestructure = true;
			//1808beta009 参照型変数の追加によりちょっと面倒になった
			HasUniqueRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!arguments[0].IsString)
		//		return name + "関数の1番目の引数が文字列ではありません";
		//	if (arguments[0] is SingleTerm)
		//	{
		//		string varName = ((SingleTerm)arguments[0]).Text;
		//		if (GlobalStatic.IdentifierDictionary.GetVariableToken(varName, null, true) == null)
		//			return name + "関数の1番目の引数が変数名ではありません";
		//	}
		//	if (arguments.Count == 1)
		//		return null;
		//	if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の変数が数値ではありません";
		//	if (arguments.Count == 2)
		//		return null;
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableToken var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
			if (var == null)
				// throw new CodeEE("VARSIZEの1番目の引数(\"" + arguments[0].GetStrValue(exm) + "\")が変数名ではありません");
				throw new CodeEE(string.Format(trerror.NotVariableName.Text, Name, 1, arguments[0].GetStrValue(exm)));
			int dim = 0;
			if (arguments.Count == 2 && arguments[1] != null)
				dim = (int)arguments[1].GetIntValue(exm);
			if (Config.VarsizeDimConfig && dim > 0)
				dim--;
			return var.GetLength(dim);
		}
		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			arguments[0].Restructure(exm);
			if (arguments.Count > 1)
				arguments[1].Restructure(exm);
			if (arguments[0] is SingleTerm && (arguments.Count == 1 || arguments[1] is SingleTerm))
			{
				VariableToken var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
				if (var == null || var.IsReference)//可変長の場合は定数化できない
					return false;
				return true;
			}
			return false;
		}
	}

	private sealed class CheckfontMethod : FunctionMethod
	{
		public CheckfontMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;//起動中に変わることもそうそうないはず……
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			using System.Drawing.Text.InstalledFontCollection ifc = new();
			long isInstalled = 0;
			foreach (FontFamily ff in ifc.Families)
			{
				#region EE_フォントファイル対応
				if (ff.Name == str)
				{
					isInstalled = 1;
					break;
				}
			}
			foreach (FontFamily ff in GlobalStatic.Pfc.Families)
			{
				if (ff.Name == str)
				{
					isInstalled = 1;
					break;
				}
			}
			#endregion
			return (isInstalled);
		}

	}

	private sealed class CheckdataMethod : FunctionMethod
	{
		public CheckdataMethod(EraSaveFileType type)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
			this.type = type;
		}

		readonly EraSaveFileType type;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long target = arguments[0].GetIntValue(exm);
			if (target < 0)
				// throw new CodeEE(Name + "の引数に負の値(" + target.ToString() + ")が指定されました");
				throw new CodeEE(string.Format(trerror.ArgIsNegative.Text, Name, 1, target));
			else if (target > int.MaxValue)
				// throw new CodeEE(Name + "の引数(" + target.ToString() + ")が大きすぎます");
				throw new CodeEE(string.Format(trerror.ArgIsTooLarge.Text, Name, 1, target));
			EraDataResult result = exm.VEvaluator.CheckData((int)target, type);
			exm.VEvaluator.RESULTS = result.DataMes;
			exm.VEvaluator.RESULT_ARRAY[1] = result.Version;
			return (long)result.State;
		}
	}

	/// <summary>
	/// ファイル名をstringで指定する版・CHKVARDATAとCHKCHARADATAはこっちに分類
	/// </summary>
	private sealed class CheckdataStrMethod : FunctionMethod
	{
		public CheckdataStrMethod(EraSaveFileType type)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
			this.type = type;
		}

		readonly EraSaveFileType type;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string datFilename = arguments[0].GetStrValue(exm);
			EraDataResult result = exm.VEvaluator.CheckData(datFilename, type);
			exm.VEvaluator.RESULTS = result.DataMes;
			return (long)result.State;
		}
	}

	/// <summary>
	/// ファイル探索関数
	/// </summary>
	private sealed class FindFilesMethod : FunctionMethod
	{
		public FindFilesMethod(EraSaveFileType type)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String }, OmitStart = 0 },
				];
			CanRestructure = false;
			this.type = type;
		}

		readonly EraSaveFileType type;

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count > 1)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments.Count == 0 || arguments[0] == null)
		//		return null;
		//	if (!arguments[0].IsString)
		//		return name + "関数の1番目の引数が文字列ではありません";
		//	return null;
		//}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string pattern = "*";
			if (arguments.Count > 0 && arguments[0] != null)
				pattern = arguments[0].GetStrValue(exm);
			List<string> filepathes = VariableEvaluator.GetDatFiles(type == EraSaveFileType.CharVar, pattern);
			string[] results = exm.VEvaluator.VariableData.DataStringArray[(int)(VariableCode.RESULTS & VariableCode.__LOWERCASE__)];
			if (filepathes.Count <= results.Length)
				filepathes.CopyTo(results);
			else
				filepathes.CopyTo(0, results, 0, results.Length);
			return filepathes.Count;
		}
	}


	private sealed class IsSkipMethod : FunctionMethod
	{
		public IsSkipMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return exm.Process.SkipPrint ? 1L : 0L;
		}
	}

	private sealed class MesSkipMethod : FunctionMethod
	{
		public MesSkipMethod(bool warn)
		{
			ReturnType = typeof(long);
			argumentTypeArray = null;
			CanRestructure = false;
			this.warn = warn;
		}

		readonly bool warn;
		public override string CheckArgumentType(string name, List<AExpression> arguments)
		{
			if (arguments.Count > 0)
				// return name + "関数の引数が多すぎます";
				return string.Format(trerror.TooManyFuncArgs.Text, name);
			if (warn)
				// ParserMediator.Warn("関数MOUSESKIP()は推奨されません。代わりに関数MESSKIP()を使用してください", GlobalStatic.Process.GetScaningLine(), 1, false, false, null);
				ParserMediator.Warn(string.Format(trerror.FuncDeprecated.Text, name, "MESSKIP"), GlobalStatic.Process.GetScaningLine(), 1, false, false, null);
			return null;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return GlobalStatic.Console.MesSkip ? 1L : 0L;
		}
	}


	private sealed class GetColorMethod : FunctionMethod
	{
		public GetColorMethod(bool isDef)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = isDef;
			defaultColor = isDef;
		}

		readonly bool defaultColor;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			Color color = defaultColor ? Config.ForeColor : GlobalStatic.Console.StringStyle.Color;
			return color.ToArgb() & 0xFFFFFF;
		}
	}

	private sealed class GetFocusColorMethod : FunctionMethod
	{
		public GetFocusColorMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return Config.FocusColor.ToArgb() & 0xFFFFFF;
		}
	}

	private sealed class GetBGColorMethod : FunctionMethod
	{
		public GetBGColorMethod(bool isDef)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = isDef;
			defaultColor = isDef;
		}

		readonly bool defaultColor;
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			Color color = defaultColor ? Config.BackColor : GlobalStatic.Console.bgColor;
			return color.ToArgb() & 0xFFFFFF;
		}
	}

	private sealed class GetStyleMethod : FunctionMethod
	{
		public GetStyleMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			FontStyle fontstyle = GlobalStatic.Console.StringStyle.FontStyle;
			long ret = 0;
			if ((fontstyle & FontStyle.Bold) == FontStyle.Bold)
				ret |= 1;
			if ((fontstyle & FontStyle.Italic) == FontStyle.Italic)
				ret |= 2;
			if ((fontstyle & FontStyle.Strikeout) == FontStyle.Strikeout)
				ret |= 4;
			if ((fontstyle & FontStyle.Underline) == FontStyle.Underline)
				ret |= 8;
			return ret;
		}
	}

	private sealed class GetFontMethod : FunctionMethod
	{
		public GetFontMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return GlobalStatic.Console.StringStyle.Fontname;
		}
	}

	private sealed class BarStringMethod : FunctionMethod
	{
		public BarStringMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long var = arguments[0].GetIntValue(exm);
			long max = arguments[1].GetIntValue(exm);
			long length = arguments[2].GetIntValue(exm);
			return ExpressionMediator.CreateBar(var, max, length);
		}
	}

	private sealed class CurrentAlignMethod : FunctionMethod
	{
		public CurrentAlignMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (exm.Console.Alignment == DisplayLineAlignment.LEFT)
				return "LEFT";
			else if (exm.Console.Alignment == DisplayLineAlignment.CENTER)
				return "CENTER";
			else
				return "RIGHT";
		}
	}

	private sealed class CurrentRedrawMethod : FunctionMethod
	{
		public CurrentRedrawMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return (exm.Console.Redraw == GameView.ConsoleRedraw.None) ? 0L : 1L;
		}
	}

	private sealed class ColorFromNameMethod : FunctionMethod
	{
		public ColorFromNameMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string colorName = arguments[0].GetStrValue(exm);
			Color color = Color.FromName(colorName);
			int i;
			if (color.A > 0)
				i = (color.R << 16) + (color.G << 8) + color.B;
			else
			{
				if (colorName.Equals("transparent", StringComparison.OrdinalIgnoreCase))
					// throw new CodeEE("無色透明(Transparent)は色として指定できません");
					throw new CodeEE(trerror.TransparentUnsupported.Text);
				//throw new CodeEE("指定された色名\"" + colorName + "\"は無効な色名です");
				i = -1;
			}
			return i;
		}
	}

	private sealed class ColorFromRGBMethod : FunctionMethod
	{
		public ColorFromRGBMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long r = arguments[0].GetIntValue(exm);
			if (r < 0 || r > 255)
				// throw new CodeEE("第１引数が0から255の範囲外です");
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, 1, r, 0, 255));
			long g = arguments[1].GetIntValue(exm);
			if (g < 0 || g > 255)
				// throw new CodeEE("第２引数が0から255の範囲外です");
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, 2, g, 0, 255));
			long b = arguments[2].GetIntValue(exm);
			if (b < 0 || b > 255)
				// throw new CodeEE("第３引数が0から255の範囲外です");
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, 3, b, 0, 255));
			return (r << 16) + (g << 8) + b;
		}
	}
	/// <summary>
	/// 1810 作ったけど保留
	/// </summary>
	// 使われてない
	//private sealed class GetRefMethod : FunctionMethod
	//{
	//	public GetRefMethod()
	//	{
	//		ReturnType = typeof(string);
	//		argumentTypeArray = null;
	//		CanRestructure = false;
	//	}
	//	public override string CheckArgumentType(string name, IOperandTerm[] arguments)
	//	{
	//		if (arguments.Count < 1)
	//			return name + "関数には少なくとも1つの引数が必要です";
	//		if (arguments.Count > 1)
	//			return name + "関数の引数が多すぎます";
	//		if (arguments[0] == null)
	//			return name + "関数の1番目の引数は省略できません";
	//		if (!(arguments[0] is UserDefinedRefMethodNoArgTerm))
	//			return name + "関数の1番目の引数が関数参照ではありません";
	//		return null;
	//	}
	//	public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
	//	{
	//		return ((UserDefinedRefMethodNoArgTerm)arguments[0]).GetRefName();
	//	}
	//}
	#endregion

	#region 定数取得
	private sealed class MoneyStrMethod : FunctionMethod
	{
		public MoneyStrMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String}, OmitStart = 1 }
				];
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常2つ、1つ省略可能で1～2の引数が必要。
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (arguments[0].GetOperandType() != typeof(Int64))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(string)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long money = arguments[0].GetIntValue(exm);
			if ((arguments.Count < 2) || (arguments[1] == null))
				return Config.MoneyFirst ? Config.MoneyLabel + money.ToString() : money.ToString() + Config.MoneyLabel;
			string format = arguments[1].GetStrValue(exm);
			string ret;
			try
			{
				ret = money.ToString(format);
			}
			catch (FormatException)
			{
				// throw new CodeEE("MONEYSTR関数の第2引数の書式指定が間違っています");
				throw new CodeEE(string.Format(trerror.InvalidFormat.Text, Name, 2));
			}
			return Config.MoneyFirst ? Config.MoneyLabel + ret : ret + Config.MoneyLabel;
		}
	}

	private sealed class GetPrintCPerLineMethod : FunctionMethod
	{
		public GetPrintCPerLineMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return Config.PrintCPerLine;
		}
	}

	private sealed class PrintCLengthMethod : FunctionMethod
	{
		public PrintCLengthMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return Config.PrintCLength;
		}
	}

	private sealed class GetSaveNosMethod : FunctionMethod
	{
		public GetSaveNosMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return Config.SaveDataNos;
		}
	}

	private sealed class GettimeMethod : FunctionMethod
	{
		public GettimeMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long date = DateTime.Now.Year;
			date = date * 100 + DateTime.Now.Month;
			date = date * 100 + DateTime.Now.Day;
			date = date * 100 + DateTime.Now.Hour;
			date = date * 100 + DateTime.Now.Minute;
			date = date * 100 + DateTime.Now.Second;
			date = date * 1000 + DateTime.Now.Millisecond;
			return date;//17桁。2京くらい。
		}
	}

	private sealed class GettimesMethod : FunctionMethod
	{
		public GettimesMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
		}
	}

	private sealed class GetmsMethod : FunctionMethod
	{
		public GetmsMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//西暦0001年1月1日からの経過時間をミリ秒で。
			return DateTime.Now.Ticks / 10000;
		}
	}

	private sealed class GetSecondMethod : FunctionMethod
	{
		public GetSecondMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//西暦0001年1月1日からの経過時間を秒で。
			//Ticksは100ナノ秒単位であるが実際にはそんな精度はないので無駄。
			return DateTime.Now.Ticks / 10000000;
		}
	}
	#endregion

	#region 数学関数
	private sealed class RandMethod : FunctionMethod
	{
		public RandMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int}, OmitStart = 1 }
				];
			CanRestructure = false;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常2つ、1つ省略可能で1～2の引数が必要。
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments.Count == 1)
		//	{
		//		if (arguments[0] == null)
		//			return name + "関数には少なくとも1つの引数が必要です";
		//		if ((arguments[0].GetOperandType() != typeof(Int64)))
		//			return name + "関数の1番目の引数の型が正しくありません";
		//		return null;
		//	}
		//	//1番目は省略可能
		//	if ((arguments[0] != null) && (arguments[0].GetOperandType() != typeof(Int64)))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long min = 0;
			long max;
			if (arguments.Count == 1)
				max = arguments[0].GetIntValue(exm);
			else
			{
				if (arguments[0] != null)
					min = arguments[0].GetIntValue(exm);
				max = arguments[1].GetIntValue(exm);
			}
			if (max <= min)
			{
				if (min == 0)
					// throw new CodeEE("RANDの最大値に0以下の値(" + max.ToString() + ")が指定されました");
					throw new CodeEE(string.Format(trerror.NegativeMaximum.Text, Name, max));
				else
					// throw new CodeEE("RANDの最大値に最小値以下の値(" + max.ToString() + ")が指定されました");
					throw new CodeEE(string.Format(trerror.MaximumLowerThanMinimum.Text, Name, max));
			}
			return exm.VEvaluator.GetNextRand(max - min) + min;
		}
	}

	private sealed class MaxMethod : FunctionMethod
	{
		readonly bool isMax;
		public MaxMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.VariadicInt}, OmitStart = 1 }
				];
			isMax = true;
			CanRestructure = true;
		}
		public MaxMethod(bool max)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.VariadicInt}, OmitStart = 1 }
				];
			isMax = max;
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数は省略できません";
		//		if (arguments[i].GetOperandType() != typeof(Int64))
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数の型が正しくありません";
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = arguments[0].GetIntValue(exm);

			for (int i = 1; i < arguments.Count; i++)
			{
				long newRet = arguments[i].GetIntValue(exm);
				if (isMax)
				{
					if (ret < newRet)
						ret = newRet;
				}
				else
				{
					if (ret > newRet)
						ret = newRet;
				}
			}
			return ret;
		}
	}

	private sealed class AbsMethod : FunctionMethod
	{
		public AbsMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = arguments[0].GetIntValue(exm);
			//普通は使わない値なので例外として投げてしまう方向性で
			if (ret == long.MinValue)
				throw new CodeEE(string.Format(trerror.MinInt64CanNotApplyABS.Text, Name, long.MinValue));
			return Math.Abs(ret);
		}
	}

	private sealed class PowerMethod : FunctionMethod
	{
		public PowerMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long x = arguments[0].GetIntValue(exm);
			long y = arguments[1].GetIntValue(exm);
			double pow = Math.Pow(x, y);
			if (double.IsNaN(pow))
				// throw new CodeEE("累乗結果が非数値です");
				throw new CodeEE(string.Format(trerror.ResultIsNaN.Text, Name));
			else if (double.IsInfinity(pow))
				//throw new CodeEE("累乗結果が無限大です");
				throw new CodeEE(string.Format(trerror.ResultIsInfinity.Text, Name));
			else if ((pow >= long.MaxValue) || (pow <= long.MinValue))
				//throw new CodeEE("累乗結果(" + pow.ToString() + ")が64ビット符号付き整数の範囲外です");
				throw new CodeEE(string.Format(trerror.ResultIsOutOfTheRangeOfInt64.Text, Name, pow));
			return (long)pow;
		}
	}

	private sealed class SqrtMethod : FunctionMethod
	{
		public SqrtMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = arguments[0].GetIntValue(exm);
			if (ret < 0)
				// throw new CodeEE("SQRT関数の引数に負の値が指定されました");
				throw new CodeEE(string.Format(trerror.ArgIsNegative.Text, Name, 1, ret));
			return (long)Math.Sqrt(ret);
		}
	}

	private sealed class CbrtMethod : FunctionMethod
	{
		public CbrtMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = arguments[0].GetIntValue(exm);
			if (ret < 0)
				// throw new CodeEE("CBRT関数の引数に負の値が指定されました");
				throw new CodeEE(string.Format(trerror.ArgIsNegative.Text, Name, 1, ret));
			return (long)Math.Pow(ret, 1.0 / 3.0);
		}
	}

	private sealed class LogMethod : FunctionMethod
	{
		readonly double Base;
		public LogMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			Base = Math.E;
			CanRestructure = true;
		}
		public LogMethod(double b)
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			Base = b;
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = arguments[0].GetIntValue(exm);
			if (ret <= 0)
				// throw new CodeEE("対数関数の引数に0以下の値が指定されました");
				throw new CodeEE(string.Format(trerror.ArgIsNotMoreThan0.Text, Name, 1, ret));
			//　今の段階は発生しない
			//if (Base <= 0.0d)
			//	throw new CodeEE("対数関数の底に0以下の値が指定されました");
			double dret = ret;
			if (Base == Math.E)
				dret = Math.Log(dret);
			else
				dret = Math.Log10(dret);
			if (double.IsNaN(dret))
				// throw new CodeEE("計算値が非数値です");
				throw new CodeEE(string.Format(trerror.ResultIsNaN.Text, Name));
			else if (double.IsInfinity(dret))
				// throw new CodeEE("計算値が無限大です");
				throw new CodeEE(string.Format(trerror.ResultIsInfinity.Text, Name));
			else if ((dret >= long.MaxValue) || (dret <= long.MinValue))
				// throw new CodeEE("計算結果(" + dret.ToString() + ")が64ビット符号付き整数の範囲外です");
				throw new CodeEE(string.Format(trerror.ResultIsOutOfTheRangeOfInt64.Text, Name, dret));
			return (long)dret;
		}
	}

	private sealed class ExpMethod : FunctionMethod
	{
		public ExpMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = arguments[0].GetIntValue(exm);
			double dret = Math.Exp(ret);
			if (double.IsNaN(dret))
				// throw new CodeEE("計算値が非数値です");
				throw new CodeEE(string.Format(trerror.ResultIsNaN.Text, Name));
			else if (double.IsInfinity(dret))
				// throw new CodeEE("計算値が無限大です");
				throw new CodeEE(string.Format(trerror.ResultIsInfinity.Text, Name));
			else if ((dret >= long.MaxValue) || (dret <= long.MinValue))
				// throw new CodeEE("計算結果(" + dret.ToString() + ")が64ビット符号付き整数の範囲外です");
				throw new CodeEE(string.Format(trerror.ResultIsOutOfTheRangeOfInt64.Text, Name, dret));

			return (long)dret;
		}
	}

	private sealed class SignMethod : FunctionMethod
	{

		public SignMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = arguments[0].GetIntValue(exm);
			return Math.Sign(ret);
		}
	}

	private sealed class GetLimitMethod : FunctionMethod
	{
		public GetLimitMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long value = arguments[0].GetIntValue(exm);
			long min = arguments[1].GetIntValue(exm);
			long max = arguments[2].GetIntValue(exm);
			long ret;
			if (value < min)
				ret = min;
			else if (value > max)
				ret = max;
			else
				ret = value;
			return ret;
		}
	}
	#endregion

	#region 変数操作系
	private sealed class SumArrayMethod : FunctionMethod
	{
		readonly bool isCharaRange;
		public SumArrayMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefIntArray, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			isCharaRange = false;
			CanRestructure = false;
		}
		public SumArrayMethod(bool isChara)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.CharacterData | ArgType.RefIntArray | ArgType.AllowConstRef, ArgType.Int, ArgType.Int }, OmitStart = 1 }
				];
			isCharaRange = isChara;
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 3)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm))
		//		return name + "関数の1番目の引数が変数ではありません";
		//	VariableTerm varToken = (VariableTerm)arguments[0];
		//	if (varToken.IsString)
		//		return name + "関数の1番目の引数が数値変数ではありません";
		//	if (isCharaRange && !varToken.Identifier.IsCharacterData)
		//		return name + "関数の1番目の引数がキャラクタ変数ではありません";
		//	if (!isCharaRange && !varToken.Identifier.IsArray1D && !varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D)
		//		return name + "関数の1番目の引数が配列変数ではありません";
		//	if (arguments.Count == 1)
		//		return null;
		//	if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の変数が数値ではありません";
		//	if (arguments.Count == 2)
		//		return null;
		//	if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の変数が数値ではありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm varTerm = (VariableTerm)arguments[0];
			long index1 = (arguments.Count >= 2 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
			long index2 = (arguments.Count == 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLastLength());

			FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);
			if (!isCharaRange)
			{
				p.IsArrayRangeValid(index1, index2, "SUMARRAY", 2L, 3L);
				return VariableEvaluator.GetArraySum(p, index1, index2);
			}
			else
			{
				long charaNum = exm.VEvaluator.CHARANUM;
				if (index1 >= charaNum || index1 < 0 || index2 > charaNum || index2 < 0)
					// throw new CodeEE("SUMCARRAY関数の範囲指定がキャラクタ配列の範囲を超えています(" + index1.ToString() + "～" + index2.ToString() + ")");
					throw new CodeEE(string.Format(trerror.CharacterRangeInvalid.Text, Name, index1, index2));
				return VariableEvaluator.GetArraySumChara(p, index1, index2);
			}
		}
	}

	private sealed class MatchMethod : FunctionMethod
	{
		readonly bool isCharaRange;
		public MatchMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefAny1D | ArgType.AllowConstRef, ArgType.SameAsFirst, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			isCharaRange = false;
			CanRestructure = false;
			HasUniqueRestructure = true;
		}
		public MatchMethod(bool isChara)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					//new ArgTypeList{ ArgTypes = { ArgType.CharacterData | ArgType.RefAny1D | ArgType.AllowConstRef | ArgType.Any, ArgType.SameAsFirst, ArgType.Int, ArgType.Int }, OmitStart = 2 },
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.SameAsFirst, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			isCharaRange = isChara;
			CanRestructure = false;
			HasUniqueRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments.Count > 4)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm))
		//		return name + "関数の1番目の引数が変数ではありません";
		//	VariableTerm varToken = (VariableTerm)arguments[0];
		//	if (isCharaRange && !varToken.Identifier.IsCharacterData)
		//		return name + "関数の1番目の引数がキャラクタ変数ではありません";
		//	if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
		//		return name + "関数は二重配列・三重配列には対応していません";
		//	if (!isCharaRange && !varToken.Identifier.IsArray1D)
		//		return name + "関数の1番目の引数が配列変数ではありません";
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != arguments[0].GetOperandType())
		//		return name + "関数の1番目の引数と2番目の引数の型が異なります";
		//	if ((arguments.Count >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
		//		return name + "関数の4番目の引数の型が正しくありません";
		//	return null;
		//}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm varTerm = arguments[0] as VariableTerm;
			long start = (arguments.Count > 2 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
			long end = (arguments.Count > 3 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLength());

			FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);
			if (!isCharaRange)
			{
				p.IsArrayRangeValid(start, end, "MATCH", 3L, 4L);
				if (arguments[0].GetOperandType() == typeof(long))
				{
					long targetValue = arguments[1].GetIntValue(exm);
					return VariableEvaluator.GetMatch(p, targetValue, start, end);
				}
				else
				{
					string targetStr = arguments[1].GetStrValue(exm);
					return VariableEvaluator.GetMatch(p, targetStr, start, end);
				}
			}
			else
			{
				long charaNum = exm.VEvaluator.CHARANUM;
				if (start >= charaNum || start < 0 || end > charaNum || end < 0)
					// throw new CodeEE("CMATCH関数の範囲指定がキャラクタ配列の範囲を超えています(" + start.ToString() + "～" + end.ToString() + ")");
					throw new CodeEE(string.Format(trerror.CharacterRangeInvalid.Text, Name, start, end));
				if (arguments[0].GetOperandType() == typeof(long))
				{
					long targetValue = arguments[1].GetIntValue(exm);
					return VariableEvaluator.GetMatchChara(p, targetValue, start, end);
				}
				else
				{
					string targetStr = arguments[1].GetStrValue(exm);
					return VariableEvaluator.GetMatchChara(p, targetStr, start, end);
				}
			}
		}

		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			arguments[0].Restructure(exm);
			for (int i = 1; i < arguments.Count; i++)
			{
				if (arguments[i] == null)
					continue;
				arguments[i] = arguments[i].Restructure(exm);
			}
			return false;
		}
	}

	private sealed class GroupMatchMethod : FunctionMethod
	{
		public GroupMatchMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.VariadicSameAsFirst } },
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	Type baseType = arguments[0].GetOperandType();
		//	for (int i = 1; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数は省略できません";
		//		if (arguments[i].GetOperandType() != baseType)
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数の型が正しくありません";
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long ret = 0;
			if (arguments[0].GetOperandType() == typeof(long))
			{
				long baseValue = arguments[0].GetIntValue(exm);
				for (int i = 1; i < arguments.Count; i++)
				{
					if (baseValue == arguments[i].GetIntValue(exm))
						ret += 1;
				}
			}
			else
			{
				string baseString = arguments[0].GetStrValue(exm);
				for (int i = 1; i < arguments.Count; i++)
				{
					if (baseString == arguments[i].GetStrValue(exm))
						ret += 1;
				}
			}
			return ret;
		}
	}

	private sealed class NosamesMethod : FunctionMethod
	{
		public NosamesMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.VariadicSameAsFirst } },
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	Type baseType = arguments[0].GetOperandType();
		//	for (int i = 1; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数は省略できません";
		//		if (arguments[i].GetOperandType() != baseType)
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数の型が正しくありません";
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (arguments[0].GetOperandType() == typeof(long))
			{
				long[] valueArray = new long[arguments.Count];
				for (int i = 0; i < arguments.Count; i++)
				{
					valueArray[i] = arguments[i].GetIntValue(exm);
				}
				var resultArray = valueArray.Distinct();
				if (resultArray.Count() != arguments.Count)
					return 0L;
			}
			else
			{
				string[] stringArray = new string[arguments.Count];
				for (int i = 0; i < arguments.Count; i++)
				{
					stringArray[i] = arguments[i].GetStrValue(exm);
				}
				var resultArray = stringArray.Distinct();
				if (resultArray.Count() != arguments.Count)
					return 0L;
			}
			return 1L;
		}
	}

	private sealed class AllsamesMethod : FunctionMethod
	{
		public AllsamesMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.VariadicSameAsFirst } },
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	Type baseType = arguments[0].GetOperandType();
		//	for (int i = 1; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数は省略できません";
		//		if (arguments[i].GetOperandType() != baseType)
		//			return name + "関数の" + (i + 1).ToString() + "番目の引数の型が正しくありません";
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (arguments[0].GetOperandType() == typeof(long))
			{
				long baseValue = arguments[0].GetIntValue(exm);
				for (int i = 1; i < arguments.Count; i++)
				{
					if (baseValue != arguments[i].GetIntValue(exm))
						return 0L;
				}
			}
			else
			{
				string baseValue = arguments[0].GetStrValue(exm);
				for (int i = 1; i < arguments.Count; i++)
				{
					if (baseValue != arguments[i].GetStrValue(exm))
						return 0L;
				}
			}
			return 1L;
		}
	}

	private sealed class MaxArrayMethod : FunctionMethod
	{
		readonly bool isCharaRange;
		readonly bool isMax;
		readonly string funcName;
		public MaxArrayMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefInt1D | ArgType.AllowConstRef, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			isCharaRange = false;
			isMax = true;
			funcName = "MAXARRAY";
			CanRestructure = false;
		}
		public MaxArrayMethod(bool isChara)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.CharacterData | ArgType.RefInt1D | ArgType.AllowConstRef, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			isCharaRange = isChara;
			isMax = true;
			if (isCharaRange)
				funcName = "MAXCARRAY";
			else
				funcName = "MAXARRAY";
			CanRestructure = false;
		}
		public MaxArrayMethod(bool isChara, bool isMaxFunc)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = isChara
				? [
						new ArgTypeList{ ArgTypes = { ArgType.CharacterData | ArgType.RefInt1D | ArgType.AllowConstRef, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				]
				: [
						new ArgTypeList{ ArgTypes = { ArgType.RefInt1D | ArgType.AllowConstRef, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			isCharaRange = isChara;
			isMax = isMaxFunc;
			funcName = (isMax ? "MAX" : "MIN") + (isCharaRange ? "C" : "") + "ARRAY";
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 3)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm))
		//		return name + "関数の1番目の引数が変数ではありません";
		//	VariableTerm varToken = (VariableTerm)arguments[0];
		//	if (isCharaRange && !varToken.Identifier.IsCharacterData)
		//		return name + "関数の1番目の引数がキャラクタ変数ではありません";
		//	if (!varToken.IsInteger)
		//		return name + "関数の1番目の引数が数値変数ではありません";
		//	if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
		//		return name + "関数は二重配列・三重配列には対応していません";
		//	if (!varToken.Identifier.IsArray1D)
		//		return name + "関数の1番目の引数が配列変数ではありません";
		//	if ((arguments.Count >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm vTerm = (VariableTerm)arguments[0];
			long start = (arguments.Count > 1 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
			long end = (arguments.Count > 2 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : vTerm.GetLength());
			FixedVariableTerm p = vTerm.GetFixedVariableTerm(exm);
			if (!isCharaRange)
			{
				p.IsArrayRangeValid(start, end, funcName, 2L, 3L);
				return VariableEvaluator.GetMaxArray(p, start, end, isMax);
			}
			else
			{
				long charaNum = exm.VEvaluator.CHARANUM;
				if (start >= charaNum || start < 0 || end > charaNum || end < 0)
					throw new CodeEE(string.Format(trerror.CharacterRangeInvalid.Text, funcName, start, end));
				return VariableEvaluator.GetMaxArrayChara(p, start, end, isMax);
			}
		}
	}

	private sealed class GetbitMethod : FunctionMethod
	{
		public GetbitMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	string ret = base.CheckArgumentType(name, arguments);
		//	if (ret != null)
		//		return ret;
		//	if (arguments[1] is SingleTerm)
		//	{
		//		Int64 m = ((SingleTerm)arguments[1]).Int;
		//		if (m < 0 || m > 63)
		//			return "GETBIT関数の第２引数(" + m.ToString() + ")が範囲(０～６３)を超えています";
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long n = arguments[0].GetIntValue(exm);
			long m = arguments[1].GetIntValue(exm);
			if ((m < 0) || (m > 63))
				// throw new CodeEE("GETBIT関数の第２引数(" + m.ToString() + ")が範囲(０～６３)を超えています");
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, 2, m, 0, 63));
			int mi = (int)m;
			return (n >> mi) & 1;
		}
	}

	private sealed class GetnumMethod : FunctionMethod
	{
		public GetnumMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefAny | ArgType.AllowConstRef, ArgType.String, ArgType.Int }, OmitStart = 2 },
				];
			CanRestructure = true;
			HasUniqueRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count != 2)
		//		return name + "関数には2つの引数が必要です";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != typeof(string))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm vToken = (VariableTerm)arguments[0];
			VariableCode varCode = vToken.Identifier.Code;
			string varname = "";
			#region EE_ERD
			if (arguments.Count > 2)
				varname = vToken.Identifier.Name + "@" + arguments[2].GetIntValue(exm);
			else
				varname = vToken.Identifier.Name;
			#endregion
			string key = arguments[1].GetStrValue(exm);
			#region EE_ERD
			// if (exm.VEvaluator.Constant.TryKeywordToInteger(out int ret, varCode, key, -1))
			if (exm.VEvaluator.Constant.TryKeywordToInteger(out int ret, varCode, key, -1, varname))
				#endregion
				return ret;
			else
				return -1;
		}
		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			arguments[1] = arguments[1].Restructure(exm);
			return arguments[1] is SingleTerm;
		}
	}

	private sealed class GetnumBMethod : FunctionMethod
	{
		public GetnumBMethod()
		{
			ReturnType = typeof(Int64);
			argumentTypeArray = new Type[] { typeof(string), typeof(string) };
			CanRestructure = true;
		}
		/*
		public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		{
			string errStr = base.CheckArgumentType(name, arguments);
			if (errStr != null)
				return errStr;
			if (arguments[0] == null)
				return name + "関数の1番目の引数は省略できません";
			if (arguments[0] is SingleTerm)
			{
				string varName = ((SingleTerm)arguments[0]).Text;
				if (GlobalStatic.IdentifierDictionary.GetVariableToken(varName, null, true) == null)
					return name + "関数の1番目の引数が変数名ではありません";
			}
			return null;
		}
		*/
		public override Int64 GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableToken var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
			if (var == null)
				throw new CodeEE("GETNUMBの1番目の引数(\"" + arguments[0].GetStrValue(exm) + "\")が変数名ではありません");
			string key = arguments[1].GetStrValue(exm);
			#region EE_ERD
			//GETNUMBは使ってないのでテストしていない
			// if (exm.VEvaluator.Constant.TryKeywordToInteger(out int ret, var.Code, key, -1))
			if (exm.VEvaluator.Constant.TryKeywordToInteger(out int ret, var.Code, key, -1, arguments[0].GetStrValue(exm)))
			#endregion
				return ret;
			else
				return -1;
		}
	}

	private sealed class GetPalamLVMethod : FunctionMethod
	{
		public GetPalamLVMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	string errStr = base.CheckArgumentType(name, arguments);
		//	if (errStr != null)
		//		return errStr;
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long value = arguments[0].GetIntValue(exm);
			long maxLv = arguments[1].GetIntValue(exm);

			return exm.VEvaluator.getPalamLv(value, maxLv);
		}
	}

	private sealed class GetExpLVMethod : FunctionMethod
	{
		public GetExpLVMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	string errStr = base.CheckArgumentType(name, arguments);
		//	if (errStr != null)
		//		return errStr;
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long value = arguments[0].GetIntValue(exm);
			long maxLv = arguments[1].GetIntValue(exm);

			return exm.VEvaluator.getExpLv(value, maxLv);
		}
	}

	private sealed class FindElementMethod : FunctionMethod
	{
		public FindElementMethod(bool last)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefAny1D | ArgType.AllowConstRef, ArgType.SameAsFirst, ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			CanRestructure = true; //すべて定数項ならできるはず
			HasUniqueRestructure = true;
			isLast = last;
			funcName = isLast ? "FINDLASTELEMENT" : "FINDELEMENT";
		}

		readonly bool isLast;
		readonly string funcName;
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments.Count > 5)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm varToken))
		//		return name + "関数の1番目の引数が変数ではありません";
		//	if (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D)
		//		return name + "関数は二重配列・三重配列には対応していません";
		//	if (!varToken.Identifier.IsArray1D)
		//		return name + "関数の1番目の引数が配列変数ではありません";
		//	Type baseType = arguments[0].GetOperandType();
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != baseType)
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
		//		return name + "関数の4番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 5) && (arguments[4] != null) && (arguments[4].GetOperandType() != typeof(Int64)))
		//		return name + "関数の5番目の引数の型が正しくありません";
		//	return null;
		//}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			bool isExact = false;
			VariableTerm varTerm = (VariableTerm)arguments[0];

			long start = (arguments.Count > 2 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
			long end = (arguments.Count > 3 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : varTerm.GetLength();
			if (arguments.Count > 4 && arguments[4] != null)
				isExact = arguments[4].GetIntValue(exm) != 0;

			FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);
			p.IsArrayRangeValid(start, end, funcName, 3L, 4L);

			if (arguments[0].GetOperandType() == typeof(long))
			{
				long targetValue = arguments[1].GetIntValue(exm);
				return VariableEvaluator.FindElement(p, targetValue, start, end, isExact, isLast);
			}
			else
			{
				Regex targetString;
				try
				{
					targetString = RegexFactory.GetRegex(arguments[1].GetStrValue(exm));
				}
				catch (ArgumentException e)
				{
					// throw new CodeEE("第2引数が正規表現として不正です");
					throw new CodeEE(string.Format(trerror.InvalidRegexArg.Text, Name, 2, e.Message));
				}
				return VariableEvaluator.FindElement(p, targetString, start, end, isExact, isLast);
			}
		}


		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			arguments[0].Restructure(exm);
			VariableTerm varToken = arguments[0] as VariableTerm;
			bool isConst = varToken.Identifier.IsConst;
			for (int i = 1; i < arguments.Count; i++)
			{
				if (arguments[i] == null)
					continue;
				arguments[i] = arguments[i].Restructure(exm);
				if (isConst && !(arguments[i] is SingleTerm))
					isConst = false;
			}
			return isConst;
		}
	}

	private sealed class InRangeMethod : FunctionMethod
	{
		public InRangeMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long value = arguments[0].GetIntValue(exm);
			long min = arguments[1].GetIntValue(exm);
			long max = arguments[2].GetIntValue(exm);
			return ((value >= min) && (value <= max)) ? 1L : 0L;
		}
	}

	private sealed class InRangeArrayMethod : FunctionMethod
	{
		public InRangeArrayMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefInt1D | ArgType.AllowConstRef, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 3 },
				];
			CanRestructure = false;
		}
		public InRangeArrayMethod(bool isChara)
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.CharacterData | ArgType.RefInt1D | ArgType.AllowConstRef, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 3 },
				];
			isCharaRange = isChara;
			CanRestructure = false;
		}
		private readonly bool isCharaRange;
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments.Count > 6)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm))
		//		return name + "関数の1番目の引数が変数ではありません";
		//	VariableTerm varToken = (VariableTerm)arguments[0];
		//	if (isCharaRange && !varToken.Identifier.IsCharacterData)
		//		return name + "関数の1番目の引数がキャラクタ変数ではありません";
		//	if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
		//		return name + "関数は二重配列・三重配列には対応していません";
		//	if (!isCharaRange && !varToken.Identifier.IsArray1D)
		//		return name + "関数の1番目の引数が配列変数ではありません";
		//	if (!varToken.IsInteger)
		//		return name + "関数の1番目の引数が数値型変数ではありません";
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != typeof(Int64))
		//		return name + "関数の2番目の引数が数値型ではありません";
		//	if (arguments[2] == null)
		//		return name + "関数の3番目の引数は省略できません";
		//	if (arguments[2].GetOperandType() != typeof(Int64))
		//		return name + "関数の3番目の引数が数値型ではありません";
		//	if ((arguments.Count >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
		//		return name + "関数の4番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 5) && (arguments[4] != null) && (arguments[4].GetOperandType() != typeof(Int64)))
		//		return name + "関数の5番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long min = arguments[1].GetIntValue(exm);
			long max = arguments[2].GetIntValue(exm);

			VariableTerm varTerm = arguments[0] as VariableTerm;
			long start = (arguments.Count > 3 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : 0;
			long end = (arguments.Count > 4 && arguments[4] != null) ? arguments[4].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLength());

			FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);

			if (!isCharaRange)
			{
				p.IsArrayRangeValid(start, end, "INRANGEARRAY", 4L, 5L);
				return VariableEvaluator.GetInRangeArray(p, min, max, start, end);
			}
			else
			{
				long charaNum = exm.VEvaluator.CHARANUM;
				if (start >= charaNum || start < 0 || end > charaNum || end < 0)
					// throw new CodeEE("INRANGECARRAY関数の範囲指定がキャラクタ配列の範囲を超えています(" + start.ToString() + "～" + end.ToString() + ")");
					throw new CodeEE(string.Format(trerror.CharacterRangeInvalid.Text, Name, start, end));
				return VariableEvaluator.GetInRangeArrayChara(p, min, max, start, end);
			}
		}
	}

	private sealed class ArrayMultiSortMethod : FunctionMethod
	{
		public ArrayMultiSortMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefAny1D, ArgType.RefAnyArray | ArgType.Variadic }, OmitStart = 1 },
				];
			CanRestructure = false;
			HasUniqueRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return string.Format("{0}関数:少なくとも{1}の引数が必要です", name, 2);
		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format("{0}関数:{1}番目の引数は省略できません", name, i + 1);
		//		if (!(arguments[i] is VariableTerm varTerm) || varTerm.Identifier.IsCalc || varTerm.Identifier.IsConst)
		//			return string.Format("{0}関数:{1}番目の引数が変数ではありません", name, i + 1);
		//		if (varTerm.Identifier.IsCharacterData)
		//			return string.Format("{0}関数:{1}番目の引数がキャラクタ変数です", name, i + 1);
		//		if (i == 0 && !varTerm.Identifier.IsArray1D)
		//			return string.Format("{0}関数:{1}番目の引数が一次元配列ではありません", name, i + 1);
		//		#region EM_私家版_ARRAYMSORT_三次元配列修正
		//		//if (!varTerm.Identifier.IsArray1D && !varTerm.Identifier.IsArray2D && !varTerm.Identifier.IsArray2D)
		//		if (!varTerm.Identifier.IsArray1D && !varTerm.Identifier.IsArray2D && !varTerm.Identifier.IsArray3D)
		//			return string.Format("{0}関数:{1}番目の引数が配列変数ではありません", name, i + 1);
		//		#endregion
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm varTerm = arguments[0] as VariableTerm;
			int[] sortedArray;
			if (varTerm.Identifier.IsInteger)
			{
				List<KeyValuePair<long, int>> sortList = [];
				long[] array = (long[])varTerm.Identifier.GetArray();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == 0)
						break;
					if (array[i] < long.MinValue || array[i] > long.MaxValue)
						return 0;
					sortList.Add(new KeyValuePair<long, int>(array[i], i));
				}
				//素ではintの範囲しか扱えないので一工夫
				sortList.Sort((a, b) => { return Math.Sign(a.Key - b.Key); });
				sortedArray = new int[sortList.Count];
				for (int i = 0; i < sortedArray.Length; i++)
					sortedArray[i] = sortList[i].Value;
			}
			else
			{
				List<KeyValuePair<string, int>> sortList = [];
				string[] array = (string[])varTerm.Identifier.GetArray();
				for (int i = 0; i < array.Length; i++)
				{
					if (string.IsNullOrEmpty(array[i]))
						#region EM_私家版_ARRAYMSORT_文字列配列処理修正
						//return 0;
						break;
					#endregion
					sortList.Add(new KeyValuePair<string, int>(array[i], i));
				}
				sortList.Sort((a, b) => { return a.Key.CompareTo(b.Key); });
				sortedArray = new int[sortList.Count];
				for (int i = 0; i < sortedArray.Length; i++)
					sortedArray[i] = sortList[i].Value;
			}
			foreach (VariableTerm term in arguments.Cast<VariableTerm>())//もう少し賢い方法はないものだろうか
			{
				if (term.Identifier.IsArray1D)
				{
					if (term.IsInteger)
					{
						var array = (long[])term.Identifier.GetArray();
						var clone = (long[])array.Clone();
						if (array.Length < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							array[i] = clone[sortedArray[i]];
					}
					else
					{
						var array = (string[])term.Identifier.GetArray();
						var clone = (string[])array.Clone();
						if (array.Length < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							array[i] = clone[sortedArray[i]];
					}
				}
				else if (term.Identifier.IsArray2D)
				{
					if (term.IsInteger)
					{
						var array = (long[,])term.Identifier.GetArray();
						var clone = (long[,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								array[i, x] = clone[sortedArray[i], x];
					}
					else
					{
						var array = (string[,])term.Identifier.GetArray();
						var clone = (string[,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								array[i, x] = clone[sortedArray[i], x];
					}
				}
				else if (term.Identifier.IsArray3D)
				{
					if (term.IsInteger)
					{
						var array = (long[,,])term.Identifier.GetArray();
						var clone = (long[,,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								for (int y = 0; y < array.GetLength(2); y++)
									array[i, x, y] = clone[sortedArray[i], x, y];
					}
					else
					{
						var array = (string[,,])term.Identifier.GetArray();
						var clone = (string[,,])array.Clone();
						if (array.GetLength(0) < sortedArray.Length)
							return 0;
						for (int i = 0; i < sortedArray.Length; i++)
							for (int x = 0; x < array.GetLength(1); x++)
								for (int y = 0; y < array.GetLength(2); y++)
									array[i, x, y] = clone[sortedArray[i], x, y];
					}
				}
				// else { throw new ExeEE("異常な配列"); }
				else { throw new ExeEE(trerror.AbnormalArray.Text); }
			}
			return 1;
		}
		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			for (int i = 0; i < arguments.Count; i++)
				arguments[i] = arguments[i].Restructure(exm);
			return false;
		}
	}
	#endregion

	#region 文字列操作系
	private sealed class StrlenMethod : FunctionMethod
	{
		public StrlenMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			return LangManager.GetStrlenLang(str);
		}
	}

	private sealed class StrlenuMethod : FunctionMethod
	{
		public StrlenuMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			return str.Length;
		}
	}

	private sealed class SubstringMethod : FunctionMethod
	{
		public SubstringMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.Int}, OmitStart = 1 }
				];
			CanRestructure = true;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常３つ、２つ省略可能で１～３の引数が必要。
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 3)
		//		return name + "関数の引数が多すぎます";

		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (arguments[0].GetOperandType() != typeof(string))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	//2、３は省略可能
		//	if ((arguments.Count >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の引数の型が正しくありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			int start = 0;
			int length = -1;
			if ((arguments.Count >= 2) && (arguments[1] != null))
				start = (int)arguments[1].GetIntValue(exm);
			if ((arguments.Count >= 3) && (arguments[2] != null))
				length = (int)arguments[2].GetIntValue(exm);

			return LangManager.GetSubStringLang(str, start, length);
		}
	}

	private sealed class SubstringuMethod : FunctionMethod
	{
		public SubstringuMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.Int}, OmitStart = 1 }
				];
			CanRestructure = true;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常３つ、２つ省略可能で１～３の引数が必要。
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 3)
		//		return name + "関数の引数が多すぎます";

		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (arguments[0].GetOperandType() != typeof(string))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	//2、３は省略可能
		//	if ((arguments.Count >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の引数の型が正しくありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			int start = 0;
			int length = -1;
			if ((arguments.Count >= 2) && (arguments[1] != null))
				start = (int)arguments[1].GetIntValue(exm);
			if ((arguments.Count >= 3) && (arguments[2] != null))
				length = (int)arguments[2].GetIntValue(exm);
			if ((start >= str.Length) || (length == 0))
				return "";
			if ((length < 0) || (length > str.Length))
				length = str.Length;
			if (start <= 0)
			{
				if (length == str.Length)
					return str;
				else
					start = 0;
			}
			if ((start + length) > str.Length)
				length = str.Length - start;

			return str.Substring(start, length);
		}
	}

	private sealed class StrfindMethod : FunctionMethod
	{
		public StrfindMethod(bool unicode)
		{
			ReturnType = typeof(long);
			argumentTypeArray = null;
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.Int}, OmitStart = 2 }
				];
			CanRestructure = true;
			this.unicode = unicode;
		}

		readonly bool unicode;
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常３つ、１つ省略可能で２～３の引数が必要。
		//	if (arguments.Count < 2)
		//		return name + "関数には少なくとも2つの引数が必要です";
		//	if (arguments.Count > 3)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (arguments[0].GetOperandType() != typeof(string))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	if (arguments[1] == null)
		//		return name + "関数の2番目の引数は省略できません";
		//	if (arguments[1].GetOperandType() != typeof(string))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	//3つ目は省略可能
		//	if ((arguments.Count >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{

			string target = arguments[0].GetStrValue(exm);
			string word = arguments[1].GetStrValue(exm);
			int UFTstart = 0;
			if ((arguments.Count >= 3) && (arguments[2] != null))
			{
				if (unicode)
				{
					UFTstart = (int)arguments[2].GetIntValue(exm);
				}
				else
				{
					UFTstart = LangManager.GetUFTIndex(target, (int)arguments[2].GetIntValue(exm));
				}
			}
			if (UFTstart < 0 || UFTstart >= target.Length)
				return -1;
			int index = target.IndexOf(word, UFTstart, StringComparison.Ordinal);
			if (index > 0 && !unicode)
			{
				string subStr = target.Substring(0, index);
				index = LangManager.GetStrlenLang(subStr);
			}
			return index;
		}
	}

	private sealed class StrCountMethod : FunctionMethod
	{
		public StrCountMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(string)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			Regex reg;
			try
			{
				reg = RegexFactory.GetRegex(arguments[1].GetStrValue(exm));
			}
			catch (ArgumentException e)
			{
				// throw new CodeEE("第2引数が正規表現として不正です：" + e.Message);
				throw new CodeEE(string.Format(trerror.InvalidRegexArg.Text, Name, 2, e.Message));
			}
			return reg.Matches(arguments[0].GetStrValue(exm)).Count;
		}
	}

	private sealed class ToStrMethod : FunctionMethod
	{
		public ToStrMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String }, OmitStart = 1 }
				];
			CanRestructure = true;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常2つ、1つ省略可能で1～2の引数が必要。
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (arguments[0].GetOperandType() != typeof(Int64))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(string)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long i = arguments[0].GetIntValue(exm);
			if ((arguments.Count < 2) || (arguments[1] == null))
				return i.ToString();
			string format = arguments[1].GetStrValue(exm);
			string ret;
			try
			{
				ret = i.ToString(format);
			}
			catch (FormatException)
			{
				// throw new CodeEE("TOSTR関数の書式指定が間違っています");
				throw new CodeEE(string.Format(trerror.InvalidFormat.Text, Name, 2));
			}
			return ret;
		}
	}

	private sealed class ToIntMethod : FunctionMethod
	{
		public ToIntMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			if (str == null || string.IsNullOrEmpty(str))
				return 0;
			//全角文字が入ってるなら無条件で0を返す
			if (str.Length < LangManager.GetStrlenLang(str))
				return 0;
			CharStream st = new(str);
			if (!char.IsDigit(st.Current) && st.Current != '+' && st.Current != '-')
				return 0;
			else if ((st.Current == '+' || st.Current == '-') && !char.IsDigit(st.Next))
				return 0;
			long ret = LexicalAnalyzer.ReadInt64(st, true);
			if (!st.EOS)
			{
				if (st.Current == '.')
				{
					st.ShiftNext();
					while (!st.EOS)
					{
						if (!char.IsDigit(st.Current))
							return 0;
						st.ShiftNext();
					}
				}
				else
					return 0;
			}
			return ret;
		}
	}

	//TOUPPER等の処理を汎用化するためのenum
	enum StrFormType
	{
		Upper = 0,
		Lower = 1,
		Half = 2,
		Full = 3,
	};

	private sealed class StrChangeStyleMethod : FunctionMethod
	{
		readonly StrFormType strType;
		public StrChangeStyleMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			strType = StrFormType.Upper;
			CanRestructure = true;
		}
		public StrChangeStyleMethod(StrFormType type)
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			strType = type;
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			if (str == null || string.IsNullOrEmpty(str))
				return "";
			switch (strType)
			{
				case StrFormType.Upper:
					return str.ToUpper();
				case StrFormType.Lower:
					return str.ToLower();
				//andEmuera: Strings.StrConv は Windows の NLS API 依存で Android では例外になるため自前実装に置き換え
				case StrFormType.Half:
					return Runtime.Utils.JapaneseText.ToNarrow(str);
				case StrFormType.Full:
					return Runtime.Utils.JapaneseText.ToWide(str);
			}
			return "";
		}
	}

	private sealed class LineIsEmptyMethod : FunctionMethod
	{
		public LineIsEmptyMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return GlobalStatic.Console.EmptyLine ? 1L : 0L;
		}
	}

	#region EM_私家版_REPLACE拡張
	private sealed class ReplaceMethod : FunctionMethod
	{
		public ReplaceMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.String, ArgType.Int }, OmitStart = 3 },
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.RefString1D | ArgType.AllowConstRef, ArgType.Int } },
				];
			HasUniqueRestructure = true;
			CanRestructure = false;
		}

		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			return arguments.Count < 4 || arguments[3].GetIntValue(exm) != 1;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常2つ、1つ省略可能で1～2の引数が必要。
		//	if (arguments.Count < 3)
		//		return name + "関数には少なくとも3つの引数が必要です";
		//	if (arguments.Count > 4)
		//		return name + "関数の引数が多すぎます";
		//	for (int i = 0; i < 3; i++)
		//		if (arguments[i].GetOperandType() != typeof(string))
		//			return string.Format("{0}関数:{1}番目の引数が文字列ではありません", name, i + 1);
		//	if (arguments.Count == 4 && arguments[3].GetOperandType() != typeof(Int64))
		//		return string.Format("{0}関数:4番目の引数が整数ではありません", name);
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string baseString = arguments[0].GetStrValue(exm);
			Regex reg = null;
			int type = arguments.Count == 4 ? (int)arguments[3].GetIntValue(exm) : 0;
			if (type != 2)
			{
				try
				{
					reg = RegexFactory.GetRegex(arguments[1].GetStrValue(exm));
				}
				catch (ArgumentException e)
				{
					// throw new CodeEE("第２引数が正規表現として不正です：" + e.Message);
					throw new CodeEE(string.Format(trerror.InvalidRegexArg.Text, Name, 2, e.Message));
				}
			}
			if (arguments.Count == 4)
			{
				switch (type)
				{
					case 1:
						{
							if (!(arguments[2] is VariableTerm varTerm) || varTerm.Identifier.IsCalc || !varTerm.Identifier.IsArray1D || !varTerm.Identifier.IsString || varTerm.Identifier.IsConst)
								throw new CodeEE(string.Format(trerror.ArgIsNotNDStrArray.Text, Name, 3, 1));
							var items = (arguments[2] as VariableTerm).Identifier.GetArray() as string[];
							int idx = 0;
							return reg.Replace(baseString, (Match match) =>
							{
								if (idx < items.Length)
								{
									return items[idx++];
								}
								return string.Empty;
							});
						}
					case 2:
						{
							// 正規表現を使わず
							return baseString.Replace(arguments[1].GetStrValue(exm), arguments[2].GetStrValue(exm));
						}
				}
			}
			// type == 0 or > 2 or omitted.
			return reg.Replace(baseString, arguments[2].GetStrValue(exm));
		}
	}
	#endregion

	private sealed class UnicodeMethod : FunctionMethod
	{
		public UnicodeMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(long)];
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long i = arguments[0].GetIntValue(exm);
			if ((i < 0) || (i > 0xFFFF))
				// throw new CodeEE("UNICODE関数に範囲外の値(" + i.ToString() + ")が渡されました");
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, 1, i, 0, 0xFFFF));
			//改行関係以外の制御文字は警告扱いに変更
			//とはいえ、改行以外の制御文字を意図的に渡すのはそもそもコーディングに問題がありすぎるので、エラーでもいい気はする
			if ((i < 0x001F && i != 0x000A && i != 0x000D) || (i >= 0x007F && i <= 0x009F))
			{
				//コード実行中の場合
				if (GlobalStatic.Process.getCurrentLine != null)
					// GlobalStatic.Console.PrintSystemLine("注意:" + GlobalStatic.Process.getCurrentLine.Position.Value.Filename + "の" + GlobalStatic.Process.getCurrentLine.Position.Value.LineNo.ToString() + "行目でUNICODE関数に制御文字に対応する値(0x" + String.Format("{0:X}", i) + ")が渡されました");
					GlobalStatic.Console.PrintSystemLine(string.Format(trerror.WarnPrefix.Text,
						GlobalStatic.Process.getCurrentLine.Position.Value.Filename,
						GlobalStatic.Process.getCurrentLine.Position.Value.LineNo,
						string.Format(trerror.InvalidUnicode.Text, Name, i)));
				else
					//ParserMediator.Warn("UNICODE関数に制御文字に対応する値(0x" + String.Format("{0:X}", i) + ")が渡されました", GlobalStatic.Process.scaningLine, 1, false, false, null);
					ParserMediator.Warn(string.Format(trerror.InvalidUnicode.Text, Name, i), GlobalStatic.Process.scaningLine, 1, false, false, null);
				return "";
			}
			string s = new(new char[] { (char)i });

			return s;
		}
	}

	private sealed class UnicodeByteMethod : FunctionMethod
	{
		public UnicodeByteMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string target = arguments[0].GetStrValue(exm);
			int length = Encoding.UTF32.GetEncoder().GetByteCount(target.ToCharArray(), 0, target.Length, false);
			byte[] bytes = new byte[length];
			Encoding.UTF32.GetEncoder().GetBytes(target.ToCharArray(), 0, target.Length, bytes, 0, false);
			long i = BitConverter.ToInt32(bytes, 0);

			return i;
		}
	}

	private sealed class ConvertIntMethod : FunctionMethod
	{
		public ConvertIntMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long toBase = arguments[1].GetIntValue(exm);
			if ((toBase != 2) && (toBase != 8) && (toBase != 10) && (toBase != 16))
				// new CodeEE("CONVERT関数の第２引数は2, 8, 10, 16のいずれかでなければなりません");
				throw new CodeEE(string.Format(trerror.ArgShouldBeSpecificValue.Text, Name, 2, "2, 8, 10, 16"));
			return Convert.ToString(arguments[0].GetIntValue(exm), (int)toBase);
		}
	}

	private sealed class IsNumericMethod : FunctionMethod
	{
		public IsNumericMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string baseStr = arguments[0].GetStrValue(exm);

			//全角文字があるなら数値ではない
			if (baseStr.Length < LangManager.GetStrlenLang(baseStr))
				return 0;
			CharStream st = new(baseStr);
			if (!char.IsDigit(st.Current) && st.Current != '+' && st.Current != '-')
				return 0;
			else if ((st.Current == '+' || st.Current == '-') && !char.IsDigit(st.Next))
				return 0;
			if (!LexicalAnalyzer.NumericCheck(st))
				return (0);
			if (!st.EOS)
			{
				if (st.Current == '.')
				{
					st.ShiftNext();
					while (!st.EOS)
					{
						if (!char.IsDigit(st.Current))
							return 0;
						st.ShiftNext();
					}
				}
				else
					return 0;
			}
			return 1;
		}
	}

	private sealed class EscapeMethod : FunctionMethod
	{
		public EscapeMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return Regex.Escape(arguments[0].GetStrValue(exm));
		}
	}

	private sealed class EncodeToUniMethod : FunctionMethod
	{
		public EncodeToUniMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { null };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int }, OmitStart = 1 },
				];
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常2つ、1つ省略可能で1～2の引数が必要。
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 2)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (arguments[0].GetOperandType() != typeof(string))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	if ((arguments.Count >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
		//		return name + "関数の2番目の引数の型が正しくありません";
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string baseStr = arguments[0].GetStrValue(exm);
			if (baseStr.Length == 0)
				return -1;
			long position = (arguments.Count > 1 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
			if (position < 0)
				// throw new CodeEE("ENCOIDETOUNI関数の第２引数(" + position.ToString() + ")が負の値です");
				throw new CodeEE(string.Format(trerror.ArgIsNegative.Text, Name, 2, position));
			if (position >= baseStr.Length)
				// throw new CodeEE("ENCOIDETOUNI関数の第２引数(" + position.ToString() + ")が第１引数の文字列(" + baseStr + ")の文字数を超えています");
				throw new CodeEE(string.Format(trerror.EncodeToUni2ndArgError.Text, Name, position, baseStr));
			return char.ConvertToUtf32(baseStr, (int)position);
		}
	}

	public sealed class CharAtMethod : FunctionMethod
	{
		public CharAtMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string), typeof(long)];
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			long pos = arguments[1].GetIntValue(exm);
			if (pos < 0 || pos >= str.Length)
				return "";
			return str[(int)pos].ToString();
		}
	}

	public sealed class GetLineStrMethod : FunctionMethod
	{
		public GetLineStrMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(str))
				// throw new CodeEE("GETLINESTR関数の引数が空文字列です");
				throw new CodeEE(string.Format(trerror.ArgIsEmptyString.Text, Name, 1));
			return exm.Console.getStBar(str);
		}
	}

	public sealed class StrFormMethod : FunctionMethod
	{
		public StrFormMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			HasUniqueRestructure = true;
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			string destStr;
			try
			{
				StrFormWord wt = LexicalAnalyzer.AnalyseFormattedString(new CharStream(str), FormStrEndWith.EoL, false);
				StrForm strForm = StrForm.FromWordToken(wt);
				destStr = strForm.GetString(exm);
			}
			catch (CodeEE e)
			{
				// throw new CodeEE("STRFORM関数:文字列\"" + str + "\"の展開エラー:" + e.Message);
				throw new CodeEE(string.Format(trerror.InvalidFormString.Text, Name, str, e.Message));
			}
			catch
			{
				// throw new CodeEE("STRFORM関数:文字列\"" + str+ "\"の展開処理中にエラーが発生しました");
				throw new CodeEE(string.Format(trerror.UnexectedFormStringErr.Text, Name, str));
			}
			return destStr;
		}
		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			arguments[0].Restructure(exm);
			//引数が文字列式等ならお手上げなので諦める
			if (!(arguments[0] is SingleTerm) && !(arguments[0] is VariableTerm))
				return false;
			//引数が確定値でない文字列変数なら無条件で不可（結果が可変なため）
			if ((arguments[0] is VariableTerm) && !((VariableTerm)arguments[0]).Identifier.IsConst)
				return false;
			string str = arguments[0].GetStrValue(exm);
			try
			{
				StrFormWord wt = LexicalAnalyzer.AnalyseFormattedString(new CharStream(str), FormStrEndWith.EoL, false);
				StrForm strForm = StrForm.FromWordToken(wt);
				if (!strForm.IsConst)
					return false;
			}
			catch
			{
				//パースできないのはエラーがあるかここではわからないからとりあえず考えない
				return false;
			}
			return true;
		}
	}

	public sealed class JoinMethod : FunctionMethod
	{
		public JoinMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefAnyArray | ArgType.AllowConstRef, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			HasUniqueRestructure = true;
			CanRestructure = true;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 1)
		//		return name + "関数には少なくとも1つの引数が必要です";
		//	if (arguments.Count > 4)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments[0] == null)
		//		return name + "関数の1番目の引数は省略できません";
		//	if (!(arguments[0] is VariableTerm))
		//		return name + "関数の1番目の引数が変数ではありません";
		//	VariableTerm varToken = (VariableTerm)arguments[0];
		//	if (!varToken.Identifier.IsArray1D && !varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D)
		//		return name + "関数の1番目の引数が配列変数ではありません";
		//	if (arguments.Count == 1)
		//		return null;
		//	if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(string)))
		//		return name + "関数の2番目の変数が文字列ではありません";
		//	if (arguments.Count == 2)
		//		return null;
		//	if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
		//		return name + "関数の3番目の変数が数値ではありません";
		//	if (arguments.Count == 3)
		//		return null;
		//	if ((arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
		//		return name + "関数の4番目の変数が数値ではありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm varTerm = (VariableTerm)arguments[0];
			string delimiter = (arguments.Count >= 2 && arguments[1] != null) ? arguments[1].GetStrValue(exm) : ",";
			long index1 = (arguments.Count >= 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
			long index2 = (arguments.Count == 4 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : varTerm.GetLastLength() - index1;

			FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);

			if (index2 < 0)
				// throw new CodeEE("STRJOINの第4引数(" + index2.ToString()+ ")が負の値になっています");
				throw new CodeEE(string.Format(trerror.ArgIsNegative.Text, Name, 4, index2));

			p.IsArrayRangeValid(index1, index1 + index2, "STRJOIN", 2L, 3L);
			return VariableEvaluator.GetJoinedStr(p, delimiter, index1, index2);
		}
		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			//第1変数は変数名なので、定数文字列変数だと事故が起こるので独自対応
			VariableTerm varTerm = (VariableTerm)arguments[0];
			bool canRerstructure = varTerm.Identifier.IsConst;
			for (int i = 1; i < arguments.Count; i++)
			{
				if (arguments[i] == null)
					continue;
				arguments[i] = arguments[i].Restructure(exm);
				canRerstructure &= arguments[i] is SingleTerm;
			}
			return canRerstructure;
		}
	}

	public sealed class GetConfigMethod : FunctionMethod
	{
		public GetConfigMethod(bool typeisInt)
		{
			if (typeisInt)
			{
				funcname = "GETCONFIG";
				ReturnType = typeof(long);
			}
			else
			{
				funcname = "GETCONFIGS";
				ReturnType = typeof(string);
			}
			argumentTypeArray = [typeof(string)];
			CanRestructure = true;
		}
		private readonly string funcname;
		private SingleTerm GetSingleTerm(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			if (str == null || str.Length == 0)
				// throw new CodeEE(funcname + "関数に空文字列が渡されました");
				throw new CodeEE(string.Format(trerror.ArgIsEmptyString.Text, Name, 1));
			string errMes = null;
			SingleTerm term = ConfigData.GetConfigValueInERB(str, ref errMes);
			if (errMes != null)
				// throw new CodeEE(funcname + "関数:" + errMes);
				throw new CodeEE(errMes);
			return term;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (ReturnType != typeof(long))
				throw new ExeEE(funcname + "関数:不正な呼び出し");
			SingleTerm term = GetSingleTerm(exm, arguments);
			if (term is not SingleLongTerm singleLongTerm)
				// throw new CodeEE(funcname + "関数:型が違います（GETCONFIGS関数を使用してください）");
				throw new CodeEE(string.Format(trerror.InvalidType.Text, Name, "GETCONFIGS"));
			return singleLongTerm.Int;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (ReturnType != typeof(string))
				throw new ExeEE(funcname + "関数:不正な呼び出し");
			SingleTerm term = GetSingleTerm(exm, arguments);
			if (term is not SingleStrTerm singleStrTerm)
				// throw new CodeEE(funcname + "関数:型が違います（GETCONFIG関数を使用してください）");
				throw new CodeEE(string.Format(trerror.InvalidType.Text, Name, "GETCONFIG"));
			return singleStrTerm.Str;
		}
	}
	#endregion

	#region html系

	private sealed class HtmlGetPrintedStrMethod : FunctionMethod
	{
		public HtmlGetPrintedStrMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int }, OmitStart = 0 }
				];
			CanRestructure = false;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	//通常１つ。省略可能。
		//	if (arguments.Count > 1)
		//		return name + "関数の引数が多すぎます";
		//	if (arguments.Count == 0|| arguments[0] == null)
		//		return null;
		//	if (arguments[0].GetOperandType() != typeof(Int64))
		//		return name + "関数の1番目の引数の型が正しくありません";
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long lineNo = 0;
			if (arguments.Count > 0)
				lineNo = arguments[0].GetIntValue(exm);
			if (lineNo < 0)
				// throw new CodeEE("引数を0未満にできません");
				throw new CodeEE(string.Format(trerror.ArgIsNegative.Text, Name, 1, lineNo));
			ConsoleDisplayLine[] dispLines = exm.Console.GetDisplayLines(lineNo);
			if (dispLines == null)
				return "";
			return HtmlManager.DisplayLine2Html(dispLines, true);
		}
	}

	private sealed class HtmlPopPrintingStrMethod : FunctionMethod
	{
		public HtmlPopPrintingStrMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [];
			CanRestructure = false;
		}

		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			ConsoleDisplayLine[] dispLines = exm.Console.PopDisplayingLines();
			if (dispLines == null)
				return "";
			return HtmlManager.DisplayLine2Html(dispLines, false);
		}
	}

	private sealed class HtmlToPlainTextMethod : FunctionMethod
	{
		public HtmlToPlainTextMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return HtmlManager.Html2PlainText(arguments[0].GetStrValue(exm));
		}
	}
	private sealed class HtmlEscapeMethod : FunctionMethod
	{
		public HtmlEscapeMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return HtmlManager.Escape(arguments[0].GetStrValue(exm));
		}
	}
	#endregion

	#region 画像処理系
	/// <summary>
	/// argNo番目の引数をGraphicsImageのIDを示す整数値として読み取り、 GraphicsImage又はnullを返す。
	/// </summary>
	private static GraphicsImage ReadGraphics(string Name, ExpressionMediator exm, List<AExpression> arguments, int argNo)
	{
		long target = arguments[argNo].GetIntValue(exm);
		if (target < 0)//funcname + "関数:GraphicsIDに負の値(" + target.ToString() + ")が指定されました"
					   // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGraphicsID0, Name, target));
			throw new CodeEE(string.Format(trerror.GIdIsNegative.Text, Name, target));
		else if (target > int.MaxValue)//funcname + "関数:GraphicsIDの値(" + target.ToString() + ")が大きすぎます"
									   // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGraphicsID1, Name, target));
			throw new CodeEE(string.Format(trerror.GIdIsTooLarge.Text, Name, target));
		return AppContents.GetGraphics((int)target);
	}
	/// <summary>
	/// 引数で指定したIDのGraphicsImageを読み取り、 GraphicsImage又はnullを返す。
	/// </summary>
	public static GraphicsImage ReadGraphics(int target)
	{
		if (target < 0)//funcname + "関数:GraphicsIDに負の値(" + target.ToString() + ")が指定されました"
					   // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGraphicsID0, Name, target));
			throw new CodeEE(string.Format(trerror.GIdIsNegative.Text, "HTML_PRINT", target));
		else if (target > int.MaxValue)//funcname + "関数:GraphicsIDの値(" + target.ToString() + ")が大きすぎます"
									   // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGraphicsID1, Name, target));
			throw new CodeEE(string.Format(trerror.GIdIsTooLarge.Text, "HTML_PRINT", target));
		return AppContents.GetGraphics(target);
	}

	/// <summary>
	/// argNo番目の引数を整数値として読み取り、 アルファ値を含むColor構造体にして返す。
	/// </summary>
	private static Color ReadColor(string Name, ExpressionMediator exm, List<AExpression> arguments, int argNo)
	{
		long c64 = arguments[argNo].GetIntValue(exm);
		if (c64 < 0 || c64 > 0xFFFFFFFF)
			// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodColorARGB0, Name, c64));
			throw new CodeEE(string.Format(trerror.InvalidColorARGB.Text, Name, c64));
		return Color.FromArgb((int)(c64 >> 24) & 0xFF, (int)(c64 >> 16) & 0xFF, (int)(c64 >> 8) & 0xFF, (int)c64 & 0xFF);
	}

	/// <summary>
	/// argNo番目を含む2つの引数を整数値として読み取り、Point形式にして返す。
	/// </summary>
	private static Point ReadPoint(string Name, ExpressionMediator exm, List<AExpression> arguments, int argNo)
	{
		long x64 = arguments[argNo].GetIntValue(exm);
		if (x64 < int.MinValue || x64 > int.MaxValue)
			// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name,x64, argNo+1));
			throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, argNo + 1, x64, int.MinValue, int.MaxValue));
		long y64 = arguments[argNo + 1].GetIntValue(exm);
		if (y64 < int.MinValue || y64 > int.MaxValue)
			// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name,y64, argNo+1+1));
			throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, argNo + 2, y64, int.MinValue, int.MaxValue));
		return new Point((int)x64, (int)y64);
	}

	/// <summary>
	/// argNo番目を含む4つの引数を整数値として読み取り、Rectangle形式にして返す。
	/// </summary>
	private static Rectangle ReadRectangle(string Name, ExpressionMediator exm, List<AExpression> arguments, int argNo)
	{
		long x64 = arguments[argNo].GetIntValue(exm);
		if (x64 < int.MinValue || x64 > int.MaxValue)
			// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, x64, argNo + 1));
			throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, argNo + 1, x64, int.MinValue, int.MaxValue));
		long y64 = arguments[argNo + 1].GetIntValue(exm);
		if (y64 < int.MinValue || y64 > int.MaxValue)
			// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, y64, argNo + 1 + 1));
			throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, argNo + 2, y64, int.MinValue, int.MaxValue));

		long w64 = arguments[argNo + 2].GetIntValue(exm);
		if (w64 < int.MinValue || w64 > int.MaxValue || w64 == 0)
			// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, w64, argNo + 2 + 1));
			throw new CodeEE(string.Format(trerror.ArgIsOutOfRangeExcept.Text, Name, argNo + 3, w64, int.MinValue, int.MaxValue, 0));
		long h64 = arguments[argNo + 3].GetIntValue(exm);
		if (h64 < int.MinValue || h64 > int.MaxValue || h64 == 0)
			// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, h64, argNo + 3 + 1));
			throw new CodeEE(string.Format(trerror.ArgIsOutOfRangeExcept.Text, Name, argNo + 4, h64, int.MinValue, int.MaxValue, 0));
		return new Rectangle((int)x64, (int)y64, (int)w64, (int)h64);
	}

	/// <summary>
	/// argNo番目の引数を5x5のカラーマトリクス配列変数として読み取り、 5x5のfloat[][]形式にして返す。
	/// </summary>
	private static float[][] ReadColormatrix(string Name, ExpressionMediator exm, List<AExpression> arguments, int argNo)
	{
		//数値型二次元以上配列変数のはず
		FixedVariableTerm p = ((VariableTerm)arguments[argNo]).GetFixedVariableTerm(exm);
		long e1, e2;
		float[][] cm = new float[5][];
		if (p.Identifier.IsArray2D)
		{
			long[,] array;
			if (p.Identifier.IsCharacterData)
			{
				array = p.Identifier.GetArrayChara((int)p.Index1) as long[,];
				e1 = p.Index2;
				e2 = p.Index3;
			}
			else
			{
				array = p.Identifier.GetArray() as long[,];
				e1 = p.Index1;
				e2 = p.Index2;
			}
			if (e1 < 0 || e2 < 0 || e1 + 5 > array.GetLength(0) || e2 + 5 > array.GetLength(1))
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGColorMatrix0, Name, e1, e2));
				throw new CodeEE(string.Format(trerror.InvalidColorMatrix.Text, Name, e1, e2));
			for (int x = 0; x < 5; x++)
			{
				cm[x] = new float[5];
				for (int y = 0; y < 5; y++)
				{
					cm[x][y] = array[e1 + x, e2 + y] / 256f;
				}
			}
		}
		if (p.Identifier.IsArray3D)
		{
			long[,,] array; long e3;
			if (p.Identifier.IsCharacterData)
			{
				throw new NotImplCodeEE();
			}
			else
			{
				array = p.Identifier.GetArray() as long[,,];
				e1 = p.Index1;
				e2 = p.Index2;
				e3 = p.Index3;
			}
			if (e1 < 0 || e1 >= array.GetLength(0))
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGColorMatrix0, Name, e2, e3));
				throw new CodeEE(string.Format(trerror.InvalidColorMatrix.Text, Name, e2, e3));
			if (e2 < 0 || e3 < 0 || e2 + 5 > array.GetLength(1) || e3 + 5 > array.GetLength(2))
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGColorMatrix0, Name, e2, e3));
				throw new CodeEE(string.Format(trerror.InvalidColorMatrix.Text, Name, e2, e3));
			for (int x = 0; x < 5; x++)
			{
				cm[x] = new float[5];
				for (int y = 0; y < 5; y++)
				{
					cm[x][y] = array[e1, e2 + x, e3 + y] / 256f;
				}
			}
		}
		return cm;
	}

	public sealed class GraphicsStateMethod : FunctionMethod
	{
		public GraphicsStateMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			switch (Name)
			{
				case "GCREATED":
					return 1;
				case "GWIDTH":
					return g.Width;
				case "GHEIGHT":
					return g.Height;
				#region EE_GDRAWTEXTに付随する要素
				case "GGETFONTSIZE":
					return g.Fontsize;
				case "GGETFONTSTYLE":
					return g.Fontstyle;
				case "GGETPEN":
					return g.Pen.Color.ToArgb() & 0xffffffffL;
				case "GGETPENWIDTH":
					return (long)g.Pen.Width;
				case "GGETBRUSH":
					SolidBrush b = (SolidBrush)g.Brush;
					return b.Color.ToArgb() & 0xffffffffL;
					#endregion
			}
			throw new ExeEE("GraphicsState:" + Name + ":異常な分岐");
		}
	}
	#region EE_GGETFONT
	public sealed class GraphicsStateStrMethod : FunctionMethod
	{
		public GraphicsStateStrMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return "";
			switch (Name)
			{
				case "GGETFONT":
					return g.Fontname;
			}
			throw new ExeEE("GraphicsState:" + Name + ":Abnormal branching");
		}
	}
	#endregion

	public sealed class GraphicsGetColorMethod : FunctionMethod
	{
		public GraphicsGetColorMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			//失敗したら負の値を返す。他と戻り値違うけど仕方ないね
			if (!g.IsCreated)
				return -1;
			Point p = ReadPoint(Name, exm, arguments, 1);
			if (p.X < 0 || p.X >= g.Width || p.X < 0 || p.Y >= g.Height)
				return -1;
			Color c = g.GGetColor(p.X, p.Y);
			//Color.ToArgb()はInt32の負の値をとることがあり、Int64にうまく変換できない?（と思ったが気のせいだった
			return c.ToArgb() & 0xFFFFFFFFL;
		}
	}

	public sealed class GraphicsSetColorMethod : FunctionMethod
	{
		public GraphicsSetColorMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			Color c = ReadColor(Name, exm, arguments, 1);
			Point p = ReadPoint(Name, exm, arguments, 2);
			if (p.X < 0 || p.X >= g.Width || p.X < 0 || p.Y >= g.Height)
				return 0;
			g.GSetColor(c, p.X, p.Y);
			return 1;
		}
	}

	public sealed class GraphicsSetBrushMethod : FunctionMethod
	{
		public GraphicsSetBrushMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			Color c = ReadColor(Name, exm, arguments, 1);
			g.GSetBrush(new SolidBrush(c));
			return 1;
		}
	}

	#region EE_GDRAWTEXT追加に伴いGSETFONTを改良
	public sealed class GraphicsSetFontMethod : FunctionMethod
	{
		public GraphicsSetFontMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(Int64) };
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 2 }
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count > 2)
		//		return null;
		//	return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			string fontname = arguments[1].GetStrValue(exm);
			long fontsize = arguments[2].GetIntValue(exm);
			FontStyle fs = FontStyle.Regular;
			if (arguments.Count > 3)
			{
				long style = arguments[3].GetIntValue(exm);

				if ((style & 1) != 0)
					fs |= FontStyle.Bold;
				if ((style & 2) != 0)
					fs |= FontStyle.Italic;
				if ((style & 4) != 0)
					fs |= FontStyle.Strikeout;
				if ((style & 8) != 0)
					fs |= FontStyle.Underline;
			}

			Font styledFont;
			try
			{
				#region EE_フォントファイル対応
				foreach (FontFamily ff in GlobalStatic.Pfc.Families)
				{
					if (ff.Name == fontname)
					{
						styledFont = new Font(ff, fontsize, fs, GraphicsUnit.Pixel);
						goto foundfont;
					}
				}
				// styledFont = new Font(fontname, fontsize, FontStyle.Regular, GraphicsUnit.Pixel);
				styledFont = new Font(fontname, fontsize, fs, GraphicsUnit.Pixel);
			}
			catch
			{
				return 0;
			}
		foundfont:
			#endregion
			// g.GSetFont(styledFont);
			g.GSetFont(styledFont, fs);
			return 1;
		}
	}
	#endregion

	public sealed class GraphicsSetPenMethod : FunctionMethod
	{
		public GraphicsSetPenMethod()
		{
			ReturnType = typeof(long);
			// 私家版のバグだと思う
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			Color c = ReadColor(Name, exm, arguments, 1);
			long width = arguments[2].GetIntValue(exm);
			g.GSetPen(new Pen(c, width));
			return 1;
		}
	}

	#region EE_GDASHSTYLE
	public sealed class GraphicsSetDashStyleMethod : FunctionMethod
	{
		public GraphicsSetDashStyleMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;

			g.GDashStyle(arguments[1].GetIntValue(exm), arguments[2].GetIntValue(exm));
			return 1;
		}
	}
	#endregion

	#region EE_GDRAWTEXT
	public sealed class GraphicsDrawStringMethod : FunctionMethod
	{
		public GraphicsDrawStringMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 2 }
				];
			CanRestructure = false;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
		//	if (arguments.Count > 4)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	if (arguments.Count != 2 && arguments.Count != 4)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);

		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);

		//		if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	if (arguments.Count <= 4)
		//		return null;
		//	return null;
		//}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			string text = arguments[1].GetStrValue(exm);
			if (arguments.Count == 2)
			{
				g.GDrawString(text, 0, 0);
			}
			else if (arguments.Count == 4)
			{
				Point p = ReadPoint(Name, exm, arguments, 2);
				g.GDrawString(text, p.X, p.Y);
			}
			//生成する画像のサイズを取得
			var bitmap = new Bitmap(16, 16);
			//Graphics canvas = Graphics.FromImage(bitmap);
			var graphics = Graphics.FromImage(bitmap);
			Font font = g.Fnt;
			if (font == null)
				font = new Font(Config.FontName, 100, GlobalStatic.Console.StringStyle.FontStyle, GraphicsUnit.Pixel);
			var size = graphics.MeasureString(text, font, int.MaxValue, StringFormat.GenericTypographic);

			//TextRenderer
			//Size tsize = TextRenderer.MeasureText(canvas, text, g.Fnt,
			//    new Size(2000, 2000), TextFormatFlags.NoPadding);
			//test用
			long[] resultArray = exm.VEvaluator.RESULT_ARRAY;
			resultArray[1] = (long)size.Width;
			resultArray[2] = (long)size.Height;
			return 1;
		}
	}
	#endregion
	#region EE_GGETTEXTSIZE
	public sealed class GraphicsGetTextSizeMethod : FunctionMethod
	{
		public GraphicsGetTextSizeMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.Int, ArgType.Int }, OmitStart = 3 }
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count > 2)
		//		return null;
		//	return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			string text = arguments[0].GetStrValue(exm);
			//生成する画像のサイズを取得
			string fontname = arguments[1].GetStrValue(exm);
			long fontsize = arguments[2].GetIntValue(exm);
			FontStyle fs = FontStyle.Regular;
			if (arguments.Count > 3)
			{
				long style = arguments[3].GetIntValue(exm);
				if ((style & 1) != 0)
					fs |= FontStyle.Bold;
				if ((style & 2) != 0)
					fs |= FontStyle.Italic;
				if ((style & 4) != 0)
					fs |= FontStyle.Strikeout;
				if ((style & 8) != 0)
					fs |= FontStyle.Underline;
			}
			Font fnt = new(fontname, fontsize, fs, GraphicsUnit.Pixel);
			var bitmap = new Bitmap(16, 16);
			//Graphics canvas = Graphics.FromImage(bitmap);
			var graphics = Graphics.FromImage(bitmap);
			var size = graphics.MeasureString(text, fnt, int.MaxValue, StringFormat.GenericTypographic);

			//TextRenderer
			//Size tsize = TextRenderer.MeasureText(canvas, text, fnt,
			//    new Size(2000, 2000), TextFormatFlags.NoPadding);
			long[] resultArray = exm.VEvaluator.RESULT_ARRAY;
			//resultArray[1] = (Int64)tsize.Width;
			resultArray[1] = (long)size.Height;
			return (long)size.Width;
		}
	}
	#endregion
	#region EE_GDRAWGWITHROTATE
	// 使われてない
	//public sealed class GraphicsRotateMethod : FunctionMethod
	//{
	//	public GraphicsRotateMethod()
	//	{
	//		ReturnType = typeof(Int64);
	//		argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
	//		CanRestructure = false;
	//	}
	//	public override string CheckArgumentType(string name, IOperandTerm[] arguments)
	//	{
	//		if (arguments.Count < 2)
	//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
	//		if (arguments.Count > 4)
	//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
	//		if (arguments.Count != 2 && arguments.Count != 4)
	//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);
	//		return null;
	//	}
	//	public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
	//	{
	//		if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
	//			throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
	//		GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
	//		if (!g.IsCreated)
	//			return 0;
	//		Int64 angle = arguments[1].GetIntValue(exm);

	//		//座標省略してたらx/2,y/2で渡す
	//		if (arguments.Count == 2)
	//		{
	//			g.GRotate(angle, g.Width / 2, g.Height / 2);
	//		}
	//		else
	//		{
	//			Point p = ReadPoint(Name, exm, arguments, 2);
	//			g.GRotate(angle, p.X, p.Y);
	//		}
	//		return 1;
	//	}
	//}
	public sealed class GraphicsDrawGWithRotateMethod : FunctionMethod
	{
		public GraphicsDrawGWithRotateMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 3 }
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 3)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 3);
		//	if (arguments.Count > 5)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	if (arguments.Count != 3 && arguments.Count != 5)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage dest = ReadGraphics(Name, exm, arguments, 0);
			if (!dest.IsCreated)
				return 0;
			GraphicsImage src = ReadGraphics(Name, exm, arguments, 1);
			if (!src.IsCreated)
				return 0;
			long angle = arguments[2].GetIntValue(exm);

			//座標省略してたらx/2,y/2で渡す
			if (arguments.Count == 3)
			{
				dest.GDrawGWithRotate(src, angle, src.Width / 2, src.Height / 2);
			}
			else
			{
				Point p = ReadPoint(Name, exm, arguments, 3);
				dest.GDrawGWithRotate(src, angle, p.X, p.Y);
			}
			return 1;
		}
	}
	#endregion
	#region EE_失敗作
	//brushの参照がうまくいかないので保留
	/**
	public sealed class GraphicsGetBrushMethod : FunctionMethod
	{
		public GraphicsGetBrushMethod()
		{
			ReturnType = typeof(Int64);
			 argumentTypeArray = [typeof(Int64)];
			CanRestructure = false;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			Color c = 
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			return (SolidBrush());
		}
	}
	**/
	#endregion
	#region EE_GDRAWLINE
	public sealed class GraphicsDrawLineMethod : FunctionMethod
	{
		public GraphicsDrawLineMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			Point fromP = ReadPoint(Name, exm, arguments, 1);
			Point forP = ReadPoint(Name, exm, arguments, 3);
			g.GDrawLine(fromP.X, fromP.Y, forP.X, forP.Y);
			return 1;
		}
	}
	#endregion

	public sealed class SpriteStateMethod : FunctionMethod
	{
		public SpriteStateMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string imgname = arguments[0].GetStrValue(exm);
			ASprite img = AppContents.GetSprite(imgname);
			if (img == null || !img.IsCreated)
				return 0;
			switch (Name)
			{
				case "SPRITECREATED":
					return 1;
				case "SPRITEWIDTH":
					return img.DestBaseSize.Width;
				case "SPRITEHEIGHT":
					return img.DestBaseSize.Height;
				case "SPRITEPOSX":
					return img.DestBasePosition.X;
				case "SPRITEPOSY":
					return img.DestBasePosition.Y;
			}
			throw new ExeEE("SpriteStateMethod:" + Name + ":異常な分岐");
		}
	}

	public sealed class SpriteSetPosMethod : FunctionMethod
	{
		public SpriteSetPosMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string imgname = arguments[0].GetStrValue(exm);
			ASprite img = AppContents.GetSprite(imgname);
			if (img == null || !img.IsCreated)
				return 0;
			Point p = ReadPoint(Name, exm, arguments, 1);
			switch (Name)
			{
				case "SPRITEMOVE":
					img.DestBasePosition.Offset(p);
					return 1;
				case "SPRITESETPOS":
					img.DestBasePosition = p;
					return 1;
			}
			throw new ExeEE("SpriteStateMethod:" + Name + ":異常な分岐");
		}
	}

	public sealed class SpriteGetColorMethod : FunctionMethod
	{
		public SpriteGetColorMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string imgname = arguments[0].GetStrValue(exm);
			ASprite img = AppContents.GetSprite(imgname);
			//他と違って失敗は0ではなく負の値
			if (img == null || !img.IsCreated)
				return -1;
			Point p = ReadPoint(Name, exm, arguments, 1);
			if (p.X < 0 || p.X >= img.DestBaseSize.Width)
				return -1;
			if (p.Y < 0 || p.Y >= img.DestBaseSize.Height)
				return -1;
			Color c = img.SpriteGetColor(p.X, p.Y);
			//Color.ToArgb()はInt32の負の値をとることがあり、Int64にうまく変換できない？（と思ったが気のせいだった
			//return ((long)c.A) << 24 + c.R << 16 + c.G << 8 + c.B;
			return c.ToArgb() & 0xFFFFFFFFL;
		}
	}

	public sealed class ClientSizeMethod : FunctionMethod
	{
		public ClientSizeMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			switch (Name)
			{
				case "CLIENTWIDTH":
					return exm.Console.ClientWidth;
				case "CLIENTHEIGHT":
					return exm.Console.ClientHeight;
			}
			throw new ExeEE("ClientSize:" + Name + ":異常な分岐");
		}
	}

	public sealed class GraphicsCreateMethod : FunctionMethod
	{
		public GraphicsCreateMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (g.IsCreated)
				return 0;

			Point p = ReadPoint(Name, exm, arguments, 1);
			int width = p.X; int height = p.Y;
			if (width <= 0)//{0}関数:GraphicsのWidthに0以下の値({1})が指定されました
						   // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth0, Name, width));
				throw new CodeEE(string.Format(trerror.GParamIsNegative.Text, Name, "Width", width));
			else if (width > AbstractImage.MAX_IMAGESIZE)//{0}関数:GraphicsのWidthに{2}以上の値({1})が指定されました
														 // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth1, Name, width, AbstractImage.MAX_IMAGESIZE));
				throw new CodeEE(string.Format(trerror.GParamTooLarge.Text, Name, "Width", AbstractImage.MAX_IMAGESIZE, width));
			if (height <= 0)//{0}関数:GraphicsのHeightに0以下の値({1})が指定されました
							// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight0, Name, height));
				throw new CodeEE(string.Format(trerror.GParamIsNegative.Text, Name, "Height", height));
			else if (height > AbstractImage.MAX_IMAGESIZE)//{0}関数:GraphicsのHeightに{2}以上の値({1})が指定されました
														  // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight1, Name, height, AbstractImage.MAX_IMAGESIZE));
				throw new CodeEE(string.Format(trerror.GParamTooLarge.Text, Name, "Height", AbstractImage.MAX_IMAGESIZE, height));

			g.GCreate(width, height, false);
			return 1;

		}
	}

	public sealed class GraphicsCreateFromFileMethod : FunctionMethod
	{
		public GraphicsCreateFromFileMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.Int }, OmitStart = 2 }
				];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (g.IsCreated)
				return 0;

			string filename = arguments[1].GetStrValue(exm);
			bool isRelative = false;
			if (arguments.Count > 2)
				isRelative = arguments[2].GetIntValue(exm) != 0;

			Bitmap bmp = null;
			try
			{
				string filepath = filename;
				if (!Path.IsPathRooted(filepath))
				{
					if (isRelative)
						filepath = filename;
					else
						//andEmuera: ERB が '\' 区切りで書いていても Android で解決できるよう区切りを揃える。
						//GetValidPath と違いサンドボックスはしない (上流は絶対パス指定も許している)
						filepath = Program.ContentDir + PortablePath.Normalize(filename);
				}
				if (!File.Exists(filepath))
					return 0;
				#region EM_私家版_webp
				// bmp = new Bitmap(filepath);
				bmp = Utils.LoadImage(filepath);
				if (bmp == null) return 0;
				#endregion
				if (bmp.Width > AbstractImage.MAX_IMAGESIZE || bmp.Height > AbstractImage.MAX_IMAGESIZE)
					return 0;
				g.GCreateFromF(bmp, Config.TextDrawingMode == TextDrawingMode.WINAPI);
			}
			catch (Exception e)
			{
				if (e is CodeEE)
					throw;
			}
			finally
			{
				if (bmp != null)
					bmp.Dispose();
			}
			//画像ファイルではなかった、などによる失敗
			if (!g.IsCreated)
				return 0;
			return 1;
		}
	}

	public sealed class GraphicsDisposeMethod : FunctionMethod
	{
		public GraphicsDisposeMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			g.GDispose();
			return 1;
		}
	}
	/// <summary>
	/// SPRITECREATE(str imgName, int gID, int x, int y, int width, int height)
	/// SPRITECREATE(str imgName, int gID)
	/// </summary>
	public sealed class SpriteCreateMethod : FunctionMethod
	{
		public SpriteCreateMethod()
		{
			ReturnType = typeof(long);
			//  argumentTypeArray = [typeof(string)];
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int } },
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int } },
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{

		//	if (arguments.Count < 2)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
		//	if (arguments.Count > 6)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	if (arguments[0] == null)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, 0 + 1);
		//	if (arguments[1] == null)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, 1 + 1);
		//	if (arguments[0].GetOperandType() != typeof(string))
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, 0 + 1);
		//	if (arguments[1].GetOperandType() != typeof(Int64))
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, 1 + 1);
		//	if (arguments.Count == 2)
		//		return null;
		//	if (arguments.Count != 6)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);
		//	for (int i = 2; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
		//		if (arguments[i].GetOperandType() != typeof(Int64))
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			string imgname = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(imgname))
				return 0;
			ASprite img = AppContents.GetSprite(imgname);
			if (img != null && img.IsCreated)
				return 0;
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 1);
			if (!g.IsCreated)
				return 0;

			Rectangle rect = new(0, 0, g.Width, g.Height);
			if (arguments.Count == 6)
			{//四角形は正でも負でもよいが親画像の外を指してはいけない
				rect = ReadRectangle(Name, exm, arguments, 2);
				#region EM_私家版_SPRITECREATE範囲制限緩和
				//if (rect.X + rect.Width < 0 || rect.X + rect.Width > g.Width || rect.Y + rect.Height < 0 || rect.Y + rect.Height > g.Height)
				//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodCIMGCreateOutOfRange0, Name));

				if (!rect.IntersectsWith(new Rectangle(0, 0, g.Width, g.Height)))
					// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodCIMGCreateOutOfRange0, Name));
					throw new CodeEE(string.Format(trerror.ImgRefOutOfRange.Text, Name));
				#endregion
			}
			AppContents.CreateSpriteG(imgname, g, rect);
			return 1;
		}
	}

	public sealed class SpriteDisposeMethod : FunctionMethod
	{
		public SpriteDisposeMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string imgname = arguments[0].GetStrValue(exm);
			ASprite img = AppContents.GetSprite(imgname);
			if (img == null || !img.IsCreated)
				return 0;
			AppContents.SpriteDispose(imgname);
			return 1;
		}
	}

	public sealed class SpriteDisposeAllMethod : FunctionMethod
	{
		public SpriteDisposeAllMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return AppContents.SpriteDisposeAll(arguments[0].GetIntValue(exm) != 0);
		}
	}


	/// <summary>
	/// GCLEAR(int ID, int cARGB)
	/// </summary>
	public sealed class GraphicsClearMethod : FunctionMethod
	{
		#region EM_私家版_GCLEAR拡張
		public GraphicsClearMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int } },
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int } }
				];
			argumentTypeArray = null;
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{

		//	if (arguments.Count != 2 && arguments.Count != 6)
		//		return string.Format("{0}関数には2つもしくは6つの引数が必要です", name);
		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
		//		if (arguments[i].GetOperandType() != typeof(Int64))
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			Color c = ReadColor(Name, exm, arguments, 1);
			if (!g.IsCreated)
				return 0;
			if (arguments.Count == 2)
				g.GClear(c);
			else
				g.GClear(c, (int)arguments[2].GetIntValue(exm), (int)arguments[3].GetIntValue(exm), (int)arguments[4].GetIntValue(exm), (int)arguments[5].GetIntValue(exm));
			return 1;
		}
		#endregion
	}

	/// <summary>
	/// GFILLRECTANGLE(int ID, int cARGB, int x, int y, int width, int height)
	/// </summary>
	public sealed class GraphicsFillRectangleMethod : FunctionMethod
	{
		public GraphicsFillRectangleMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;
			Rectangle rect = ReadRectangle(Name, exm, arguments, 1);
			g.GFillRectangle(rect);
			return 1;
		}
	}

	/// <summary>
	/// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight)
	/// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight, var CM)
	/// </summary>
	public sealed class GraphicsDrawGMethod : FunctionMethod
	{
		public GraphicsDrawGMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int,
							ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int,
							ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.RefInt2D | ArgType.AllowConstRef }, OmitStart = 10 },
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int,
							ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int,
							ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.RefInt3D | ArgType.AllowConstRef }, OmitStart = 10 },
				];
			CanRestructure = false;
			HasUniqueRestructure = true;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 10)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 10);
		//	if (arguments.Count > 11)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	for (int i = 0; i < 10; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
		//		if (typeof(Int64) != arguments[i].GetOperandType())
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	if (arguments.Count == 10)
		//		return null;
		//	if (!(arguments[10] is VariableTerm varToken) || !varToken.IsInteger || (!varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D))
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodGraphicsColorMatrix0, name);
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage dest = ReadGraphics(Name, exm, arguments, 0);
			if (!dest.IsCreated)
				return 0;
			GraphicsImage src = ReadGraphics(Name, exm, arguments, 1);
			if (!src.IsCreated)
				return 0;
			Rectangle destRect = ReadRectangle(Name, exm, arguments, 2);
			Rectangle srcRect = ReadRectangle(Name, exm, arguments, 6);
			if (arguments.Count == 10 || arguments[10] == null)
			{
				dest.GDrawG(src, destRect, srcRect);
				return 1;
			}
			float[][] cm = ReadColormatrix(Name, exm, arguments, 10);
			dest.GDrawG(src, destRect, srcRect, cm);
			return 1;
		}

		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			for (int i = 0; i < arguments.Count; i++)
			{
				if (arguments[i] == null)
					continue;
				//11番目の引数はColorMatrixの配列を指しているので定数にしてはいけない
				if (i == 10)
					arguments[i].Restructure(exm);
				else
					arguments[i] = arguments[i].Restructure(exm);
			}
			return false;
		}
	}

	/// <summary>
	/// GDRAWGWITHMASK(int ID, int srcID, int maskID, int destX, int destY)
	/// </summary>
	public sealed class GraphicsDrawGWithMaskMethod : FunctionMethod
	{
		public GraphicsDrawGWithMaskMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}


		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage dest = ReadGraphics(Name, exm, arguments, 0);
			if (!dest.IsCreated)
				return 0;
			GraphicsImage src = ReadGraphics(Name, exm, arguments, 1);
			if (!src.IsCreated)
				return 0;
			GraphicsImage mask = ReadGraphics(Name, exm, arguments, 2);
			if (!mask.IsCreated)
				return 0;
			if (src.Width != mask.Width || src.Height != mask.Height)
				return 0;
			Point destPoint = ReadPoint(Name, exm, arguments, 3);
			if (destPoint.X + src.Width > dest.Width || destPoint.Y + src.Height > dest.Height)
				return 0;
			dest.GDrawGWithMask(src, mask, destPoint);
			return 1;
		}


	}

	/// <summary>
	/// GDRAWCIMG(int ID, str imgName)
	/// GDRAWCIMG(int ID, str imgName, int destX, int destY)
	/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight)
	/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight, var CM)
	/// </summary>
	public sealed class GraphicsDrawSpriteMethod : FunctionMethod
	{
		public GraphicsDrawSpriteMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String } },
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.Int, ArgType.Int } },
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.RefInt2D | ArgType.AllowConstRef }, OmitStart = 6 },
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.RefInt3D | ArgType.AllowConstRef }, OmitStart = 6 },
				];
			CanRestructure = false;
			HasUniqueRestructure = true;
		}

		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{
		//	if (arguments.Count < 2)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
		//	if (arguments.Count > 7)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	if (arguments.Count != 2 && arguments.Count != 4 && arguments.Count != 6 && arguments.Count != 7)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);

		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);

		//		if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	if (arguments.Count <= 6)
		//		return null;
		//	if (!(arguments[6] is VariableTerm varToken) || !varToken.IsInteger || (!varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D))
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodGraphicsColorMatrix0, name);
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage dest = ReadGraphics(Name, exm, arguments, 0);
			if (!dest.IsCreated)
				return 0;

			string imgname = arguments[1].GetStrValue(exm);
			ASprite img = AppContents.GetSprite(imgname);
			if (img == null || !img.IsCreated)
				return 0;

			Rectangle destRect = new(0, 0, img.DestBaseSize.Width, img.DestBaseSize.Height);
			if (arguments.Count == 2)
			{
				dest.GDrawCImg(img, destRect);
				return 1;
			}
			if (arguments.Count == 4)
			{
				Point p = ReadPoint(Name, exm, arguments, 2);
				destRect.X = p.X;
				destRect.Y = p.Y;
				dest.GDrawCImg(img, destRect);
				return 1;
			}
			if (arguments.Count == 6)
			{
				destRect = ReadRectangle(Name, exm, arguments, 2);
				dest.GDrawCImg(img, destRect);
				return 1;
			}
			//if (arguments.Count == 7)
			destRect = ReadRectangle(Name, exm, arguments, 2);
			float[][] cm = ReadColormatrix(Name, exm, arguments, 6);
			dest.GDrawCImg(img, destRect, cm);
			return 1;
		}

		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			for (int i = 0; i < arguments.Count; i++)
			{
				if (arguments[i] == null)
					continue;
				//7番目の引数はColorMatrixの配列を指しているので定数にしてはいけない
				if (i == 6)
					arguments[i].Restructure(exm);
				else
					arguments[i] = arguments[i].Restructure(exm);
			}
			return false;
		}
	}

	/// <summary>
	/// int SPRITEANIMECREATE (string name, int width, int height)
	/// </summary>
	public sealed class SpriteAnimeCreateMethod : FunctionMethod
	{
		public SpriteAnimeCreateMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			string imgname = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(imgname))
				return 0;
			//リソースチェック・既に存在しているならば失敗
			ASprite img = AppContents.GetSprite(imgname);
			if (img != null && img.IsCreated)
				return 0;
			Point pos = ReadPoint(Name, exm, arguments, 1);
			if (pos.X <= 0)//{0}関数:GraphicsのWidthに0以下の値({1})が指定されました
						   // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth0, Name, pos.X));
				throw new CodeEE(string.Format(trerror.GParamIsNegative.Text, Name, "Width", pos.X));
			else if (pos.X > AbstractImage.MAX_IMAGESIZE)//{0}関数:GraphicsのWidthに{2}以上の値({1})が指定されました
														 // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth1, Name, pos.X, AbstractImage.MAX_IMAGESIZE));
				throw new CodeEE(string.Format(trerror.GParamTooLarge.Text, Name, "Width", AbstractImage.MAX_IMAGESIZE, pos.X));
			if (pos.Y <= 0)//{0}関数:GraphicsのHeightに0以下の値({1})が指定されました
						   // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight0, Name, pos.Y));
				throw new CodeEE(string.Format(trerror.GParamIsNegative.Text, Name, "Height", pos.Y));
			else if (pos.Y > AbstractImage.MAX_IMAGESIZE)//{0}関数:GraphicsのHeightに{2}以上の値({1})が指定されました
														 // throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight1, Name, pos.Y, AbstractImage.MAX_IMAGESIZE));
				throw new CodeEE(string.Format(trerror.GParamTooLarge.Text, Name, "Height", AbstractImage.MAX_IMAGESIZE, pos.Y));
			AppContents.CreateSpriteAnime(imgname, pos.X, pos.Y);
			return 1;
		}
	}


	/// <summary>
	/// SPRITEANIMEADDFRAME (string name, int graphID, int x, int y, int width, int height, int offsetx, int offsety, int delay)
	/// </summary>
	public sealed class SpriteAnimeAddFrameMethod : FunctionMethod
	{
		public SpriteAnimeAddFrameMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(long), typeof(long), typeof(long), typeof(long), typeof(long), typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}

		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			string imgname = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(imgname))
				return 0;
			if (AppContents.GetSprite(imgname) == null)
				return 0;
			SpriteAnime img = AppContents.GetSprite(imgname) as SpriteAnime;
			if (img == null && !img.IsCreated)
				return 0;
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 1);
			if (!g.IsCreated)
				return 0;
			Rectangle rect = ReadRectangle(Name, exm, arguments, 2);
			//四角形は正でなければならず、かつ親画像の外を指してはいけない
			if (rect.Width <= 0 || rect.Height <= 0 ||
				rect.X < 0 || rect.X + rect.Width > g.Width || rect.Y < 0 || rect.Y + rect.Height > g.Height)
				return 0;
			//throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodCIMGCreateOutOfRange0, Name));
			Point offset = ReadPoint(Name, exm, arguments, 6);
			long delay = arguments[8].GetIntValue(exm);
			if (delay <= 0 || delay > int.MaxValue)
				return 0;
			img.AddFrame(g, rect, offset, (int)delay);
			return 1;
		}
	}


	/// <summary>
	/// CBGCLEAR
	/// </summary>
	public sealed class CBGClearMethod : FunctionMethod
	{
		public CBGClearMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
			//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
			exm.Console.CBG_Clear();
			return 1;
		}
	}

	/// <summary>
	/// CBGREMOVERANGE(int zmin, int zmax)
	/// </summary>
	public sealed class CBGRemoveRangeMethod : FunctionMethod
	{
		public CBGRemoveRangeMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{

			long x64 = arguments[0].GetIntValue(exm);
			long y64 = arguments[1].GetIntValue(exm);
			unchecked
			{
				exm.Console.CBG_ClearRange((int)x64, (int)y64);
			}
			return 1;
		}
	}
	/// <summary>
	/// CBGCLEARBUTTON
	/// </summary>
	public sealed class CBGClearButtonMethod : FunctionMethod
	{
		public CBGClearButtonMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
			//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
			exm.Console.CBG_ClearButton();
			return 1;
		}
	}
	/// <summary>
	/// CBGREMOVEBMAP
	/// </summary>
	public sealed class CBGRemoveBMapMethod : FunctionMethod
	{
		public CBGRemoveBMapMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
			//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
			exm.Console.CBG_ClearBMap();
			return 1;
		}
	}
	/// <summary>
	/// CBGSETG(int ID, int x, int y, int zdepth)
	/// </summary>
	public sealed class CBGSetGraphicsMethod : FunctionMethod
	{
		public CBGSetGraphicsMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));

			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated || g.Bitmap == null)
				return 0;
			Point p = ReadPoint(Name, exm, arguments, 1);
			long z64 = arguments[3].GetIntValue(exm);
			if (z64 < int.MinValue || z64 > int.MaxValue || z64 == 0)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, z64, 3 + 1));
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRangeExcept.Text, Name, 4, z64, int.MinValue, int.MaxValue, 0));
			exm.Console.CBG_SetGraphics(g, p.X, p.Y, (int)z64);
			return 1;

		}
	}

	/// <summary>
	/// CBGSETBMAPG(int ID, int x, int y, int zdepth)
	/// </summary>
	public sealed class CBGSetBMapGMethod : FunctionMethod
	{
		public CBGSetBMapGMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));

			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated || g.Bitmap == null)
				return 0;
			exm.Console.CBG_SetButtonMap(g);
			return 1;

		}
	}

	/// <summary>
	/// CBGSETCIMG(str imgName, int x, int y, int zdepth)
	/// </summary>
	public sealed class CBGSetCIMGMethod : FunctionMethod
	{
		public CBGSetCIMGMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string), typeof(long), typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
			//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));

			string imgname = arguments[0].GetStrValue(exm);
			ASprite img = AppContents.GetSprite(imgname);
			if (img == null || !img.IsCreated)
				return 0;
			Point p = ReadPoint(Name, exm, arguments, 1);
			long z64 = arguments[3].GetIntValue(exm);
			if (z64 < int.MinValue || z64 > int.MaxValue || z64 == 0)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, z64, 3 + 1));
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRangeExcept.Text, Name, 4, z64, int.MinValue, int.MaxValue, 0));
			if (!exm.Console.CBG_SetImage(img, p.X, p.Y, (int)z64))
				return 0;
			return 1;

		}
	}

	/// <summary>
	/// CBGSETBUTTONCIMG(int button, str imgName, str imgName, int x, int y,int zdepth str tooltipmes)
	/// </summary>
	public sealed class CBGSETButtonSpriteMethod : FunctionMethod
	{
		public CBGSETButtonSpriteMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(string), typeof(Int64), typeof(Int64), typeof(Int64), typeof(string) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.String, ArgType.String, ArgType.Int, ArgType.Int, ArgType.Int, ArgType.String }, OmitStart = 6 },
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{

		//	if (arguments.Count < 6)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 6);
		//	if (arguments.Count > 7)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	if (arguments.Count != 6 && arguments.Count != 7)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);

		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);

		//		if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));

			long b64 = arguments[0].GetIntValue(exm);
			if (b64 < 0 || b64 > 0xFFFFFF)
				return 0;
			string imgnameN = arguments[1].GetStrValue(exm);
			ASprite imgN = AppContents.GetSprite(imgnameN);
			string imgnameB = arguments[2].GetStrValue(exm);
			ASprite imgB = AppContents.GetSprite(imgnameB);

			Point p = ReadPoint(Name, exm, arguments, 3);
			long z64 = arguments[5].GetIntValue(exm);
			if (z64 < int.MinValue || z64 > int.MaxValue || z64 == 0)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, z64, 5 + 1));
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRangeExcept.Text, Name, 6, z64, int.MinValue, int.MaxValue, 0));
			string tooltip = null;
			if (arguments.Count > 6)
				tooltip = arguments[6].GetStrValue(exm);
			if (!exm.Console.CBG_SetButtonImage((int)b64, imgN, imgB, p.X, p.Y, (int)z64, tooltip))
				return 0;
			return 1;

		}
	}

	static readonly short[] keytoggle = new short[256];
	private sealed class GetKeyStateMethod : FunctionMethod
	{
		public GetKeyStateMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (!exm.Console.IsActive)//アクティブでないならスルー
				return 0;
			long keycode = arguments[0].GetIntValue(exm);
			if (keycode < 0 || keycode > 255)
				return 0;
			short s = WinInput.GetKeyState((int)keycode);
			short toggle = keytoggle[keycode];
			keytoggle[keycode] = (short)((s & 1) + 1);//初期値0、トグル状態に応じて1か2を代入。
			switch (Name)
			{
				case "GETKEY": return (s < 0) ? 1 : 0;
				case "GETKEYTRIGGERED": return (s < 0) && (toggle != keytoggle[keycode]) ? 1 : 0;//初回はtrue、2回目以降はトグル状態が前回と違う場合のみ1
			}
			throw new ExeEE("異常な分岐");
		}
	}

	private sealed class MousePosMethod : FunctionMethod
	{
		public MousePosMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			switch (Name)
			{
				case "MOUSEX": return exm.Console.GetMousePosition().X;
				case "MOUSEY": return exm.Console.GetMousePosition().Y;
			}
			throw new ExeEE("異常な名前");
		}
	}
	#region EE_MOUSEB
	private sealed class MouseButtonMethod : FunctionMethod
	{
		public MouseButtonMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//if (exm.Console.SelectingButton != null)
			//	return exm.Console.SelectingButton.ToString();
			bool b = exm.Console.AlwaysRefresh;
			Point point = exm.Console.Window.MainPicBox.PointToClient(Control.MousePosition);
			exm.Console.AlwaysRefresh = true;
			if (exm.Console.Window.MainPicBox.ClientRectangle.Contains(point))
				exm.Console.MoveMouse(point);
			exm.Console.AlwaysRefresh = b;
			if (exm.Console.PointingSring != null)
			{
				if (!exm.Console.PointingSring.IsButton)
					return "";
				if (exm.Console.PointingSring.IsInteger)
					return exm.Console.PointingSring.Input.ToString();
				return exm.Console.PointingSring.Inputs;
			}
			return "";
		}
	}
	#endregion


	private sealed class IsActiveMethod : FunctionMethod
	{
		public IsActiveMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return exm.Console.IsActive ? 1 : 0;
		}
	}

	private sealed class SetAnimeTimerMethod : FunctionMethod
	{
		public SetAnimeTimerMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long i64 = arguments[0].GetIntValue(exm);
			if (i64 < int.MinValue || i64 > short.MaxValue)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, i64, 1));
				throw new CodeEE(string.Format(trerror.ArgIsOutOfRange.Text, Name, 1, i64, int.MinValue, int.MaxValue));
			exm.Console.setRedrawTimer((int)i64);
			return 1;
		}
	}

	/// <summary>
	/// int SAVETEXT str text, int fileNo{, int force_savdir, int force_UTF8}
	/// </summary>
	private sealed class SaveTextMethod : FunctionMethod
	{
		public SaveTextMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = new Type[] { typeof(string) ,typeof(Int64), typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Any, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{

		//	if (arguments.Count < 2)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
		//	if (arguments.Count > 4)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
		//		#region EM_私家版_LoadText＆SaveText機能拡張
		//		if (i == 1 && arguments[i].GetOperandType() == typeof(string)) continue;
		//		#endregion
		//		if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	return null;
		//}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			#region EM_私家版_LoadText＆SaveText機能拡張
			//string savText = arguments[0].GetStrValue(exm);
			//Int64 i64 = arguments[1].GetIntValue(exm);
			//if (i64 < 0 || i64 > int.MaxValue)
			//	return 0;
			//bool forceSavdir = arguments.Count > 2 && (arguments[2].GetIntValue(exm) != 0);
			//bool forceUTF8 = arguments.Count > 3 && (arguments[3].GetIntValue(exm) != 0);
			//int fileIndex = (int)i64;
			//string filepath = forceSavdir ?
			//	GetSaveDataPathText(fileIndex, Config.ForceSavDir) :
			//	GetSaveDataPathText(fileIndex, Config.SavDir);
			//Encoding encoding = forceUTF8 ?
			//	Encoding.GetEncoding("UTF-8") :
			//	Config.SaveEncode;
			//try
			//{
			//	if (forceSavdir)
			//		Config.ForceCreateSavDir();
			//	else
			//		Config.Config.CreateSavDir();
			//	System.IO.File.WriteAllText(filepath, savText, encoding);
			//}
			//catch { return 0; }
			string savText = arguments[0].GetStrValue(exm), filepath;
			long i64 = -1;
			bool forceSavdir = arguments.Count > 2 && (arguments[2].GetIntValue(exm) != 0);
			bool forceUTF8 = arguments.Count > 3 && (arguments[3].GetIntValue(exm) != 0);


			if (arguments[1].GetOperandType() == typeof(long))
			{
				i64 = arguments[1].GetIntValue(exm);
				if (i64 < 0 || i64 > int.MaxValue)
					return 0;
				int fileIndex = (int)i64;
				filepath = forceSavdir ?
				GetSaveDataPathText(fileIndex, Config.ForceSavDir) :
				GetSaveDataPathText(fileIndex, Config.SavDir);
			}
			else
			{
				filepath = Utils.GetValidPath(arguments[1].GetStrValue(exm));
				if (filepath == null) return 0;
				string tmp = Path.HasExtension(filepath) ? Path.GetExtension(filepath).ToLower().Substring(1) : "";
				if (!Config.ValidExtension.Contains(tmp))
					filepath = Path.ChangeExtension(filepath, "txt");
				forceUTF8 = true;
			}

			// Encoding encoding = forceUTF8 ?
			// 	Encoding.GetEncoding("UTF-8") :
			// 	Config.SaveEncode;
			try
			{
				if (i64 >= 0)
				{
					if (forceSavdir)
						Config.ForceCreateSavDir();
					else
						Config.CreateSavDir();
				}
				else
				{
					//andEmuera: '\' 決め打ちだと Android で親フォルダが作られず、
					//"dat\人物DT_XML.txt" という名前のファイルがゲームフォルダ直下にできてしまう
					//(書き込みは成功するが LOADTEXT が読めず、周回引き継ぎが無言で空になる)
					var parent = Path.GetDirectoryName(filepath);
					if (!string.IsNullOrEmpty(parent))
						Directory.CreateDirectory(parent);
				}

				File.WriteAllText(filepath, savText, Config.SaveEncode);
			}
			catch { return 0; }
			#endregion
			return 1;
		}
	}
	/// <summary>
	/// str LOADTEXT int fileNo{, int force_savdir, int force_UTF8}
	/// </summary>
	private sealed class LoadTextMethod : FunctionMethod
	{
		public LoadTextMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64) };
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Any, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		//public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		//{

		//	if (arguments.Count < 1)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 1);
		//	if (arguments.Count > 3)
		//		return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
		//	for (int i = 0; i < arguments.Count; i++)
		//	{
		//		if (arguments[i] == null)
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
		//		#region EM_私家版_LoadText＆SaveText機能拡張
		//		if (i == 0 && arguments[i].GetOperandType() == typeof(string)) continue;
		//		#endregion
		//		if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
		//			return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
		//	}
		//	return null;
		//}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			#region EM_私家版_LoadText＆SaveText機能拡張
			//Int64 i64 = arguments[0].GetIntValue(exm);
			//if (i64 < 0 || i64 > int.MaxValue)
			//	return "";
			//bool forceSavdir = arguments.Count > 1 && (arguments[1].GetIntValue(exm) != 0);
			//bool forceUTF8 = arguments.Count > 2 && (arguments[2].GetIntValue(exm) != 0);
			//int fileIndex = (int)i64;
			//string filepath = forceSavdir ?
			//	GetSaveDataPathText(fileIndex, Config.ForceSavDir) :
			//	GetSaveDataPathText(fileIndex, Config.SavDir);
			//Encoding encoding = forceUTF8 ?
			//	Encoding.GetEncoding("UTF-8") :
			//	Config.SaveEncode;
			//if (!System.IO.File.Exists(filepath))
			//	return "";
			//string ret;
			//try
			//{
			//	ret = System.IO.File.ReadAllText(filepath, encoding);
			//}
			//catch { return ""; }
			//return ret;
			string ret = "", filepath;
			long i64 = -1;
			bool forceSavdir = arguments.Count > 1 && (arguments[1].GetIntValue(exm) != 0);
			bool forceUTF8 = arguments.Count > 2 && (arguments[2].GetIntValue(exm) != 0);
			if (arguments[0].GetOperandType() == typeof(long))
			{
				i64 = arguments[0].GetIntValue(exm);
				if (i64 < 0 || i64 > int.MaxValue)
					return "";
				int fileIndex = (int)i64;
				filepath = forceSavdir ?
				GetSaveDataPathText(fileIndex, Config.ForceSavDir) :
				GetSaveDataPathText(fileIndex, Config.SavDir);
			}
			else
			{
				filepath = Utils.GetValidPath(arguments[0].GetStrValue(exm));
				if (filepath == null) return string.Empty;
				string tmp = Path.HasExtension(filepath) ? Path.GetExtension(filepath).ToLower().Substring(1) : "";
				if (!Config.ValidExtension.Contains(tmp))
					return "";
			}

			if (!File.Exists(filepath))
				return "";
			try
			{
				ret = File.ReadAllText(filepath, EncodingHandler.DetectEncoding(filepath));
			}
			catch { return ""; }
			//一貫性の観点で\rには死んでもらう
			return ret.Replace("\r", "");
			#endregion
		}
	}



	private static string GetSaveDataPathText(int index, string dir) { return string.Format("{0}txt{1:00}.txt", dir, index); }
	private static string GetSaveDataPathGraphics(int index) { return string.Format("{0}img{1:0000}.png", Config.SavDir, index); }

	/// <summary>
	/// int GSAVE int ID, int fileNo
	/// </summary>
	public sealed class GraphicsSaveMethod : FunctionMethod
	{
		public GraphicsSaveMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (!g.IsCreated)
				return 0;

			long i64 = arguments[1].GetIntValue(exm);
			if (i64 < 0 || i64 > int.MaxValue)
				return 0;

			string filepath = GetSaveDataPathGraphics((int)i64);
			try
			{
				Config.CreateSavDir();
				g.Bitmap.Save(filepath);
			}
			catch
			{
				return 0;
			}
			return 1;
		}
	}
	/// <summary>
	/// int GLOAD int ID, int fileNo
	/// </summary>
	public sealed class GraphicsLoadMethod : FunctionMethod
	{
		public GraphicsLoadMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(long), typeof(long)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				// throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				throw new CodeEE(string.Format(trerror.GDIPlusOnly.Text, Name));
			GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
			if (g.IsCreated)
				return 0;

			long i64 = arguments[1].GetIntValue(exm);
			if (i64 < 0 || i64 > int.MaxValue)
				return 0;

			string filepath = GetSaveDataPathGraphics((int)i64);
			Bitmap bmp = null;
			try
			{
				if (!File.Exists(filepath))
					return 0;
				#region EM_私家版_webp
				// bmp = new Bitmap(filepath);
				bmp = Utils.LoadImage(filepath);
				if (bmp == null) return 0;
				#endregion
				if (bmp.Width > AbstractImage.MAX_IMAGESIZE || bmp.Height > AbstractImage.MAX_IMAGESIZE)
					return 0;
				g.GCreateFromF(bmp, Config.TextDrawingMode == TextDrawingMode.WINAPI);
			}
			catch (Exception e)
			{
				if (e is CodeEE)
					throw;
			}
			finally
			{
				if (bmp != null)
					bmp.Dispose();
			}
			if (!g.IsCreated)
				return 0;
			return 1;
		}
	}

	#endregion

	#region EE_EXISTSOUND
	private sealed class ExistSoundMethod : FunctionMethod
	{
		public ExistSoundMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string str = arguments[0].GetStrValue(exm);
			//andEmuera: '\' 決め打ち + カレントディレクトリ相対は Android で成立しない。
			//PLAYSOUND (Instraction.Child.cs) と同じ SoundDir を見る
			string filepath = Path.GetFullPath(Program.SoundDir + PortablePath.Normalize(str));
			if (File.Exists(filepath))
				return 1;
			return 0;
		}
	}
	#endregion

	#region EE_EXISTFUNCTION

	public sealed class ExistFunctionMethod : FunctionMethod
	{

		public ExistFunctionMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int}, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string functionname = arguments[0].GetStrValue(exm);
			if (arguments.Count == 1 || arguments[1].GetIntValue(exm) == 0)
			{
				// andEmuera: 大文字化は要らない。この分岐に入る条件が「辞書が大小無視」であり、
				// 辞書 (noneventLabelDic) は Config.StrComper = OrdinalIgnoreCase で引くので、
				// 事前に ToUpper しても結果は変わらない。EXISTFUNCTION はロード時対応の
				// 機能検出で何千回も呼ばれ、その ToUpper がカルチャ依存 (ICU) で効いていた
				FunctionLabelLine func = GlobalStatic.LabelDictionary.GetNonEventLabel(functionname);
				if (func == null)
					return 0;
				if (func.IsMethod)
				{
					if (func.MethodType == typeof(string))
						return 3;
					else if (func.MethodType == typeof(long))
						return 2;

				}
				return 1;
			}
			else
			{
				foreach (string funcname in GlobalStatic.Process.LabelDictionary.NoneventKeys)
				{
					// andEmuera: 上に同じ。両辺の ToUpper は大小無視の比較そのもので、
					// しかも右辺はループの外で決まっている (全候補ぶん作り直していた)
					if (string.Equals(funcname, functionname, StringComparison.OrdinalIgnoreCase))
					{
						FunctionLabelLine func = GlobalStatic.LabelDictionary.GetNonEventLabel(funcname);

						if (func.IsMethod)
						{
							if (func.MethodType == typeof(string))
								return 3;
							else if (func.MethodType == typeof(long))
								return 2;

						}
						return 1;
					}
				}
				return 0;
			}
		}
	}
	#endregion

	#region EE_GETMEMORYUSAGE
	private sealed class GetUsingMemoryMethod : FunctionMethod
	{
		public GetUsingMemoryMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			using (System.Diagnostics.Process memory = System.Diagnostics.Process.GetCurrentProcess())
			{
				return memory.WorkingSet64;
			}
		}
	}
	#endregion
	#region EE_CLEARMEMORY
	private sealed class ClearMemoryMethod : FunctionMethod
	{
		public ClearMemoryMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			using (System.Diagnostics.Process destmemory = System.Diagnostics.Process.GetCurrentProcess())
			{
				long destmemorysize = destmemory.WorkingSet64;
				GC.Collect();
				using (System.Diagnostics.Process memory = System.Diagnostics.Process.GetCurrentProcess())
				{
					return destmemorysize - memory.WorkingSet64;
				}
			}
		}
	}
	#endregion
	#region EE_textbox拡張
	private sealed class GetTextBoxMethod : FunctionMethod
	{
		public GetTextBoxMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			return exm.Console.Window.TextBox.Text;
		}
	}
	private sealed class ChangeTextBoxMethod : FunctionMethod
	{
		public ChangeTextBoxMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArray = [typeof(string)];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			exm.Console.Window.ChangeTextBox(arguments[0].GetStrValue(exm));
			return 1;
		}
	}
	#endregion
	#region EE_GETERDNAME
	private sealed class ErdNameMethod : FunctionMethod
	{
		public ErdNameMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.RefAny | ArgType.AllowConstRef, ArgType.Int, ArgType.Int }, OmitStart = 2 },
				];
			CanRestructure = true;
			HasUniqueRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			VariableTerm vToken = (VariableTerm)arguments[0];
			string varname = "";
			if (arguments.Count > 2)
				varname = vToken.Identifier.Name + "@" + arguments[2].GetIntValue(exm);
			else
				varname = vToken.Identifier.Name;
			long value = arguments[1].GetIntValue(exm);
			if (exm.VEvaluator.Constant.TryIntegerToKeyword(out string ret, value, varname))
				return ret;
			else
				return "";
		}
		public override bool UniqueRestructure(ExpressionMediator exm, List<AExpression> arguments)
		{
			arguments[1] = arguments[1].Restructure(exm);
			return arguments[1] is SingleTerm;
		}
	}
	#endregion
	#region EE_GETDISPLAYLINE
	private sealed class GetDisplayLineMethod : FunctionMethod
	{
		public GetDisplayLineMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int }},
				];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			//修正に失敗したので差し戻す
			//long num = arguments[0].GetIntValue(exm)-exm.Console.DeletedLines;
			long num = arguments[0].GetIntValue(exm);
			if (num < 0 || num >= exm.Console.DisplayLineList.Count)
				return "";
			else
				return exm.Console.DisplayLineList[(int)num].ToString();
		}
	}
	#endregion
	#region EE_GETDOINGFUNCTION
	private sealed class GetDoingFunctionMethod : FunctionMethod
	{
		public GetDoingFunctionMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = [];
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			LogicalLine line = exm.Process.GetScaningLine();
			if ((line == null) || (line.ParentLabelLine == null))
				return "";//システム待機中のデバッグモードから呼び出し
			return line.ParentLabelLine.LabelName;
		}
	}
	#endregion
	#region EE_SystemInput拡張
	private sealed class FlowInputMethod : FunctionMethod
	{
		public FlowInputMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = new ArgTypeList[] {
					new() { ArgTypes = { ArgType.Int, ArgType.Int, ArgType.Int, ArgType.Int }, OmitStart = 1 },
				};
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{

			exm.Process.flowinputDef = arguments[0].GetIntValue(exm);
			if (arguments.Count > 1)
				exm.Process.flowinput = arguments[1].GetIntValue(exm) != 0 ? true : false ;
			if (arguments.Count > 2)
				exm.Process.flowinputCanSkip = arguments[2].GetIntValue(exm) != 0 ? true : false ;
			if (arguments.Count > 3)
				exm.Process.flowinputForceSkip = arguments[3].GetIntValue(exm) != 0 ? true : false;
			return 0;
		}
	}
	private sealed class FlowInputsMethod : FunctionMethod
	{
		public FlowInputsMethod()
		{
			ReturnType = typeof(long);
			argumentTypeArrayEx = new ArgTypeList[] {
					new() { ArgTypes = { ArgType.Int, ArgType.String }, OmitStart = 1 },
				};
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{

			exm.Process.flowinputString = arguments[0].GetIntValue(exm) != 0 ? true : false ;
			if (arguments.Count > 1)
				exm.Process.flowinputDefString = arguments[1].GetStrValue(exm);
			return 0;
		}
	}
	#endregion

	#region daughter-patch追加
	private sealed class GetMethMethod : FunctionMethod
	{
		public GetMethMethod()
		{
			ReturnType = typeof(Int64);
			// argumentTypeArray = null;
			argumentTypeArrayEx = new ArgTypeList[] {
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int, ArgType.VariadicAny }, OmitStart = 1 },
				};
			CanRestructure = false;
		}

		public override Int64 GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			List<AExpression> methArgs = new List<AExpression>(arguments.Skip(2).ToArray());
			var term = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, name, methArgs, true);

			if (term == null)
			{
				if (arguments.Count < 2 || arguments[1] == null)
					throw new CodeEE(string.Format(trerror.NotDefinedUserFunc.Text, name));
				else
					return arguments[1].GetIntValue(exm);
			}
			else if (!term.IsInteger)
				throw new CodeEE(string.Format(trerror.IsNotInt.Text, name));
			else
				return term.GetIntValue(exm);
		}
	}
	private sealed class GetMethsMethod : FunctionMethod
	{
		public GetMethsMethod()
		{
			ReturnType = typeof(string);
			// argumentTypeArray = null;
			argumentTypeArrayEx = new ArgTypeList[] {
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.String, ArgType.VariadicAny }, OmitStart = 1 },
				};
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			List<AExpression> methArgs = new List<AExpression>(arguments.Skip(2).ToArray());
			var term = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, name, methArgs, true);

			if (term == null)
			{
				if (arguments.Count < 2 || arguments[1] == null)
					throw new CodeEE(string.Format(trerror.NotDefinedUserFunc.Text, name));
				else
					return arguments[1].GetStrValue(exm);
			}
			else if (!term.IsString)
				throw new CodeEE(string.Format(trerror.IsNotStr.Text, name));
			else
				return term.GetStrValue(exm);
		}
	}
	private sealed class ExistMethMethod : FunctionMethod
	{
		public ExistMethMethod()
		{
			ReturnType = typeof(Int64);
			argumentTypeArray = new Type[] { typeof(string) };
			CanRestructure = true;
		}

		public override Int64 GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string name = arguments[0].GetStrValue(exm);
			AExpression term;
			try
			{
				term = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, name, new List<AExpression>(), true);
			}
			catch (CodeEE)
			{
				return 0;
			}

			if (term == null)
			{
				return 0;
			}
			else
			{
				Int64 res = 0;
				if (term.IsInteger) res |= 1;
				if (term.IsString) res |= 2;
				return res;
			}
		}
	}

	#endregion
	//Bitmap Cache
	#region Bitmap Cache
	private sealed class BitmapCacheEnableMethod : FunctionMethod
	{
		public BitmapCacheEnableMethod()
		{
			ReturnType = typeof(long);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int }, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			long argument0 = arguments[0].GetIntValue(exm);
			GlobalStatic.Console.bitmapCacheEnabledForNextLine = argument0 != 0;
			return 0;
		}
	}
	#endregion

	//HOTKEY STATE
	private sealed class HotkeyStateMethod : FunctionMethod
	{
		public HotkeyStateMethod()
		{
			ReturnType = typeof(Int64);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int, ArgType.Int}, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			Int64 argument0 = arguments[0].GetIntValue(exm);
			Int64 argument1 = arguments[1].GetIntValue(exm);
			GlobalStatic.Console.Window.hotkeyState.HotkeyStateSet((nint)argument0, (nint)argument1);
			return 0;
		}
	}

	private sealed class HotkeyStateInitMethod : FunctionMethod
	{
		public HotkeyStateInitMethod()
		{
			ReturnType = typeof(Int64);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.Int}, OmitStart = 1 },
				];
			CanRestructure = false;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			Int64 argument0 = arguments[0].GetIntValue(exm);
			GlobalStatic.Console.Window.hotkeyState.HotkeyStateInit((nint)argument0);
			return 0;
		}
	}
	#region EE_OUTPUTLOG拡張
	private sealed class OutputlogMethod : FunctionMethod
	{
		public OutputlogMethod()
		{
			ReturnType = typeof(Int64);
			// argumentTypeArray = null;
			argumentTypeArrayEx = [
					new ArgTypeList{ ArgTypes = { ArgType.String, ArgType.Int}, OmitStart = 0 },
				];
			CanRestructure = false;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
		{
			string filename = "";
			if (arguments.Count > 0)
				filename = arguments[0].GetStrValue(exm);
			bool hideInfo = false;
			if (arguments.Count > 1)
				hideInfo = arguments[1].GetIntValue(exm) == 1;

	
			exm.Console.OutputLog(filename, hideInfo);
			return 1;
		}

	}
		#endregion
	}
