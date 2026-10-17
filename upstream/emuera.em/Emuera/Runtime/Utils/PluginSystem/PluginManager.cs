using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Script;
using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using static MinorShift.Emuera.Runtime.Utils.EvilMask.Utils;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem
{
	public class PluginManager
	{

		public static PluginManager GetInstance()
		{
			if (instance == null)
			{
				instance = new PluginManager();
			}

			return instance;
		}

		private PluginManager()
		{

		}

		static private PluginManager instance;

		/// <summary>
		/// Unsafe rudimentary method to execute ERB line of code from Plugin.
		/// Instead of using this method better implement direct API call that works directly with game state, bypassing ERB interpreter
		/// </summary>
		/// <param name="line">Line of code to execute</param>
		public void ExecuteLine(string line)
		{
			var logicalLine = LogicalLineParser.ParseLine(line, null);
			InstructionLine func = (InstructionLine)logicalLine;
			ArgumentParser.SetArgumentTo(func);
			func.Function.Instruction.DoInstruction(expressionMediator, func, processState);
		}

		public void Print(string text)
		{
			expressionMediator.Console.Print(text);
		}
		public void PrintError(string text)
		{
			expressionMediator.Console.PrintError(text);
		}
		public void PrintC(string text, bool aligmentRight = false)
		{
			expressionMediator.Console.PrintC(text, aligmentRight);
		}
		public void PrintPlain(string text)
		{
			expressionMediator.Console.PrintPlain(text);
		}
		public void PrintPlainWithSingleLine(string text)
		{
			expressionMediator.Console.PrintPlainWithSingleLineFix(text);
		}
		public void PrintSingleLine(string text)
		{
			expressionMediator.Console.PrintSingleLine(text);
		}
		public void PrintSystemLine(string text)
		{
			expressionMediator.Console.PrintSystemLine(text);
		}
		public void PrintTemporaryLine(string text)
		{
			expressionMediator.Console.PrintTemporaryLine(text);
		}
		public void PrintButton(string text, long id)
		{
			expressionMediator.Console.PrintButton(text, id);
		}
		public void PrintButtonC(string text, long id, bool aligmentRight = false)
		{
			expressionMediator.Console.PrintButtonC(text, id, aligmentRight);
		}
		public void PrintBar(string text = "", bool isConst = true)
		{
			if (text == "")
			{
				expressionMediator.Console.PrintBar();
			}
			else
			{
				expressionMediator.Console.printCustomBar(text, isConst);
			}
		}
		public void PrintHtml(string htmlText, bool toBuffer = false)
		{
			expressionMediator.Console.PrintHtml(htmlText, toBuffer);
		}
		public void PrintImage(string resourceName, int width, int height, int y, string buttonResourceName = null, string mapResourceName = null)
		{
			MixedNum widthNum = new()
			{
				isPx = true,
				num = width
			};
			MixedNum heightNum = new()
			{
				isPx = true,
				num = height
			};
			MixedNum yNum = new()
			{
				isPx = true,
				num = y
			};
			expressionMediator.Console.PrintImg(resourceName, buttonResourceName, mapResourceName, heightNum, widthNum, yNum);
		}
		public void PrintNewLine()
		{
			expressionMediator.Console.NewLine();
		}
		public void FlushConsole(bool force = false)
		{
			expressionMediator.Console.PrintFlush(force);
		}
		public void DebugPrint(string text)
		{
			expressionMediator.Console.DebugPrint(text);
		}

		public void ClearDisplay()
		{
			expressionMediator.Console.ClearDisplay();
		}
		public void SetBgColor(Color color)
		{
			expressionMediator.Console.SetBgColor(color);
		}
		public void SetFont(string fontName)
		{
			expressionMediator.Console.SetFont(fontName);
		}
		public Point GetMousePosition()
		{
			return expressionMediator.Console.GetMousePosition();
		}
		public void WaitInput(bool oneInput = true, int timelimit = -1)
		{
			InputRequest request = new()
			{
				OneInput = oneInput,
				Timelimit = timelimit
			};
			expressionMediator.Console.WaitInput(request);
		}
		public void ReadAnyKey()
		{
			expressionMediator.Console.ReadAnyKey();

		}
		public void Await(int time)
		{
			expressionMediator.Console.Await(time);
		}
		public void ForceStopTimer()
		{
			expressionMediator.Console.forceStopTimer();
		}
		public void Quit(bool force = false)
		{
			if (force)
			{
				expressionMediator.Console.ForceQuit();
			}
			else
			{
				expressionMediator.Console.Quit();
			}
		}

		public long[] GetCharacterIDs()
		{
			return expressionMediator.VEvaluator.VariableData.CharacterList.Select(v => v.NO).ToArray();
		}
		public long GetIntVar(string name, int index = 0)
		{
			return expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name].GetIntValue(expressionMediator, [index]);
		}
		public string GetStrVar(string name, int index = 0)
		{
			return expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name].GetStrValue(expressionMediator, [index]);
		}
		public void SetIntVar(string name, long val, int index = 0)
		{
			expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name].SetValue(val, [index]);
		}
		public void SetStrVar(string name, string val, int index = 0)
		{
			expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name].SetValue(val, [index]);
		}

		public void SetCharVar(string name, long charId, string key, long value)
		{
			var variable = expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name];
			var errPos = "";
			var dict = expressionMediator.VEvaluator.Constant.GetKeywordDictionary(out errPos, VariableCode.CFLAG, 1, key);
			variable.SetValue(value, [charId, dict[key]]);
		}
		public void SetCharVar(string name, long charId, long key, long value)
		{
			var variable = expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name];
			variable.SetValue(value, [charId, key]);
		}
		public long GetCharVar(string name, long charId, long key)
		{
			var variable = expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name];
			return variable.GetIntValue(expressionMediator, [charId, key]);
		}
		public long GetCharVar(string name, long charId, string key)
		{
			var variable = expressionMediator.VEvaluator.VariableData.GetVarTokenDic()[name];
			var errPos = "";
			var dict = expressionMediator.VEvaluator.Constant.GetKeywordDictionary(out errPos, VariableCode.CFLAG, 1, key);
			return variable.GetIntValue(expressionMediator, [charId, dict[key]]);
		}

		public System.Data.DataTable GetDataTable(string name)
		{
			return expressionMediator.VEvaluator.VariableData.DataDataTables[name];
		}

		public static PluginAPICharContext CreateCharContext(long charId)
		{
			return new PluginAPICharContext(charId);
		}


		/// <summary>
		/// Returns call stacktrace from emuera/ERB side
		/// </summary>
		public IEnumerable<String> GetStackTrace(){
			LogicalLine parent;
			List<String> stackTraceCollecter = [];
			int depth = 0;

			// add initial caller
			stackTraceCollecter.Add(this.processState.CurrentLine.Position.Value.Filename + ":" + this.processState.CurrentLine.Position.Value.LineNo.ToString() + "@" +  this.processState.CurrentLine.ParentLabelLine.LabelName);
			// loop call stack
			while ((parent = this.processState.GetReturnAddressSequensial(depth++)) != null)
			{
				if (parent.Position != null)
				{
					stackTraceCollecter.Add(parent.Position.Value.Filename + ":" + parent.Position.Value.LineNo.ToString() + "@" + parent.ParentLabelLine.LabelName);
				}
			}
			return stackTraceCollecter;
		}

		/// <summary>
		/// Load all DLL plugins from Plugins directory of the game
		/// </summary>
		public void LoadPlugins()
		{
			ClearMethods();

			//andEmuera: Android では WinForms 依存の DLL を読み込めないため、
			//よく使われるプラグインの内蔵実装を先に登録する。
			//同名の DLL があれば後の読み込みで上書きされる。
			foreach (var builtin in BuiltinPlugins.GetMethods())
				AddMethod(builtin);

			//andEmuera: Plugins/*.dll は読みに行かない。
			//
			//プラグイン DLL は本家 Emuera の本体アセンブリを参照しているので、
			//この移植版では Assembly.LoadFrom / GetTypes() が必ず解決に失敗する
			//("Could not load file or assembly 'Emuera, Version=1.824.0.0'")。
			//読もうとすると、ゲームフォルダに DLL を置いてあるだけで起動のたびに
			//「読み込めませんでした」が本数ぶん並ぶ
			//(erablue_resort は LAUNCH_BROWSER.dll と StringBuilderPlugin.dll の 2 本)。
			//読んでも必ず失敗する以上、警告に意味は無い。
			//
			//必要なものは BuiltinPlugins に内蔵実装を足して代替する。
			//ExistPlugin も立てない。立てると「外部プラグイン機能が有効になっています」の
			//注意まで出るが、この移植版では実際に有効になることが無い。
			GlobalStatic.ExistPlugin = false;
		}

		public IPluginMethod GetMethod(string name)
		{
			var key = name;
			if (Config.Config.IgnoreCase)
			{
				key = key.ToUpper();
			}
			return methods[key];
		}

		public bool HasMethod(string name)
		{
			var key = name;
			if (Config.Config.IgnoreCase)
			{
				key = key.ToUpper();
			}
			return methods.ContainsKey(key);
		}

		internal void SetParent(Process process, ProcessState processState, ExpressionMediator expressionMediator)
		{
			this.process = process;
			this.processState = processState;
			this.expressionMediator = expressionMediator;
			PluginAPICharContext.CacheVariables(expressionMediator);
			CacheVariables();
		}

		internal void CacheVariables()
		{
			DAY = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["DAY"], expressionMediator, VariableCode.DAY);
			MONEY = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["MONEY"], expressionMediator, VariableCode.MONEY);
			ITEM = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["ITEM"], expressionMediator, VariableCode.ITEM);
			FLAG = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["FLAG"], expressionMediator, VariableCode.FLAG);
			TFLAG = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TFLAG"], expressionMediator, VariableCode.TFLAG);
			UP = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["UP"], expressionMediator, VariableCode.UP);
			PALAMLV = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["PALAMLV"], expressionMediator, VariableCode.PALAMLV);
			EXPLV = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["EXPLV"], expressionMediator, VariableCode.EXPLV);
			EJAC = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["EJAC"], expressionMediator, VariableCode.EJAC);
			DOWN = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["DOWN"], expressionMediator, VariableCode.DOWN);
			RESULT = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["RESULT"], expressionMediator, VariableCode.RESULT);
			COUNT = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["COUNT"], expressionMediator, VariableCode.COUNT);
			TARGET = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TARGET"], expressionMediator, VariableCode.TARGET);
			ASSI = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["ASSI"], expressionMediator, VariableCode.ASSI);
			MASTER = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["MASTER"], expressionMediator, VariableCode.MASTER);
			NOITEM = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["NOITEM"], expressionMediator, VariableCode.NOITEM);
			LOSEBASE = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["LOSEBASE"], expressionMediator, VariableCode.LOSEBASE);
			SELECTCOM = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["SELECTCOM"], expressionMediator, VariableCode.SELECTCOM);
			ASSIPLAY = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["ASSIPLAY"], expressionMediator, VariableCode.ASSIPLAY);
			PREVCOM = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["PREVCOM"], expressionMediator, VariableCode.PREVCOM);
			TIME = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TIME"], expressionMediator, VariableCode.TIME);
			ITEMSALES = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["ITEMSALES"], expressionMediator, VariableCode.ITEMSALES);
			PLAYER = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["PLAYER"], expressionMediator, VariableCode.PLAYER);
			NEXTCOM = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["NEXTCOM"], expressionMediator, VariableCode.NEXTCOM);
			PBAND = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["PBAND"], expressionMediator, VariableCode.PBAND);
			BOUGHT = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["BOUGHT"], expressionMediator, VariableCode.BOUGHT);

			GLOBAL = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["GLOBAL"], expressionMediator, VariableCode.GLOBAL);
			RANDDATA = new GlobalInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["RANDDATA"], expressionMediator, VariableCode.RANDDATA);

			SAVESTR = new GlobalString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["SAVESTR"], expressionMediator, VariableCode.SAVESTR);
			TSTR = new GlobalString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TSTR"], expressionMediator, VariableCode.TSTR);
			STR = new GlobalString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["STR"], expressionMediator, VariableCode.STR);
			RESULTS = new GlobalString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["RESULTS"], expressionMediator, VariableCode.RESULTS);
			GLOBALS = new GlobalString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["GLOBALS"], expressionMediator, VariableCode.GLOBALS);

			NO = new GlobalConstInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["NO"], expressionMediator, VariableCode.NO);
			ITEMPRICE = new GlobalConstInt1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["ITEMPRICE"], expressionMediator, VariableCode.ITEMPRICE);

			ABLNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["ABLNAME"], expressionMediator, VariableCode.ABLNAME);
			TALENTNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TALENTNAME"], expressionMediator, VariableCode.TALENTNAME);
			EXPNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["EXPNAME"], expressionMediator, VariableCode.EXPNAME);
			MARKNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["MARKNAME"], expressionMediator, VariableCode.MARKNAME);
			PALAMNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["PALAMNAME"], expressionMediator, VariableCode.PALAMNAME);
			ITEMNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["ITEMNAME"], expressionMediator, VariableCode.ITEMNAME);
			TRAINNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TRAINNAME"], expressionMediator, VariableCode.TRAINNAME);
			BASENAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["BASENAME"], expressionMediator, VariableCode.BASENAME);
			SOURCENAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["SOURCENAME"], expressionMediator, VariableCode.SOURCENAME);
			EXNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["EXNAME"], expressionMediator, VariableCode.EXNAME);
			EQUIPNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["EQUIPNAME"], expressionMediator, VariableCode.EQUIPNAME);
			TEQUIPNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TEQUIPNAME"], expressionMediator, VariableCode.TEQUIPNAME);
			FLAGNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["FLAGNAME"], expressionMediator, VariableCode.FLAGNAME);
			TFLAGNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TFLAGNAME"], expressionMediator, VariableCode.TFLAGNAME);
			CFLAGNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["CFLAGNAME"], expressionMediator, VariableCode.CFLAGNAME);
			TCVARNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TCVARNAME"], expressionMediator, VariableCode.TCVARNAME);
			CSTRNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["CSTRNAME"], expressionMediator, VariableCode.CSTRNAME);
			STAINNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["STAINNAME"], expressionMediator, VariableCode.STAINNAME);

			CDFLAGNAME1 = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["CDFLAGNAME1"], expressionMediator, VariableCode.CDFLAGNAME1);
			CDFLAGNAME2 = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["CDFLAGNAME2"], expressionMediator, VariableCode.CDFLAGNAME2);
			STRNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["STRNAME"], expressionMediator, VariableCode.STRNAME);
			TSTRNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["TSTRNAME"], expressionMediator, VariableCode.TSTRNAME);
			SAVESTRNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["SAVESTRNAME"], expressionMediator, VariableCode.SAVESTRNAME);
			GLOBALNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["GLOBALNAME"], expressionMediator, VariableCode.GLOBALNAME);
			GLOBALSNAME = new GlobalConstString1dWrapper(expressionMediator.VEvaluator.VariableData.GetVarTokenDic()["GLOBALSNAME"], expressionMediator, VariableCode.GLOBALSNAME);
		}

		private void ClearMethods()
		{
			methods.Clear();
		}

		private void AddMethod(IPluginMethod method)
		{
			var key = method.Name;
			if (Config.Config.IgnoreCase)
			{
				key = key.ToUpper();
			}
			//andEmuera: 内蔵実装と同名の DLL があれば DLL 側を優先させる (重複で例外にしない)
			methods[key] = method;
		}

		private Dictionary<string, IPluginMethod> methods = [];
		private Process process;
		private ProcessState processState;
		private ExpressionMediator expressionMediator;

		public GlobalInt1dWrapper DAY;
		public GlobalInt1dWrapper MONEY;
		public GlobalInt1dWrapper ITEM;
		public GlobalInt1dWrapper FLAG;
		public GlobalInt1dWrapper TFLAG;
		public GlobalInt1dWrapper UP;
		public GlobalInt1dWrapper PALAMLV;
		public GlobalInt1dWrapper EXPLV;
		public GlobalInt1dWrapper EJAC;
		public GlobalInt1dWrapper DOWN;
		public GlobalInt1dWrapper RESULT;
		public GlobalInt1dWrapper COUNT;
		public GlobalInt1dWrapper TARGET;
		public GlobalInt1dWrapper ASSI;
		public GlobalInt1dWrapper MASTER;
		public GlobalInt1dWrapper NOITEM;
		public GlobalInt1dWrapper LOSEBASE;
		public GlobalInt1dWrapper SELECTCOM;
		public GlobalInt1dWrapper ASSIPLAY;
		public GlobalInt1dWrapper PREVCOM;
		public GlobalInt1dWrapper TIME;
		public GlobalInt1dWrapper ITEMSALES;
		public GlobalInt1dWrapper PLAYER;
		public GlobalInt1dWrapper NEXTCOM;
		public GlobalInt1dWrapper PBAND;
		public GlobalInt1dWrapper BOUGHT;

		public GlobalInt1dWrapper GLOBAL;
		public GlobalInt1dWrapper RANDDATA;

		public GlobalString1dWrapper SAVESTR;
		public GlobalString1dWrapper TSTR;
		public GlobalString1dWrapper STR;
		public GlobalString1dWrapper RESULTS;
		public GlobalString1dWrapper GLOBALS;
		
		public GlobalConstInt1dWrapper NO;
		public GlobalConstInt1dWrapper ITEMPRICE;

		public GlobalConstString1dWrapper ABLNAME;
		public GlobalConstString1dWrapper TALENTNAME;
		public GlobalConstString1dWrapper EXPNAME;
		public GlobalConstString1dWrapper MARKNAME;
		public GlobalConstString1dWrapper PALAMNAME;
		public GlobalConstString1dWrapper ITEMNAME;
		public GlobalConstString1dWrapper TRAINNAME;
		public GlobalConstString1dWrapper BASENAME;
		public GlobalConstString1dWrapper SOURCENAME;
		public GlobalConstString1dWrapper EXNAME;
		public GlobalConstString1dWrapper EQUIPNAME;
		public GlobalConstString1dWrapper TEQUIPNAME;
		public GlobalConstString1dWrapper FLAGNAME;
		public GlobalConstString1dWrapper TFLAGNAME;
		public GlobalConstString1dWrapper CFLAGNAME;
		public GlobalConstString1dWrapper TCVARNAME;
		public GlobalConstString1dWrapper CSTRNAME;
		public GlobalConstString1dWrapper STAINNAME;

		public GlobalConstString1dWrapper CDFLAGNAME1;
		public GlobalConstString1dWrapper CDFLAGNAME2;
		public GlobalConstString1dWrapper STRNAME;
		public GlobalConstString1dWrapper TSTRNAME;
		public GlobalConstString1dWrapper SAVESTRNAME;
		public GlobalConstString1dWrapper GLOBALNAME;
		public GlobalConstString1dWrapper GLOBALSNAME;
	}
}
