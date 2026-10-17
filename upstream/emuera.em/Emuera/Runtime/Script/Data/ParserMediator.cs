using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MinorShift.Emuera;

//1756 新設。ParserやLexicalAnalyzerなどが知りたい情報をまとめる
//本当は引数として渡すべきなのかもしれないが全てのParserの引数を書きなおすのが面倒なのでstatic
internal partial class ParserMediator
{
	static ParserMediator()
	{
		RenameDic = [];
	}
	/// <summary>
	/// emuera.config等で発生した警告
	/// Initializeより前に発生する
	/// </summary>
	/// <param name="str"></param>
	/// <param name="?"></param>
	public static void ConfigWarn(string str, ScriptPosition? pos, int level, string stack)
	{
		if (level <Config.DisplayWarningLevel && !Program.AnalysisMode)
			return;
		warningList.Add(new ParserWarning(str, pos, level, stack));
	}

	static EmueraConsole console;
	public static void Initialize(EmueraConsole console)
	{
		ParserMediator.console = console;
	}

	#region Rename
	public static Dictionary<string, string> RenameDic { get; private set; }
	//1756 Process.Load.csより移動
	public static void LoadEraExRenameFile(string filepath)
	{
		if (!File.Exists(filepath))
		{
			return;
		}
		if (RenameDic.Count > 0)
			RenameDic.Clear();

		var fileLine = File.ReadAllLines(filepath, EncodingHandler.DetectEncoding(filepath));
		ScriptPosition? pos = null;
		Regex regex = unEscapedCommaRegex();
		try
		{
			var lineNo = 0;
			foreach (var line in fileLine)
			{
				pos = new ScriptPosition(filepath, lineNo);
				if (line.StartsWith(';'))
					continue;
				var tokens = regex.Split(line);
				if (tokens.Length == 2)
				{
					//右がERB中の表記、左が変換先になる。
					string key = $"[[{tokens[1].Trim()}]]";
					string value = tokens[0].Trim();
					RenameDic[key] = value;
				}
				lineNo++;
			}
		}
		catch (Exception e)
		{
			throw new CodeEE(e.Message, pos);
		}
	}
	#endregion

	static object warningListLock = new();

	public static void Warn(string str, ScriptPosition? pos, int level)
	{
		Warn(str, pos, level, null);
	}

	public static void Warn(string str, ScriptPosition? pos, int level, string stack)
	{
		if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
			return;
		if (console != null && !console.RunERBFromMemory)
		{
			lock (warningListLock)
			{
				warningList.Add(new ParserWarning(str, pos, level, stack));
			}
		}
	}

	/// <summary>
	/// Parser中での警告出力
	/// </summary>
	/// <param name="str"></param>
	/// <param name="line"></param>
	/// <param name="level">警告レベル.0:軽微なミス.1:無視できる行.2:行が実行されなければ無害.3:致命的</param>
	public static void Warn(string str, LogicalLine line, int level, bool isError, bool isBackComp)
	{
		Warn(str, line, level, isError, isBackComp, null);
	}

	public static void Warn(string str, LogicalLine line, int level, bool isError, bool isBackComp, string stack)
	{
		if (isError)
		{
			line.IsError = true;
			line.ErrMes = str;
		}
		if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
			return;
		if (isBackComp && !Config.WarnBackCompatibility)
			return;
		if (console != null && !console.RunERBFromMemory)
			warningList.Add(new ParserWarning(str, line.Position, level, stack));
		//				console.PrintWarning(str, line.Position, level);
	}

	private static List<ParserWarning> warningList = [];

	public static bool HasWarning { get { return warningList.Count > 0; } }
	public static void ClearWarningList()
	{
		warningList.Clear();
	}

	public static void FlushWarningList()
	{
		for (int i = 0; i < warningList.Count; i++)
		{
			ParserWarning warning = warningList[i];
			console.PrintWarning(warning.WarningMes, warning.WarningPos, warning.WarningLevel);
			if (warning.StackTrace != null)
			{
				string[] stacks = warning.StackTrace.Split('\n');
				for (int j = 0; j < stacks.Length; j++)
				{
					console.PrintSystemLine(stacks[j]);
				}
			}
		}
		warningList.Clear();
	}

	private sealed class ParserWarning
	{
		public ParserWarning(string mes, ScriptPosition? pos, int level, string stackTrace)
		{
			WarningMes = mes;
			WarningPos = pos;
			WarningLevel = level;
			StackTrace = stackTrace;
		}
		public string WarningMes;
		public ScriptPosition? WarningPos;
		public int WarningLevel;
		public string StackTrace;
	}

	[GeneratedRegex(@"(?<!\\),")]
	private static partial Regex unEscapedCommaRegex();

	public static void SingleLine(string str)
	{
		console.PrintSingleLine(str);
	}

}
