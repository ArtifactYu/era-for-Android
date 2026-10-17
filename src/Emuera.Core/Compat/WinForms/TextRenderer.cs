// andEmuera: System.Windows.Forms.TextRenderer の SkiaSharp 実装。
//
// Emuera はここで得た「文字列の表示幅」を桁揃え・ボタン折り返しの基準にしている。
// つまり本クラスの計測結果が WebView 側の実描画とズレるとレイアウトが崩れるため、
// Android 側では WebView に同じフォントファイル・同じ px サイズを渡して一致させる。

using System.Drawing;

namespace System.Windows.Forms
{
	[Flags]
	public enum TextFormatFlags
	{
		Default = 0,
		GlyphOverhangPadding = 0,
		Left = 0,
		Top = 0,
		HorizontalCenter = 0x00000001,
		Right = 0x00000002,
		VerticalCenter = 0x00000004,
		Bottom = 0x00000008,
		WordBreak = 0x00000010,
		SingleLine = 0x00000020,
		ExpandTabs = 0x00000040,
		NoClipping = 0x00000100,
		ExternalLeading = 0x00000200,
		NoPrefix = 0x00000800,
		Internal = 0x00001000,
		TextBoxControl = 0x00002000,
		PathEllipsis = 0x00004000,
		EndEllipsis = 0x00008000,
		ModifyString = 0x00010000,
		RightToLeft = 0x00020000,
		WordEllipsis = 0x00040000,
		NoFullWidthCharacterBreak = 0x00080000,
		HidePrefix = 0x00100000,
		PrefixOnly = 0x00200000,
		PreserveGraphicsClipping = 0x01000000,
		PreserveGraphicsTranslateTransform = 0x02000000,
		NoPadding = 0x10000000,
		LeftAndRightPadding = 0x20000000,
	}

	public static class TextRenderer
	{
		public static Size MeasureText(string text, Font font)
			=> MeasureText(text.AsSpan(), font);

		public static Size MeasureText(ReadOnlySpan<char> text, Font font)
		{
			if (text.IsEmpty || font == null)
				return new Size(0, font?.Height ?? 0);
			// 主フォントに無い文字は代替フォントで描かれるので、幅もそちらに合わせる
			// (計測と描画がズレると桁揃えとクリック判定が狂う)
			float w = GlyphFallback.Measure(font, text);
			return new Size((int)Math.Ceiling(w), font.Height);
		}

		public static Size MeasureText(string text, Font font, Size proposedSize)
			=> MeasureText(text.AsSpan(), font);

		public static Size MeasureText(string text, Font font, Size proposedSize, TextFormatFlags flags)
			=> MeasureText(text.AsSpan(), font);

		public static Size MeasureText(Graphics dc, string text, Font font, Size proposedSize, TextFormatFlags flags)
			=> MeasureText(text.AsSpan(), font);

		public static Size MeasureText(Graphics dc, ReadOnlySpan<char> text, Font font, Size proposedSize, TextFormatFlags flags)
			=> MeasureText(text, font);

		public static Size MeasureText(Graphics dc, ReadOnlySpan<char> text, Font font)
			=> MeasureText(text, font);

		public static void DrawText(Graphics dc, string text, Font font, Point pt, Color foreColor)
			=> DrawText(dc, text.AsSpan(), font, pt, foreColor, TextFormatFlags.Default);

		public static void DrawText(Graphics dc, string text, Font font, Point pt, Color foreColor, TextFormatFlags flags)
			=> DrawText(dc, text.AsSpan(), font, pt, foreColor, flags);

		public static void DrawText(Graphics dc, ReadOnlySpan<char> text, Font font, Point pt, Color foreColor, TextFormatFlags flags)
			=> DrawCore(dc, text, font, pt, foreColor, null);

		public static void DrawText(Graphics dc, ReadOnlySpan<char> text, Font font, Point pt, Color foreColor, Color backColor, TextFormatFlags flags)
			=> DrawCore(dc, text, font, pt, foreColor, backColor);

		public static void DrawText(Graphics dc, string text, Font font, Rectangle bounds, Color foreColor, TextFormatFlags flags)
			=> DrawCore(dc, text.AsSpan(), font, new Point(bounds.X, bounds.Y), foreColor, null);

		public static void DrawText(Graphics dc, string text, Font font, Rectangle bounds, Color foreColor, Color backColor, TextFormatFlags flags)
			=> DrawCore(dc, text.AsSpan(), font, new Point(bounds.X, bounds.Y), foreColor, backColor);

		public static void DrawText(Graphics dc, ReadOnlySpan<char> text, Font font, Rectangle bounds, Color foreColor, TextFormatFlags flags)
			=> DrawCore(dc, text, font, new Point(bounds.X, bounds.Y), foreColor, null);

		static void DrawCore(Graphics dc, ReadOnlySpan<char> text, Font font, Point pt, Color foreColor, Color? backColor)
		{
			if (dc == null || font == null || text.IsEmpty)
				return;

			string s = text.ToString();
			if (backColor.HasValue && backColor.Value.A != 0)
			{
				using var back = new SolidBrush(backColor.Value);
				var size = MeasureText(text, font);
				dc.FillRectangle(back, pt.X, pt.Y, size.Width, size.Height);
			}
			using var brush = new SolidBrush(foreColor);
			dc.DrawString(s, font, brush, new PointF(pt.X, pt.Y));
		}
	}
}
