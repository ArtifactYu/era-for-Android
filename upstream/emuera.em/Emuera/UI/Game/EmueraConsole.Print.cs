using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using MinorShift.Emuera.UI.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static MinorShift.Emuera.Runtime.Utils.EvilMask.Utils;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;
using trmb = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.MessageBox;
using trsl = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.SystemLine;

namespace MinorShift.Emuera.GameView;

//1820 EmueraConsoleのうちdisplayLineListやprintBufferに触るもの
//いつかEmueraConsoleから分離したい
internal sealed partial class EmueraConsole : IDisposable
{
	private readonly List<ConsoleDisplayLine> displayLineList;
	public bool noOutputLog;
	public Color bgColor = Config.BackColor;

	private readonly PrintStringBuffer printBuffer;
	#region EE_BINPUT
	public PrintStringBuffer PrintBuffer { get { return printBuffer; } }
	#endregion
	readonly StringMeasure stringMeasure = new();

	#region EM_私家版_StringMeasure獲得
	public StringMeasure StrMeasure
	{
		get
		{
			return stringMeasure;
		}
	}
	#endregion
	public void ClearDisplay()
	{
		#region EE_AnchorのCB機能移植
		CBProc.ClearScreen();
		#endregion
		displayLineList.Clear();
		_htmlElementListDict.Clear();
		#region EM_私家版_描画拡張
		ConsoleEscapedParts.Clear();
		#endregion
		logicalLineCount = 0;
		#region GETDISPLAYLINE修正
		//issue:修正できてない 未だにどこかでずれてる
		deletedLines = 0;
		#endregion
		lineNo = 0;
		lastDrawnLineNo = -1;
		verticalScrollBarUpdate();
		window.Refresh();//OnPaint発行
	}


	#region Print系

	//private bool useUserStyle = true;
	public bool UseUserStyle { get; set; }
	public bool UseSetColorStyle { get; set; }
	private StringStyle defaultStyle = new(Config.ForeColor, FontStyle.Regular, null);
	private StringStyle userStyle = new(Config.ForeColor, FontStyle.Regular, null);
	//private StringStyle style = new StringStyle(Config.ForeColor, FontStyle.Regular, null);
	private StringStyle Style
	{
		get
		{
			if (!UseUserStyle)
				return defaultStyle;
			if (UseSetColorStyle)
				return userStyle;
			//PRINTD系(SETCOLORを無視する)
			if (userStyle.Color == defaultStyle.Color)
				return userStyle;
			return new StringStyle(defaultStyle.Color, userStyle.FontStyle, userStyle.Fontname);
		}
	}
	//private StringStyle Style { get { return (useUserStyle ? userStyle : defaultStyle); } }
	public StringStyle StringStyle { get { return userStyle; } }
	public void SetStringStyle(FontStyle fs) { userStyle.FontStyle = fs; }
	public void SetStringStyle(Color color) { userStyle.Color = color; userStyle.ColorChanged = color != Config.ForeColor; }
	public void SetFont(string fontname) { if (!string.IsNullOrEmpty(fontname)) userStyle.Fontname = fontname; else userStyle.Fontname = Config.FontName; }
	private DisplayLineAlignment alignment = DisplayLineAlignment.LEFT;
	public DisplayLineAlignment Alignment { get { return alignment; } set { alignment = value; } }
	public void ResetStyle()
	{
		userStyle = defaultStyle;
		alignment = DisplayLineAlignment.LEFT;
	}

	public bool EmptyLine { get { return printBuffer.IsEmpty; } }

	/// <summary>
	/// DRAWLINE用文字列
	/// </summary>
	string stBar;

	Stopwatch _drawStopwatch;
	bool forceTextBoxColor;
	public void SetBgColor(Color color)
	{
		bgColor = color;
		forceTextBoxColor = true;
		//REDRAWされない場合はTextBoxの色は変えずにフラグだけ立てる
		//最初の再描画時に現在の背景色に合わせる
		if (redraw == ConsoleRedraw.None && window.ScrollBar.Value == window.ScrollBar.Maximum)
			return;
		//色変化が速くなりすぎないように一定時間以内の再呼び出しは強制待ちにする
		if (_drawStopwatch == null)
		{
			_drawStopwatch = Stopwatch.StartNew();
		}
		else
		{
			while (_drawStopwatch.ElapsedMilliseconds < msPerFrame)
			{
				Application.DoEvents();
			}
		}
		RefreshStrings(true);
		_drawStopwatch.Restart();
	}

