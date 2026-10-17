using MinorShift.Emuera.Runtime.Config;
using System.Drawing;

namespace MinorShift.Emuera.UI.Game;

#nullable enable
/// <summary>
/// 装飾付文字列(ConsoleStyledString)用のスタイル構造体
/// </summary>
internal struct StringStyle
{
	public StringStyle(Color color, FontStyle fontStyle, string? fontname)
	{
		Color = color;
		ButtonColor = Config.FocusColor;
		ColorChanged = false;//こっちのパターンでは色変更を後で検知
		FontStyle = fontStyle;
		if (string.IsNullOrEmpty(fontname))
			Fontname = Config.FontName;
		else
			Fontname = fontname;
		FontSize = 0;
	}

	/// <summary>
	/// HTML用。ColorChangedを固定する。
	/// </summary>
	//統合_netEmuera_fontサイズ: fontSize は <font size> 用。0 は未指定
	public StringStyle(Color color, bool colorChanged, Color buttonColor, FontStyle fontStyle, string fontname, int fontSize = 0)
	{
		Color = color;
		ButtonColor = buttonColor;
		ColorChanged = colorChanged;
		FontStyle = fontStyle;
		if (string.IsNullOrEmpty(fontname))
			Fontname = Config.FontName;
		else
			Fontname = fontname;
		FontSize = fontSize;
	}

	public Color Color;
	public Color ButtonColor;
	public bool ColorChanged;
	public FontStyle FontStyle;
	public string Fontname;
	#region 統合_netEmuera_fontサイズ
	/// <summary>
	/// &lt;font size&gt; で指定されたピクセル単位の文字の大きさ。0 は未指定。
	/// 設定のフォントサイズは実行中に変わるので、既定値をここに写し取らない
	/// </summary>
	public int FontSize;
	#endregion
	public override bool Equals(object? obj)
	{
		if (obj == null || obj is not StringStyle ss)
			return false;
		return Color == ss.Color && ButtonColor == ss.ButtonColor && ColorChanged == ss.ColorChanged && FontStyle == ss.FontStyle && FontSize == ss.FontSize && Fontname.Equals(ss.Fontname, Config.SCIgnoreCase);
	}
	public override int GetHashCode()
	{
		return Color.GetHashCode() ^ ButtonColor.GetHashCode() ^ ColorChanged.GetHashCode() ^ FontStyle.GetHashCode() ^ FontSize.GetHashCode() ^ Fontname.GetHashCode();
	}
	public static bool operator ==(StringStyle x, StringStyle y)
	{
		return x.Color == y.Color && x.ButtonColor == y.ButtonColor && x.ColorChanged == y.ColorChanged && x.FontStyle == y.FontStyle && x.FontSize == y.FontSize && x.Fontname.Equals(y.Fontname, Config.SCIgnoreCase);
	}
	public static bool operator !=(StringStyle x, StringStyle y)
	{
		return !(x == y);
	}
}
