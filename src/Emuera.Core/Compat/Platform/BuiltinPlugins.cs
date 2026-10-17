// andEmuera: 内蔵プラグイン。
//
// 上流は Plugins/*.dll を Assembly.LoadFrom で読み込むが、Android では
// WinForms に依存した DLL を持ち込めない。よく使われるものを C# で内蔵実装し、
// 実処理はプラットフォーム側 (Android の Intent など) に委譲する。

using System;
using System.Collections.Generic;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem
{
	public static class BuiltinPlugins
	{
		/// <summary>URL を開く処理。Android 側で Intent を発行する実装を設定する。</summary>
		public static Action<string> BrowserLauncher { get; set; }

		/// <summary>直近に開こうとした URL。ホストが確認ダイアログを出す用途に使う。</summary>
		public static string LastRequestedUrl { get; private set; }

		/// <summary>
		/// URL を既定のブラウザで開く。Android では Process.Start(UseShellExecute) が使えないため、
		/// スクリプトから URL を開く経路はすべてここを通す。
		/// </summary>
		public static void OpenUrl(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return;
			if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
				!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
				return;

			LastRequestedUrl = url;
			BrowserLauncher?.Invoke(url);
		}

		internal static IEnumerable<IPluginMethod> GetMethods()
		{
			yield return new LaunchBrowserMethod();
		}

		/// <summary>
		/// LAUNCH_BROWSER.dll 相当。CALLSHARP LAUNCH_BROWSER("https://...") で呼ばれる。
		/// </summary>
		sealed class LaunchBrowserMethod : IPluginMethod
		{
			public string Name => "LAUNCH_BROWSER";

			public string Description => "既定のブラウザで URL を開く (andEmuera 内蔵実装)";

			public void Execute(PluginMethodParameter[] args)
			{
				if (args == null || args.Length == 0)
					return;
				// スキームの検証も含めて OpenUrl 側で行う
				OpenUrl(args[0]?.strValue);
			}
		}
	}
}
