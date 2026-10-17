// andEmuera: Emuera を WebView / ブラウザから操作できるようにするホスト。
//
// 表示は当面「上流と同じ描画結果を PNG にして貼る」暫定モード。
// 上流の EmueraConsole がそのまま描くので表示互換は完全で、
// タップ座標をそのままマウス座標として渡せるためボタン操作もそのまま動く。
// 行モデルを JSON で送る本命モードは、この土台の上に載せ替える。

using MinorShift.Emuera.Api;
using MinorShift.Emuera.Forms;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MinorShift.Emuera.WebHost
{
	public sealed class EmueraWebHost : IWindowHost, IDisposable
	{
		readonly object gate = new();
		readonly MiniWebServer server;

		EmueraEngine engine;
		byte[] screenCache;
		int screenCacheGeneration = -1;
		int generation;

		// 計測 (/stats)。すべて gate の中で更新する
		ulong lastFrameHash;
		bool hasLastFrameHash;
		int encodedCount;
		int skippedCount;
		double renderMs, hashMs, encodeMs, inputWaitMs;

		// 1 入力ぶんの内訳。「処理中」が消えるのはスクリプトが終わった瞬間なので、
		// 体感の待ち時間はここに全部入っている (エンコードと転送はこの外側)
		string lastInputType;
		double lastInputTotalMs, lastInputPaintMs;
		long lastInputPaints;
		readonly double[] recentInputMs = new double[32];
		int recentInputCount;

		// 画面フレームの生成・送出
		readonly CancellationTokenSource cts = new();
		readonly SemaphoreSlim frameSignal = new(0, 1);
		Task producer;
		int lastSentGeneration = -1;
		bool sentAnyFrame;
		volatile bool binaryFrames = true;
		volatile bool ackPending;
		long ackDeadlineTicks;

		/// <summary>ack が返らないまま次のフレームを止め続けないための保険。</summary>
		static readonly TimeSpan AckTimeout = TimeSpan.FromSeconds(2);

		readonly bool useConfigWidth;

		/// <param name="useConfigWidth">
		/// true なら emuera.config のウィンドウ幅で描画し、画面側で縮小表示させる。
		/// スマホの実ピクセル幅で描くと era のレイアウトが横に収まらないため既定で有効。
		/// </param>
		public EmueraWebHost(string gameDir, int width = 960, int height = 1440, int port = 0, bool useConfigWidth = true)
		{
			GameDir = gameDir;
			Width = width;
			Height = height;
			this.useConfigWidth = useConfigWidth;

			server = new MiniWebServer(port);
			server.OnRequest = HandleRequest;
			server.OnMessage = HandleMessage;
			server.OnClientConnected = () =>
			{
				// 繋ぎ直した画面は何も持っていない。内容が前と同じでも 1 枚は必ず送る
				lock (gate)
					sentAnyFrame = false;
				ackPending = false;
				lastState = null;   // 状態も 1 回は必ず送り直す
				SendMetrics();
				NotifyRedraw();
				SendFontWarning();
			};
		}

		public string GameDir { get; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public string Url => server.Url + "?token=" + server.AccessToken;

		/// <summary>ログ出力先 (Android では Logcat へ流す)。</summary>
		public Action<string> Log { get; set; }

		public async Task StartAsync()
		{
			server.Start();
			producer = Task.Run(() => ProduceFramesAsync(cts.Token));
			Log?.Invoke($"サーバー起動: {Url}");
			// Task.Run で包むのは Android のため。
			// 上流の ErbLoader は ERB 1 本ごとに await Task.Run(...) しており、
			// ConfigureAwait(false) が無いので SynchronizationContext があると
			// 2,697 回ぶんの継続がすべて UI スレッドのルーパへ post される。
			// ここで一段挟めば以降のコンテキストは null (スレッドプール) になる。
			// 描画はオフスクリーンの SkiaSharp なので UI スレッド固有の要求は無い
			engine = await Task.Run(() => EmueraEngine.StartAsync(GameDir, this, Width, Height, useConfigWidth));
			var size = engine.ClientSize;
			Width = size.Width;
			Height = size.Height;
			Log?.Invoke($"ゲーム読み込み完了 IsError={engine.IsError} 描画サイズ={Width}x{Height}");
			if (engine.FontWarning != null)
				Log?.Invoke(engine.FontWarning);
			SendMetrics();
			NotifyRedraw();
		}

		#region IWindowHost

		public void RequestRedraw() => NotifyRedraw();

		public void SetTitle(string title) => Send(new { t = "title", v = title });

		public void SetInputText(string text) => Send(new { t = "input", v = text });

		public void SetInputPosition(int xOffset, int yOffset, int width)
			=> Send(new { t = "inputpos", x = xOffset, y = yOffset, w = width });

		public void ResetInputPosition() => Send(new { t = "inputpos", x = 0, y = 0, w = 0 });

		public void ShowToolTip(string text, int x, int y) => Send(new { t = "tip", v = text, x, y });

		public void RequestClose() => Send(new { t = "close" });

		public void RequestReboot() => Send(new { t = "reboot" });

		/// <summary>
		/// アニメ用タイマー (スレッドプール) からの描画をスクリプト実行と排他にする。
		/// 待たない (TryEnter のタイムアウト 0) ので、ロック順序の逆転もデッドロックも起きない。
		/// 同一スレッドからの再入 (スクリプト実行中の RefreshStrings) は Monitor が再帰的に通す。
		/// </summary>
		public bool TryRunExclusive(Action action)
		{
			if (!Monitor.TryEnter(gate))
				return false;
			try { action(); }
			finally { Monitor.Exit(gate); }
			return true;
		}

		/// <summary>エンコード中の再入よけ。呼ぶのはスクリプト実行スレッドだけ。</summary>
		bool pushingFrame;

		/// <summary>直近に割り込みで焼いた時刻。</summary>
		long lastPushTicks;

		// 実行中の割り込み描画がどこで落ちているかを /stats から見るための内訳。
		// 「途中が出ない」ときに、呼ばれていないのか・間引かれたのか・
		// 中身が同じで送らなかったのかを切り分ける
		long liveCalls, liveThrottled, liveNoGate, liveSent;

		/// <summary>
		/// スクリプト実行中に途中経過を焼く間隔 (ms)。
		/// <c>ANDEMUERA_LIVE_FRAME_MS=0</c> で従来動作 (実行中は 1 枚も出さない) に戻せる。
		/// </summary>
		static readonly long LiveFrameIntervalMs =
			long.TryParse(Environment.GetEnvironmentVariable("ANDEMUERA_LIVE_FRAME_MS"), out var envMs) && envMs >= 0
				? envMs
				: 100;

		/// <summary>
		/// スクリプト実行中の割り込み描画。呼び出し元は gate を握っている実行スレッド自身。
		///
		/// producer はその間 gate に入れないので、ここで焼かないと
		/// 「処理中」が明けるまで 1 枚も画面に出ない。
		///
		/// <b>間引きは ack ではなく時間で行う。</b>ack を運んでくる受信スレッドが
		/// スクリプトを回している当人なので、実行中は ack が 1 つも処理されない。
		/// ack を待つ作りにすると 1 枚出したきり <see cref="AckTimeout"/> (2 秒) まで止まる。
		/// </summary>
		public bool TryPushFrameNow()
		{
			System.Threading.Interlocked.Increment(ref liveCalls);
			if (LiveFrameIntervalMs <= 0 || !binaryFrames || engine == null || pushingFrame)
				return false;

			long now = Environment.TickCount64;
			if (now - lastPushTicks < LiveFrameIntervalMs)
			{
				System.Threading.Interlocked.Increment(ref liveThrottled);
				return false;
			}
			// 焼けなかった回も間隔は進める (失敗を連打しない)
			lastPushTicks = now;

			// 待たない。ここへはアニメ用タイマー (スレッドプール) からも来るので、
			// lock で待つと実行が明けるまでそのスレッドを止めてしまう。
			// スクリプト実行スレッド自身なら同一スレッドの再入として通る
			if (!Monitor.TryEnter(gate))
			{
				System.Threading.Interlocked.Increment(ref liveNoGate);
				return false;
			}

			// EncodeCurrentScreen → EnsureRendered → 上流の Refresh と戻ってくる道があるので
			// 自分自身への再入を止める
			pushingFrame = true;
			try
			{
				// ack は立てない。立てると実行が明けた直後の最終フレームが
				// 返るはずのない ack を 2 秒待つことになる
				bool sent = TryEncodeAndBroadcast(trackAck: false);
				if (sent)
					System.Threading.Interlocked.Increment(ref liveSent);
				return sent;
			}
			catch (Exception ex)
			{
				Log?.Invoke($"途中フレームの生成に失敗: {ex.GetType().Name}: {ex.Message}");
				return false;
			}
			finally
			{
				pushingFrame = false;
				Monitor.Exit(gate);
			}
		}

		#endregion

		/// <summary>
		/// いまの世代を 1 枚焼いて送る。送ったら true。
		///
		/// producer (通常経路) と <see cref="TryPushFrameNow"/> (実行中の割り込み) の共通部分。
		/// gate は呼び出し元が既に握っていても構わない。同一スレッドなら Monitor が再入を通す。
		/// </summary>
		/// <param name="trackAck">
		/// 送ったフレームの ack を待つか。実行中の割り込みは ack が処理されない区間なので false。
		/// </param>
		bool TryEncodeAndBroadcast(bool trackAck = true)
		{
			byte[] frame;
			lock (gate)
			{
				if (engine == null || lastSentGeneration == generation)
					return false;

				int gen = generation;
				int encodedBefore = encodedCount;
				var png = EncodeCurrentScreen();
				lastSentGeneration = gen;

				// 中身が前回と同じなら送らない (EncodeCurrentScreen が
				// 再エンコードを省いた = encodedCount が増えていない)
				if (png == null || png.Length == 0)
					return false;
				if (encodedCount == encodedBefore && sentAnyFrame)
					return false;

				frame = BuildFrame(gen, engine.ScrollState, png);
				sentAnyFrame = true;
			}

			if (trackAck)
			{
				ackPending = true;
				Volatile.Write(ref ackDeadlineTicks, Environment.TickCount64 + (long)AckTimeout.TotalMilliseconds);
			}
			server.BroadcastImage(frame);
			return true;
		}

		string lastState;
		bool lastSingleInput;
		int lastButtons = -1;

		void NotifyRedraw()
		{
			generation++;
			// スクロール位置を redraw に同梱する。画面側はこれで
			// 「バックログを見ている」ことを判定するので、往復を増やさずに済む
			var scroll = engine?.ScrollState ?? (0, 0);
			Send(new { t = "redraw", v = generation, s = scroll.Value, max = scroll.Max });

			SendState();
			WakeProducer();
		}

		/// <summary>
		/// いま実行側が何をしているか。画面側はこれで
		/// 「押せていない」のか「処理待ち」なのかを見分けて表示する。
		///
		/// 数値入力ならテンキーを出させる、という従来の <c>{t:"mode"}</c> の役目も兼ねる。
		/// </summary>
		static string DescribeState(EmueraEngine engine)
		{
			if (engine == null)
				return "loading";
			// Disabling mouse clicks must not change keyboard input types.
			// WAIT and INPUTS still need their own continuation/text controls.
			if (engine.IsError)
				return "error";
			// 実行中の判定はどの入力待ちよりも先。inputReq は前回の待ちが残っている
			if (engine.IsInProcess)
				return "busy";
			if (engine.IsWaitingMouse)
				return "mouse";
			return engine.InputMode switch
			{
				EmueraInputMode.EnterKey => "enter",
				EmueraInputMode.Integer => "integer",
				EmueraInputMode.String => "string",
				EmueraInputMode.Any => "any",
				EmueraInputMode.Void => "void",
				_ => "none",
			};
		}

		/// <summary>
		/// 状態が変わったときだけ画面へ送る。<b>generation は増やさない</b>ので、
		/// PNG の再エンコードも転送も誘発しない。
		/// </summary>
		void SendState()
		{
			string state = DescribeState(engine);

			// 選択肢の数は「入力欄が要る場面かどうか」の判断にだけ使う。
			// 表示行を走査するので gate の中で数える (実行中はそもそも数えない)。
			// スクリプト実行スレッドから来た場合は既に持っているので、Monitor の再帰で素通りする
			int buttons = 0;
			if (state is "integer" or "string" or "any")
			{
				lock (gate)
					buttons = engine.SelectableButtonCount();
			}

			bool single = engine?.IsSingleInput == true;
			if (state == lastState && buttons == lastButtons && single == lastSingleInput)
				return;
			lastState = state;
			lastButtons = buttons;
			lastSingleInput = single;
			Send(new { t = "state", v = state, n = buttons, single });
		}

		/// <summary>
		/// スクリプトを走らせる前に「処理中」を先出しする。
		///
		/// スクリプトは WebSocket の受信スレッド上で gate を握ったまま同期実行されるので、
		/// 処理に入ってしまうと画面へ何も言えなくなる。ここで先に投げておく
		/// (送信自体は WsClient のポンプが別スレッドで捌くのでブロックしない)。
		/// </summary>
		void SendBusy()
		{
			if (lastState == "busy")
				return;
			lastState = "busy";
			lastButtons = 0;
			Send(new { t = "state", v = "busy", n = 0 });
		}

		void WakeProducer()
		{
			try { frameSignal.Release(); }
			catch (SemaphoreFullException) { /* 既に起きている */ }
		}

		/// <summary>
		/// 画面フレームを作って WebSocket へ押し出すワーカー。
		///
		/// 「クライアントが表示し終える (ack) まで次を作らない」ことで、
		/// エンコードの CPU を画面の追随速度に合わせる。フリック中に
		/// 誰も見ないフレームを焼かずに済む。
		/// </summary>
		async Task ProduceFramesAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					// タイムアウトつきで待つので、ack が落ちても必ず息を吹き返す
					await frameSignal.WaitAsync(AckTimeout, token);
				}
				catch (OperationCanceledException) { break; }

				if (!binaryFrames)
					continue;
				if (ackPending)
				{
					if (Environment.TickCount64 < Volatile.Read(ref ackDeadlineTicks))
						continue;
					// 表示の返事が来ない。先へ進む
					ackPending = false;
				}

				try
				{
					TryEncodeAndBroadcast();
				}
				catch (Exception ex)
				{
					Log?.Invoke($"フレーム生成に失敗: {ex.GetType().Name}: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// 画像フレームの中身。JSON と順序を対応付けずに済むよう自己記述にする。
		/// <code>
		/// 0 : 'E','M', バージョン, 予約
		/// 4 : uint32 世代番号
		/// 8 : int32  スクロール位置
		/// 12: int32  スクロール最大値
		/// 16: PNG
		/// </code>
		/// </summary>
		const int FrameHeaderSize = 16;

		static byte[] BuildFrame(int generation, (int Value, int Max) scroll, byte[] png)
		{
			var frame = new byte[FrameHeaderSize + png.Length];
			frame[0] = (byte)'E';
			frame[1] = (byte)'M';
			frame[2] = 1;
			frame[3] = 0;
			BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(4), (uint)generation);
			BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(8), scroll.Value);
			BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(12), scroll.Max);
			png.CopyTo(frame, FrameHeaderSize);
			return frame;
		}

		void Send(object payload) => server.Broadcast(JsonSerializer.Serialize(payload));

		/// <summary>
		/// 等幅フォントが無いときの注意書き。読み込みは接続より先に終わっているので、
		/// 起動時ではなく画面が繋がってきたときに送る。
		/// </summary>
		void SendFontWarning()
		{
			if (engine?.FontWarning != null)
				ShowToolTip(engine.FontWarning, 0, 0);
		}

		/// <summary>
		/// 画面側がフリック量を行数へ換算するのに要る情報。接続時とリサイズ後に送る。
		/// </summary>
		void SendMetrics()
		{
			if (engine == null)
				return;
			Send(new { t = "metrics", lineHeight = Math.Max(engine.LineHeight, 1), w = Width, h = Height });
		}

		/// <summary>
		/// 再描画を伴わないスクロール要求への返事。画面側はこれで次の要求を送れるようになる。
		/// </summary>
		void SendScrollState()
		{
			var scroll = engine?.ScrollState ?? (0, 0);
			Send(new { t = "scrollstate", s = scroll.Value, max = scroll.Max });
		}

		HttpResponse HandleRequest(HttpRequestInfo request)
		{
			switch (request.Path)
			{
				case "/":
				case "/index.html":
					return HttpResponse.Html(LoadIndexHtml());

				case "/screen.png":
					return HttpResponse.Png(GetScreenPng());

				case "/status":
					lock (gate)
					{
						return HttpResponse.Text(JsonSerializer.Serialize(new
						{
							ready = engine != null,
							error = engine?.IsError ?? false,
							width = Width,
							height = Height,
							generation,
						}));
					}

				case "/stats":
					lock (gate)
					{
						long paints = engine?.PaintCount ?? 0;
						var recent = RecentInputMs();
						var sorted = (double[])recent.Clone();
						Array.Sort(sorted);
						return HttpResponse.Text(JsonSerializer.Serialize(new
						{
							generation,
							paints,
							paintsPerGen = generation > 0 ? Math.Round((double)paints / generation, 2) : 0,
							// 「処理中」の実体。scriptMs は描画を除いたスクリプト自身の時間
							lastInput = lastInputType == null ? null : new
							{
								type = lastInputType,
								totalMs = Math.Round(lastInputTotalMs, 1),
								scriptMs = Math.Round(lastInputTotalMs - lastInputPaintMs, 1),
								paintMs = Math.Round(lastInputPaintMs, 1),
								paints = lastInputPaints,
							},
							inputCount = recentInputCount,
							inputMaxMs = sorted.Length == 0 ? 0 : Math.Round(sorted[^1], 1),
							inputMedianMs = sorted.Length == 0 ? 0 : Math.Round(sorted[sorted.Length / 2], 1),
							recentInputMs = Array.ConvertAll(recent, v => Math.Round(v, 1)),
							encoded = encodedCount,
							skipped = skippedCount,
							// 実行中の割り込み描画の内訳。
							// calls = RepaintNow から呼ばれた回数 / throttled = 間隔で見送り /
							// noGate = 他スレッドが gate を持っていて見送り / sent = 実際に焼いて送った
							live = new { calls = liveCalls, throttled = liveThrottled, noGate = liveNoGate, sent = liveSent, intervalMs = LiveFrameIntervalMs },
							pngBytes = screenCache?.Length ?? 0,
							renderMs = Math.Round(renderMs, 2),
							hashMs = Math.Round(hashMs, 2),
							encodeMs = Math.Round(encodeMs, 2),
							inputWaitMs = Math.Round(inputWaitMs, 2),
							pngFilter = EmueraEngine.PngFilter.ToString(),
							pngZLibLevel = EmueraEngine.PngZLibLevel,
							clients = server.ClientCount,
							binaryFrames,
							ackPending,
						}));
					}

#if ERAFLOW_TEST
				case "/choices":
					lock (gate)
						return HttpResponse.Text(JsonSerializer.Serialize(engine?.GetSelectableChoices() ?? []));
				case "/diagnostics":
					lock (gate)
						return HttpResponse.Text(JsonSerializer.Serialize(engine?.GetDisplayLines().TakeLast(32).ToArray() ?? []));
#endif

				default:
					return HttpResponse.NotFound();
			}
		}

		byte[] GetScreenPng()
		{
			lock (gate)
			{
				if (engine == null)
					return [];
				if (screenCache != null && screenCacheGeneration == generation)
					return screenCache;
				return EncodeCurrentScreen();
			}
		}

		/// <summary>
		/// 現在の画面を PNG にする。gate を保持した状態で呼ぶこと。
		///
		/// 世代が進んでいても中身が変わっていないことがある (アニメ用タイマーの空回り、
		/// ボタン外のポインタ移動など)。ピクセルのハッシュが前回と同じなら
		/// 再エンコードを丸ごと省く。ハッシュは数 ms、エンコードは 2 桁 ms なので割に合う。
		/// </summary>
		byte[] EncodeCurrentScreen()
		{
			var sw = Stopwatch.StartNew();
			engine.EnsureRendered();
			renderMs = sw.Elapsed.TotalMilliseconds;

			sw.Restart();
			ulong hash = engine.HashBackBuffer();
			hashMs = sw.Elapsed.TotalMilliseconds;

			if (screenCache != null && hasLastFrameHash && hash == lastFrameHash)
			{
				skippedCount++;
				screenCacheGeneration = generation;
				return screenCache;
			}

			sw.Restart();
			screenCache = engine.RenderPng();
			encodeMs = sw.Elapsed.TotalMilliseconds;

			encodedCount++;
			lastFrameHash = hash;
			hasLastFrameHash = true;
			screenCacheGeneration = generation;
			return screenCache;
		}

		void HandleMessage(string json)
		{
			try
			{
				using var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;
				string type = root.GetProperty("t").GetString();

				// 画面が「表示し終えた」と言ってきた。次のフレームを作ってよい
				if (type == "ack")
				{
					ackPending = false;
					WakeProducer();
					return;
				}

				// 画面の受け取り方の申告。従来の /screen.png 取得に戻す ?legacy=1 用
				if (type == "hello")
				{
					binaryFrames = !root.TryGetProperty("binary", out var bin) || bin.ValueKind != JsonValueKind.False;
					ackPending = false;
					lock (gate)
						sentAnyFrame = false;
					NotifyRedraw();
					return;
				}

				// 画面に変化が無かった操作で PNG を作り直さないための印
				// (端まで送ったあとのフリックなど。1600x1374 の再エンコードは重い)
				bool changed = true;
				bool metricsChanged = false;

				// タップ 1 回ごとの結果。画面側はこれで波紋の色を決める
				EmueraTapResult? tapResult = null;
				int tapId = GetInt(root, "id");

				// スクリプトを走らせうる操作は、gate を取る前に「処理中」を伝える。
				// 実行中は受信スレッドごと止まるので、ここを逃すと言う機会が無い
				if (type is "click" or "submit" or "enter" or "skip")
					SendBusy();

				var waited = Stopwatch.StartNew();
				lock (gate)
				{
					inputWaitMs = waited.Elapsed.TotalMilliseconds;
					if (engine == null)
						return;

					// gate を握っている間が「処理中」の実体。描画の回数と時間も一緒に取る
					long paintsBefore = engine.PaintCount;
					double paintMsBefore = engine.PaintMs;
					var work = Stopwatch.StartNew();

					// 上流は表示状態 (EscapedParts) を OnPaint の中で確定する。BINPUT など
					// 入力処理がそれを参照するので、処理に入る前に描画済みを保証しておく。
					// 通常は描画済みなので何もしない
					engine.EnsureRendered();

					switch (type)
					{
						case "click":
							tapResult = engine.Click(GetInt(root, "x"), GetInt(root, "y"), GetBool(root, "right"));
							break;

						// ポインタ移動だけの通知 (PC のマウス)。上流の MoveMouse は
						// 「この後で RefreshStrings が必要か」を返すので、そのまま changed に使う
						case "move":
							changed = engine.MoveMouse(GetInt(root, "x"), GetInt(root, "y"));
							break;

						case "scroll":
							engine.Scroll(GetInt(root, "x"), GetInt(root, "y"), GetInt(root, "delta"));
							break;

						// フリックによるバックログ送り。正の値で過去へ
						case "scrollLines":
							changed = engine.ScrollLines(GetInt(root, "n"), GetInt(root, "x"), GetInt(root, "y"));
							break;

						case "latest":
							changed = engine.ScrollToLatest();
							break;

						case "submit":
							tapResult = engine.SubmitInput(root.TryGetProperty("v", out var v) ? v.GetString() : string.Empty);
							break;

						case "enter":
							tapResult = engine.PressEnter();
							break;

						// 操作バーのスキップボタン。最後に触った位置での右クリックとして
						// 画面の長押しと同じ経路へ流す
						case "skip":
							tapResult = engine.MessageSkip();
							changed = true;   // 選択表示が変わることがあるので必ず反映させる
							break;

						case "resize":
							int w = GetInt(root, "w");
							int h = GetInt(root, "h");
							// レイアウト確定前は 0 が飛んでくることがある。潰れた画面を作らないよう弾く
							if (w < 200 || h < 200)
								break;
							engine.Resize(w, h);
							var newSize = engine.ClientSize;
							Width = newSize.Width;
							Height = newSize.Height;
							metricsChanged = true;
							break;

						case "images":
							DisplayOptions.ShowImages = GetBool(root, "v");
							engine.MarkDirty();
							break;
					}

					RecordInput(type, work.Elapsed.TotalMilliseconds,
								engine.PaintCount - paintsBefore, engine.PaintMs - paintMsBefore);
				}

				if (metricsChanged)
					SendMetrics();

				if (tapResult != null)
					Send(new { t = "tap", id = tapId, v = tapResult.Value.ToString().ToLowerInvariant() });

				// 操作の結果を必ず反映させる (実行側が Refresh を出さない経路もあるため)
				if (changed)
					NotifyRedraw();
				else if (type is "scrollLines" or "latest")
					SendScrollState();  // 端に達していても画面側の待ちを解く

				// SendBusy で先出しした「処理中」を実際の状態へ戻す。
				// NotifyRedraw を通った場合は既に送られているので、ここでは何も起きない
				SendState();
			}
			catch (Exception ex)
			{
				Log?.Invoke($"メッセージ処理に失敗: {ex.GetType().Name}: {ex.Message}");
				Send(new { t = "error", v = "游戏处理失败：" + ex.Message });
			}
		}

		/// <summary>
		/// 1 入力ぶんの内訳を残す。gate の中から呼ぶこと。
		///
		/// スクリプトを走らせうる操作だけを見る (ポインタ移動やリサイズは待ち時間の話ではない)。
		/// スクリプト実行中の描画は 1 枚も画面に出ないので、<c>paintMs</c> がそのまま
		/// 「捨てている時間」の上限になる。
		/// </summary>
		void RecordInput(string type, double totalMs, long paints, double paintMs)
		{
			if (type is not ("click" or "submit" or "enter" or "skip"))
				return;

			lastInputType = type;
			lastInputTotalMs = totalMs;
			lastInputPaints = paints;
			lastInputPaintMs = paintMs;

			// 直近ぶんを輪で持つ。最大値と中央値だけ見られれば十分
			recentInputMs[recentInputCount % recentInputMs.Length] = totalMs;
			recentInputCount++;
		}

		/// <summary>直近の入力時間 (新しい順)。gate の中から呼ぶこと。</summary>
		double[] RecentInputMs()
		{
			int n = Math.Min(recentInputCount, recentInputMs.Length);
			var values = new double[n];
			for (int i = 0; i < n; i++)
				values[i] = recentInputMs[(recentInputCount - 1 - i) % recentInputMs.Length];
			return values;
		}

		static int GetInt(JsonElement e, string name)
			=> e.TryGetProperty(name, out var v) && v.TryGetInt32(out int i) ? i : 0;

		static bool GetBool(JsonElement e, string name)
			=> e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

		static string LoadIndexHtml()
		{
			var asm = Assembly.GetExecutingAssembly();
			string name = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("player.html", StringComparison.Ordinal));
			if (name == null)
				return "<h1>index.html が埋め込まれていません</h1>";
			using var stream = asm.GetManifestResourceStream(name);
			using var reader = new StreamReader(stream, Encoding.UTF8);
			return reader.ReadToEnd();
		}

		public void Dispose()
		{
			cts.Cancel();
			WakeProducer();
			try { producer?.Wait(TimeSpan.FromSeconds(1)); }
			catch { /* 終了時なので握り潰す */ }
			cts.Dispose();
			server.Dispose();
			engine?.Dispose();
		}
	}
}
