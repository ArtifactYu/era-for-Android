using System.Windows.Forms;
using System.IO;
using System;

namespace MinorShift.Emuera.Runtime.Utils;

public static class AssemblyData
{
	static AssemblyData()
	{
		ExePath = Environment.ProcessPath;
		//andEmuera: Android の区切りは '/' なので '\' 固定にしない
		#region eee_カレントディレクトリー
		WorkingDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
		#endregion
		ExeDir = Path.GetDirectoryName(ExePath) + Path.DirectorySeparatorChar;
		ExeName = Path.GetFileName(ExePath);
		emueraVer = typeof(AssemblyData).Assembly.GetName().Version;

		//統合: EM+EE と .netEmuera の統合版なので、どちらの名前でもない独自の名前を出す
		EmueraVersionText = "EmueraEX " + Application.ProductVersion;
	}

	/// <summary>
	/// 実行ファイルのパス
	/// </summary>
	public static readonly string ExePath;

	public readonly static Version emueraVer;

	public readonly static string EmueraVersionText;

	/// <summary>
	/// 実行ファイルのディレクトリ。最後に\を付けたstring
	/// </summary>
	public static readonly string ExeDir;

	#region eee_カレントディレクトリー
	/// <summary>
	/// 実行ファイルのディレクトリ。最後に\を付けたstring
	/// </summary>
	public static readonly string WorkingDir;
	#endregion

	/// <summary>
	/// 実行ファイルの名前。ディレクトリなし
	/// </summary>
	public static readonly string ExeName;

	/// <summary>
	/// 2重起動防止。既に同名exeが実行されているならばtrueを返す
	/// </summary>
	/// <returns></returns>
	public static bool PrevInstance()
	{
		string thisProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
		if (System.Diagnostics.Process.GetProcessesByName(thisProcessName).Length > 1)
		{
			return true;
		}
		return false;

	}
}
