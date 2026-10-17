// andEmuera: era のスクリプトが書くパスを、実行環境の区切り文字へ正規化する。
//
// 上流は Replace('/', '\\') で区切りを '\' に固定していた。Windows では '/' も '\' も
// 区切りとして扱われるので問題にならないが、Android(ext4) では '\' は区切りではなく
// ファイル名に使える普通の文字である。そのため
//     resources/1001ペコリーヌ/顔_デフォルト.webp
// が丸ごと 1 個のファイル名に化け、File.Exists / Directory.Exists が必ず false になる。
//
// Path.GetFullPath を使わないのは、実行中の OS のパス規則に固定されて
// Windows 上から Android の挙動を検証できなくなるため。区切り文字を引数に取る
// 純粋関数にしてあり、TestHarness の --selftest-path が両方の区切りで検証する。

using System.Collections.Generic;
using System.IO;

namespace MinorShift.Emuera.Runtime.Utils
{
	public static class PortablePath
	{
		/// <summary>実行環境の区切り文字。テストからのみ差し替える。</summary>
		internal static char Separator = Path.DirectorySeparatorChar;

		/// <summary>
		/// ERB が '/' でも '\' でも書ける相対パスを、実行環境の区切りへ揃えるだけの版。
		/// ゲームフォルダ配下への閉じ込めはしないので、絶対パスも許す命令 (GCREATEFROMFILE 等) で使う。
		/// </summary>
		public static string Normalize(string path) => Normalize(path, Separator);

		/// <summary>区切り文字を明示する版。TestHarness がプラットフォーム差を検証するのに使う。</summary>
		public static string Normalize(string path, char sep)
		{
			if (string.IsNullOrEmpty(path))
				return path;
			return sep == '/' ? path.Replace('\\', '/') : path.Replace('/', '\\');
		}

		/// <summary>
		/// root (末尾に区切りを含むゲームフォルダ) 配下の実パスへ解決する。
		/// 絶対パス・ドライブ指定・UNC は受け付けず null を返す。
		/// </summary>
		public static string CombineUnderRoot(string root, string path) => CombineUnderRoot(root, path, Separator);

		/// <summary>区切り文字を明示する版。TestHarness がプラットフォーム差を検証するのに使う。</summary>
		public static string CombineUnderRoot(string root, string path, char sep)
		{
			if (path == null || path.IndexOf('\0') >= 0)
				return null;
			// 上流は Path.GetPathRoot で判定していたが、Android では "C:\..." を素通ししてしまう。
			// どの環境でも同じ結果になるよう明示的に判定する。
			if (IsRootedAnyPlatform(path))
				return null;

			// 文字列置換ではなくセグメント単位で畳む。上流の Replace("..\\", "") は
			// String.Replace が置換後を再走査しないため "....//etc" が "..\etc" に化け、
			// ゲームフォルダの外へ出られた。ENUMFILES と LOADTEXT が呼び出し元なので塞いでおく。
			var segments = new List<string>();
			int start = 0;
			for (int i = 0; i <= path.Length; i++)
			{
				if (i < path.Length && path[i] != '/' && path[i] != '\\')
					continue;
				string seg = path.Substring(start, i - start);
				start = i + 1;
				if (seg.Length == 0 || seg == ".")
					continue;
				if (seg == "..")
				{
					// 上流同様「黙って落とす」。ただし段組みなので root より上へは決して出られない。
					if (segments.Count > 0)
						segments.RemoveAt(segments.Count - 1);
					continue;
				}
				segments.Add(seg);
			}
			return segments.Count == 0 ? root : root + string.Join(sep, segments);
		}

		static bool IsRootedAnyPlatform(string path)
		{
			if (path.Length == 0)
				return false;
			if (path[0] == '/' || path[0] == '\\')   // 絶対パス / UNC
				return true;
			if (path.Length >= 2 && path[1] == ':')  // ドライブ指定
				return true;
			return false;
		}
	}
}
