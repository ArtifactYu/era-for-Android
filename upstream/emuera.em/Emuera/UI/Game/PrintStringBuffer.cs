using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.UI.Game;

/*
 * ConsoleStyledString = string + StringStyle
 * ConsoleButtonString = (ConsoleStyledString) * n + ButtonValue
 * ConsoleDisplayLine = (ConsoleButtonString) * n
 * PrintStringBufferはERBのPRINT命令からConsoleDisplayLineを作る
*/

/// <summary>
/// PRINT命令を貯める＆最終的に解決するクラス
/// </summary>
internal sealed class PrintStringBuffer
{
	public PrintStringBuffer(EmueraConsole parent)
	{
		this.parent = parent;
	}
	readonly EmueraConsole parent;
	readonly StringBuilder builder = new(2000);
	List<AConsoleDisplayNode> m_stringList = [];
	StringStyle lastStringStyle;
	List<ConsoleButtonString> m_buttonList = [];

	bool isLastLineEnd = true;

	public int BufferStrLength
	{
		get
		{
			int length = 0;
			foreach (AConsoleDisplayNode css in m_stringList)
			{
				if (css is ConsoleStyledString)
					length += css.Text.Length;
				else
					length += 1;
			}
			return length;
		}
	}

	public void Append(AConsoleDisplayNode part)
	{
		if (builder.Length != 0)
		{
			m_stringList.Add(new ConsoleStyledString(builder.ToString(), lastStringStyle));
			builder.Clear();
		}
		m_stringList.Add(part);
	}

	public void Append(string str, StringStyle style, bool force_button = false, bool lineEnd = true)
	{
		isLastLineEnd = lineEnd;
		if (BufferStrLength > 2000)
			return;
		if (force_button)
			fromCssToButton();
		if (builder.Length == 0 || lastStringStyle == style)
		{
			if (builder.Length > 2000)
				return;
			if (builder.Length + str.Length > 2000)
				str = str[..(2000 - builder.Length)] + trerror.BufferOverFlow.Text;
			builder.Append(str);
			lastStringStyle = style;
		}
		else
		{
			m_stringList.Add(new ConsoleStyledString(builder.ToString(), lastStringStyle));
			builder.Clear();
			builder.Append(str);
			lastStringStyle = style;
		}
		if (force_button)
			fromCssToButton();
	}

	public void AppendButton(string str, StringStyle style, string input)
	{
		fromCssToButton();
		m_stringList.Add(new ConsoleStyledString(str, style));
		if (m_stringList.Count == 0)
			return;
		m_buttonList.Add(createButton(m_stringList, input));
		m_stringList.Clear();
	}



	public void AppendButton(string str, StringStyle style, long input)
	{
		fromCssToButton();
		m_stringList.Add(new ConsoleStyledString(str, style));
		if (m_stringList.Count == 0)
			return;
		m_buttonList.Add(createButton(m_stringList, input));
		m_stringList.Clear();
	}

	public void AppendPlainText(string str, StringStyle style)
	{
		fromCssToButton();
		m_stringList.Add(new ConsoleStyledString(str, style));
		if (m_stringList.Count == 0)
			return;
		m_buttonList.Add(createPlainButton(m_stringList));
		m_stringList.Clear();
	}

	#region EM_私家版_HTML_PRINT拡張
	public void AppendButton(ConsoleButtonString button)
	{
		fromCssToButton();
		m_buttonList.Add(button);
	}
	#endregion

	public bool IsEmpty
	{
		get
		{
			return m_buttonList.Count == 0 && builder.Length == 0 && m_stringList.Count == 0;
		}
	}

	public override string ToString()
	{
		StringBuilder buf = new();
		foreach (ConsoleButtonString button in m_buttonList)
			buf.Append(button.ToString());
		foreach (AConsoleDisplayNode css in m_stringList)
			buf.Append(css.Text);
		buf.Append(builder);
		return buf.ToString();
	}

