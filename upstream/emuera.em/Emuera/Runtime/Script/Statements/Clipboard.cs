using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MinorShift.Emuera.UI.Game;
using System.Runtime.CompilerServices;

namespace MinorShift.Emuera.Runtime.Script.Statements;

internal partial class ClipboardProcessor
{
	private readonly bool classicMode; // New Lines Only mode

	private readonly Forms.MainWindow mainWin;

	private bool minTimePassed; //Has enough time passed since the last Clipboard update?
	private bool postWaiting; //Is there text waiting to be sent to clipboard?
	private static System.Timers.Timer minTimer; //Minimum timer for refrehsing the clipboard to prevent spam

	private int MaxCB; //Max length in lines of the output to clipboard
	private int ScrollPos; //Position of the clipboard output in the buffer
	private int ScrollCount; //Lines to scroll at a time
	private int NewLineCount; //Number of new lines
	private int OldNewLineCount; //Number of lines in the last update, used for Classic mode + scrolling back to bottom
	private string OldText; //Last set of lines sent to the clipboard
	private CircularBuffer<string> lineBuffer; //Buffer for processed strings ready for clipboard

	private bool Initialized;

	internal enum CBTriggers
	{
		LeftClick,
		MiddleClick,
		DoubleLeftClick,
		AnyKeyWait,
		InputWait,
	}

	public ClipboardProcessor(Forms.MainWindow parent)
	{
		classicMode = Config.Config.CBNewLinesOnly;

		mainWin = parent;

		minTimePassed = true;
		postWaiting = false;

		MaxCB = Config.Config.CBMaxCB;
		ScrollPos = 0; //FIXIT - Expand it, add a button, etc
		ScrollCount = Config.Config.CBScrollCount; //FIXIT - Actually use it
		NewLineCount = 0;
		OldNewLineCount = 0;

		if (!Config.Config.CBUseClipboard) return;

		Init();
	}

	public void Init()
	{
		if (Initialized)
			return;
		lineBuffer = new CircularBuffer<string>(Config.Config.CBBufferSize);
		minTimer = new System.Timers.Timer(Config.Config.CBMinTimer) { AutoReset = false };
		minTimer.Elapsed += MinTimerDone;
		OldText = "";
		Initialized = true;
	}

	public void Reset()
	{
		lineBuffer = null;
		minTimer.Dispose();
		OldText = null;
		Initialized = false;
	}

	public void SetTimerInterval(int interval)
	{
		if (!Initialized)
			return;
		minTimer.Interval = interval;
	}

	public void SetMaxCB(int value)
	{
		MaxCB = value;
	}

	public void SetScrollCount(int value)
	{
		ScrollCount = value;
	}

	public bool ScrollUp(int value)
	{
		if (!Config.Config.CBUseClipboard) return false;
		if (ScrollPos == 0 && classicMode && ScrollCount > OldNewLineCount) ScrollPos = OldNewLineCount;
		else ScrollPos += ScrollCount * value;
		if (lineBuffer.Count < ScrollPos) ScrollPos = lineBuffer.Count - ScrollCount;
		SendToCB(true);
		return true;
	}

	public bool ScrollDown(int value)
	{
		if (!Config.Config.CBUseClipboard) return false;
		ScrollPos -= ScrollCount;
		if (ScrollPos < 0) ScrollPos = 0;
		SendToCB(true);

		return true;
	}

	private void MinTimerDone(object source, System.Timers.ElapsedEventArgs e)
	{
		minTimePassed = true;
		if (postWaiting) SendToCB(true);
	}

	private bool MinTimeCheck()
	{
		if (minTimePassed)
		{
			minTimePassed = false;
			minTimer.Start();
			return true;
		}
		else return false;
	}

	//FIXIT - Autoprocess old lines or just ditch?
	public void AddLine(ConsoleDisplayLine inputLine, bool left)
	{
		if (!Config.Config.CBUseClipboard) return;

		NewLineCount++;
		string processed = ProcessLine(inputLine.ToString());
		lineBuffer.Enqueue(processed);
	}

	public void DelLine(int count)
	{
		if (!Config.Config.CBUseClipboard || count <= 0) return;

		NewLineCount = Math.Max(0, NewLineCount - count);
		if (count >= lineBuffer.Count) lineBuffer.Clear();
		else
		{
			while (count > 0)
			{
				lineBuffer.Dequeue();
				count--;
			}
		}
	}

	public void ClearScreen()
	{
		if (!Config.Config.CBUseClipboard) return;
		if (Config.Config.CBClearBuffer)
		{
			lineBuffer.Clear();
			ScrollPos = 0;
			NewLineCount = 0;
		}
		else
		{
			lineBuffer.Enqueue("");
		}
	}

	public void Check(CBTriggers type)
	{
		if (!Config.Config.CBUseClipboard) return;
		switch (type)
		{
			case CBTriggers.LeftClick:
				if (!Config.Config.CBTriggerLeftClick) return;
				break;

			case CBTriggers.MiddleClick:
				if (!Config.Config.CBTriggerMiddleClick) return;
				break;

			case CBTriggers.DoubleLeftClick:
				if (!Config.Config.CBTriggerDoubleLeftClick) return;
				break;

			case CBTriggers.AnyKeyWait:
				if (!Config.Config.CBTriggerAnyKeyWait) return;
				break;

			case CBTriggers.InputWait:
				if (!Config.Config.CBTriggerInputWait) return;
				break;

			default:
				return;
		}

		ScrollPos = 0;
		SendToCB(false);
	}

	private void SendToCB(bool force)
	{
		if (!Config.Config.CBUseClipboard) return;
		if (NewLineCount == 0 && !force) return;
		if (!MinTimeCheck())
		{
			postWaiting = true;
			return;
		}

		if (NewLineCount == 0 && ScrollPos == 0) NewLineCount = OldNewLineCount;

		int length;
		if (classicMode && ScrollPos == 0) length = Math.Min(NewLineCount, lineBuffer.Count);
		else length = Math.Min(MaxCB, lineBuffer.Count - ScrollPos);
		if (length <= 0) return;

		var builder = new DefaultInterpolatedStringHandler(length * 2, 0);
		for (int count = 0; count < length; count++)
		{
			builder.AppendLiteral(lineBuffer[lineBuffer.Count - length - ScrollPos + count]);
			builder.AppendLiteral("\n");
		}
		var newText = builder.ToString();

		if (newText == OldText) return;
		try
		{
			mainWin.Invoke(() => Clipboard.SetDataObject(newText, false, 3, 200));
			if (ScrollPos == 0) OldNewLineCount = NewLineCount;
			NewLineCount = 0;
			OldText = newText;
			postWaiting = false;
		}
		catch (Exception)
		{
			//FIXIT - For now it just fails silently
		}
	}


	[GeneratedRegex("<.*?>")]
	private static partial Regex HTMLTagRegex();

	public static string StripHTML(string input)
	{
		// still faster to use String.Contains to check if we need to do this at all first, supposedly
		if (Config.Config.CBIgnoreTags && input.Contains('<'))
		{
			// regex is faster and simpler than a for loop you nerds
			return HTMLTagRegex().Replace(input, Config.Config.CBReplaceTags);
		}
		return input;
	}

	private static string ProcessLine(string input)
	{
		return StripHTML(input);
	}
}
