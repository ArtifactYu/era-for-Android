// andEmuera: Emuera.Core を外部 (TestHarness / Android アプリ) から使うための公開ファサード。
//
// 上流の EmueraConsole は internal なので、起動手順をここに閉じ込めて公開 API だけを見せる。
// 起動シーケンスは上流 Program.Main + MainWindow のコンストラクタ + EmueraConsole.Initialize を踏襲する。

using MinorShift.Emuera.Forms;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Config.JSON;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MinorShift.Emuera.Api
{
	/// <summary>実行側が待っている入力の種類。</summary>
	public enum EmueraInputMode
	{
		/// <summary>入力待ちではない。</summary>
		None,
		/// <summary>Enter か画面タップを待っている。</summary>
		EnterKey,
		/// <summary>整数値を待っている。</summary>
		Integer,
		/// <summary>文字列を待っている。</summary>
		String,
		/// <summary>整数・文字列どちらでもよい。</summary>
		Any,
		/// <summary>強制待ち (INPUT の待機)。入力そのものを受け付けない。</summary>
		Void,
	}

	/// <summary>
	/// タップ / クリックが実行側にどう扱われたか。
	///
	/// 画面は 1 枚の画像が貼り替わるだけなので、何も起きなかったときに
	/// 「押せていない」のか「処理待ち」なのかが利用者から見分けられない。
	/// 判断材料は <see cref="Forms.MainWindow.HandleClick"/> の分岐がすべて持っているので、
	/// それをそのまま外へ出す。
	/// </summary>
	public enum EmueraTapResult
	{
		/// <summary>入力が実行側に渡った。</summary>
		Accepted,
		/// <summary>入力待ちではあるが、タップ位置に選択肢が無かった。</summary>
		NoTarget,
		/// <summary>バックログを遡っていたので、最新行へ戻しただけ。</summary>
		Backlog,
		/// <summary>スクリプト実行中なので捨てられた。</summary>
		Busy,
		/// <summary>マウス操作が無効化されている (emuera.config)。</summary>
		Disabled,
	}

	/// <summary>
	/// PNG の行フィルタ。可逆なので出力画素は変わらず、速度と大きさだけが変わる。
	/// SkiaSharp の型を外へ出さないための写し。
	/// </summary>
	public enum PngFilterMode
	{
		/// <summary>フィルタを使わない (最速)。</summary>
		NoFilters,
		/// <summary>無変換フィルタのみ。</summary>
		None,
		/// <summary>左の画素との差分のみ。文字の多い行に効く。</summary>
		Sub,
		/// <summary>上の行との差分のみ。背景が平坦な画面に効く。</summary>
		Up,
		/// <summary>平均フィルタのみ。</summary>
		Avg,
		/// <summary>Paeth フィルタのみ。</summary>
		Paeth,
		/// <summary>毎行すべてのフィルタを試す (Skia の既定。最小サイズ・最遅)。</summary>
		All,
	}

	public sealed class EmueraEngine : IDisposable
	{
		MainWindow window;
		EmueraConsole console;

		EmueraEngine() { }

		/// <summary>ゲームフォルダ (csv / erb を含むフォルダ)。</summary>
		public string GameDir { get; private set; }

		/// <summary>
		/// ゲームを読み込んで起動する。ERB / CSV のロードとタイトル処理までを行う。
		/// </summary>
		/// <param name="gameDir">csv / erb / resources を含むフォルダ</param>
		/// <param name="host">画面側への通知を受け取る実装</param>
		/// <param name="clientWidth">描画領域の幅 (ピクセル)</param>
		/// <param name="clientHeight">描画領域の高さ (ピクセル)</param>
		public static async Task<EmueraEngine> StartAsync(string gameDir, IWindowHost host, int clientWidth = 800, int clientHeight = 600, bool useConfigWidth = false)
		{
			ArgumentNullException.ThrowIfNull(gameDir);
			ArgumentNullException.ThrowIfNull(host);

			var engine = new EmueraEngine { GameDir = gameDir };

			Program.Initialize(gameDir);

			if (!Directory.Exists(Program.CsvDir))
				throw new DirectoryNotFoundException($"csv フォルダが見つかりません: {Program.CsvDir}");
			if (!Directory.Exists(Program.ErbDir))
				throw new DirectoryNotFoundException($"erb フォルダが見つかりません: {Program.ErbDir}");

			ConfigData.Instance.LoadConfig();
			// EraFlow legacy compatibility: old scripts can name subroutines after
			// expression functions introduced by EM+EE (for example @SAVETEXT).
			// Keep parsing strict; permit name overloading, never skip invalid lines.
			ConfigData.Instance.GetConfigItem(ConfigCode.AllowFunctionOverloading).SetValue(true);
			Config.SetConfig(ConfigData.Instance);
			JSONConfig.Load();

			Lang.LoadLanguageFiles();
			Lang.SetLanguage();

			// EE_フォントファイル対応: font フォルダの読み込みは Program.Initialize が済ませている。
			// Config を読んだ今なら、実際に使われるフォントが等幅かどうかを確かめられる
			// (画面へ出すのは接続後なので、ここでは判定して持たせるだけ)
			engine.FontWarning = CheckMonospaced();

			// era のバリアントは emuera.config のウィンドウ幅を前提にレイアウトを組んでいる。
			// スマホの実ピクセル幅で描くと 1 行が入りきらないため、
			// 設定どおりの幅で描いて画面側で縮小表示させる (縦横比は端末に合わせる)。
			if (useConfigWidth && Config.WindowX > 0 && clientWidth > 0)
			{
				double aspect = (double)clientHeight / clientWidth;
				clientWidth = Config.WindowX;
				clientHeight = FitToLines((int)(clientWidth * aspect));
				engine.fixedWidth = clientWidth;
			}

			engine.window = new MainWindow(host);
			engine.window.SetClientSize(clientWidth, clientHeight);
			engine.console = new EmueraConsole(engine.window);
			engine.window.Console = engine.console;

			await engine.console.Initialize();

			return engine;
		}

		/// <summary>
		/// 等幅フォントが用意できていないときの警告文。問題なければ null。
		/// 呼び出し側 (Android / TestHarness) がログにも出す。
		/// </summary>
		public string FontWarning { get; private set; }

		/// <summary>
		/// 実際に使われるフォントが等幅かどうかを確かめる。
		///
		/// era のスクリプトは PRINTC / PRINTBUTTONC の桁揃えをエンジンに任せており、
		/// エンジンは半角スペースで詰めてから「実測幅が枠を超える間スペースを剥がす」で調整する
		/// (EmueraConsole.Print.cs の CreateTypeCString)。
		/// 比例フォントだとラベル自身が枠より広く、詰めたスペースが 1 つ残らず剥がされるため、
		/// 選択肢が横一列に繋がってしまう。崩れる条件はここで必ず捕まえられる。
		///
		/// 1 文字ずつだと切り上げ誤差が乗るので、まとめて測って比べる。
		/// </summary>
		static string CheckMonospaced()
		{
			var font = Config.DefaultFont;
			if (font == null)
				return null;

			// 判定式は System.Drawing.FontMetrics に 1 本化してある。
			// フォントを選ぶ側 (Android の SetupFonts) と同じ基準で見るため
			if (System.Drawing.FontMetrics.IsMonospaced(font, out int half, out int latin, out int full))
				return null;
			if (half <= 0)
				return null;

			// 実測値も添える。「フォントは合っているのに等幅にならない」ときに
			// どの比が崩れているのかが分からないと、実機でしか切り分けられなくなる
			return $"等幅フォントが見つかりません (指定: {Config.FontName} / 実際: {font.FontFamily.Name} / {Config.FontSize}px" +
				$" — 半角スペース×32={half}px 半角M×32={latin}px 全角×16={full}px)。" +
				"選択肢の桁揃えが崩れます。ゲームフォルダの font/ を端末へコピーするか、" +
				"fonts/ フォルダに等幅フォント (BIZ UDGothic など) を置いてください。";
		}

		static int MeasureWidth(string text, System.Drawing.Font font)
			=> System.Windows.Forms.TextRenderer.MeasureText(text, font).Width;

		/// <summary>
		/// <see cref="InspectFonts"/> の結果。Config も EmueraConsole も internal なので、
		/// 外 (TestHarness) から検証するための窓口としてここに置く。
		/// </summary>
		public sealed class FontDiagnostics
		{
			/// <summary>emuera.config が指定しているフォント名。</summary>
			public string RequestedName { get; init; }
			/// <summary>実際に解決されたフォント名。</summary>
			public string ActualName { get; init; }
			public int FontSize { get; init; }

			/// <summary>半角スペース <see cref="SampleCount"/> 個ぶんの幅。</summary>
			public int SpaceWidth { get; init; }
			/// <summary>半角文字 (M) <see cref="SampleCount"/> 個ぶんの幅。</summary>
			public int LatinWidth { get; init; }
			/// <summary>全角スペース <see cref="SampleCount"/>/2 個ぶんの幅。</summary>
			public int FullWidth { get; init; }
			/// <summary>太字で測った半角文字ぶんの幅 (合成太字が送り幅を変えないことの確認)。</summary>
			public int BoldLatinWidth { get; init; }

			/// <summary>PRINTC の文字数設定。</summary>
			public int PrintCLength { get; init; }
			/// <summary>PRINTC が詰めた結果の幅 (ラベルごと)。</summary>
			public (string Label, int PaddedWidth, int SlotWidth)[] PrintCSamples { get; init; }

			public const int SampleCount = System.Drawing.FontMetrics.SampleCount;

			/// <summary>
			/// PRINTC の桁揃えが成立する条件。半角スペース・半角文字・全角の送り幅が
			/// 「1 : 1 : 2」になっていること。
			/// </summary>
			public bool IsMonospaced =>
				System.Drawing.FontMetrics.RatioOk(SpaceWidth, LatinWidth, FullWidth);

			/// <summary>太字にしても送り幅が変わらないこと (変わると桁が揃わない)。</summary>
			public bool BoldKeepsAdvance => Math.Abs(LatinWidth - BoldLatinWidth) <= 2;
		}

		/// <summary>
		/// 実際に使われるフォントの送り幅を調べる。PRINTC の桁揃えは
		/// 「半角スペース N 個の幅 = 半角文字 N 個の幅」に依存しているので、ここが崩れると表示が壊れる。
		/// </summary>
		/// <param name="gameDir">csv / erb / emuera.config を含むフォルダ</param>
		/// <param name="useGameFonts">ゲームフォルダの font/ を使うか (false で「入れ忘れた端末」を再現)</param>
		/// <param name="useSystemFonts">OS にインストールされたフォントを名前で引くか
		/// (false で「そのフォントが入っていない Android」を再現)</param>
		/// <param name="fallbackFontPath">
		/// 名前で引けなかったときの受け皿にするフォントファイル。
		/// 端末では <c>MainActivity.SetupFonts</c> が APK 同梱の BIZ UDGothic などをここに据えるため、
		/// これを渡さないと <b>font/ を同梱しないゲーム (eraTOWN 等) の実機の状態を再現できない</b>
		/// (PC の既定フォントに落ちてしまう)。
		/// </param>
		public static FontDiagnostics InspectFonts(string gameDir, bool useGameFonts = true, bool useSystemFonts = true,
			string fallbackFontPath = null)
		{
			ArgumentNullException.ThrowIfNull(gameDir);

			var previousFallback = System.Drawing.FontResolver.Fallback;
			System.Drawing.FontResolver.Clear();
			GlobalStatic.Pfc = new System.Drawing.Text.PrivateFontCollection();
			System.Drawing.FontResolver.UseSystemFonts = useSystemFonts;
			if (fallbackFontPath != null)
				System.Drawing.FontResolver.Fallback = SkiaSharp.SKTypeface.FromFile(fallbackFontPath)
					?? throw new FileNotFoundException("受け皿にするフォントを読めません", fallbackFontPath);

			Program.Initialize(gameDir);       // ここで font/ が登録される
			ConfigData.Instance.LoadConfig();

			if (!useGameFonts)
			{
				System.Drawing.FontResolver.Clear();
				GlobalStatic.Pfc = new System.Drawing.Text.PrivateFontCollection();
			}
			UI.FontFactory.ClearFont();        // 解決済みのフォントを捨てる

			int n = FontDiagnostics.SampleCount;
			var font = Config.DefaultFont;
			var bold = UI.FontFactory.GetFont("", System.Drawing.FontStyle.Bold) ?? font;

			var samples = new[] { "[101]次のキャラへ", "[130]画像フォルダ選択(2)", "[90]通常能力", "  " };
			var printC = new (string, int, int)[samples.Length];
			int slot = MeasureWidth(new string(' ', Config.PrintCLength), font);
			for (int i = 0; i < samples.Length; i++)
				printC[i] = (samples[i], SimulatePrintC(samples[i], font, slot), slot);

			try
			{
				return new FontDiagnostics
				{
					RequestedName = Config.FontName,
					ActualName = font?.FontFamily.Name,
					FontSize = Config.FontSize,
					SpaceWidth = MeasureWidth(new string(' ', n), font),
					LatinWidth = MeasureWidth(new string('M', n), font),
					FullWidth = MeasureWidth(new string('　', n / 2), font),
					BoldLatinWidth = MeasureWidth(new string('M', n), bold),
					PrintCLength = Config.PrintCLength,
					PrintCSamples = printC,
				};
			}
			finally
			{
				System.Drawing.FontResolver.UseSystemFonts = true;
				System.Drawing.FontResolver.Fallback = previousFallback;
			}
		}

		/// <summary>
		/// EmueraConsole.CreateTypeCString (private) と同じ手順で左詰めしたときの幅。
		/// 等幅なら PRINTCの文字数 ぶんの枠にぴたりと収まり、比例フォントだと
		/// 詰めたスペースが全部剥がされてラベル素の幅になる。
		/// </summary>
		static int SimulatePrintC(string label, System.Drawing.Font font, int slotWidth)
		{
			int length = System.Text.Encoding.GetEncoding("Shift-JIS").GetByteCount(label);
			string padded = label;
			if (length < Config.PrintCLength + 1)
				padded += new string(' ', Config.PrintCLength + 1 - length);
			int width = MeasureWidth(padded, font);
			while (width > slotWidth && padded.Length > 0 && padded[^1] == ' ')
			{
				padded = padded[..^1];
				width = MeasureWidth(padded, font);
			}
			return width;
		}

		/// <summary>
		/// そのゲームで実際に使われる本文フォントを解決して返す。
		/// <c>Config</c> が internal なので、グリフ欠けの調査 (TestHarness の
		/// <c>--selftest-glyph</c>) から本文フォントを触るための窓口。
		/// </summary>
		/// <param name="gameDir">csv / erb / emuera.config を含むフォルダ</param>
		/// <param name="useSystemFonts">OS にインストールされたフォントを名前で引くか
		/// (false で「そのフォントが入っていない Android」を再現)</param>
		/// <param name="fallbackFontPath">
		/// 名前で引けなかったときの受け皿にするフォントファイル。端末では
		/// <c>MainActivity.SetupFonts</c> が APK 同梱の BIZ UDGothic をここに据える。
		/// <see cref="InspectFonts"/> と違って受け皿は<b>戻さない</b> — 呼び出し側は
		/// 返ったフォントで測り続けるので、代替フェイスの解決もその構成のままでないと
		/// 実機を再現したことにならない。
		/// </param>
		public static System.Drawing.Font ResolveDefaultFont(string gameDir, bool useSystemFonts = true,
			string fallbackFontPath = null)
		{
			ArgumentNullException.ThrowIfNull(gameDir);

			System.Drawing.FontResolver.Clear();
			System.Drawing.GlyphFallback.Clear();
			GlobalStatic.Pfc = new System.Drawing.Text.PrivateFontCollection();
			System.Drawing.FontResolver.UseSystemFonts = useSystemFonts;
			if (fallbackFontPath != null)
				System.Drawing.FontResolver.Fallback = SkiaSharp.SKTypeface.FromFile(fallbackFontPath)
					?? throw new FileNotFoundException("受け皿にするフォントを読めません", fallbackFontPath);
			try
			{
				Program.Initialize(gameDir);       // ここで font/ が登録される
				ConfigData.Instance.LoadConfig();
				UI.FontFactory.ClearFont();        // 解決済みのフォントを捨てる
				return Config.DefaultFont;
			}
			finally
			{
				System.Drawing.FontResolver.UseSystemFonts = true;
			}
		}

		/// <summary>読み込みに失敗して停止しているか。</summary>
		public bool IsError => console != null && console.IsError;

		/// <summary>スクリプト実行中か (入力待ちでないか)。</summary>
		public bool IsInProcess => console != null && console.IsInProcess;

		/// <summary>入力欄の内容を確定して実行側に渡す。</summary>
		public EmueraTapResult SubmitInput(string text, bool skipMessage = false)
		{
			if (console == null)
				return EmueraTapResult.Busy;
			window.MarkDirty();
			window.TextBox.Text = text ?? string.Empty;
			return window.PressEnterKey(skipMessage, false);
		}

		/// <summary>
		/// タッチ位置をポインタ位置として控える。
		///
		/// 上流は MOUSEX/MOUSEY を Cursor.Position から、入力後のホバーし直しを
		/// Control.MousePosition から取る。Android には常駐のポインタが無いので、
		/// 最後に触った場所をポインタ位置として置いておく。
		/// これが無いと MOUSEX() が 0、MOUSEY() が -画面高 に張り付き、
		/// ポインタ位置に箱を出すゲームが画面の隅に描いてしまう
		/// (ShinEraTenseiP の INPUT_YN_POPUP がターンエンドの確認をこの形で出す)。
		/// </summary>
		static void SetPointer(System.Drawing.Point point)
		{
			System.Windows.Forms.Control.MousePosition = point;
			System.Windows.Forms.Cursor.Position = point;
		}

		/// <summary>画面のタップ / クリック。座標は描画領域内のピクセル。</summary>
		public EmueraTapResult Click(int x, int y, bool rightButton = false)
		{
			if (console == null)
				return EmueraTapResult.Busy;
			window.MarkDirty();
			var point = new System.Drawing.Point(x, y);
			// 選択中のボタンはポインタ位置で決まるので、先に移動させてから確定する
			SetPointer(point);
			console.MoveMouse(point);
			return window.HandleClick(point, rightButton
				? System.Windows.Forms.MouseButtons.Right
				: System.Windows.Forms.MouseButtons.Left);
		}

		/// <summary>
		/// ポインタ移動 (ボタンのフォーカス表示を更新する)。
		/// 戻り値は上流と同じ「この後で再描画が必要かどうか」。
		/// </summary>
		public bool MoveMouse(int x, int y)
		{
			if (console == null)
				return false;
			SetPointer(new System.Drawing.Point(x, y));
			if (!console.MoveMouse(new System.Drawing.Point(x, y)))
				return false;
			window.MarkDirty();
			return true;
		}

		/// <summary>ホイール / フリックによるスクロール。delta は WinForms と同じく 120 単位。</summary>
		/// <remarks>
		/// これは <c>INPUTMOUSEKEY</c> 待ちにホイール量を渡す経路。
		/// 通常のバックログ送りは <see cref="ScrollLines"/> を使う。
		/// </remarks>
		public void Scroll(int x, int y, int delta)
		{
			if (console == null)
				return;
			window.MarkDirty();
			SetPointer(new System.Drawing.Point(x, y));
			console.MouseWheel(new System.Drawing.Point(x, y), delta);
		}

		/// <summary>1 行の高さ (ピクセル)。画面側がフリック量を行数へ換算するのに使う。</summary>
		public int LineHeight => Config.LineHeight;

		/// <summary>
		/// ログの現在位置と総行数。<c>Value == Max</c> なら最新行を表示している。
		/// 単位は表示行で、EmueraConsole が描画のたびに更新する。
		/// </summary>
		public (int Value, int Max) ScrollState
			=> window == null ? (0, 0) : (window.ScrollBar.Value, window.ScrollBar.Maximum);

		/// <summary>
		/// バックログを行単位で送る。正の値で過去へ、負の値で最新へ向かう。
		/// 何か起きたら true (端に達していて動かなかったときは false)。
		///
		/// 上流はこの処理を MainWindow のホイールハンドラに持っており、
		/// EmueraConsole.MouseWheel は INPUTMOUSEKEY 待ちのときしか反応しない。
		/// </summary>
		public bool ScrollLines(int lines, int x = 0, int y = 0)
		{
			if (console == null || window == null || lines == 0)
				return false;

			// INPUTMOUSEKEY 待ちのときはホイールとして実行側に渡す (上流と同じ扱い)。
			// 過去へ遡る向きが WinForms のホイール上回転 (delta 正) にあたる
			if (console.IsWaitingPrimitive)
			{
				console.MouseWheel(new System.Drawing.Point(x, y), lines > 0 ? 120 : -120);
				return true;
			}

			var bar = window.ScrollBar;
			if (!bar.Enabled || bar.Maximum <= bar.Minimum)
				return false;

			// 過去へ遡る = Value を減らす。画面側は「上フリックで過去へ」を正で送ってくる
			int value = Math.Clamp(bar.Value - lines, bar.Minimum, bar.Maximum);
			if (value == bar.Value)
				return false;

			bar.Value = value;
			// RefreshStrings は msPerFrame 未満の再描画を握り潰すので、スクロールは常に force する
			console.RefreshStrings(true);
			return true;
		}

		/// <summary>最新行まで戻す。動いたら true。</summary>
		public bool ScrollToLatest() => window != null && window.ReturnToLatestLine();

		/// <summary>メッセージ待ちを進める (Enter 相当)。</summary>
		public EmueraTapResult PressEnter(bool skipMessage = false)
		{
			if (window == null)
				return EmueraTapResult.Busy;
			window.MarkDirty();
			return window.PressEnterKey(skipMessage, false);
		}

		/// <summary>
		/// 右クリック相当。操作バーの「スキップ」ボタン用。
		///
		/// Windows の右クリックは「カーソルがある場所で右ボタンを押す」なので、
		/// ボタン自身は座標を持たなくても、最後に触った位置 (SetPointer が控えている
		/// Cursor.Position) で押したことにすれば画面の長押しと同じ結果になる。
		/// 選択肢の上にポインタが残っていればその選択肢への右クリックになり、
		/// INPUTMOUSEKEY 待ちでも上流と同じくポインタ位置が入力値になる。
		/// </summary>
		public EmueraTapResult MessageSkip()
		{
			if (console == null)
				return EmueraTapResult.Busy;
			var pointer = System.Windows.Forms.Cursor.Position;
			return Click(pointer.X, pointer.Y, rightButton: true);
		}

		/// <summary>現在の画面サイズ。</summary>
		public System.Drawing.Size ClientSize => window?.GetWindowSize() ?? System.Drawing.Size.Empty;

		/// <summary>
		/// いま何の入力を待っているか。画面側はこれを見て、数値入力なら
		/// テンキーを出す・全角数字を半角に直すといった対応ができる。
		/// </summary>
		public bool IsSingleInput => console?.IsWaintingOnePhrase == true;
		public EmueraInputMode InputMode
		{
			get
			{
				var req = console?.inputReq;
				if (req == null)
					return EmueraInputMode.None;
				return req.InputType switch
				{
					Runtime.InputType.IntValue or Runtime.InputType.IntButton => EmueraInputMode.Integer,
					Runtime.InputType.StrValue or Runtime.InputType.StrButton => EmueraInputMode.String,
					Runtime.InputType.AnyValue => EmueraInputMode.Any,
					Runtime.InputType.EnterKey or Runtime.InputType.AnyKey => EmueraInputMode.EnterKey,
					Runtime.InputType.Void => EmueraInputMode.Void,
					_ => EmueraInputMode.None,
				};
			}
		}

		/// <summary><c>INPUTMOUSEKEY</c> 待ちか (画面のどこを押したかがそのまま入力値になる)。</summary>
		public bool IsWaitingMouse => console != null && console.IsWaitingPrimitive;

		/// <summary>マウス操作が有効か (emuera.config の UseMouse)。false なら画面タップは一切効かない。</summary>
		public static bool UseMouse => Config.UseMouse;

		/// <summary>
		/// いま選べる選択肢の数。0 なら「画面のどこを押しても選べない = 入力欄が要る」。
		///
		/// 上流の EmueraConsole が入力値からボタンを探すとき
		/// (EmueraConsole.cs の InputInteger 経路) と同じ手順を踏む。
		/// 表示行を後ろから見て、最新世代のボタンだけを数える。
		/// 世代の違うボタン (= もう選べない) に当たったらそこで打ち切れるので、
		/// 走査量は「いま出ている選択肢の数」で頭打ちになる。
		/// </summary>
		public object[] GetSelectableChoices()
		{
			if (console == null) return [];
			return console.DisplayLineList.SelectMany(l => l.Buttons)
				.Where(b => b.IsButton && b.Generation == console.LastButtonGeneration)
				.Select(b => (object)new { text = b.ToString(), value = b.IsInteger ? b.Input.ToString(System.Globalization.CultureInfo.InvariantCulture) : b.Inputs }).ToArray();
		}
		public int SelectableButtonCount()
		{
			var lines = console?.DisplayLineList;
			if (lines == null)
				return 0;

			long generation = console.LastButtonGeneration;
			int count = 0;
			for (int i = lines.Count - 1; i >= 0; i--)
			{
				foreach (var button in lines[i].Buttons)
				{
					if (button.Generation == generation)
						count++;
					else if (button.Generation != 0)
						return count;   // 後ろから回しているので、世代が変わったらもう無い
				}
			}
			return count;
		}

		/// <summary>
		/// PNG の行フィルタ。既定は None (フィルタ 0 = 無変換だけを使う)。
		///
		/// Skia の既定は「毎行 5 種のフィルタを試して一番小さいものを選ぶ」で、
		/// era の画面 (平坦な背景 + 文字) では割に合わない。
		/// TestHarness の --verify-encoders で実測した結果 (1600x1129 / erablue_resort):
		/// <code>
		/// All/6 (既定)  38.0ms  141KB      ← 現行
		/// None/3         7.5ms  115KB      ← 5 倍速くて 18% 小さい
		/// </code>
		/// フィルタと圧縮レベルは可逆なので、出力画素はどの設定でも完全に一致する。
		/// </summary>
		public static PngFilterMode PngFilter { get; set; } = PngFilterMode.None;

		/// <summary>PNG の zlib 圧縮レベル (0〜9)。Skia の既定は 6。</summary>
		public static int PngZLibLevel { get; set; } = 3;

		static SkiaSharp.SKPngEncoderFilterFlags ToSkiaFilter(PngFilterMode mode) => mode switch
		{
			PngFilterMode.NoFilters => SkiaSharp.SKPngEncoderFilterFlags.NoFilters,
			PngFilterMode.None => SkiaSharp.SKPngEncoderFilterFlags.None,
			PngFilterMode.Sub => SkiaSharp.SKPngEncoderFilterFlags.Sub,
			PngFilterMode.Up => SkiaSharp.SKPngEncoderFilterFlags.Up,
			PngFilterMode.Avg => SkiaSharp.SKPngEncoderFilterFlags.Avg,
			PngFilterMode.Paeth => SkiaSharp.SKPngEncoderFilterFlags.Paeth,
			_ => SkiaSharp.SKPngEncoderFilterFlags.AllFilters,
		};

		/// <summary>
		/// 現在の画面を PNG バイト列にして返す。WebView へ送るための暫定表示モードで使う。
		/// 描画は必要なときだけ行う (<see cref="MainWindow.EnsureRendered"/>)。
		/// </summary>
		public byte[] RenderPng() => RenderPng(PngFilter, PngZLibLevel);

		/// <summary>エンコード設定を明示する版。ベンチと検証で使う。</summary>
		public byte[] RenderPng(PngFilterMode filter, int zlibLevel)
		{
			window?.EnsureRendered();
			return window?.BackBuffer?.EncodePng(ToSkiaFilter(filter), zlibLevel);
		}

		/// <summary>backBuffer が現在の表示状態を反映していることを保証する。</summary>
		public void EnsureRendered() => window?.EnsureRendered();

		/// <summary>次の <see cref="EnsureRendered"/> で必ず描き直させる。</summary>
		public void MarkDirty() => window?.MarkDirty();

		/// <summary>
		/// 直近の描画結果の内容ハッシュ。前回と一致するなら画面は変わっていないので、
		/// PNG の再エンコードも転送も省ける。<see cref="EnsureRendered"/> の後に呼ぶこと。
		/// </summary>
		public ulong HashBackBuffer()
		{
			var bmp = window?.BackBuffer?.SkBitmap;
			return bmp == null ? 0 : FrameHash.Compute(bmp.GetPixelSpan());
		}

		/// <summary>これまでに実行したフル描画の回数。</summary>
		public long PaintCount => window?.PaintCount ?? 0;

		/// <summary>
		/// これまでにフル描画へ費やした累計時間 (ミリ秒)。
		/// 入力の前後で差分を取ると「1 回の操作で何 ms 描いていたか」が出る。
		/// </summary>
		public double PaintMs => window?.PaintMs ?? 0;

		int fixedWidth;

		/// <summary>
		/// 描画領域の高さを決める。
		///
		/// Emuera は下端から上へ行を並べるので、一番使う選択肢は必ず最下行に来る。
		/// さらに emuera.config のフォントサイズが行の高さを上回っていることがあり
		/// (例: フォント 16px / 行高 17px)、そのままだと最下行の下側が画像の外にはみ出して欠ける。
		/// 行の高さの倍数に揃えて、行が半端な位置で切れないようにする。
		/// (最下行が下にはみ出す分は MainWindow 側がビットマップに余白を持たせて受け止める)
		/// </summary>
		static int FitToLines(int height)
		{
			int lineHeight = Config.LineHeight;
			if (lineHeight <= 0)
				return Math.Max(height, 64);
			return Math.Max(height / lineHeight * lineHeight, lineHeight * 4);
		}

		/// <summary>
		/// 描画領域のサイズ変更 (画面回転など) を通知する。
		/// 幅を固定している場合は、縦横比だけ画面に合わせて高さを決める。
		/// </summary>
		public void Resize(int width, int height)
		{
			if (window == null || width <= 0 || height <= 0)
				return;
			if (fixedWidth > 0)
			{
				double aspect = (double)height / width;
				width = fixedWidth;
				height = (int)(width * aspect);
			}
			window.SetClientSize(width, FitToLines(height));
		}

		/// <summary>
		/// 表示中のログを行ごとのテキストで返す (先頭が最古)。
		/// 画像だけでは「文字が無い」のか「描かれていない」のか分からないので、
		/// 表示崩れを調べるときの突き合わせに使う。
		/// </summary>
		public string[] GetDisplayLines()
			=> console?.GetLog(true).Split('\n').Select(s => s.TrimEnd('\r')).ToArray() ?? [];

		/// <summary>
		/// 直近の描画で使った「奥行きごとのパーツ数」。
		/// 上流の OnPaint は<b>奥行き 0 のときにだけ通常の行テキストを描く</b>ので、
		/// ここに 0 が無い画面では文字が一切出ない。表示崩れの切り分けに使う。
		/// </summary>
		public (int Depth, int Count)[] EscapedPartDepths()
			=> console?.EscapedParts?.Select(kv => (kv.Key, kv.Value.Count))
				.OrderByDescending(x => x.Key).ToArray() ?? [];

		/// <summary>直近の描画で使ったパーツの位置。表示崩れの調査用。</summary>
		public (int Depth, string Kind, int Top, int Bottom, int Line)[] EscapedPartBoxes()
			=> console?.EscapedParts?
				.SelectMany(kv => kv.Value.Select(p => (
					Depth: kv.Key,
					Kind: p.GetType().Name,
					p.Top,
					p.Bottom,
					Line: p.Parent?.ParentLine?.LineNo ?? -1)))
				.OrderByDescending(x => x.Depth).ThenBy(x => x.Line).ToArray() ?? [];

		/// <summary>描画領域の高さ。上流の絶対配置 div はこの値を基準に置かれる。</summary>
		public int CanvasHeight => window?.MainPicBox?.Height ?? 0;

		/// <summary>現在の画面を PNG に書き出す (移植の目視確認用)。</summary>
		public void SaveScreenshot(string path)
		{
			window?.EnsureRendered();
			window?.BackBuffer?.Save(path, System.Drawing.ImageFormat.Png);
		}

		public void Dispose()
		{
			console?.Dispose();
			window?.Dispose();
		}
	}
}