	public ConsoleDisplayLine AppendAndFlushErrButton(string str, StringStyle style, string input, ScriptPosition? pos, StringMeasure sm)
	{
		fromCssToButton();
		m_stringList.Add(new ConsoleStyledString(str, style));
		if (m_stringList.Count == 0)
			return null;
		m_buttonList.Add(createButton(m_stringList, input, pos));
		m_stringList.Clear();
		return FlushSingleLine(sm, false);
	}

	public ConsoleDisplayLine FlushSingleLine(StringMeasure stringMeasure, bool temporary)
	{
		fromCssToButton();
		setWidthToButtonList(m_buttonList, stringMeasure, true);
		var line = new ConsoleDisplayLine([.. m_buttonList], true, temporary);
		clearBuffer();
		return line;
	}

	public ConsoleDisplayLine[] Flush(StringMeasure stringMeasure, bool temporary)
	{
		fromCssToButton();
		ConsoleDisplayLine[] ret = ButtonsToDisplayLines(m_buttonList, stringMeasure, false, temporary);
		// EraFlow: an empty printable buffer has no final display line.
		if (ret.Length > 0) ret[^1].IsLineEnd = isLastLineEnd;
		clearBuffer();
		return ret;
	}

	private static ConsoleDisplayLine m_buttonsToDisplayLine(List<ConsoleButtonString> lineButtonList, bool firstLine, bool temporary)
	{
		ConsoleButtonString[] dispLineButtonArray = new ConsoleButtonString[lineButtonList.Count];
		lineButtonList.CopyTo(dispLineButtonArray);
		lineButtonList.Clear();
		return new ConsoleDisplayLine(dispLineButtonArray, firstLine, temporary);
	}

	#region EM_私家版_HTML_divタグ
	//public static ConsoleDisplayLine[] ButtonsToDisplayLines(List<ConsoleButtonString> buttonList, StringMeasure stringMeasure, bool nobr, bool temporary)
	public static ConsoleDisplayLine[] ButtonsToDisplayLines(List<ConsoleButtonString> buttonList, StringMeasure stringMeasure, bool nobr, bool temporary, bool subDiv = false, int divWidth = 0)
	#endregion
	{
		if (buttonList.Count == 0)
			return [];
		setWidthToButtonList(buttonList, stringMeasure, nobr);
		List<ConsoleDisplayLine> lineList = [];
		List<ConsoleButtonString> lineButtonList = [];
		#region EM_私家版_HTML_divタグ
		// int windowWidth = Config.DrawableWidth;
		// bool firstLine = true;
		int windowWidth = divWidth > 0 ? divWidth : Config.DrawableWidth;
		bool firstLine = !subDiv; // divの中にIsLogicalLineが常にFalse
		#endregion
		for (int i = 0; i < buttonList.Count; i++)
		{
			if (buttonList[i] == null)
			{//強制改行フラグ
				lineList.Add(m_buttonsToDisplayLine(lineButtonList, firstLine, temporary));
				firstLine = false;
				buttonList.RemoveAt(i);
				i--;
				continue;
			}
			if (nobr || buttonList[i].PointX + buttonList[i].Width <= windowWidth)
			{//改行不要モードであるか表示可能領域に収まるならそのままでよい
				lineButtonList.Add(buttonList[i]);
				continue;
			}
			//新しい表示行を作る

			//ボタンを分割するか？
			//「ボタンの途中で行を折りかえさない」がfalseなら分割する
			//このボタンが単体で表示可能領域を上回るなら分割必須
			//クリック可能なボタンでないなら分割する。ただし「ver1739以前の非ボタン折り返しを再現する」ならクリックの可否を区別しない
			if (!Config.ButtonWrap || lineButtonList.Count == 0 || !buttonList[i].IsButton && !Config.CompatiLinefeedAs1739)
			{//ボタン分割する
				#region EM_私家版_HTML_divタグ
				int divIndex = getDivideIndex(buttonList[i], stringMeasure, windowWidth);
				#endregion
				if (divIndex > 0)
				{
					ConsoleButtonString newButton = buttonList[i].DivideAt(divIndex, stringMeasure);
					//newButton.CalcPointX(buttonList[i].PointX + buttonList[i].Width);
					#region EmuEra-Rikaichan
					if (Config.RikaiEnabled)
						buttonList[i].StrArray[0].NextLine = newButton.StrArray[0];
					#endregion
					buttonList.Insert(i + 1, newButton);
					lineButtonList.Add(buttonList[i]);
					i++;
				}
				else if (divIndex == 0 && lineButtonList.Count > 0)
				{//まるごと次の行に送る
				}
				else//分割できない要素のみで構成されたボタンは分割できない
				{
					lineButtonList.Add(buttonList[i]);
					continue;
				}
			}
			lineList.Add(m_buttonsToDisplayLine(lineButtonList, firstLine, temporary));
			firstLine = false;
			//位置調整
			//				shiftX = buttonList[i].PointX;
			int pointX = 0;
			for (int j = i; j < buttonList.Count; j++)
			{
				if (buttonList[j] == null)//強制改行を挟んだ後は調整無用
					break;
				buttonList[j].CalcPointX(pointX);
				pointX += buttonList[j].Width;
			}
			i--;//buttonList[i]は新しい行に含めないので次の行のために再検討する必要がある(直後のi++と相殺)
		}
		#region EmuEra-Rikaichan
		if (Config.RikaiEnabled)
		{
			for (int i = lineButtonList.Count - 1; i > 0; i--)
			{
				lineButtonList[i - 1].StrArray[0].NextLine = lineButtonList[i].StrArray[0];
			}
		}
		#endregion
		if (lineButtonList.Count > 0)
		{
			lineList.Add(m_buttonsToDisplayLine(lineButtonList, firstLine, temporary));
		}
		ConsoleDisplayLine[] ret = new ConsoleDisplayLine[lineList.Count];
		lineList.CopyTo(ret);
		return ret;
	}