	//完全に独立したHTML
	//統合_netEmuera_island層: layer は第2引数。小さいほど奥に描く
	public void PrintHTMLIsland(string html, int layer = 0)
	{
		#region 統合_netEmuera_island判定
		//島の中身も当たり判定に使うので、div に親ボタンを持たせておく。
		//行に属さないので ConsoleEscapedParts には登録しない（登録は行番号で引くため）
		ConsoleDisplayLine[] lines = HtmlManager.Html2DisplayLine(html, stringMeasure, this);
		if (Config.TextDrawingMode != TextDrawingMode.WINAPI)
			foreach (ConsoleDisplayLine line in lines)
				foreach (ConsoleButtonString button in line.Buttons)
				{
					if (button?.StrArray == null)
						continue;
					button.FilterEscaped();
					if (button.EscapedParts == null)
						continue;
					foreach (var p in button.EscapedParts)
						p.Parent = button;
				}
		#region 統合_netEmuera_island層
		if (!_htmlElementListDict.TryGetValue(layer, out var elementList))
		{
			elementList = [];
			_htmlElementListDict[layer] = elementList;
		}
		elementList.AddRange(lines);
		#endregion
		#endregion
	}
	public void ClearHTMLIsland()
	{
		_htmlElementListDict.Clear();
	}
	#region 統合_netEmuera_island層
	/// <summary>HTML_PRINT_ISLAND_CLEAR に層が指定されたとき。その層だけ消す</summary>
	public void ClearHTMLIsland(int layer)
	{
		if (_htmlElementListDict.TryGetValue(layer, out var elementList))
			elementList.Clear();
	}
	#endregion


	/// <summary>
	/// 最後に描画した時にlineNoの値
	/// </summary>
	int lastDrawnLineNo = -1;
	int lineNo;
	public int GetLineNo { get { return lineNo; } }
	long logicalLineCount;
	#region GETDISPLAYLINE修正
	long deletedLines;
	#endregion
	public long LineCount { get { return logicalLineCount; } }
	#region GETDISPLAYLINE修正
	public long DeletedLines { get { return deletedLines; } }
	#endregion
	private void addRangeDisplayLine(ConsoleDisplayLine[] lineList)
	{
		for (int i = 0; i < lineList.Length; i++)
			addDisplayLine(lineList[i], false);
	}

	private void addDisplayLine(ConsoleDisplayLine line, bool force_LEFT)
	{
		#region EE_AnchorのCB機能移植
		if (Config.CBUseClipboard)
			CBProc.AddLine(line, force_LEFT);
		#endregion
		if (LastLineIsTemporary)
			deleteLine(1);
		//不適正なFontのチェック
		AConsoleDisplayNode errorStr = null;
		#region EM_私家版_描画拡張
		foreach (ConsoleButtonString button in line.Buttons)
		{
			foreach (AConsoleDisplayNode css in button.StrArray)
			{
				if (css.Error)
				{
					errorStr = css;
					goto ScanBreak;
				}
			}
			if (Config.TextDrawingMode != TextDrawingMode.WINAPI)
			{
				button.FilterEscaped();
				if (button.EscapedParts != null)
				{
					foreach (var p in button.EscapedParts)
					{
						p.Parent = button;
						ConsoleEscapedParts.Add(p, lineNo, p.Depth,
							(int)Math.Ceiling((float)p.Top / Config.LineHeight) + lineNo,
							(int)Math.Floor((float)Math.Max(0, p.Bottom - 1) / Config.LineHeight) + lineNo);
					}
				}
			}
		}
	ScanBreak:
		#endregion
		if (errorStr != null)
		{
			Dialog.Show(trmb.IllegalFontError.Text, trmb.IllegalFontError.Text);
			Quit();
			return;
		}
		if (force_LEFT)
			line.SetAlignment(DisplayLineAlignment.LEFT);
		else
			line.SetAlignment(alignment);
		line.LineNo = lineNo;
		//Bitmap Cache
		line.bitmapCacheEnabled = GlobalStatic.Console.bitmapCacheEnabledForNextLine;
		if (displayLineList.Count != 0 &&
					!displayLineList[^1].IsLineEnd)
		{
			var lastline = displayLineList[^1];
			deleteLine(1);
			line.ShiftPositionX(lastline.Buttons[^1].PointX + lastline.Buttons[^1].Width);
			line.ChangeStr([.. lastline.Buttons, .. line.Buttons]);
		}
		displayLineList.Add(line);
		lineNo++;
		if (line.IsLogicalLine && displayLineList[^1].IsLineEnd)
			logicalLineCount++;
		if (lineNo == int.MaxValue)
		{
			lastDrawnLineNo = -1;
			lineNo = 0;
		}
		if (logicalLineCount == long.MaxValue)
		{
			logicalLineCount = 0;
		}
		#region EM_私家版_描画拡張
		if (displayLineList.Count > Config.MaxLog)
		// displayLineList.RemoveAt(0);
		{
			if (Config.TextDrawingMode != TextDrawingMode.WINAPI)
				ConsoleEscapedParts.RemoveAt(displayLineList[0].LineNo);
			displayLineList.RemoveAt(0);
			#region GETDISPLAYLINE修正
			deletedLines++;
			#endregion
		}
		#endregion
	}


