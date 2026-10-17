// andEmuera: 依存ゼロの最小 HTTP + WebSocket サーバー。
//
// ASP.NET Core を持ち込まないのは、Android 上で確実に動く構成にしたいため。
// ハンドシェイクだけ自前で行い、フレーム処理は .NET 標準の
// WebSocket.CreateFromStream に任せている。

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinorShift.Emuera.WebHost
{
	public sealed class HttpRequestInfo
	{
		public string Method { get; init; }
		public string Path { get; init; }
		public string Query { get; init; }
		public Dictionary<string, string> Headers { get; init; }
	}

	public sealed class HttpResponse
	{
		public int StatusCode { get; init; } = 200;
		public string ContentType { get; init; } = "text/plain; charset=utf-8";
		public byte[] Body { get; init; } = [];
		public string CacheControl { get; init; } = "no-store";

		public static HttpResponse Text(string text)
			=> new() { Body = Encoding.UTF8.GetBytes(text) };

		public static HttpResponse Html(string html)
			=> new() { ContentType = "text/html; charset=utf-8", Body = Encoding.UTF8.GetBytes(html) };

		public static HttpResponse Png(byte[] data)
			=> new() { ContentType = "image/png", Body = data ?? [] };

		public static HttpResponse NotFound()
			=> new() { StatusCode = 404, Body = Encoding.UTF8.GetBytes("Not Found") };
	}

	/// <summary>
	/// 1 接続ぶんの送信キュー。
	///
	/// WebSocket.SendAsync は同一ソケットに対する並行呼び出しを許さない
	/// (「送信が 1 つ未完了」で InvalidOperationException になる)。再描画通知は
	/// スクリプト実行スレッド・WS 受信スレッド・アニメ用タイマーのスレッドから飛んでくるので、
	/// 送信はこのポンプ 1 本に集約する。
	///
	/// テキスト (JSON) は順序どおり全部送るが、画像は最新の 1 枚だけ保持して古いものを捨てる。
	/// フリック中に生成が表示に先行しても、送信段でフレームが合流する。
	/// </summary>
	sealed class WsClient(WebSocket socket)
	{
		readonly Queue<byte[]> texts = new();
		readonly SemaphoreSlim signal = new(0, 1);
		readonly object queueGate = new();
		byte[] pendingImage;

		public WebSocket Socket => socket;

		public void SendText(byte[] utf8)
		{
			lock (queueGate)
				texts.Enqueue(utf8);
			Kick();
		}

		/// <summary>画像フレームを差し替える。まだ送っていない古いフレームは捨てる。</summary>
		public void SendImage(byte[] frame)
		{
			lock (queueGate)
				pendingImage = frame;
			Kick();
		}

		void Kick()
		{
			try { signal.Release(); }
			catch (SemaphoreFullException) { /* 既に起きている */ }
		}

		public async Task PumpAsync(CancellationToken token)
		{
			try
			{
				while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
				{
					await signal.WaitAsync(token);
					while (true)
					{
						byte[] text = null, image = null;
						lock (queueGate)
						{
							if (texts.Count > 0)
								text = texts.Dequeue();
							else
								(image, pendingImage) = (pendingImage, null);
						}
						if (text != null)
						{
							await socket.SendAsync(text, WebSocketMessageType.Text, true, token);
							continue;
						}
						if (image != null)
						{
							await socket.SendAsync(image, WebSocketMessageType.Binary, true, token);
							continue;
						}
						break;
					}
				}
			}
			catch (OperationCanceledException) { }
			catch (WebSocketException) { }
			catch (ObjectDisposedException) { }
		}
	}

	public sealed class MiniWebServer : IDisposable
	{
		const string WebSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

		readonly TcpListener listener;
		readonly CancellationTokenSource cts = new();
		readonly ConcurrentDictionary<WsClient, byte> clients = new();

		public MiniWebServer(int port = 0, IPAddress address = null)
		{
			listener = new TcpListener(address ?? IPAddress.Loopback, port);
			listener.Start();
			Port = ((IPEndPoint)listener.LocalEndpoint).Port;
		}

		public int Port { get; }
		public string AccessToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

		public string Url => $"http://127.0.0.1:{Port}/";

		/// <summary>GET リクエストの処理。</summary>
		public Func<HttpRequestInfo, HttpResponse> OnRequest { get; set; }

		/// <summary>WebSocket でクライアントから届いたテキスト。</summary>
		public Action<string> OnMessage { get; set; }

		/// <summary>クライアントが接続したときに呼ばれる。</summary>
		public Action OnClientConnected { get; set; }

		public void Start() => _ = Task.Run(AcceptLoopAsync);

		async Task AcceptLoopAsync()
		{
			while (!cts.IsCancellationRequested)
			{
				TcpClient client;
				try
				{
					client = await listener.AcceptTcpClientAsync(cts.Token);
				}
				catch (OperationCanceledException) { break; }
				catch (ObjectDisposedException) { break; }

				_ = Task.Run(() => HandleClientAsync(client));
			}
		}

		async Task HandleClientAsync(TcpClient client)
		{
			try
			{
				client.NoDelay = true;
				using var stream = client.GetStream();
				var request = await ReadRequestAsync(stream);
				if (request == null)
					return;
				if (!Array.Exists(request.Query.Split('&'), part => part == "token=" + AccessToken))
				{
					await WriteResponseAsync(stream, new HttpResponse { StatusCode = 403 });
					return;
				}

				if (request.Headers.TryGetValue("upgrade", out var upgrade) &&
					upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase))
				{
					await HandleWebSocketAsync(stream, request);
					return;
				}

				var response = OnRequest?.Invoke(request) ?? HttpResponse.NotFound();
				await WriteResponseAsync(stream, response);
			}
			catch (IOException) { /* 切断は日常茶飯事 */ }
			catch (SocketException) { }
			finally
			{
				client.Dispose();
			}
		}

		static async Task<HttpRequestInfo> ReadRequestAsync(NetworkStream stream)
		{
			var buffer = new byte[8192];
			int filled = 0;
			int headerEnd = -1;

			while (filled < buffer.Length)
			{
				int read = await stream.ReadAsync(buffer.AsMemory(filled));
				if (read == 0)
					return null;
				filled += read;
				headerEnd = IndexOfHeaderEnd(buffer, filled);
				if (headerEnd >= 0)
					break;
			}
			if (headerEnd < 0)
				return null;

			string head = Encoding.UTF8.GetString(buffer, 0, headerEnd);
			var lines = head.Split("\r\n");
			var requestLine = lines[0].Split(' ');
			if (requestLine.Length < 2)
				return null;

			string target = requestLine[1];
			int q = target.IndexOf('?');
			var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 1; i < lines.Length; i++)
			{
				int colon = lines[i].IndexOf(':');
				if (colon > 0)
					headers[lines[i][..colon].Trim()] = lines[i][(colon + 1)..].Trim();
			}

			return new HttpRequestInfo
			{
				Method = requestLine[0],
				Path = q >= 0 ? target[..q] : target,
				Query = q >= 0 ? target[(q + 1)..] : string.Empty,
				Headers = headers,
			};
		}

		static int IndexOfHeaderEnd(byte[] buffer, int length)
		{
			for (int i = 3; i < length; i++)
			{
				if (buffer[i] == '\n' && buffer[i - 1] == '\r' && buffer[i - 2] == '\n' && buffer[i - 3] == '\r')
					return i + 1;
			}
			return -1;
		}

		static async Task WriteResponseAsync(NetworkStream stream, HttpResponse response)
		{
			var header = new StringBuilder();
			header.Append("HTTP/1.1 ").Append(response.StatusCode).Append(' ')
				  .Append(response.StatusCode == 200 ? "OK" : "Error").Append("\r\n");
			header.Append("Content-Type: ").Append(response.ContentType).Append("\r\n");
			header.Append("Content-Length: ").Append(response.Body.Length).Append("\r\n");
			header.Append("Cache-Control: ").Append(response.CacheControl).Append("\r\n");
			header.Append("Connection: close\r\n\r\n");

			await stream.WriteAsync(Encoding.UTF8.GetBytes(header.ToString()));
			if (response.Body.Length > 0)
				await stream.WriteAsync(response.Body);
			await stream.FlushAsync();
		}

		async Task HandleWebSocketAsync(NetworkStream stream, HttpRequestInfo request)
		{
			if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var key))
				return;

			string accept = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(key + WebSocketGuid)));
			string handshake =
				"HTTP/1.1 101 Switching Protocols\r\n" +
				"Upgrade: websocket\r\n" +
				"Connection: Upgrade\r\n" +
				$"Sec-WebSocket-Accept: {accept}\r\n\r\n";
			await stream.WriteAsync(Encoding.UTF8.GetBytes(handshake));
			await stream.FlushAsync();

			using var socket = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: null,
				keepAliveInterval: TimeSpan.FromSeconds(30));

			var client = new WsClient(socket);
			using var pumpCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
			var pump = client.PumpAsync(pumpCts.Token);
			clients.TryAdd(client, 0);
			OnClientConnected?.Invoke();

			var buffer = new byte[16 * 1024];
			try
			{
				while (socket.State == WebSocketState.Open && !cts.IsCancellationRequested)
				{
					var result = await socket.ReceiveAsync(buffer, cts.Token);
					if (result.MessageType == WebSocketMessageType.Close)
						break;
					if (result.MessageType == WebSocketMessageType.Text)
						OnMessage?.Invoke(Encoding.UTF8.GetString(buffer, 0, result.Count));
				}
			}
			catch (OperationCanceledException) { }
			catch (WebSocketException) { }
			finally
			{
				clients.TryRemove(client, out _);
				pumpCts.Cancel();
				try { await pump; } catch { }
			}
		}

		/// <summary>接続中の全クライアントへテキストを送る。</summary>
		public void Broadcast(string message)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			foreach (var client in clients.Keys)
				client.SendText(bytes);
		}

		/// <summary>
		/// 接続中の全クライアントへ画像フレームを送る。
		/// まだ送り終えていない古いフレームがあれば差し替える (latest-wins)。
		/// </summary>
		public void BroadcastImage(byte[] frame)
		{
			foreach (var client in clients.Keys)
				client.SendImage(frame);
		}

		/// <summary>接続中のクライアント数。</summary>
		public int ClientCount => clients.Count;

		public void Dispose()
		{
			cts.Cancel();
			listener.Stop();
			cts.Dispose();
		}
	}
}
