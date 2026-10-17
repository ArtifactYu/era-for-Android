using MinorShift.Emuera.Runtime.Config;
using System.Collections.Generic;
using System.Drawing;

namespace MinorShift.Emuera.UI;

internal class FontFactory
{

	static readonly Dictionary<(string fontname, int fontSize, FontStyle fontStyle), Font> fontDic = [];

	public static Font GetFont(string requestFontName, FontStyle style)
	{
		/*
		string fontname = requestFontName;
		if (string.IsNullOrEmpty(requestFontName))
			fontname = Config.FontName;
		if (!fontDic.ContainsKey((fontname, Config.FontSize, style)))
		{
			var font = new Font(fontname, Config.FontSize, style, GraphicsUnit.Pixel);
			if (font == null)
			{
				return null;
			}
			else
			{
				fontDic.Add((fontname, Config.FontSize, style), font);
			}

		}
		#region EE_フォントファイル対応
		int fontsize = Config.FontSize;
		Font styledFont;
		foreach (FontFamily ff in GlobalStatic.Pfc.Families)
		{
			if (ff.Name == fontname)
			{
				styledFont = new Font(ff, fontsize, style, GraphicsUnit.Pixel);
				break;
			}
		}
		#endregion
		return fontDic[(fontname, Config.FontSize, style)];
		*/

		string fn = requestFontName;
		if (string.IsNullOrEmpty(requestFontName))
			fn = Config.FontName;

		// andEmuera: fontStyleDic はメソッドローカルだったため毎回作り直され、
		// 呼ばれるたびに new Font していた。文字列の実測幅は 1 パートごとに引かれるので、
		// Android では計測 1 回ごとに SKFont/SKTypeface の生成が走って効いてくる。
		// 既存の static な fontDic に一本化する。
		var key = (fn, Config.FontSize, style);
		if (fontDic.TryGetValue(key, out var cached))
			return cached;

		int fontsize = Config.FontSize;
		Font styledFont;
		try
		{
			#region EE_フォントファイル対応
			foreach (FontFamily ff in GlobalStatic.Pfc.Families)
			{
				if (ff.Name == fn)
				{
					styledFont = new Font(ff, fontsize, style, GraphicsUnit.Pixel);
					goto foundfont;
				}
			}
			styledFont = new Font(fn, fontsize, style, GraphicsUnit.Pixel);
		}
		catch
		{
			return null;
		}
	foundfont:
		#endregion
		fontDic.Add(key, styledFont);
		return styledFont;
	}

	public static void ClearFont()
	{
		foreach (var font in fontDic)
		{
			font.Value.Dispose();
		}
		fontDic.Clear();
	}

	#region 統合_netEmuera_fontサイズ
	/// <summary>
	/// &lt;font size&gt; で大きさが指定されたとき用。既定の大きさなら従来の GetFont に回す。
	/// GetFont 本体は andEmuera 側が丸ごと書き換えているので、そこには触らずここに足す
	/// </summary>
	public static Font GetFont(string requestFontName, FontStyle style, int requestFontSize)
	{
		if (requestFontSize <= 0 || requestFontSize == Config.FontSize)
			return GetFont(requestFontName, style);
		string fn = requestFontName;
		if (string.IsNullOrEmpty(requestFontName))
			fn = Config.FontName;
		var key = (fn, requestFontSize, style);
		if (fontDic.TryGetValue(key, out var cached))
			return cached;
		Font sizedFont;
		try
		{
			#region EE_フォントファイル対応
			foreach (FontFamily ff in GlobalStatic.Pfc.Families)
			{
				if (ff.Name == fn)
				{
					sizedFont = new Font(ff, requestFontSize, style, GraphicsUnit.Pixel);
					goto foundfont;
				}
			}
			sizedFont = new Font(fn, requestFontSize, style, GraphicsUnit.Pixel);
		}
		catch
		{
			return null;
		}
	foundfont:
		#endregion
		fontDic.Add(key, sizedFont);
		return sizedFont;
	}
	#endregion
}
