// andEmuera: 主フォントが持たない文字を、端末の別フォントで描くためのフォールバック。
//
// Skia の DrawText はフォントリンクをしない。Windows の GDI+ / TextRenderer は
// 持っていない文字を勝手に別フォントへ回すため、**PC では出ていた記号が端末では
// .notdef (豆腐) になる**。erablue_resort の同梱フォント (BIZ UDGothic) で言うと
// ✕ (U+2715) が 400 箇所、❤ (U+2764) が 84 箇所、一部の CSV の简体字が該当する。
//
// 桁揃えを壊さないことが最優先。era は PRINTC の桁を「半角スペースで詰めてから
// 実測幅が枠を超える間だけ剥がす」で作る (docs/porting-notes.md の
// 「PRINTC の桁揃えは等幅フォント前提」) ので、代替グリフの送り幅は
// **主フォントの半角セルの整数倍にスナップ**して、グリッドを動かさない。
//
// 計測 (MeasureText) と描画 (DrawString) が同じ幅を返すことは絶対条件なので、
// 両方とも <see cref="Walk"/> という 1 本の走査を通す。

using System.Collections.Concurrent;
using SkiaSharp;

namespace System.Drawing
{
	/// <summary>
	/// 主フォントに無いグリフだけを別フォントで補う。
	/// <see cref="FontResolver"/> が「フォント名 → フェイス」を解決するのに対し、
	/// こちらは「1 文字 → その字を持っているフェイス」を解決する。
	/// </summary>
	public static class GlyphFallback
	{
		/// <summary>
		/// 代替フェイスを初めて決めたときに 1 行だけ呼ばれる。
		/// Android は Android.Util.Log、TestHarness は Console につなぐ。
		/// </summary>
		public static Action<string> Log { get; set; }

		/// <summary>
		/// 半角セル 1 個に収まったとみなす上限 (半角送り幅に対する比)。
		/// これを超える字は全角 1 セルに置く。丸め誤差ぶんの余裕を持たせてある。
		/// </summary>
		const float HalfCellTolerance = 1.15f;

		#region グリフの有無

		/// <summary>
		/// タイプフェイスごとのグリフ有無。BMP は配列で持つので 2 回目以降はほぼ無料。
		/// 追加面 (絵文字など) は数が知れているので辞書。
		/// </summary>
		sealed class Coverage
		{
			const byte Unknown = 0, Present = 1, Absent = 2;

			readonly SKTypeface typeface;
			readonly byte[] basic = new byte[0x10000];
			readonly ConcurrentDictionary<int, bool> astral = new();

			internal Coverage(SKTypeface typeface) => this.typeface = typeface;

			internal bool Has(int codePoint)
			{
				if (codePoint >= 0x10000)
					return astral.GetOrAdd(codePoint, cp => typeface.GetGlyph(cp) != 0);

				byte state = basic[codePoint];
				if (state != Unknown)
					return state == Present;
				bool has = typeface.GetGlyph(codePoint) != 0;
				basic[codePoint] = has ? Present : Absent;   // 競合しても同じ値を書くだけ
				return has;
			}
		}

		/// <summary>
		/// <c>ANDEMUERA_NO_GLYPH_FALLBACK=1</c> でフォールバックを丸ごと止める。
		/// 走査ぶんのコストを A/B で測るためと、万一この経路が悪さをしたときに
		/// 変更前の挙動 (欠け文字は豆腐) へ戻せるようにするため。
		/// </summary>
		static readonly bool disabled =
			Environment.GetEnvironmentVariable("ANDEMUERA_NO_GLYPH_FALLBACK") == "1";

		static readonly ConcurrentDictionary<SKTypeface, Coverage> coverages = new();

		static Coverage CoverageOf(SKTypeface typeface)
			=> coverages.GetOrAdd(typeface, face => new Coverage(face));

		/// <summary>そのフォントがこのコードポイントを持っているか。</summary>
		public static bool Covers(Font font, int codePoint)
		{
			var typeface = font?.Typeface;
			return typeface == null || CoverageOf(typeface).Has(codePoint);
		}

		/// <summary>
		/// 文字列全体を主フォントだけで描けるか。
		/// 描けるなら呼び出し側は従来どおりの 1 発の DrawText / MeasureText に落ちる。
		/// </summary>
		internal static bool AllCovered(Font font, ReadOnlySpan<char> text)
		{
			if (disabled)
				return true;
			var typeface = font?.Typeface;
			if (typeface == null)
				return true;

			var coverage = CoverageOf(typeface);
			for (int i = 0; i < text.Length; i++)
			{
				int codePoint = text[i];
				if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
					codePoint = char.ConvertToUtf32(text[i], text[++i]);
				if (!coverage.Has(codePoint))
					return false;
			}
			return true;
		}

