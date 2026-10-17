using System.Drawing;
using System.Text;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.UI.Game;

namespace MinorShift.Emuera.GameView;

/// <summary>
/// 描画の最小単位
/// </summary>
abstract class AConsoleDisplayPart
{
	public bool Error { get; protected set; }

	public string Str { get; protected set; }
	public string AltText { get; protected set; }
	#region EM_私家版_描画拡張
	// public int PointX { get; set; }
	public virtual int PointX { get; set; }
	#endregion
	public float XsubPixel { get; set; }
	public float WidthF { get; set; }
	public int Width { get; set; }
	public virtual int Top { get { return 0; } }
	public virtual int Bottom { get { return Config.FontSize; } }
	public abstract bool CanDivide { get; }

	public abstract void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode);

	public abstract void SetWidth(StringMeasure sm, float subPixel);
	public override string ToString()
	{
		if (Str == null)
			return "";
		return Str;
	}

	#region EM_私家版_描画拡張
	public ConsoleButtonString Parent { get; set; }
	public int Depth { get; set; }
	public virtual StringBuilder BuildString(StringBuilder sb)
	{
		if (Str != null) sb.Append(Str);
		return sb;
	}
	#endregion

	#region EmuEra-Rikaichan
	public bool rikaichaned = false;
	public int[] Ends = null;
	public AConsoleDisplayPart NextLine;
	#endregion
}

/// <summary>
/// 色つき
/// </summary>
abstract class AConsoleColoredPart : AConsoleDisplayPart
{
	protected Color Color { get; set; }
	protected Color ButtonColor { get; set; }
	protected bool colorChanged;
}
