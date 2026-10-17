// andEmuera: System.Drawing.Font / FontFamily の SkiaSharp 実装。
//
// Emuera はフォントを GraphicsUnit.Pixel で生成し、Size をピクセル em サイズとして扱う。
// SkiaSharp の SKFont.Size も同じくピクセル em サイズなので、そのまま対応づけられる。

using System.IO;
using SkiaSharp;

namespace System.Drawing
{
	public sealed class FontFamily : IDisposable
	{
		internal SKTypeface Typeface { get; }

		public FontFamily(string name)
		{
			Name = name;
			Typeface = FontResolver.Resolve(name, FontStyle.Regular);
		}

		internal FontFamily(SKTypeface typeface)
		{
			Typeface = typeface;
			Name = typeface.FamilyName;
		}

		public string Name { get; }

		/// <summary>em 単位でのフォントデザイン単位。GDI+ 互換のため 2048 を返す。</summary>
		public int GetEmHeight(FontStyle style) => 2048;

		/// <summary>行送り (デザイン単位)。Skia のメトリクスから em 比で換算する。</summary>
		public int GetLineSpacing(FontStyle style)
		{
			using var font = new SKFont(Typeface, 2048f);
			var m = font.Metrics;
			return (int)Math.Ceiling(m.Descent - m.Ascent + m.Leading);
		}

		public int GetCellAscent(FontStyle style)
		{
			using var font = new SKFont(Typeface, 2048f);
			return (int)Math.Ceiling(-font.Metrics.Ascent);
		}

		public int GetCellDescent(FontStyle style)
		{
			using var font = new SKFont(Typeface, 2048f);
			return (int)Math.Ceiling(font.Metrics.Descent);
		}

		public static FontFamily GenericMonospace => new("monospace");
		public static FontFamily GenericSansSerif => new("sans-serif");
		public static FontFamily GenericSerif => new("serif");

		public void Dispose() { }
	}

	public sealed class Font : IDisposable
	{
		public Font(string familyName, float emSize)
			: this(familyName, emSize, FontStyle.Regular, GraphicsUnit.Point) { }

		public Font(string familyName, float emSize, FontStyle style)
			: this(familyName, emSize, style, GraphicsUnit.Point) { }

		public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit)
		{
			Name = familyName;
			Size = emSize;
			Style = style;
			Unit = unit;
			Typeface = FontResolver.Resolve(familyName, style);
			SkFont = CreateSkFont(Typeface, SizeInPixels, style);
		}