		#endregion

		#region 代替フェイスの解決

		/// <summary>コードポイント → 代替フェイス。見つからなかったことも覚える (null)。</summary>
		static readonly ConcurrentDictionary<int, SKTypeface> substitutes = new();

		/// <summary>
		/// そのコードポイントを持つフェイスを探す。無ければ null。
		///
		/// 端末の日本語フォント (<see cref="FontResolver.Fallback"/>、Android 側が
		/// 起動時に入れている) を先に見る。ここが当たれば簡体字などは 1 本で片付き、
		/// フォントマネージャに毎回探させずに済む。
		/// </summary>
		public static SKTypeface Substitute(int codePoint)
			=> substitutes.GetOrAdd(codePoint, ResolveSubstitute);

		static SKTypeface ResolveSubstitute(int codePoint)
		{
			var preferred = FontResolver.Fallback;
			if (preferred != null && preferred.GetGlyph(codePoint) != 0)
				return Announce(codePoint, preferred);

			SKTypeface matched = null;
			try { matched = SKFontManager.Default.MatchCharacter(null, codePoint); }
			catch { }
			// MatchCharacter は当たらなくても既定フォントを返すことがある
			if (matched == null || matched.GetGlyph(codePoint) == 0)
				return null;
			return Announce(codePoint, matched);
		}

		static SKTypeface Announce(int codePoint, SKTypeface face)
		{
			Log?.Invoke($"代替フォント: {char.ConvertFromUtf32(codePoint)} U+{codePoint:X4} → {face.FamilyName}");
			return face;
		}

		/// <summary>(フェイス, サイズ, 合成太字, 合成斜体) → 描画用フォント。</summary>
		static readonly ConcurrentDictionary<(SKTypeface, float, bool, float), SKFont> fonts = new();

		/// <summary>代替フェイスを主フォントと同じサイズ・同じ合成設定で使う。</summary>
		static SKFont FontFor(SKTypeface face, Font primary)
		{
			var key = (face, primary.SkFont.Size, primary.SkFont.Embolden, primary.SkFont.SkewX);
			return fonts.GetOrAdd(key, k => new SKFont(k.Item1, k.Item2)
			{
				Embolden = k.Item3,
				SkewX = k.Item4,
			});
		}

		#endregion

		#region 計測と描画

		/// <summary>
		/// <c>ANDEMUERA_MEASURE_PROFILE=1</c> で計測の回数と時間を数える。
		///
		/// era は PRINTC の桁揃えを実測幅で作る (スペースを 1 個ずつ剥がしては測り直す) ので、
		/// 1 コマンドで何千回も通りうる。スクリプト時間の何割がここなのかを見るための窓。
		/// </summary>
		static readonly bool profile =
			Environment.GetEnvironmentVariable("ANDEMUERA_MEASURE_PROFILE") == "1";

		static long measureCalls, measureChars, measureTicks;

		/// <summary>これまでの計測回数。<see cref="profile"/> が立っているときだけ増える。</summary>
		public static long MeasureCalls => System.Threading.Interlocked.Read(ref measureCalls);

		/// <summary>これまでに計測した文字数の累計。</summary>
		public static long MeasureChars => System.Threading.Interlocked.Read(ref measureChars);

