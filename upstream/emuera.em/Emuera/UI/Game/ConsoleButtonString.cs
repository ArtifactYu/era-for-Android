using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MinorShift.Emuera.UI.Game;

/// <summary>
/// ボタン。1つ以上の装飾付文字列（ConsoleStyledString）からなる。
/// </summary>
internal sealed class ConsoleButtonString
{
	#region EM_私家版_imgマースク
	void getLastImg()
	{
		for (int i = strArray.Length - 1; i > -1; i--)
		{
			if (strArray[i] is ConsoleImagePart img)
			{
				mask = img;
				break;
			}
		}
	}
	public ConsoleButtonString(EmueraConsole console, AConsoleDisplayNode[] strs)
	{
		parent = console;
		strArray = strs;
		IsButton = false;
		PointX = -1;
		Width = -1;
		ErrPos = null;
	}
	public ConsoleButtonString(EmueraConsole console, AConsoleDisplayNode[] strs, long input)
		: this(console, strs)
	{
		Input = input;
		Inputs = input.ToString();
		IsButton = true;
		IsInteger = true;
		getLastImg();
		if (console != null)
		{
			Generation = parent.NewButtonGeneration;
			console.UpdateGeneration();
		}
		ErrPos = null;
	}
	public ConsoleButtonString(EmueraConsole console, AConsoleDisplayNode[] strs, string inputs)
		: this(console, strs)
	{
		Inputs = inputs;
		IsButton = true;
		IsInteger = false;
		getLastImg();
		if (console != null)
		{
			Generation = parent.NewButtonGeneration;
			console.UpdateGeneration();
		}
		ErrPos = null;
	}

	public ConsoleButtonString(EmueraConsole console, AConsoleDisplayNode[] strs, long input, string inputs)
		: this(console, strs)
	{
		Input = input;
		Inputs = inputs;
		IsButton = true;
		IsInteger = true;
		getLastImg();
		if (console != null)
		{
			Generation = parent.NewButtonGeneration;
			console.UpdateGeneration();
		}
		ErrPos = null;
	}
	public ConsoleButtonString(EmueraConsole console, AConsoleDisplayNode[] strs, string inputs, ScriptPosition? pos)
		: this(console, strs)
	{
		Inputs = inputs;
		IsButton = true;
		IsInteger = false;
		getLastImg();
		if (console != null)
		{
			Generation = parent.NewButtonGeneration;
			console.UpdateGeneration();
		}
		ErrPos = pos;
	}
	public long GetMappedColor(int pointX, int pointY)
	{
		if (mask != null)
		{
			var offsetX = pointX - PointX - mask.PointX - Config.DrawingParam_ShapePositionShift;
			var offsetY = pointY - parent.GetLinePointY(ParentLine.LineNo) - mask.Top;
			if (offsetX > 0 && offsetX < mask.Width && offsetY > 0 && offsetY < mask.Bottom - mask.Top)
				return mask.GetMappingColor(offsetX, offsetY);
		}
		return 0;
	}

	//Bitmap Cache
	public Bitmap bitmapCache;

	ConsoleImagePart mask;
	#endregion

	AConsoleDisplayNode[] strArray;
	public AConsoleDisplayNode[] StrArray { get { return strArray; } }
	EmueraConsole parent;

	public ConsoleDisplayLine ParentLine { get; set; }
	public bool IsButton { get; private set; }
	public bool IsInteger { get; private set; }
	public long Input { get; private set; }
	public string Inputs { get; private set; }
	public int PointX { get; set; }
	public bool PointXisLocked { get; set; }
	public int Width { get; set; }
	public float XsubPixel { get; set; }
	public long Generation { get; private set; }
	public ScriptPosition? ErrPos { get; set; }
	public string Title { get; set; }


	public int RelativePointX { get; private set; }
	public void LockPointX(int rel_px)
	{
		PointX = rel_px * Config.FontSize / 100;
		XsubPixel = rel_px * Config.FontSize / 100.0f - PointX;
		PointXisLocked = true;
		RelativePointX = rel_px;
	}

