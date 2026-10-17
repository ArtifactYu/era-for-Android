// andEmuera: 上流 UI/Framework/Forms/MainWindow.cs (WinForms) の置き換え。
//
// 上流の EmueraConsole は「MainWindow が持つコントロール」を直接触って
// 描画領域サイズ・スクロール位置・入力欄・ツールチップを扱う。
// Android 版でも EmueraConsole をそのまま使い回したいので、
// WinForms を持たない互換クラスを用意し、状態変化を UI 層 (WebView) へ通知する。

using MinorShift.Emuera.Api;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.UI.Game;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinorShift.Emuera.Forms
{
	/// <summary>
	/// MainWindow が起こした変化を UI 層 (Android / WebView) へ伝えるための受け口。
	/// Phase 2 で WebConsole がこれを実装する。
	/// </summary>
	public interface IWindowHost
	{
		/// <summary>コンソールの再描画要求。</summary>
		void RequestRedraw();

		/// <summary>ウィンドウタイトルの変更。</summary>
		void SetTitle(string title);

		/// <summary>入力欄の内容を差し替える。</summary>
		void SetInputText(string text);

		/// <summary>入力欄の表示位置・幅を変更する (行内入力)。</summary>
		void SetInputPosition(int xOffset, int yOffset, int width);

		/// <summary>入力欄の位置指定を解除する。</summary>
		void ResetInputPosition();

		/// <summary>ツールチップを表示する。Android では長押しで表示する。</summary>
		void ShowToolTip(string text, int x, int y);

		/// <summary>アプリの終了要求。</summary>
		void RequestClose();

		/// <summary>再起動要求 (Ctrl+R 相当)。</summary>
		void RequestReboot();

		/// <summary>
		/// 実行側の状態 (表示行・EscapedParts) を触る処理を排他で走らせる。
		/// 走らせられなければ false を返し、呼び出し側はその回を諦める。
		///
		/// アニメ用の redrawTimer はスレッドプールから発火するので、
		/// スクリプト実行中の状態を並行に読み書きしてしまう。既定実装は素通りで、
		/// ロックを持つホスト (WebHost) だけが実際の排他を行う。
		/// </summary>
		bool TryRunExclusive(Action action)
		{
			action();
			return true;
		}

		/// <summary>
		/// いまの画面を 1 枚その場で焼いて送る。送れたら true。
		///
		/// スクリプト実行中は実行スレッドがホストの排他を握ったままなので、
		/// 転送側のワーカーは 1 枚もエンコードできない。呼び出し元 (RepaintNow) は
		/// その排他を持っているスレッド自身なので、ここで焼けば「処理中」の間も
		/// 画面が進む。PC 版の右クリックのように途中経過を見せるための経路。
		///
		/// 間引きは転送側と同じ ack で行う。表示が追いついていなければ何もしないので、
		/// 遅い端末では自然に枚数が減る。既定実装は何もしない
		/// (転送を持たないホスト。TestHarness など)。
		/// </summary>
		bool TryPushFrameNow() => false;
	}

	public sealed class MainWindow : IDisposable
	{
		enum TextBoxState
		{
			Normal,
			WatingToChange,
			Changed,
			ScrollBack,
		}

		TextBoxState textBoxState = TextBoxState.Normal;

		public MainWindow(IWindowHost host)
		{
			Host = host;
			MainPicBox = new PictureBox { Width = 800, Height = 600 };
			ScrollBar = new VScrollBar { Minimum = 0, Maximum = 0, Value = 0 };
			TextBox = new RichTextBox();
			ToolTip = new ToolTip();

			// 上流 MainWindow の textBoxHandleScrollValueChanged と同じ扱い。
			// 行内入力欄は最新行を表示しているときだけ行に貼り付ける
			ScrollBar.ValueChanged += (_, _) =>
			{
				if (TextBoxIgnoreScrollBarChanges)
					return;
				if (ScrollBar.Value < ScrollBar.Maximum && TextBoxPosChanged)
					ScrollBackTextBoxPos();
				else if (ScrollBar.Value == ScrollBar.Maximum && TextBoxPosScrolledBack)
					ApplyTextBoxChanges();
			};
		}

		public IWindowHost Host { get; }

		public PictureBox MainPicBox { get; }
		public VScrollBar ScrollBar { get; }
		public RichTextBox TextBox { get; }
		public ToolTip ToolTip { get; }

		// EmueraConsole / HotkeyState は上流で internal のため、公開度を合わせる
		internal EmueraConsole Console { get; set; }
		internal HotkeyState hotkeyState { get; set; }

		public bool Created { get; set; } = true;
		public string Text { get; set; } = "Emuera";

		public bool TextBoxIgnoreScrollBarChanges { get; set; }
		public bool TextBoxPosChanged => textBoxState == TextBoxState.Changed;
		public bool TextBoxPosScrolledBack => textBoxState == TextBoxState.ScrollBack;
		public bool TextBoxPosWatingToChange => textBoxState == TextBoxState.WatingToChange;

		/// <summary>描画領域のサイズを UI 層から設定する (画面回転・リサイズ時)。</summary>
		public void SetClientSize(int width, int height)
		{
			MainPicBox.Width = width;
			MainPicBox.Height = height;
			MarkDirty();
		}

		public Point GetWindowPos() => new(0, 0);

		public Size GetWindowSize() => new(MainPicBox.Width, MainPicBox.Height);

		/// <summary>最後に要求された行内入力欄の位置。履歴から戻ったときに貼り直す。</summary>
		(int X, int Y, int Width) nextInputPos;

		public void SetTextBoxPos(int xOffset, int yOffset, int width)
		{
			nextInputPos = (xOffset, yOffset, width);
			textBoxState = TextBoxState.WatingToChange;
			Host?.SetInputPosition(xOffset, yOffset, width);
		}

		public void ResetTextBoxPos()
		{
			textBoxState = TextBoxState.Normal;
			Host?.ResetInputPosition();
		}

		/// <summary>履歴を遡っている間は行内位置を解除する (上流は既定位置へ戻す)。</summary>
		public void ScrollBackTextBoxPos()
		{
			textBoxState = TextBoxState.ScrollBack;
			Host?.ResetInputPosition();
		}

		public void ApplyTextBoxChanges()
		{
			// 上流は「位置指定待ち」と「履歴から戻ってきた」の両方をここで確定する
			if (textBoxState != TextBoxState.WatingToChange && textBoxState != TextBoxState.ScrollBack)
				return;
			if (textBoxState == TextBoxState.ScrollBack)
				Host?.SetInputPosition(nextInputPos.X, nextInputPos.Y, nextInputPos.Width);
			textBoxState = TextBoxState.Changed;
		}

		public void ChangeTextBox(string str)
		{
			TextBox.Text = str ?? string.Empty;
			Host?.SetInputText(TextBox.Text);
		}

		public void update_lastinput() { }

		public void clear_richText() => ChangeTextBox(string.Empty);

		public void SetupIcon(Icon icon) { }

		public void TranslateUI() { }

		public void ResetCheckedLanguage() { }

		public void ShowConfigDialog() { }

		/// <summary>
		/// 画面のタップ / クリック。上流 MainWindow の mainPicBox_MouseClick と同じ判定を行う。
		/// 選択肢のボタンは「MoveMouse で選択 → PressEnterKey で確定」という 2 段構えなので、
		/// EmueraConsole.MouseDown を呼ぶだけでは決定にならない。
		///
		/// 戻り値は「このタップがどう扱われたか」。画面が変わらなかったときに
		/// 「押せていない」のか「処理待ち」なのかを利用者へ伝えるために使う。
		/// どの分岐にも入らずに末尾まで落ちたら、入力待ちなのに選択肢の外を押している。
		/// </summary>
		public EmueraTapResult HandleClick(Point location, MouseButtons button)
		{
			if (!Runtime.Config.Config.UseMouse)
				return EmueraTapResult.Disabled;
			if (Console == null || Console.IsInProcess)
				return EmueraTapResult.Busy;

			// INPUTMOUSEKEY 待ち: 押下情報をそのまま渡す
			if (Console.IsWaitingPrimitive)
			{
				Console.MouseDown(location, button);
				if (ScrollBar.Value == ScrollBar.Maximum && Console.SelectingButton != null)
					GlobalStatic.Process.InputInteger(6, Console.SelectingButton.GetMappedColor(location.X, location.Y));
				return EmueraTapResult.Accepted;
			}

			bool isBacklog = ScrollBar.Value != ScrollBar.Maximum;
			string str = Console.SelectedString;

			// 履歴を遡っている最中のタップは、まず最新行へ戻す
			if (isBacklog && (button == MouseButtons.Left || button == MouseButtons.Right))
				ReturnToLatestLine();

			// メッセージ待ち (ボタン以外をタップした場合)
			if (Console.IsWaitingEnterKey && str == null)
			{
				if (isBacklog)
					return EmueraTapResult.Backlog;
				if (Console.IsError)
				{
					if (button == MouseButtons.Left)
					{
						PressEnterKey(false, true);
						return EmueraTapResult.Accepted;
					}
					return EmueraTapResult.NoTarget;
				}
				PressEnterKey(button == MouseButtons.Right, true);
				return EmueraTapResult.Accepted;
			}

			// マウス入力を受け付ける INPUT 系でボタンをタップした場合
			if (Console.IsWaintingInputWithMouse && !Console.IsError && str != null)
			{
				if (!isBacklog)
					GlobalStatic.Process.InputInteger(3, Console.SelectingButton.GetMappedColor(location.X, location.Y));
				GlobalStatic.Process.InputString(1, str);
				GlobalStatic.Process.InputInteger(1, button == MouseButtons.Right ? 2 : 1);
				Console.PressEnterKey(button == MouseButtons.Right, str, true);
				return EmueraTapResult.Accepted;
			}

			// ボタン以外をタップした INPUT 待ち
			if (Console.IsWaintingInputWithMouse && !Console.IsError)
			{
				TextBox.Text = string.Empty;
				if (str != null)
					GlobalStatic.VEvaluator.RESULTS_ARRAY[1] = str;
				GlobalStatic.VEvaluator.RESULT_ARRAY[1] = button == MouseButtons.Right ? 2 : 1;
				GlobalStatic.VEvaluator.RESULT_ARRAY[2] = 0;
				Console.inputReq.Timelimit = 0;
				PressEnterKey(false, true);
				return EmueraTapResult.Accepted;
			}

			// 通常の選択肢: ボタンの入力文字列を入力欄に入れて確定する
			if (str != null && button == MouseButtons.Left)
			{
				TextBox.Text = str;
				PressEnterKey(false, true);
				return EmueraTapResult.Accepted;
			}

			// ここまで来たら何も起きていない。バックログから戻したのなら
			// それは「押せなかった」ではなく「戻した」として伝える
			return isBacklog ? EmueraTapResult.Backlog : EmueraTapResult.NoTarget;
		}

		/// <summary>
		/// 履歴を遡っていたら最新行へ戻す。動いたら true。
		///
		/// 上流はマウスでもキーでも「まず最新行へ戻してから入力を処理する」
		/// (mainPicBox_MouseDown と richTextBox1_KeyDown の PageUp/PageDown 以外)。
		/// 遡ったまま実行させると、OnPaint が見えている範囲のボタンしか登録しないため
		/// 次の画面の BINPUT が「ボタンが一つも無い」と判断して入力を受け付けなくなる。
		/// </summary>
		public bool ReturnToLatestLine()
		{
			if (Console == null || ScrollBar.Value == ScrollBar.Maximum)
				return false;
			ScrollBar.Value = ScrollBar.Maximum;
			Console.RefreshStrings(true);
			return true;
		}

		/// <summary>入力欄の内容を確定して実行側へ渡す (上流 MainWindow と同じ手順)。</summary>
		public EmueraTapResult PressEnterKey(bool mesSkip, bool inputsByMouse)
		{
			if (Console == null || Console.IsInProcess)
				return EmueraTapResult.Busy;
			// 上流のキー入力と同じく、実行の前に最新行へ戻す
			ReturnToLatestLine();
			string str = TextBox.Text;
			TextBox.Text = string.Empty;
			Host?.SetInputText(string.Empty);
			Console.PressEnterKey(mesSkip, str, inputsByMouse);
			return EmueraTapResult.Accepted;
		}

		public void Reboot() => Host?.RequestReboot();

		public void GotoTitle() => Console?.GotoTitle();

		public Task ReloadErb() => Task.CompletedTask;

		Bitmap backBuffer;
		volatile bool painting;
		volatile bool bufferDirty = true;
		long paintCount;
		long paintTicks;

		/// <summary>RenderOffscreen を実行した回数。/stats で「1 世代あたり何回描いたか」を見る。</summary>
		public long PaintCount => System.Threading.Interlocked.Read(ref paintCount);

		/// <summary>
		/// フル描画に費やした累計時間。差分を取って「1 入力の中で何 ms 描いていたか」を見る。
		///
		/// スクリプトは gate を握ったまま同期実行され、その間の描画は 1 枚も画面に出ない
		/// (転送側は gate を取れない)。捨てている描画がどれだけあるかはここでしか分からない。
		/// </summary>
		public double PaintMs =>
			System.Threading.Interlocked.Read(ref paintTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

		/// <summary>表示内容が変わったかもしれないことを記録する。次の EnsureRendered で描き直す。</summary>
		public void MarkDirty() => bufferDirty = true;

		/// <summary>
		/// backBuffer が現在の表示状態を反映していることを保証する。
		///
		/// 上流の EmueraConsole は EscapedParts (表示中のボタン一覧) を OnPaint の中でしか
		/// 作らず、BINPUT などがそれを参照する。描画回数を減らすときは
		/// 「参照される時点では必ず描かれている」ことをここで担保する。
		/// </summary>
		public void EnsureRendered()
		{
			if (bufferDirty)
				RenderOffscreen();
		}

		/// <summary>最下行が下にはみ出す分を受け止めるための余白 (ピクセル)。</summary>
		static int BottomOverflowMargin => Math.Max(Runtime.Config.Config.LineHeight / 2, 6);

		/// <summary>
		/// 上流の EmueraConsole は「描画時に表示状態を確定する」設計で、
		/// 表示中のボタンや HTML パーツの一覧 (EscapedParts) は OnPaint の中で作られる。
		/// BINPUT などの命令はその結果を参照するため、WebView 版でも描画パス自体は走らせる必要がある。
		/// ここではオフスクリーンの Bitmap に描き、結果を MainPicBox.Image に保持する。
		/// </summary>
		public void RenderOffscreen()
		{
			if (Console == null || painting)
				return;
			int w = Math.Max(MainPicBox.Width, 1);
			int h = Math.Max(MainPicBox.Height, 1);

			// Emuera は「描画領域の下端」に最新行を置く。emuera.config のフォントサイズが
			// 行の高さを上回っていると (例: フォント 16px / 行高 17px)、最下行の下側が
			// 領域からはみ出して欠ける。ビットマップだけ少し高く確保して受け止める。
			int bufferHeight = h + BottomOverflowMargin;

			if (backBuffer == null || backBuffer.Width != w || backBuffer.Height != bufferHeight)
			{
				backBuffer?.Dispose();
				backBuffer = new Bitmap(w, bufferHeight);
				MainPicBox.Image = backBuffer;
			}
			painting = true;
			long start = System.Diagnostics.Stopwatch.GetTimestamp();
			try
			{
				using var graph = Graphics.FromImage(backBuffer);
				Console.OnPaint(graph);
				System.Threading.Interlocked.Increment(ref paintCount);
				// 描き切ったときだけ落とす。上の早期 return (再入) では落とさない
				bufferDirty = false;
			}
			finally
			{
				System.Threading.Interlocked.Add(ref paintTicks, System.Diagnostics.Stopwatch.GetTimestamp() - start);
				painting = false;
			}
		}

		/// <summary>直近の描画結果。Phase 2 で WebView へ渡す元データにもなる。</summary>
		public Bitmap BackBuffer => backBuffer;

		public void Refresh() => RepaintNow();

		public void Invalidate() => RepaintNow();

		/// <summary>
		/// <c>ANDEMUERA_EAGER_PAINT=1</c> で「上流が描けと言ったらその場で描く」従来動作に戻す。
		/// 畳み込みの効果を A/B で測るためと、万一この経路が悪さをしたときの退避用。
		/// </summary>
		static readonly bool eagerPaint =
			Environment.GetEnvironmentVariable("ANDEMUERA_EAGER_PAINT") == "1";

		/// <summary>
		/// 上流が「今描け」と言ってきたときの処理。
		///
		/// 呼び出し元はスクリプト実行スレッドのこともあれば、アニメ用タイマーの
		/// スレッドプールスレッドのこともある。後者はスクリプトが状態を書き換えている
		/// 最中に割り込むので、ホストの排他に乗せる。取れなければその回は捨てる
		/// (スクリプトが終われば RefreshStrings(true) が飛んでくる)。
		///
		/// <b>スクリプト実行中はここでは描かない。</b>実行スレッドがホストの排他を
		/// 握ったままなので、転送側 (WebHost の producer) はその間 1 枚もエンコードできず、
		/// ここで描いても画面に出ない捨てフレームにしかならない
		/// (実測: 能力表示コマンド 1 回で 13 回描き、うち 12 回が捨て。1600x2691 で 86ms)。
		/// 代わりに <see cref="IWindowHost.TryPushFrameNow"/> でホスト自身に焼かせる。
		/// 上流の右クリック (メッセージスキップ) は途中経過が次々出るのが本来の見え方で、
		/// 最後の 1 枚だけでは別物になるため。表示が追いつかない回はホストが見送るので、
		/// 捨てフレームには戻らない。
		/// 描画時に確定する EscapedParts は、参照元 (BINPUT 系) が
		/// <see cref="EnsureRendered"/> を通るようにして担保する。
		/// </summary>
		void RepaintNow()
		{
			var host = Host;
			if (host == null)
			{
				RenderOffscreen();
				return;
			}

			// 実行中は畳む。通知 (RequestRedraw) は従来どおり出す。
			// 転送側はそれで起きて、gate が空いた時点の最新状態を 1 回だけ焼く
			if (!eagerPaint && Console != null && Console.IsInProcess)
			{
				MarkDirty();
				host.RequestRedraw();
				// 転送側は排他に入れないので、この場で 1 枚出しておく。
				// 上流の右クリック (メッセージスキップ) は途中経過が次々出るのが
				// 本来の見え方で、最後の 1 枚だけ出しても別物になってしまう。
				// 表示が追いついていなければホスト側が見送るので焼き損ねは増えない
				host.TryPushFrameNow();
				return;
			}

			bool ran = host.TryRunExclusive(() =>
			{
				RenderOffscreen();
				host.RequestRedraw();
			});
			// 描けなかった回は印だけ残す。次に EnsureRendered が呼ばれたときに反映される
			if (!ran)
				MarkDirty();
		}

		public void Close() => Host?.RequestClose();

		public bool Focus() => true;

		public object Invoke(Delegate method) => method?.DynamicInvoke();

		public object Invoke(Delegate method, params object[] args) => method?.DynamicInvoke(args);

		public void Print(string text) { }

		public void NewLine() { }

		public void Dispose()
		{
			backBuffer?.Dispose();
			MainPicBox.Dispose();
			ScrollBar.Dispose();
			TextBox.Dispose();
			ToolTip.Dispose();
		}
	}

	/// <summary>
	/// デバッグウィンドウ。Android 版では画面を持たないため、ログを溜めるだけのスタブ。
	/// </summary>
	public sealed class DebugDialog : IDisposable
	{
		readonly System.Collections.Generic.List<string> lines = [];

		public bool Created { get; set; } = true;
		public bool IsDisposed { get; private set; }

		public System.Collections.Generic.IReadOnlyList<string> Lines => lines;

		public void PrintSystemLine(string str) => lines.Add(str);

		public void PrintError(string str) => lines.Add(str);

		public void UpdateData() { }

		public void Show() { }

		public void Hide() { }

		public bool Focus() => true;

		public void SetParent(object console, object process) { }

		public void TranslateUI() { }

		public void Activate() { }

		public void Close() => Dispose();

		public void Refresh() { }

		public object Invoke(Delegate method) => method?.DynamicInvoke();

		public void Dispose() => IsDisposed = true;
	}
}
