// andEmuera: 上流 Runtime/Utils/Sound.WMP.cs (Windows Media Player COM) の置き換え。
// 再生の実体は Android の MediaPlayer に委譲する。

namespace MinorShift.Emuera.Runtime.Utils
{
	/// <summary>
	/// プラットフォーム側の音声再生実装。Android では MediaPlayer で実装する。
	/// </summary>
	public interface ISoundBackend
	{
		void Play(string filename, int repeat);
		void Stop();
		void Close();
		bool IsPlaying { get; }
		void SetVolume(int volume);
	}

	/// <summary>
	/// 上流と同じ API を保ったまま、実処理を <see cref="Backend"/> に委譲する。
	/// バックエンド未設定時は何もしない (音声なしで動作する)。
	/// </summary>
	internal class Sound
	{
		/// <summary>Android 側の起動処理で設定する。</summary>
		public static ISoundBackend Backend { get; set; }

		public void play(string filename, int repeat = 1) => Backend?.Play(filename, repeat);

		public void stop() => Backend?.Stop();

		public void close() => Backend?.Close();

		public bool isPlaying() => Backend?.IsPlaying ?? false;

		public void setVolume(int volume) => Backend?.SetVolume(volume);
	}
}

namespace MinorShift.Emuera.UI.Game
{
	/// <summary>
	/// 和英辞書ポップアップ (EmuEra-Rikaichan)。PC でマウスホバーした語を辞書引きする機能で、
	/// Android では相当する操作が無いため無効化する。上流から参照されるメンバだけを持つ。
	/// </summary>
	internal partial class Rikaichan
	{
		/// <summary>常に false。Config.RikaiEnabled が真でもこちらで止める。</summary>
		public bool enabled = false;

		public bool hidden = true;
		public string laststr = string.Empty;
		public string output = string.Empty;
		public int strpos;
		public int laststrpos;
		public int curLineY;
		public System.Drawing.Point point;
		public ConsoleStyledString css;

		public void OnPaint(System.Drawing.Graphics graph, StringMeasure measure, int width) { }

		public void Clear() { }

		public void Hide() => hidden = true;

		public void Dispose() { }
	}
}