	#region EM_私家版_描画拡張
	public AConsoleDisplayNode[] EscapedParts { get { return escaped; } }
	AConsoleDisplayNode[] escaped;
	bool escapeFilterApplied;

	public void FilterEscaped()
	{
		if (!escapeFilterApplied)
		{
			//統合_netEmuera: div は DrawTo で常に読み飛ばされるので、
			//行内に収まる大きさでもここで拾わないと一切描画されない。
			//従来は width/height が必須で行高を超えるものしか無かったため表面化していなかった
			var e = strArray.Where(p => p.Top < 0 || p.Bottom > Config.LineHeight || p is ConsoleDivPart).ToArray();
			foreach (var p in e) if (p is ConsoleDivPart div) div.IsEscaped = true;
			if (e.Length > 0) escaped = e;
			escapeFilterApplied = true;
		}
	}
	#endregion

	//indexの文字数の前方文字列とindex以降の後方文字列に分割
	public ConsoleButtonString DivideAt(int divIndex, StringMeasure sm)
	{
		if (divIndex <= 0)
			return null;
		List<AConsoleDisplayNode> cssListA = [];
		List<AConsoleDisplayNode> cssListB = [];
		int index = 0;
		int cssIndex;
		bool b = false;
		for (cssIndex = 0; cssIndex < strArray.Length; cssIndex++)
		{
			if (b)
			{
				cssListB.Add(strArray[cssIndex]);
				continue;
			}
			int length = strArray[cssIndex].Text.Length;
			if (divIndex < index + length)
			{
				ConsoleStyledString oldcss = strArray[cssIndex] as ConsoleStyledString;
				if (oldcss == null || !oldcss.CanDivide)
					throw new ExeEE("文字列分割異常");
				ConsoleStyledString newCss = oldcss.DivideAt(divIndex - index, sm);
				cssListA.Add(oldcss);
				if (newCss != null)
					cssListB.Add(newCss);
				b = true;
				continue;
			}
			else if (divIndex == index + length)
			{
				cssListA.Add(strArray[cssIndex]);
				b = true;
				continue;
			}
			index += length;
			cssListA.Add(strArray[cssIndex]);
		}
		if (cssIndex >= strArray.Length && cssListB.Count == 0)
			return null;
		AConsoleDisplayNode[] cssArrayA = new AConsoleDisplayNode[cssListA.Count];
		AConsoleDisplayNode[] cssArrayB = new AConsoleDisplayNode[cssListB.Count];
		cssListA.CopyTo(cssArrayA);
		cssListB.CopyTo(cssArrayB);
		strArray = cssArrayA;
		ConsoleButtonString ret = new(null, cssArrayB);
		CalcWidth(sm, XsubPixel);
		ret.CalcWidth(sm, 0);
		CalcPointX(PointX);
		ret.CalcPointX(PointX + Width);
		ret.parent = parent;
		ret.ParentLine = ParentLine;
		ret.IsButton = IsButton;
		ret.IsInteger = IsInteger;
		ret.Input = Input;
		ret.Inputs = Inputs;
		ret.Generation = Generation;
		ret.ErrPos = ErrPos;
		ret.Title = Title;
		return ret;
	}

	public void CalcWidth(StringMeasure sm, float subpixel)
	{
		Width = -1;
		if (strArray != null && strArray.Length > 0)
		{
			Width = 0;
			foreach (AConsoleDisplayNode css in strArray)
			{
				if (css.Width <= 0)
					css.SetWidth(sm, subpixel);
				Width += css.Width;
				subpixel = css.XsubPixel;
			}
			if (Width <= 0)
				Width = -1;
		}
		XsubPixel = subpixel;
	}

	/// <summary>
	/// 先にCalcWidthすること。
	/// </summary>
	/// <param name="sm"></param>
	public void CalcPointX(int pointx)
	{
		int px = pointx;
		if (!PointXisLocked)
			PointX = px;
		else
			px = PointX;
		for (int i = 0; i < strArray.Length; i++)
		{
			#region EM_私家版_HTML_divタグ
			if (strArray[i] is ConsoleDivPart div && !div.IsRelative) continue;
			#endregion
			strArray[i].PointX = px;
			px += strArray[i].Width;
		}
		if (strArray.Length > 0)
		{
			PointX = strArray[0].PointX;
			Width = strArray[^1].PointX + strArray[^1].Width - PointX;
			//if (Width < 0)
			//	Width = -1;
		}
	}