	/// <summary>
	/// 1810beta003新規 マークアップ用 Append とFlushを同時にやる
	/// </summary>
	/// <param name="str"></param>
	/// <param name="stringMeasure"></param>
	/// <returns></returns>
	public ConsoleDisplayLine[] PrintHtml(string str, StringMeasure stringMeasure)
	{
		throw new NotImplementedException();
	}

	#region Flush用privateメソッド

	private void clearBuffer()
	{
		builder.Clear();
		m_stringList.Clear();
		m_buttonList.Clear();
	}

	/// <summary>
	/// cssListをbuttonに変換し、buttonListに追加。
	/// この時点ではWidthなどは考えない。
	/// </summary>
	private void fromCssToButton()
	{
		if (builder.Length != 0)
		{
			m_stringList.Add(new ConsoleStyledString(builder.ToString(), lastStringStyle));
			builder.Clear();
		}
		if (m_stringList.Count == 0)
			return;
		m_buttonList.AddRange(createButtons(m_stringList));
		m_stringList.Clear();
	}

	/// <summary>
	/// 物理行を１つのボタンへ。
	/// </summary>
	/// <returns></returns>
	private ConsoleButtonString createButton(List<AConsoleDisplayNode> cssList, string input)
	{
		AConsoleDisplayNode[] cssArray = new AConsoleDisplayNode[cssList.Count];
		cssList.CopyTo(cssArray);
		cssList.Clear();
		return new ConsoleButtonString(parent, cssArray, input);
	}
	private ConsoleButtonString createButton(List<AConsoleDisplayNode> cssList, string input, ScriptPosition? pos)
	{
		AConsoleDisplayNode[] cssArray = new AConsoleDisplayNode[cssList.Count];
		cssList.CopyTo(cssArray);
		cssList.Clear();
		return new ConsoleButtonString(parent, cssArray, input, pos);
	}
	private ConsoleButtonString createButton(List<AConsoleDisplayNode> cssList, long input)
	{
		AConsoleDisplayNode[] cssArray = new AConsoleDisplayNode[cssList.Count];
		cssList.CopyTo(cssArray);
		cssList.Clear();
		return new ConsoleButtonString(parent, cssArray, input);
	}
	private ConsoleButtonString createPlainButton(List<AConsoleDisplayNode> cssList)
	{
		AConsoleDisplayNode[] cssArray = new AConsoleDisplayNode[cssList.Count];
		cssList.CopyTo(cssArray);
		cssList.Clear();
		return new ConsoleButtonString(parent, cssArray);
	}

