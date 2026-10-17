using MinorShift.Emuera.Runtime.Config;
using System.Drawing;
using System.Text;

namespace MinorShift.Emuera.UI.Game;

/// <summary>
/// 描画の最小単位
/// </summary>
abstract class AConsoleDisplayNode
{
	public bool Error { get; protected set; }

	public string Text { get; protected set; }
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

	public abstract void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isFocus, bool isBackLog, TextDrawingMode mode, bool isButton = false);

	public abstract void SetWidth(StringMeasure sm, float subPixel);
	public override string ToString()
	{
		if (Text == null)
			return "";
		return Text;
	}

	#region EM_私家版_描画拡張
	public ConsoleButtonString Parent { get; set; }
	public int Depth { get; set; }
	public virtual StringBuilder BuildString(StringBuilder sb)
	{
		if (Text != null) sb.Append(Text);
		return sb;
	}
	#endregion

	#region EmuEra-Rikaichan
	public bool rikaichaned;
	public int[] Ends;
	public AConsoleDisplayNode NextLine;
	#endregion
}

/// <summary>
/// 色つき
/// </summary>
abstract class AConsoleColoredPart : AConsoleDisplayNode
{
	protected Color Color { get; set; }
	protected Color ButtonColor { get; set; }
	protected bool colorChanged;
}
