// andEmuera: Microsoft.VisualBasic.Strings.StrConv の置き換え。
//
// StrConv は Windows の NLS API に依存しており、Android では
// PlatformNotSupportedException で落ちる。era のスクリプトは
// ひらがな⇔カタカナ・半角⇔全角の変換に使うので、自前で実装する。

using System;
using System.Text;

namespace MinorShift.Emuera.Runtime.Utils
{
	public static class JapaneseText
	{
		// 半角カナと全角カナの対応表 (同じ添字が対応する)
		const string HalfKana = "｡｢｣､･ｦｧｨｩｪｫｬｭｮｯｰｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝﾞﾟ";
		const string FullKana = "。「」、・ヲァィゥェォャュョッーアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワン゛゜";

		// 濁点・半濁点を合成できる全角カナ (清音 → 濁音 / 半濁音)
		const string Voiceless = "カキクケコサシスセソタチツテトハヒフヘホウワヰヱヲ";
		const string Voiced = "ガギグゲゴザジズゼゾダヂヅデドバビブベボヴヷヸヹヺ";
		const string SemiVoicelessBase = "ハヒフヘホ";
		const string SemiVoiced = "パピプペポ";

		/// <summary>ひらがなをカタカナにする (VbStrConv.Katakana 相当)。</summary>
		public static string ToKatakana(string str)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			var sb = new StringBuilder(str.Length);
			foreach (char c in str)
			{
				// ぁ(3041)〜ゖ(3096) を ァ(30A1)〜ヶ(30F6) へ
				sb.Append(c >= 'ぁ' && c <= 'ゖ' ? (char)(c + 0x60) : c);
			}
			return sb.ToString();
		}

		/// <summary>カタカナをひらがなにする (VbStrConv.Hiragana 相当)。</summary>
		public static string ToHiragana(string str)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			var sb = new StringBuilder(str.Length);
			foreach (char c in str)
			{
				sb.Append(c >= 'ァ' && c <= 'ヶ' ? (char)(c - 0x60) : c);
			}
			return sb.ToString();
		}

		/// <summary>半角を全角にする (VbStrConv.Wide 相当)。</summary>
		public static string ToWide(string str)
		{
			if (string.IsNullOrEmpty(str))
				return str;

			var sb = new StringBuilder(str.Length);
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				int kana = HalfKana.IndexOf(c);
				if (kana >= 0)
				{
					char full = FullKana[kana];
					// 次が濁点・半濁点なら 1 文字に合成する (ｶ + ﾞ → ガ)
					if (i + 1 < str.Length)
					{
						char next = str[i + 1];
						if (next == 'ﾞ')
						{
							int v = Voiceless.IndexOf(full);
							if (v >= 0) { sb.Append(Voiced[v]); i++; continue; }
						}
						else if (next == 'ﾟ')
						{
							int v = SemiVoicelessBase.IndexOf(full);
							if (v >= 0) { sb.Append(SemiVoiced[v]); i++; continue; }
						}
					}
					sb.Append(full);
					continue;
				}

				if (c == ' ')
					sb.Append('　');            // 半角スペース → 全角スペース
				else if (c >= '!' && c <= '~')
					sb.Append((char)(c + 0xFEE0));  // ASCII → 全角英数記号
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>全角を半角にする (VbStrConv.Narrow 相当)。</summary>
		public static string ToNarrow(string str)
		{
			if (string.IsNullOrEmpty(str))
				return str;

			var sb = new StringBuilder(str.Length);
			foreach (char c in str)
			{
				// 濁音・半濁音は清音 + 濁点に分解する (ガ → ｶ + ﾞ)
				int v = Voiced.IndexOf(c);
				if (v >= 0)
				{
					int k = FullKana.IndexOf(Voiceless[v]);
					if (k >= 0) { sb.Append(HalfKana[k]).Append('ﾞ'); continue; }
				}
				int sv = SemiVoiced.IndexOf(c);
				if (sv >= 0)
				{
					int k = FullKana.IndexOf(SemiVoicelessBase[sv]);
					if (k >= 0) { sb.Append(HalfKana[k]).Append('ﾟ'); continue; }
				}

				int kana = FullKana.IndexOf(c);
				if (kana >= 0)
				{
					sb.Append(HalfKana[kana]);
					continue;
				}

				if (c == '　')
					sb.Append(' ');
				else if (c >= '！' && c <= '～')
					sb.Append((char)(c - 0xFEE0));
				else
					sb.Append(c);
			}
			return sb.ToString();
		}
	}
}