	public void deleteLine(int argNum)
	{
		#region EE_AnchorのCB機能移植
		if (Config.CBUseClipboard)
			CBProc.DelLine(Math.Min(argNum, displayLineList.Count)); //FIXIT - Do we need to worry about the count?
		#endregion
		int delNum = 0;
		int num = argNum;
		#region EM_私家版_描画拡張
		bool deleted = false;
		while (delNum < num)
		{
			if (displayLineList.Count == 0)
				break;
			ConsoleDisplayLine line = displayLineList[^1];
			deleted = true;
			displayLineList.RemoveAt(displayLineList.Count - 1);
			lineNo--;
			if (line.IsLogicalLine)
			{
				delNum++;
				if (line.IsLineEnd)
				{
					logicalLineCount--;
				}
			}
			#region GETDISPLAYLINE修正
			//MaxLog状態からのRemoveはdummylineの挿入が無いのでdeletedLineを加算
			if (displayLineList.Count == Config.MaxLog - 1)
				deletedLines++;
			#endregion
			if (displayLineList.Count == Config.MaxLog - 2 && lineNo > displayLineList.Count)
			{
				ConsoleDisplayLine dummyline = BufferToSingleLine(true, false);
				displayLineList.Insert(0, dummyline);
			}
		}

		if (delNum < num)
		{
			lineNo = 0;
			logicalLineCount -= num - delNum;
		}

		if (deleted && Config.TextDrawingMode != TextDrawingMode.WINAPI)
			ConsoleEscapedParts.Remove(lineNo);
		#endregion
		if (lineNo < 0)
			lineNo += int.MaxValue;
		lastDrawnLineNo = -1;
		//MaxLog超過時の補充はCLEARLINEで改行が入るため1つだけ消す
		if (displayLineList.Count == Config.MaxLog)
			displayLineList.RemoveAt(0);
		#region GETDISPLAYLINE修正
		deletedLines -= num;
		#endregion
		puchi.ClearFromLine(lineNo);
		//RefreshStrings(true);
	}

	public bool LastLineIsTemporary
	{
		get
		{
			if (displayLineList.Count == 0)
				return false;
			return displayLineList[^1].IsTemporary;
		}
	}

	//空行であるかのチェック
	public bool LastLineIsEmpty
	{
		get
		{
			if (displayLineList.Count == 0)
				return false;
			return string.IsNullOrEmpty(displayLineList[^1].ToString().Trim());
		}
	}

	//最終行を書き換え＋次の行追加時にはその行を再利用するように設定
	public void PrintTemporaryLine(string str)
	{
		PrintSingleLine(str, true);
	}