	internal void ShiftPositionX(int shiftX)
	{
		PointX += shiftX;
		foreach (AConsoleDisplayNode css in strArray)
			css.PointX += shiftX;
	}

	/// <summary>
	/// forceSelecting / forceFocus は div の中身を描くときに親の ConsoleDivPart から渡される。
	/// &lt;button&gt;&lt;div&gt;…&lt;/div&gt;&lt;/button&gt; の中身はここでは非ボタンなので、自力では選択状態を持てない
	/// </summary>
	public void DrawTo(Graphics graph, int pointY, bool isBackLog, TextDrawingMode mode, bool forceSelecting = false, bool forceFocus = false)
	{
		#region 統合_netEmuera_button内div
		bool isFocus = forceFocus || (IsButton && parent.ButtonIsSelected(this));
		bool isSelecting = forceSelecting || (IsButton && parent.ButtonIsPointing(this));
		#endregion
		#region EM_私家版_描画拡張
		//foreach (AConsoleDisplayNode css in strArray)
		//	css.DrawTo(graph, pointY, isSelecting, isBackLog, mode);

		//Bitmap Cache
		if (ParentLine.bitmapCacheEnabled && strArray.Length > 1)
		{
			if (bitmapCache == null)
			{
				int width = Width + 1;
				//^ Without +1, some things get cropped. I don't know why, probably a bug somewhere.
				//TODO
				int height = Config.FontSize;
				bitmapCache = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				Graphics g = Graphics.FromImage(bitmapCache);

				int xOffset = 0;
				foreach (AConsoleDisplayNode css in strArray)
				{
					if (css is not ConsoleStyledString) continue;
					ConsoleStyledString willDrawHere = css as ConsoleStyledString;
					willDrawHere.DrawToBitmap(g, isSelecting, isBackLog, mode, xOffset);
					xOffset += css.Width;
				}

				nint index = GlobalStatic.Console.bitmapCacheArrayIndex;
				ConsoleButtonString last = GlobalStatic.Console.bitmapCacheArray[index];
				if (last != null)
				{
					last.bitmapCache.Dispose();
					last.bitmapCache = null;
				}
				GlobalStatic.Console.bitmapCacheArray[index] = this;
				index++;
				if (index >= EmueraConsole.bitmapCacheArrayCap) index = 0;
				GlobalStatic.Console.bitmapCacheArrayIndex = index;

			}
			graph.DrawImageUnscaled(bitmapCache, PointX, pointY);
			return;
		}

		foreach (AConsoleDisplayNode css in strArray)
		{
			if (css is ConsoleDivPart div) continue;
			css.DrawTo(graph, pointY, isSelecting, isFocus, isBackLog, mode, IsButton);
		}
		#endregion
	}

	#region EM_私家版_描画拡張
	public void DrawPartTo(Graphics graph, AConsoleDisplayNode css, int pointY, bool isBackLog, TextDrawingMode mode)
	{
		bool isFocus = IsButton && parent.ButtonIsSelected(this);
		bool isSelecting = IsButton && parent.ButtonIsPointing(this);
		css.DrawTo(graph, pointY, isSelecting, isFocus, isBackLog, mode);
	}
	#endregion

	readonly static StringBuilder builder = new();
	public override string ToString()
	{
		if (strArray == null)
			return "";
		builder.Clear();
		foreach (var css in strArray)
			builder.Append(css.ToString());
		return builder.ToString();
	}

	#region EM_私家版_描画拡張
	public StringBuilder BuildString(StringBuilder builder)
	{
		if (strArray != null)
			foreach (AConsoleDisplayNode css in strArray)
				css.BuildString(builder);
		return builder;
	}
	#endregion
}