	/// <summary>
	/// 物理行をボタン単位に分割。引数のcssListの内容は変更される場合がある。
	/// </summary>
	/// <returns></returns>
	private ConsoleButtonString[] createButtons(List<AConsoleDisplayNode> cssList)
	{
		StringBuilder buf = new();
		for (int i = 0; i < cssList.Count; i++)
		{
			buf.Append(cssList[i].Text);
		}
		List<ButtonPrimitive> bpList = ButtonStringCreator.SplitButton(buf.ToString());
		ConsoleButtonString[] ret = new ConsoleButtonString[bpList.Count];
		if (ret.Length == 1)
		{
			if (bpList[0].CanSelect)
				ret[0] = new ConsoleButtonString(parent, [.. cssList], bpList[0].Input);
			else
				ret[0] = new ConsoleButtonString(parent, [.. cssList]);
			return ret;
		}
		int cssStartCharIndex = 0;
		int buttonEndCharIndex = 0;
		int cssIndex = 0;
		List<AConsoleDisplayNode> buttonCssList = [];
		for (int i = 0; i < ret.Length; i++)
		{
			ButtonPrimitive bp = bpList[i];
			buttonEndCharIndex += bp.Str.Length;
			while (true)
			{
				if (cssIndex >= cssList.Count)
					break;
				AConsoleDisplayNode css = cssList[cssIndex];
				if (cssStartCharIndex + css.Text.Length >= buttonEndCharIndex)
				{//ボタンの終端を発見
					int used = buttonEndCharIndex - cssStartCharIndex;
					if (used > 0 && css.CanDivide)
					{//cssの区切りの途中でボタンの区切りがある。

						ConsoleStyledString newCss = ((ConsoleStyledString)css).DivideAt(used);
						if (newCss != null)
						{
							cssList.Insert(cssIndex + 1, newCss);
							newCss.PointX = css.PointX + css.Width;
						}
					}
					buttonCssList.Add(css);
					cssStartCharIndex += css.Text.Length;
					cssIndex++;
					break;
				}
				//ボタンの終端はまだ先。
				buttonCssList.Add(css);
				cssStartCharIndex += css.Text.Length;
				cssIndex++;
			}
			if (bp.CanSelect)
				ret[i] = new ConsoleButtonString(parent, [.. buttonCssList], bp.Input);
			else
				ret[i] = new ConsoleButtonString(parent, [.. buttonCssList]);
			buttonCssList.Clear();
		}
		return ret;

	}