	//最終行だけを書き換える
	private void changeLastLine(string str)
	{
		deleteLine(1);
		PrintSingleLine(str, false);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="str"></param>
	/// <param name="position"></param>
	/// <param name="level">警告レベル.0:軽微なミス.1:無視できる行.2:行が実行されなければ無害.3:致命的</param>
	public void PrintWarning(string str, ScriptPosition? position, int level)
	{
		if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
			return;
		//警告だけは強制表示
		bool b = force_temporary;
		force_temporary = false;
		if (position != null)
		{
			if (position.Value.LineNo >= 0)
			{
				PrintErrorButton(string.Format(trerror.Warning1.Text, level, position.Value.Filename, position.Value.LineNo, str), position, level);
				GlobalStatic.Process.printRawLine(position);
			}
			else
				PrintErrorButton(string.Format(trerror.Warning2.Text, level, position.Value.Filename, str), position, level);

		}
		else
		{
			PrintError(string.Format(trerror.Warning3.Text, level, str));
		}
		force_temporary = b;
	}



	/// <summary>
	/// ユーザー指定のフォントを無視する。ウィンドウサイズを考慮せず確実に一行で書く。システム用。
	/// </summary>
	/// <param name="str"></param>
	public void PrintSystemLine(string str)
	{
		PrintFlush(false);
		//RefreshStrings(false);
		UseUserStyle = false;
		PrintSingleLine(str, false);
	}
	public void PrintError(string str)
	{
		if (string.IsNullOrEmpty(str))
			return;
		if (Program.DebugMode)
		{
			DebugPrint(str);
			DebugNewLine();
		}
		PrintFlush(false);
		UseUserStyle = false;
		ConsoleDisplayLine dispLine = PrintPlainwithSingleLine(str);
		if (dispLine == null)
			return;
		addDisplayLine(dispLine, true);
		RefreshStrings(false);
	}

	internal void PrintErrorButton(string str, ScriptPosition? pos, int level = 0)
	{
		if (string.IsNullOrEmpty(str))
			return;
		if (Program.DebugMode)
		{
			DebugPrint(str);
			DebugNewLine();
		}
		UseUserStyle = false;
		//todo:オプションで色を変えられるように
		var errColor = Color.FromArgb(255, 255, 255, 160);
		var errerStyle = Style;
		errerStyle.Color = level switch
		{
			0 => errColor,
			1 => errColor,
			2 => errColor,
			3 => Color.Red,
			_ => Color.Red
		};

		ConsoleDisplayLine dispLine = printBuffer.AppendAndFlushErrButton(str, errerStyle, ErrorButtonsText, pos, stringMeasure);
		if (dispLine == null)
			return;
		addDisplayLine(dispLine, true);
		RefreshStrings(false);
	}

	/// <summary>
	/// 1813 従来のPrintLineを用途を考慮してPrintSingleLineとPrintSystemLineに分割
	/// </summary>
	/// <param name="str"></param>
	public void PrintSingleLine(string str) { PrintSingleLine(str, false); }
	public void PrintSingleLine(string str, bool temporary)
	{
		if (string.IsNullOrEmpty(str))
			return;
		PrintFlush(false);
		printBuffer.Append(str, Style);
		ConsoleDisplayLine dispLine = BufferToSingleLine(true, temporary);
		if (dispLine == null)
			return;
		addDisplayLine(dispLine, false);
		RefreshStrings(false);
	}

	public void Print(string str, bool lineEnd = true)
	{
		if (string.IsNullOrEmpty(str))
			return;

		var lineEndIndex = str.IndexOf('\n', StringComparison.Ordinal);
		if (lineEndIndex != -1)
		{
			string upper = str[..lineEndIndex];
			printBuffer.Append(upper, Style);
			NewLine();
			if (lineEndIndex < str.Length - 1)
			{
				string lower = str[(lineEndIndex + 1)..];
				Print(lower);
			}
			return;
		}

		printBuffer.Append(str, Style, lineEnd: lineEnd);
		return;
	}


	#region EM_私家版_HTMLパラメータ拡張
	// public void PrintImg(string str)
	public void PrintImg(string name, string nameb, string namem, MixedNum height, MixedNum width, MixedNum ypos)
	{
		//printBuffer.Append(new ConsoleImagePart(str, null, 0, 0, 0));
		printBuffer.Append(new ConsoleImagePart(name, nameb, namem, height, width, ypos));
	}
	#endregion

	#region EM_私家版_HTMLパラメータ拡張
	//public void PrintShape(string type, int[] param)
	public void PrintShape(string type, MixedNum[] param)
	#endregion
	{
		ConsoleShapePart part = ConsoleShapePart.CreateShape(type, param, userStyle.Color, userStyle.ButtonColor, false);
		printBuffer.Append(part);
	}

	#region EM_私家版_HTML_PRINT拡張
	public void PrintHtml(string str, bool toPrintBuffer)
	{
		str = puchi.ProcessHtml(str, LineCount);
		if (string.IsNullOrEmpty(str))
			return;
		if (!Enabled)
			return;
		if (toPrintBuffer)
		{
			foreach (var button in HtmlManager.Html2ButtonList(str, stringMeasure, this))
				printBuffer.AppendButton(button);
		}
		else
		{
			if (!printBuffer.IsEmpty)
			{
				ConsoleDisplayLine[] dispList = printBuffer.Flush(stringMeasure, force_temporary);
				addRangeDisplayLine(dispList);
			}
			addRangeDisplayLine(HtmlManager.Html2DisplayLine(str, stringMeasure, this));
		}
		RefreshStrings(false);
	}
	#endregion

	private int printCWidth = -1;
	private int printCWidthL = -1;
	private int printCWidthL2 = -1;
	public void PrintC(string str, bool alignmentRight)
	{
		if (string.IsNullOrEmpty(str))
			return;

		printBuffer.Append(CreateTypeCString(str, alignmentRight), Style, true);
	}

	private void calcPrintCWidth(StringMeasure stringMeasure)
	{
		string str = new(' ', Config.PrintCLength);
		Font font = Config.DefaultFont;
		printCWidth = stringMeasure.GetDisplayLength(str, font);

		//この処理要る？
		//str += " ";
		printCWidthL = stringMeasure.GetDisplayLength(str, font);

		//str += " ";
		printCWidthL2 = stringMeasure.GetDisplayLength(str, font);
	}

	private string CreateTypeCString(string str, bool alignmentRight)
	{
		if (printCWidth == -1)
			calcPrintCWidth(stringMeasure);
		int length = 0;
		int width;
		if (str != null)
			#region .NET 7化の弊害でPRINTC系の文字数カウントがおかしい不具合修正
			//length = Config.Encode.GetByteCount(str);
			length = Encoding.GetEncoding("Shift-JIS").GetByteCount(str);
		#endregion
		int printcLength = Config.PrintCLength;
		Font font;
		try
		{
			font = new Font(Style.Fontname, Config.DefaultFont.Size, Style.FontStyle, GraphicsUnit.Pixel);
		}
		catch
		{
			return str;
		}

		if (alignmentRight && (length < printcLength))
		{
			str = new string(' ', printcLength - length) + str;
			width = stringMeasure.GetDisplayLength(str, font);
			while (width > printCWidth)
			{
				if (str[0] != ' ')
					break;
				str = str.Remove(0, 1);
				width = stringMeasure.GetDisplayLength(str, font);
			}
		}
		else if ((!alignmentRight) && (length < printcLength + 1))
		{
			str += new string(' ', printcLength + 1 - length);
			width = stringMeasure.GetDisplayLength(str, font);
			while (width > printCWidthL)
			{
				if (str[^1] != ' ')
					break;
				str = str.Remove(str.Length - 1, 1);
				width = stringMeasure.GetDisplayLength(str, font);
			}
		}
		return str;
	}

	internal void PrintButton(string str, string p)
	{
		if (string.IsNullOrEmpty(str))
			return;
		printBuffer.AppendButton(str, Style, p);
	}
	internal void PrintButton(string str, long p)
	{
		if (string.IsNullOrEmpty(str))
			return;
		printBuffer.AppendButton(str, Style, p);
	}
	internal void PrintButtonC(string str, string p, bool isRight)
	{
		if (string.IsNullOrEmpty(str))
			return;
		printBuffer.AppendButton(CreateTypeCString(str, isRight), Style, p);
	}
	internal void PrintButtonC(string str, long p, bool isRight)
	{
		if (string.IsNullOrEmpty(str))
			return;
		printBuffer.AppendButton(CreateTypeCString(str, isRight), Style, p);
	}

	internal void PrintPlain(string str)
	{
		if (string.IsNullOrEmpty(str))
			return;
		printBuffer.AppendPlainText(str, Style);
	}

	public void NewLine()
	{
		PrintFlush(true);
		RefreshStrings(false);
	}

	public ConsoleDisplayLine BufferToSingleLine(bool force, bool temporary)
	{
		if (!Enabled)
			return null;
		if (!force && printBuffer.IsEmpty)
			return null;
		if (force && printBuffer.IsEmpty)
			printBuffer.Append(" ", Style);
		ConsoleDisplayLine dispLine = printBuffer.FlushSingleLine(stringMeasure, temporary | force_temporary);
		return dispLine;
	}
	public void ClearText()
	{
		window.clear_richText();
	}

	public void PrintPlainWithSingleLineFix(string str)
	{
		ConsoleDisplayLine dispLine = PrintPlainwithSingleLine(str);
		if (dispLine == null)
			return;
		addDisplayLine(dispLine, false);
		RefreshStrings(false);
	}

	internal ConsoleDisplayLine PrintPlainwithSingleLine(string str)
	{
		if (!Enabled)
			return null;
		if (string.IsNullOrEmpty(str))
			return null;
		printBuffer.AppendPlainText(str, Style);
		ConsoleDisplayLine dispLine = printBuffer.FlushSingleLine(stringMeasure, false);
		return dispLine;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="force">バッファーが空でも改行する</param>
	public void PrintFlush(bool force)
	{
		if (!Enabled)
			return;
		if (!force && printBuffer.IsEmpty)
			return;
		if (force && printBuffer.IsEmpty)
			printBuffer.Append(" ", Style);
		ConsoleDisplayLine[] dispList = printBuffer.Flush(stringMeasure, force_temporary);
		//ConsoleDisplayLine[] dispList = printBuffer.Flush(stringMeasure, temporary | force_temporary);
		addRangeDisplayLine(dispList);
		//1819描画命令は分離
		//RefreshStrings(false);
	}

	/// <summary>
	/// DRAWLINE命令に対応。これのフォントを変更できると面倒なことになるのでRegularに固定する。
	/// </summary>
	public void PrintBar()
	{
		//初期に設定済みなので見る必要なし
		//if (stBar == null)
		//    setStBar(StaticConfig.DrawLineString);

		//1806beta001 CompatiDRAWLINEの廃止、CompatiLinefeedAs1739へ移行
		//CompatiLinefeedAs1739の処理はPrintStringBuffer.csで行う
		//if (Config.CompatiDRAWLINE)
		//	PrintFlush(false);
		StringStyle ss = userStyle;
		userStyle.FontStyle = FontStyle.Regular;
		Print(stBar);
		userStyle = ss;
	}

	public void printCustomBar(string barStr, bool isConst)
	{
		if (string.IsNullOrEmpty(barStr))
			throw new CodeEE(trerror.EmptyDrawline.Text);
		StringStyle ss = userStyle;
		userStyle.FontStyle = FontStyle.Regular;
		if (isConst)
			Print(barStr);
		else
			Print(getStBar(barStr));
		userStyle = ss;
	}

	public string getDefStBar()
	{
		return stBar;
	}

	public string getStBar(string barStr)
	{
		var builder = new StringBuilder();
		builder.Append(barStr);
		int width = 0;
		Font font = Config.DefaultFont;
		while (width < Config.DrawableWidth)
		{//境界を越えるまで一文字ずつ増やす
			builder.Append(barStr);
			width = stringMeasure.GetDisplayLength(builder.ToString(), font);
		}
		while (width > Config.DrawableWidth)
		{//境界を越えたら、今度は超えなくなるまで一文字ずつ減らす（barStrに複数字の文字列がきた場合に対応するため）
			builder.Remove(builder.Length - 1, 1);
			width = stringMeasure.GetDisplayLength(builder.ToString(), font);
		}
		return builder.ToString();
	}

	public void setStBar(string barStr)
	{
		stBar = getStBar(barStr);
	}
	#endregion


	private bool outputLog(string fullpath, bool hideInfo)
	{
		StreamWriter writer = null;
		try
		{
			var log = GetLog(hideInfo);
			File.WriteAllText(fullpath, log, EncodingHandler.UTF8BOMEncoding);
		}
		catch (Exception)
		{
			Dialog.Show(trmb.FailedOutputLog.Text, trmb.FailedOutputLogError.Text);
			return false;
		}
		return true;
	}

	#region EE_OUTPUTLOG
	public bool OutputLog(string filename, bool hideInfo)
	{
		// if (filename == null)
		if (filename == "" || filename == null)
			filename = Program.ExeDir + "emuera.log";
		else
			filename = Program.ExeDir + filename;
		if (filename.IndexOf("../", StringComparison.Ordinal) >= 0)
		{
			Dialog.Show(trmb.FailedOutputLog.Text, trmb.CanNotOutputToParentDirectory.Text);
			return false;
		}
		if (!filename.StartsWith(Program.ExeDir, StringComparison.OrdinalIgnoreCase))
		{
			Dialog.Show(trmb.FailedOutputLog.Text, trmb.CanOnlyOutputToSubDirectory.Text);
			return false;
		}

		if (outputLog(filename, hideInfo))
		{
			if (window.Created)
			{
				PrintSystemLine(string.Format(trsl.LogFileHasBeenCreated.Text, filename.Replace(Program.ExeDir, "")));
				RefreshStrings(true);
			}
			return true;
		}
		else
			return false;
	}

	public bool OutputSystemLog(string filename)
	{
		if (filename == "" || filename == null)
			filename = Program.ExeDir + "emuera.log";

		if (!filename.StartsWith(Program.ExeDir, StringComparison.OrdinalIgnoreCase))
		{
			Dialog.Show(trmb.FailedOutputLog.Text, trmb.CanOnlyOutputToSubDirectory.Text);
			return false;
		}

		if (outputLog(filename, false))
		{
			if (window.Created)
			{
				PrintSystemLine(string.Format(trsl.LogFileHasBeenCreated.Text, filename.Replace(Program.ExeDir, "")));
				RefreshStrings(true);
			}
			return true;
		}
		else
			return false;
	}

	#endregion
	public string GetLog(bool hideInfo)
	{
		var builder = new StringBuilder();

		if (!hideInfo)
		{
			builder.AppendLine(trsl.EnvironmentInformation.Text);
			builder.AppendLine(AssemblyData.EmueraVersionText);
			builder.AppendLine();
			builder.AppendLine(trsl.Variant.Text);
			if (string.IsNullOrEmpty(process.gameBase.ScriptTitle))
				builder.AppendLine(trsl.NotDefinedGameBase.Text);
			else
				builder.AppendLine(process.gameBase.ScriptTitle + " " + process.gameBase.ScriptVersionText);

			var patchVersionsPath = Path.Combine(Program.ExeDir, "patch_versions");
			if (Directory.Exists(patchVersionsPath))
			{
				builder.AppendLine(trsl.PatchVersion.Text);
				var versionTexts = Directory.EnumerateFiles(patchVersionsPath, "*.txt")
						.Where(x => Path.GetExtension(x) == ".txt")
						.OrderBy(x => x, StringComparer.Ordinal)
						.Select(x => File.ReadAllText(x).Trim());
				var versionText = string.Join("+", versionTexts);
				builder.AppendLine(versionText);
			}
			builder.AppendLine();
			builder.AppendLine(trsl.Log.Text);
			builder.AppendLine();
		}
		//builder.AppendLine();
		for (int i = 0; i < displayLineList.Count; i++)
		{
			#region EE_AnchorのCB機能移植
			//builder.AppendLine(displayLineList[i].ToString());]
			builder.AppendLine(ClipboardProcessor.StripHTML(displayLineList[i].ToString()));
			#endregion
		}
		return builder.ToString();
	}

	public ConsoleDisplayLine[] GetDisplayLines(long lineNo)
	{
		if (lineNo < 0 || lineNo > displayLineList.Count)
			return null;
		int count = 0;
		List<ConsoleDisplayLine> list = [];
		for (int i = displayLineList.Count - 1; i >= 0; i--)
		{
			if (count == lineNo)
				list.Insert(0, displayLineList[i]);
			if (displayLineList[i].IsLogicalLine)
				count++;
			if (count > lineNo)
				break;
		}
		if (list.Count == 0)
			return null;
		ConsoleDisplayLine[] ret = new ConsoleDisplayLine[list.Count];
		list.CopyTo(ret);
		return ret;
	}
	public ConsoleDisplayLine[] PopDisplayingLines()
	{
		if (!Enabled)
			return null;
		if (printBuffer.IsEmpty)
			return null;
		return printBuffer.Flush(stringMeasure, force_temporary);
	}
}