		/// <summary>計測に費やした累計時間 (ミリ秒)。</summary>
		public static double MeasureMs =>
			System.Threading.Interlocked.Read(ref measureTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

		/// <summary>
		/// 表示幅。主フォントに無い文字は半角セルの整数倍に丸めて数える。
		/// <see cref="Draw"/> と必ず同じ値になること (ズレるとボタン幅・行の折り返し・
		/// クリック当たり判定の基準がまとめて狂う)。
		/// </summary>
		public static float Measure(Font font, ReadOnlySpan<char> text)
		{
			if (font == null || text.IsEmpty)
				return 0f;
			if (!profile)
				return MeasureCore(font, text);

			long start = System.Diagnostics.Stopwatch.GetTimestamp();
			float width = MeasureCore(font, text);
			System.Threading.Interlocked.Add(ref measureTicks, System.Diagnostics.Stopwatch.GetTimestamp() - start);
			System.Threading.Interlocked.Increment(ref measureCalls);
			System.Threading.Interlocked.Add(ref measureChars, text.Length);
			return width;
		}

		static float MeasureCore(Font font, ReadOnlySpan<char> text)
			=> AllCovered(font, text)
				? font.SkFont.MeasureText(text)
				: Walk(font, text, null, 0f, 0f, null);

		/// <summary>ベースライン基準で描く。x は左端。</summary>
		internal static void Draw(SKCanvas canvas, ReadOnlySpan<char> text, Font font, float x, float baseline, SKPaint paint)
		{
			if (font == null || text.IsEmpty)
				return;
			Walk(font, text, canvas, x, baseline, paint);
		}

		/// <summary>
		/// 主フォントで描けるところは連続した固まりのまま処理し、描けない文字だけ
		/// 1 文字ずつセルに収める。<paramref name="canvas"/> が null なら計測だけ。
		/// </summary>
		static float Walk(Font font, ReadOnlySpan<char> text, SKCanvas canvas, float x, float baseline, SKPaint paint)
		{
			var coverage = font.Typeface == null ? null : CoverageOf(font.Typeface);
			float half = font.HalfCell;
			float advance = 0f;
			int runStart = 0;

			for (int i = 0; i < text.Length; i++)
			{
				int codePoint = text[i];
				int length = 1;
				if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
				{
					codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
					length = 2;
				}

				if (coverage == null || coverage.Has(codePoint))
				{
					i += length - 1;
					continue;
				}

				// ここまで溜めた固まりを主フォントで片付けてから、欠けた 1 文字を置く
				if (i > runStart)
					advance += Emit(text.Slice(runStart, i - runStart), font.SkFont, canvas, x + advance, baseline, paint);
				advance += EmitSubstitute(text.Slice(i, length), codePoint, font, half, canvas, x + advance, baseline, paint);

				i += length - 1;
				runStart = i + 1;
			}

			if (text.Length > runStart)
				advance += Emit(text.Slice(runStart), font.SkFont, canvas, x + advance, baseline, paint);
			return advance;
		}

		static float Emit(ReadOnlySpan<char> run, SKFont font, SKCanvas canvas, float x, float baseline, SKPaint paint)
		{
			canvas?.DrawText(run.ToString(), x, baseline, font, paint);
			return font.MeasureText(run);
		}

		/// <summary>
		/// 欠けた 1 文字を代替フェイスで描く。送り幅は主フォントの半角セル 1 個か 2 個。
		///
		/// どちらにするかは、まず MS ゴシックの分け方 (<see cref="MsGothicWidths"/>) を見る。
		/// era のスクリプトは Windows の既定フォントの幅で桁を組んでいるので、
		/// 端末のフォントが全角で持っている字でもそちらへ合わせないと枠が崩れる。
		/// 表に無い字は代替グリフ本来の送り幅で決める。EAW の表で決め打つより
		/// 見た目が素直で (漢字が半角に潰れない・記号が全角に間延びしない)、
		/// 計測と描画で同じ判定を通すので幅は必ず一致する。
		/// </summary>
		static float EmitSubstitute(ReadOnlySpan<char> ch, int codePoint, Font font, float half,
									SKCanvas canvas, float x, float baseline, SKPaint paint)
		{
			var face = Substitute(codePoint);
			if (face == null || half <= 0f)
				return Emit(ch, font.SkFont, canvas, x, baseline, paint);   // 打つ手なし。従来どおり主フォントへ

			var substitute = FontFor(face, font);
			float natural = substitute.MeasureText(ch);
			float cell = MsGothicWidths.IsHalfWidth(codePoint) ? half
					   : natural <= half * HalfCellTolerance ? half : half * 2f;
			// セルからはみ出す字だけ横に詰める (Save/Translate/Scale なので
			// 共有キャッシュした SKFont を書き換えずに済む = スレッド安全)
			float scale = natural > cell ? cell / natural : 1f;

			if (canvas != null)
			{
				canvas.Save();
				canvas.Translate(x + (cell - natural * scale) / 2f, baseline);
				if (scale != 1f)
					canvas.Scale(scale, 1f);
				canvas.DrawText(ch.ToString(), 0f, 0f, substitute, paint);
				canvas.Restore();
			}
			return cell;
		}

		#endregion

		/// <summary>解決済みの対応を捨てる (フォント構成を切り替えるテスト用)。</summary>
		public static void Clear()
		{
			coverages.Clear();
			substitutes.Clear();
			fonts.Clear();
		}
	}
}