	//stringListにPointX、Widthを追加
	private static void setWidthToButtonList(List<ConsoleButtonString> buttonList, StringMeasure stringMeasure, bool nobr)
	{
		int pointX = 0;
		//int count = buttonList.Count;
		float subPixel = 0.0f;
		for (int i = 0; i < buttonList.Count; i++)
		{
			ConsoleButtonString button = buttonList[i];
			if (button == null)
			{//改行フラグ
				pointX = 0;
				continue;
			}
			button.CalcWidth(stringMeasure, subPixel);
			button.CalcPointX(pointX);
			pointX = button.PointX + button.Width;
			//これは何がしたいんだろう…
			if (button.PointXisLocked)
				subPixel = 0;
			//pointX += button.Width;
			subPixel = button.XsubPixel;
		}
		return;

		//1815 バグバグなのでコメントアウト Width測定の省略はいずれやりたい
		////1815 alignLeft, nobrを前提にした新方式
		////PointXの直接指定を可能にし、Width測定を一部省略
		//ConsoleStyledString lastCss = null;
		//for (int i = 0; i < buttonList.Count; i++)
		//{
		//    ConsoleButtonString button = buttonList[i];
		//    if (button == null)
		//    {//改行フラグ
		//        pointX = 0;
		//        lastCss = null;
		//        continue;
		//    }
		//    for (int j = 0; j < button.StrArray.Length; j++)
		//    {
		//        ConsoleStyledString css = button.StrArray[j];
		//        if (css.PointXisLocked)//位置固定フラグ
		//        {//位置固定なら前のcssのWidth測定を省略
		//            pointX = css.PointX;
		//            if (lastCss != null)
		//            {
		//                lastCss.Width = css.PointX - lastCss.PointX;
		//                if (lastCss.Width < 0)
		//                    lastCss.Width = 0;
		//            }
		//        }
		//        else
		//        {
		//            if (lastCss != null)
		//            {
		//                lastCss.SetWidth(stringMeasure);
		//                pointX += lastCss.Width;
		//            }
		//            css.PointX = pointX;
		//        }
		//    }
		//}
		////ConsoleButtonStringの位置・幅を決定（クリック可能域の決定のために必要）
		//for (int i = 0; i < buttonList.Count; i++)
		//{
		//    ConsoleButtonString button = buttonList[i];
		//    if (button == null || button.StrArray.Length == 0)
		//        continue;
		//    button.PointX = button.StrArray[0].PointX;
		//    lastCss = button.StrArray[button.StrArray.Length - 1];
		//    if (lastCss.Width >= 0)
		//        button.Width = lastCss.PointX - button.PointX + lastCss.Width;
		//    else if (i >= buttonList.Count - 1 || buttonList[i+1] == null || buttonList[i+1].StrArray.Length == 0)//行末
		//        button.Width = Config.WindowX;//右端のボタンについては右側全部をボタン領域にしてしまう
		//    else
		//        button.Width = buttonList[i+1].StrArray[0].PointX - button.PointX;
		//    if (button.Width < 0)
		//        button.Width = 0;//pos指定次第ではクリック不可能なボタンができてしまう。まあ仕方ない
		//}
	}

	#region EM_私家版_描画拡張
	// private static int getDivideIndex(ConsoleButtonString button, StringMeasure sm)
	private static int getDivideIndex(ConsoleButtonString button, StringMeasure sm, int divWidth = 0)
	#endregion
	{
		AConsoleDisplayNode divCss = null;
		int pointX = button.PointX;
		int strLength = 0;
		int index = 0;
		#region EM_私家版_描画拡張
		if (divWidth == 0) divWidth = Config.DrawableWidth;
		foreach (AConsoleDisplayNode css in button.StrArray)
		{
			// if (pointX + css.Width > Config.DrawableWidth)
			if (pointX + css.Width > divWidth)
			{
				if (index == 0 && !css.CanDivide)
					continue;
				divCss = css;
				break;
			}
			index++;
			strLength += css.Text.Length;
			pointX += css.Width;
		}
		if (divCss != null)
		{
			// int cssDivIndex = getDivideIndex(divCss, sm);
			int cssDivIndex = getDivideIndex(divCss, sm, divWidth);
			if (cssDivIndex > 0)
				strLength += cssDivIndex;
		}
		#endregion
		return strLength;
	}

	#region EM_私家版_描画拡張
	// private static int getDivideIndex(AConsoleDisplayNode part, StringMeasure sm)
	private static int getDivideIndex(AConsoleDisplayNode part, StringMeasure sm, int divWidth)
	#endregion
	{
		if (!part.CanDivide)
			return -1;
		#region EM_私家版_描画拡張
		if (divWidth == 0) divWidth = Config.DrawableWidth;
		ConsoleStyledString css = part as ConsoleStyledString;
		if (part == null)
			return -1;
		// int widthLimit = Config.DrawableWidth - css.PointX;
		int widthLimit = divWidth - css.PointX;
		#endregion
		string str = css.Text;
		Font font = css.Font;

		//最適なサイズを二分探索する
		var span = str.AsSpan();
		ReadOnlySpan<char> test;

		int biggestFitting = 0;
		int smallestNotFitting = str.Length;
		while (true)
		{
			int middle = (biggestFitting + smallestNotFitting) / 2;
			if (middle == biggestFitting) middle++;
			if (middle == smallestNotFitting) break;

			test = span[..middle];
			if (sm.GetDisplayLength(test, font) <= widthLimit)
			{
				biggestFitting = middle;
			}
			else
			{
				smallestNotFitting = middle;
			}
		}
		return biggestFitting;
	}
	#endregion

}