		public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit)
		{
			Name = family.Name;
			Size = emSize;
			Style = style;
			Unit = unit;
			Typeface = PickForStyle(family, style);
			SkFont = CreateSkFont(Typeface, SizeInPixels, style);
		}

		/// <summary>
		/// 上流の <c>FontFactory</c> は PrivateFontCollection の同名フェイスを先頭一致で拾うため、
		/// BIZ UDGothic のように Regular と Bold でファミリ名が同じフォントだと
		/// 太字の要求に並字のフェイスが渡ってくる。ここで登録済みの正しいフェイスへ引き直す。
		/// </summary>
		static SKTypeface PickForStyle(FontFamily family, FontStyle style)
		{
			var typeface = family.Typeface;
			if (typeface == null)
				return FontResolver.Resolve(family.Name, style);
			if (FontResolver.StyleOf(typeface) == FontResolver.FaceStyle(style))
				return typeface;
			// 別スタイルが登録されていなければ、このフェイスのまま合成に任せる
			return FontResolver.ResolveRegistered(family.Name, style) ?? typeface;
		}

		/// <summary>
		/// 太字・斜体のフェイスが実在しないときだけ Skia に合成させる。
		/// Embolden も SkewX も<b>送り幅を変えない</b>ので、PRINTC の桁揃えを壊さない
		/// (Emuera は文字列の実測幅で桁を決めるため、ここで幅が動くと表示が崩れる)。
		/// </summary>
		static SKFont CreateSkFont(SKTypeface typeface, float sizeInPixels, FontStyle style)
		{
			var font = new SKFont(typeface, sizeInPixels)
			{
				// 送り幅をヒンティングで整数へ丸めさせない。
				// Android の Skia は既定で丸めるため、フォントサイズが奇数のゲーム
				// (eraTOWN は 17px ＝ 半角セル 8.5px) だと 半角 2 個 (8+8) ≠ 全角 1 個 (17) となり、
				// PRINTC の桁揃えが成立しなくなる。PC 側は丸めないので実機だけ崩れ、再現もできなくなる。
				// 偶数サイズ (erablue_resort の 16px) では丸めても値が変わらないため影響しない
				LinearMetrics = true,
				Subpixel = true,
			};
			var actual = FontResolver.StyleOf(typeface);
			if ((style & FontStyle.Bold) != 0 && (actual & FontStyle.Bold) == 0)
				font.Embolden = true;
			if ((style & FontStyle.Italic) != 0 && (actual & FontStyle.Italic) == 0)
				font.SkewX = -0.25f;
			return font;
		}

		public Font(FontFamily family, float emSize, FontStyle style)
			: this(family, emSize, style, GraphicsUnit.Point) { }

		public Font(FontFamily family, float emSize)
			: this(family, emSize, FontStyle.Regular, GraphicsUnit.Point) { }

		public Font(Font prototype, FontStyle newStyle)
			: this(prototype.Name, prototype.Size, newStyle, prototype.Unit) { }

		internal SKTypeface Typeface { get; }
		internal SKFont SkFont { get; }

		float? halfCell;

		/// <summary>
		/// 半角 1 文字ぶんの送り幅。<see cref="GlyphFallback"/> が
		/// 「主フォントに無い文字」を置くセルの単位に使う。
		/// エンジンは PRINTC の桁を半角スペースで作るので、測るのもスペースが正しい。
		/// </summary>
		internal float HalfCell => halfCell ??= SkFont.MeasureText(" ");

		public string Name { get; }
		public float Size { get; }
		public FontStyle Style { get; }
		public GraphicsUnit Unit { get; }

		public FontFamily FontFamily => new(Typeface);

		public bool Bold => (Style & FontStyle.Bold) != 0;
		public bool Italic => (Style & FontStyle.Italic) != 0;
		public bool Underline => (Style & FontStyle.Underline) != 0;
		public bool Strikeout => (Style & FontStyle.Strikeout) != 0;

		/// <summary>ピクセル換算した em サイズ。Point 指定の場合は 96dpi で換算する。</summary>
		internal float SizeInPixels => Unit == GraphicsUnit.Point ? Size * 96f / 72f : Size;

		public float SizeInPoints => Unit == GraphicsUnit.Point ? Size : Size * 72f / 96f;

		/// <summary>GDI+ の Font.Height 相当（行の高さ、切り上げたピクセル値）。</summary>
		public int Height
		{
			get
			{
				var m = SkFont.Metrics;
				return (int)Math.Ceiling(m.Descent - m.Ascent + m.Leading);
			}
		}

		public float GetHeight() => Height;

		public float GetHeight(Graphics graphics) => Height;

		public void Dispose() => SkFont?.Dispose();

		public override string ToString() => $"[Font: Name={Name}, Size={Size}, Style={Style}]";
	}

	/// <summary>
	/// フォント名から SKTypeface を解決する。Android には Windows のフォントが無いため、
	/// ゲームフォルダの font/ やアプリ同梱フォントを先に引き当て、
	/// 無ければ端末のフォントにフォールバックする。
	///
	/// era のスクリプトは PRINTC などの桁揃えをエンジンの実測幅に任せており、
	/// 「半角 = 全角の半分」が成り立つ等幅フォントでないと表示が崩れる。
	/// ゲームが同梱している等幅フォント (BIZ UDGothic 等) をここで確実に拾うことが要。
	/// </summary>
	public static class FontResolver
	{
		/// <summary>(フォント名, フェイスのスタイル) → 実体。</summary>
		static readonly System.Collections.Generic.Dictionary<string, SKTypeface> registered =
			new(StringComparer.OrdinalIgnoreCase);

		/// <summary>フォント名 → 登録されている全スタイル。近いスタイルを選ぶのに使う。</summary>
		static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SKTypeface>> byName =
			new(StringComparer.OrdinalIgnoreCase);

		static readonly System.Collections.Concurrent.ConcurrentDictionary<(string, FontStyle), SKTypeface> cache = new();

		/// <summary>フォント名の解決に失敗したときに使う既定フォント。</summary>
		public static SKTypeface Fallback { get; set; }

		/// <summary>
		/// 端末にインストールされたフォントを名前で引くかどうか。
		/// テストで「そのフォントが入っていない端末」を再現するために切れるようにしてある。
		/// </summary>
		public static bool UseSystemFonts { get; set; } = true;

		/// <summary>タイプフェイスの実体が持っているスタイル。</summary>
		internal static FontStyle StyleOf(SKTypeface typeface)
		{
			if (typeface == null)
				return FontStyle.Regular;
			var style = FontStyle.Regular;
			if (typeface.FontStyle.Weight >= (int)SKFontStyleWeight.SemiBold)
				style |= FontStyle.Bold;
			if (typeface.FontStyle.Slant != SKFontStyleSlant.Upright)
				style |= FontStyle.Italic;
			return style;
		}

		/// <summary>フェイスの選択に関わるビットだけ取り出す (下線・打ち消し線は字形に影響しない)。</summary>
		internal static FontStyle FaceStyle(FontStyle style) => style & (FontStyle.Bold | FontStyle.Italic);

		static string Key(string name, FontStyle style) => name + " " + (int)FaceStyle(style);

		/// <summary>アプリ同梱フォントを名前で登録する。スタイルはフォント自身の値を見て振り分ける。</summary>
		public static void Register(string name, SKTypeface typeface)
		{
			if (typeface == null)
				return;
			RegisterAs(name, typeface);
			// 実ファミリ名でも引けるようにしておく
			RegisterAs(typeface.FamilyName, typeface);
			cache.Clear();
		}

		static void RegisterAs(string name, SKTypeface typeface)
		{
			if (string.IsNullOrEmpty(name))
				return;
			registered[Key(name, StyleOf(typeface))] = typeface;
			if (!byName.TryGetValue(name, out var list))
				byName[name] = list = [];
			if (!list.Contains(typeface))
				list.Add(typeface);
		}

		/// <summary>フォントファイルを読み込んで登録する。</summary>
		public static SKTypeface RegisterFile(string path, string name = null)
		{
			var typeface = SKTypeface.FromFile(path);
			if (typeface == null)
				return null;
			Register(name ?? typeface.FamilyName, typeface);
			return typeface;
		}

		/// <summary>
		/// フォルダ内の ttf / otf / ttc をまとめて登録する。
		/// 上流 Emuera が Program.Main でゲームフォルダの font/ を読むのと同じ役割。
		/// ファミリ名に加えてファイル名 (拡張子なし) でも引けるようにする —
		/// emuera.config のフォント名指定が実ファミリ名と一致しないゲームがあるため。
		/// </summary>
		/// <returns>登録できたフェイスの数。</returns>
		public static int RegisterDirectory(string dir) => RegisterDirectory(dir, out _);

		/// <inheritdoc cref="RegisterDirectory(string)"/>
		/// <param name="dir">探すフォルダ。</param>
		/// <param name="representative">
		/// 既定フォント (<see cref="Fallback"/>) の候補として使えるフェイス。並字を優先し、
		/// 無ければ最初に読めたもの。1 つも読めなければ null。
		/// 「利用者が置いたフォントを既定にする」用途で Android 側が使う。
		/// </param>
		public static int RegisterDirectory(string dir, out SKTypeface representative)
		{
			representative = null;
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
				return 0;

			var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
			int count = 0;
			SKTypeface firstAny = null;

			// どれが既定になるかを端末のファイル列挙順に委ねないよう、名前順で見る
			var paths = new System.Collections.Generic.List<string>(Directory.EnumerateFiles(dir, "*", options));
			paths.Sort(StringComparer.OrdinalIgnoreCase);

			foreach (string path in paths)
			{
				string ext = Path.GetExtension(path);
				if (!ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase) &&
					!ext.Equals(".otf", StringComparison.OrdinalIgnoreCase) &&
					!ext.Equals(".ttc", StringComparison.OrdinalIgnoreCase))
					continue;

				// ttc は 1 ファイルに複数フェイスが入っている。null が返るまで順に読む
				for (int index = 0; index < 16; index++)
				{
					SKTypeface typeface;
					try { typeface = SKTypeface.FromFile(path, index); }
					catch { break; }
					if (typeface == null)
						break;
					RegisterAs(typeface.FamilyName, typeface);
					if (index == 0)
						RegisterAs(Path.GetFileNameWithoutExtension(path), typeface);
					count++;

					// 太字ばかり拾って既定にしないよう、並字を優先する
					firstAny ??= typeface;
					if (representative == null && StyleOf(typeface) == FontStyle.Regular)
						representative = typeface;
				}
			}

			representative ??= firstAny;

			if (count > 0)
				cache.Clear();   // フォールバックで解決済みのものを捨てる
			return count;
		}

		internal static SKTypeface Resolve(string familyName, FontStyle style)
		{
			return cache.GetOrAdd((familyName ?? string.Empty, FaceStyle(style)), key => ResolveCore(key.Item1, key.Item2));
		}

		/// <summary>登録済みフォントの中からだけ解決する。無ければ null。</summary>
		internal static SKTypeface ResolveRegistered(string familyName, FontStyle style)
		{
			if (string.IsNullOrEmpty(familyName))
				return null;
			if (registered.TryGetValue(Key(familyName, style), out var exact))
				return exact;
			return byName.TryGetValue(familyName, out var list) ? PickClosest(list, FaceStyle(style)) : null;
		}

		/// <summary>要求スタイルに最も近いフェイスを選ぶ (太字要求なら太い方、斜体要求なら斜体)。</summary>
		static SKTypeface PickClosest(System.Collections.Generic.List<SKTypeface> list, FontStyle want)
		{
			SKTypeface best = null;
			int bestScore = int.MinValue;
			foreach (var typeface in list)
			{
				var have = StyleOf(typeface);
				int score = 0;
				if ((have & FontStyle.Bold) == (want & FontStyle.Bold)) score += 2;
				if ((have & FontStyle.Italic) == (want & FontStyle.Italic)) score += 1;
				if (score > bestScore) { bestScore = score; best = typeface; }
			}
			return best;
		}

		/// <summary>
		/// 日本語版 Windows のフォント名。emuera.config は全角のこの表記で書くのが era の慣習で
		/// (上流の既定値も「ＭＳ ゴシック」)、GDI+ はこの名前で引けるが
		/// <b>Skia は英語のファミリ名でしか引けない</b>。
		///
		/// この対応表が無いと、Windows のフォントが実在する <b>PC 上でだけ</b>名前解決に失敗して
		/// 既定フォント (比例) に落ちる。実機は元から Windows のフォントを持たず受け皿に落ちるので、
		/// 表が効くのは PC 側だけ — つまり「PC で再現して直す」を成立させるためのもの。
		/// </summary>
		static readonly System.Collections.Generic.Dictionary<string, string> localizedNames =
			new(StringComparer.OrdinalIgnoreCase)
			{
				["ＭＳ ゴシック"] = "MS Gothic",
				["ＭＳ Ｐゴシック"] = "MS PGothic",
				["ＭＳ 明朝"] = "MS Mincho",
				["ＭＳ Ｐ明朝"] = "MS PMincho",
				["メイリオ"] = "Meiryo",
				["游ゴシック"] = "Yu Gothic",
				["游明朝"] = "Yu Mincho",
			};

		/// <summary>指定された名前と、それに対応する英語名。</summary>
		static string[] NameCandidates(string familyName) =>
			localizedNames.TryGetValue(familyName, out string english) ? [familyName, english] : [familyName];

		static SKTypeface ResolveCore(string familyName, FontStyle style)
		{
			var weight = (style & FontStyle.Bold) != 0 ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
			var slant = (style & FontStyle.Italic) != 0 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
			var skStyle = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);

			if (string.IsNullOrEmpty(familyName))
				return Fallback ?? SKTypeface.FromFamilyName(null, skStyle) ?? SKTypeface.Default;

			// ゲームや利用者が置いたフォントが最優先
			foreach (string name in NameCandidates(familyName))
			{
				var own = ResolveRegistered(name, style);
				if (own != null)
					return own;
			}

			if (UseSystemFonts)
			{
				foreach (string name in NameCandidates(familyName))
				{
					var face = SKTypeface.FromFamilyName(name, skStyle);
					// 無いフォント名を渡すと既定フォントが返るため、名前一致を確認する
					if (face != null && string.Equals(face.FamilyName, name, StringComparison.OrdinalIgnoreCase))
						return face;
				}
			}

			return Fallback ?? SKTypeface.FromFamilyName(null, skStyle) ?? SKTypeface.Default;
		}

		public static void Clear()
		{
			cache.Clear();
			registered.Clear();
			byName.Clear();
		}
	}

	/// <summary>
	/// 「そのフォントで PRINTC の桁が揃うか」の判定。
	///
	/// エンジンは桁を半角スペースで作り、実測幅が枠を超える間だけ剥がす
	/// (EmueraConsole.Print.cs の CreateTypeCString)。したがって成立条件は
	/// <b>半角スペース : 半角文字 : 全角 = 1 : 1 : 2</b> の 1 点だけで、ここを見れば足りる。
	///
	/// 判定式を 1 か所に集めてあるのは、フォントを選ぶ側 (Android の SetupFonts) と
	/// 選んだ結果を検査する側 (EmueraEngine / TestHarness) が違う基準で判断しないため。
	/// </summary>
	public static class FontMetrics
	{
		/// <summary>まとめて測る文字数。1 文字ずつだと切り上げ誤差が乗る。</summary>
		public const int SampleCount = 32;

		/// <summary>フォント名の解決に失敗したときの受け皿を選ぶのに使う既定サイズ。</summary>
		public const float ProbeSize = 32f;

		/// <summary>計測の切り上げのぶんだけ許容する。</summary>
		const int Tolerance = 2;

		/// <summary>幅の実測値から等幅かを判定する。<see cref="SampleCount"/> 個ぶんの幅を渡すこと。</summary>
		public static bool RatioOk(int spaceWidth, int latinWidth, int fullWidth) =>
			spaceWidth > 0 &&
			Math.Abs(spaceWidth - latinWidth) <= Tolerance &&
			Math.Abs(spaceWidth - fullWidth) <= Tolerance;

		/// <summary>本文フォントが等幅か。</summary>
		public static bool IsMonospaced(Font font) => IsMonospaced(font, out _, out _, out _);

		/// <inheritdoc cref="IsMonospaced(Font)"/>
		/// <param name="font">検査するフォント。</param>
		/// <param name="spaceWidth">半角スペース <see cref="SampleCount"/> 個ぶんの幅。</param>
		/// <param name="latinWidth">半角文字 (M) 同数ぶんの幅。半角で最も広くなりやすい字。</param>
		/// <param name="fullWidth">全角スペース半数ぶんの幅。</param>
		public static bool IsMonospaced(Font font, out int spaceWidth, out int latinWidth, out int fullWidth)
		{
			spaceWidth = latinWidth = fullWidth = 0;
			if (font == null)
				return false;
			spaceWidth = Measure(font, new string(' ', SampleCount));
			latinWidth = Measure(font, new string('M', SampleCount));
			fullWidth = Measure(font, new string('　', SampleCount / 2));
			return RatioOk(spaceWidth, latinWidth, fullWidth);
		}

		/// <summary>
		/// タイプフェイス単体の判定。emuera.config を読む前 (＝本文サイズが決まる前) に
		/// 受け皿のフォントを選ぶ Android 側が使う。
		/// 等幅かどうかは em に対する比なのでサイズに依らないが、
		/// ヒンティングの丸めが効かないよう大きめの <see cref="ProbeSize"/> で測る。
		/// </summary>
		public static bool IsMonospaced(SKTypeface typeface, float sizeInPixels = ProbeSize)
		{
			if (typeface == null)
				return false;
			using var family = new FontFamily(typeface);
			using var font = new Font(family, sizeInPixels, FontStyle.Regular, GraphicsUnit.Pixel);
			return IsMonospaced(font);
		}

		/// <summary>
		/// 実際に描くときと同じ経路 (<see cref="GlyphFallback"/>) で測る。
		/// 別経路で測ると、判定は通るのに実描画では崩れる、という食い違いが起きる。
		/// </summary>
		static int Measure(Font font, string text) => (int)Math.Ceiling(GlyphFallback.Measure(font, text));
	}
}
