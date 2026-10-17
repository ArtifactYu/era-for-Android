// andEmuera: 上流 Program.cs の置き換え。
//
// 上流の Program は「WinForms のエントリポイント」と「ゲームフォルダのパス群を保持する静的クラス」を
// 兼ねている。Android 版では前者が不要なので、パス群と実行モードのフラグだけをここで持ち、
// 初期化は Android 側から Initialize() を呼んで行う。

using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace MinorShift.Emuera
{
	public static partial class Program
	{
		/// <summary>
		/// ゲームフォルダ (csv / erb / resources を含むフォルダ) を指定して初期化する。
		/// Android では /sdcard/Android/data/&lt;pkg&gt;/files/games/&lt;title&gt;/ を渡す。
		/// </summary>
		public static void Initialize(string gameDir)
		{
			MinorShift.Emuera.Compat.CasePath.Index(gameDir);
			// Shift_JIS の CSV / ERB を読むために必須
			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
			MinorShift.Emuera.GameView.PuchiGraphics.Reset();
			SetDirPaths(gameDir);
			ExeName = "Emuera";
			LoadGameFonts();
		}

		/// <summary>
		/// ゲームフォルダの font/ にあるフォントを読み込む。上流は Program.Main でこれをやっているが、
		/// Android 版は Main ごと置き換えたためここで肩代わりする。
		///
		/// これを飛ばすと emuera.config が指定した等幅フォント (erablue_resort なら BIZ UDGothic) が
		/// 見つからず、端末の比例フォントに落ちる。すると PRINTC が桁揃えに使う半角スペースが
		/// 半角文字より狭くなり、詰めたスペースを全部剥がされて選択肢が横に繋がってしまう。
		/// </summary>
		public static int LoadGameFonts()
		{
			if (!Directory.Exists(FontDir))
				return 0;

			// 上流の FontFactory は PrivateFontCollection を名前一致で走査するので、そちらにも入れる
			foreach (string path in Directory.EnumerateFiles(FontDir, "*", new EnumerationOptions
			{
				RecurseSubdirectories = true,
				IgnoreInaccessible = true,
			}))
			{
				string ext = Path.GetExtension(path);
				if (ext.Equals(".ttf", System.StringComparison.OrdinalIgnoreCase) ||
					ext.Equals(".otf", System.StringComparison.OrdinalIgnoreCase))
					GlobalStatic.Pfc.AddFontFile(path);
			}

			// ttc や、config のフォント名がファミリ名と違う場合はこちらが拾う
			return System.Drawing.FontResolver.RegisterDirectory(FontDir);
		}

		public static void SetDirPaths(string exeDir)
		{
			ExeDir = Path.GetFullPath(new DirectoryInfo(exeDir).FullName + Path.DirectorySeparatorChar);

			CsvDir = ResolveDir("csv");
			ErbDir = ResolveDir("erb");
			DebugDir = ResolveDir("debug");
			DatDir = ResolveDir("dat");
			ContentDir = ResolveDir("resources");
			SoundDir = ResolveDir("sound");
			FontDir = ResolveDir("font");
		}

		/// <summary>
		/// ゲームフォルダ直下のサブフォルダを、大文字小文字を区別せずに解決する。
		/// Windows では "CSV" でも "csv" でも同じだが Android は別物になるため、
		/// 実在するほうの名前を採用する (見つからなければ小文字で組み立てる)。
		/// </summary>
		static string ResolveDir(string name)
		{
			string exact = Path.Combine(ExeDir, name);
			if (!Directory.Exists(exact) && Directory.Exists(ExeDir))
			{
				var options = new EnumerationOptions
				{
					MatchCasing = MatchCasing.CaseInsensitive,
					RecurseSubdirectories = false,
					IgnoreInaccessible = true,
				};
				foreach (var dir in Directory.EnumerateDirectories(ExeDir, name, options))
				{
					exact = dir;
					break;
				}
			}
			return MinorShift.Emuera.Compat.CasePath.Resolve(exact) + Path.DirectorySeparatorChar;
		}

		/// <summary>実行ファイルのディレクトリ。末尾に区切り文字を付けた文字列。</summary>
		public static string ExeDir { get; private set; }
		public static string CsvDir { get; private set; }
		public static string ErbDir { get; private set; }
		public static string DebugDir { get; private set; }
		public static string DatDir { get; private set; }
		public static string ContentDir { get; private set; }
		public static string ExeName { get; private set; }
		public static string SoundDir { get; private set; }
		public static string FontDir { get; private set; }

		public static bool rebootFlag;
		public static FormWindowState RebootWinState = FormWindowState.Normal;

		public static bool AnalysisMode;
		public static List<string> AnalysisFiles;

		public static bool DebugMode { get; set; }

		/// <summary>
		/// 起動時間の内訳を time.log (ゲームフォルダ直下) に書き出す。
		///
		/// 上流の「ロード時にレポートを表示する」(Config.DisplayReport) は
		/// ERB 1 本ごとの画面出力も有効にしてしまい、計測したいものより重い描画が足される。
		/// 時間だけ知りたいときはこちらを立てる。
		/// </summary>
		public static bool BootProfile { get; set; }
			= System.Environment.GetEnvironmentVariable("ANDEMUERA_BOOT_PROFILE") == "1";

		static Program()
		{
			SetDirPaths(System.AppContext.BaseDirectory);
		}
	}
}

namespace System.Windows.Forms
{
	public enum FormWindowState
	{
		Normal = 0,
		Minimized = 1,
		Maximized = 2,
	}
}

namespace System.Media
{
	/// <summary>
	/// Windows のシステム音。Android では対応する音が無いのでフックだけ用意する。
	/// </summary>
	public sealed class SystemSound
	{
		internal SystemSound(string name) => Name = name;

		public string Name { get; }

		/// <summary>Android 側で通知音を鳴らしたい場合に設定する。</summary>
		public static Action<string> Player { get; set; }

		public void Play() => Player?.Invoke(Name);
	}

	public static class SystemSounds
	{
		public static SystemSound Asterisk { get; } = new("Asterisk");
		public static SystemSound Beep { get; } = new("Beep");
		public static SystemSound Exclamation { get; } = new("Exclamation");
		public static SystemSound Hand { get; } = new("Hand");
		public static SystemSound Question { get; } = new("Question");
	}
}
