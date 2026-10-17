using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Config.JSON;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Windows.Forms;

namespace MinorShift.Emuera;
#nullable enable
static partial class Program
{
	/*
	コードの開始地点。
	ここでMainWindowを作り、
	MainWindowがProcessを作り、
	ProcessがGameBase・ConstantData・Variableを作る。


	*.ERBの読み込み、実行、その他の処理をProcessが、
	入出力をMainWindowが、
	定数の保存をConstantDataが、
	変数の管理をVariableが行う。

	と言う予定だったが改変するうちに境界が曖昧になってしまった。

	後にEmueraConsoleを追加し、それに入出力を担当させることに。

	1750 DebugConsole追加
	 Debugを全て切り離すことはできないので一部EmueraConsoleにも担当させる

	TODO: 1819 MainWindow & Consoleの入力・表示組とProcess&Dataのデータ処理組だけでも分離したい

	*/
	/// <summary>
	/// アプリケーションのメイン エントリ ポイントです。
	/// </summary>
	[STAThread]
	static void Main(string[] args)
	{
		// memo: Shift-JISを扱うためのおまじない
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

		var rootCommand = new RootCommand("Emuera");

		#region eee_カレントディレクトリー
		var exeDirOption = new Option<string>(
			name: "--ExeDir",
			description: "与えられたフォルダのEraを起動します"
		);
		exeDirOption.AddAlias("-exedir");
		exeDirOption.AddAlias("-EXEDIR");
		rootCommand.AddOption(exeDirOption);

		var debugModeOption = new Option<bool>(
			name: "-Debug",
			description: "デバッグモード"
		);
		debugModeOption.AddAlias("-debug");
		debugModeOption.AddAlias("-DEBUG");
		rootCommand.AddOption(debugModeOption);

		var genLangOption = new Option<bool>(
			name: "-GenLang",
			description: "言語ファイルテンプレ生成"
		);
		genLangOption.AddAlias("-genlang");
		genLangOption.AddAlias("-GENLANG");
		rootCommand.AddOption(genLangOption);

		var filesArg = new Argument<string[]>(
					"解析するファイル"
				)
		{ Arity = ArgumentArity.ZeroOrMore };
		rootCommand.AddArgument(filesArg);

		var result = rootCommand.Parse(args);

		//実行ディレクトリが引数で与えられた場合
		var exeDir = result.GetValueForOption(exeDirOption);
		if (exeDir != null)
		{
			SetDirPaths(exeDir);
		}
		else
		{
			SetDirPaths(ResolveGameDir(AssemblyData.WorkingDir));
		}

		#endregion
		//エラー出力用
		//1815 .exeが東方板のNGワードに引っかかるそうなので除去
		ExeName = Path.GetFileNameWithoutExtension(AssemblyData.ExeName);


		var debugMode = result.GetValueForOption(debugModeOption);
		DebugMode = debugMode;

		var genLang = result.GetValueForOption(genLangOption);
		if (genLang)
			Lang.GenerateDefaultLangFile();

		#region EM_私家版_Emuera多言語化改造
		List<string> otherArgs = [];

		//引数の後ろにある他のフラグにマッチしなかった文字列を解析指定されたファイルとみなす
		var fileArgs = result.GetValueForArgument(filesArg);
		var analysisRequestPaths = fileArgs;
		if (analysisRequestPaths.Length > 0)
		{
			/*
			foreach (var arg in args)
			{

				//if ((args.Length > 0) && (args[0].Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase)))
				if (arg.Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase))
				{
					// argsStart = 1;//デバッグモードかつ解析モード時に最初の1っこ(-DEBUG)を飛ばす
					DebugMode = true;
				}
				else if (arg.Equals("-GENLANG", StringComparison.CurrentCultureIgnoreCase))
				{
					Lang.GenerateDefaultLangFile();
				}
				else otherArgs.Add(arg);
			}
			//if (args.Length > argsStart)
			if (otherArgs.Count > 0)
			{
			*/
			//必要なファイルのチェックにはConfig読み込みが必須なので、ここではフラグだけ立てておく
			AnalysisMode = true;
			//}
		}
		#endregion

		Application.SetCompatibleTextRenderingDefault(false);

		ProfileOptimization.SetProfileRoot(exeDir ?? ExeDir);
		ProfileOptimization.StartProfile("profile");

		ConfigData.Instance.LoadConfig();
		JSONConfig.Load();

		//WMPも終了しておく
		FunctionIdentifier.bgm.close();
		for (int i = 0; i < FunctionIdentifier.sound.Length; i++)
		{
			if (FunctionIdentifier.sound[i] != null) FunctionIdentifier.sound[i].close();
		}

		#region EM_私家版_Emuera多言語化改造
		Lang.LoadLanguageFiles();
		Lang.SetLanguage();
		#endregion
		#region EM_私家版_Icon指定機能
		Icon icon = null;
		{
			var bmp = Utils.LoadImage(Utils.GetValidPath(Config.EmueraIcon));
			if (bmp != null)
			{
				icon = Utils.MakeIconFromBmpFile(bmp);
				bmp.Dispose();
			}
		}
		#endregion

		//二重起動の禁止かつ二重起動
		if ((!Config.AllowMultipleInstances) && AssemblyData.PrevInstance())
		{
			//Dialog.Show("既に起動しています", "多重起動を許可する場合、emuera.configを書き換えて下さい");
			Dialog.Show(Lang.UI.MainWindow.MsgBox.InstaceExists.Text, Lang.UI.MainWindow.MsgBox.MultiInstanceInfo.Text);
			return;
		}
		if (!Directory.Exists(CsvDir))
		{
			//Dialog.Show("フォルダなし", "csvフォルダが見つかりません");
			Dialog.Show(Lang.UI.MainWindow.MsgBox.FolderNotFound.Text, Lang.UI.MainWindow.MsgBox.NoCsvFolder.Text);
			return;
		}
		if (!Directory.Exists(ErbDir))
		{
			//Dialog.Show("フォルダなし", "erbフォルダが見つかりません");
			Dialog.Show(Lang.UI.MainWindow.MsgBox.FolderNotFound.Text, Lang.UI.MainWindow.MsgBox.NoErbFolder.Text);
			return;
		}
		#region EE_フォントファイル対応
		//フォントファイルを読み込む
		if (Directory.Exists(FontDir))
		{
			foreach (string fontFile in Directory.GetFiles(FontDir, "*.ttf", SearchOption.AllDirectories))
				GlobalStatic.Pfc.AddFontFile(fontFile);

			foreach (string fontFile in Directory.GetFiles(FontDir, "*.otf", SearchOption.AllDirectories))
				GlobalStatic.Pfc.AddFontFile(fontFile);
		}
		#endregion

		if (DebugMode)
		{
			ConfigData.Instance.LoadDebugConfig();
			if (!Directory.Exists(DebugDir))
			{
				try
				{
					Directory.CreateDirectory(DebugDir);
				}
				catch
				{
					Dialog.Show(Lang.UI.MainWindow.MsgBox.FolderNotFound.Text, Lang.UI.MainWindow.MsgBox.FailedCreateDebugFolder.Text);
					return;
				}
			}
		}

		if (AnalysisMode)
		{
			AnalysisFiles = [];
			#region EM_私家版_Emuera多言語化改造
			// for (int i = argsStart; i < args.Length; i++)
			foreach (var path in analysisRequestPaths)
			{
				//if (!File.Exists(args[i]) && !Directory.Exists(args[i]))
				if (!File.Exists(path) && !Directory.Exists(path))
				{
					MessageBox.Show(Lang.UI.MainWindow.MsgBox.ArgPathNotExists.Text);
					return;
				}
				//if ((File.GetAttributes(args[i]) & FileAttributes.Directory) == FileAttributes.Directory)
				if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
				{
					//List<KeyValuePair<string, string>> fnames = Config.Config.GetFiles(args[i] + "\\", "*.ERB");
					List<KeyValuePair<string, string>> fnames = Config.GetFiles(path + "\\", "*.ERB");
					for (int j = 0; j < fnames.Count; j++)
					{
						AnalysisFiles.Add(fnames[j].Value);
					}
				}
				else
				{
					//if (Path.GetExtension(args[i]).ToUpper() != ".ERB")
					if (!Path.GetExtension(path).Equals(".ERB", StringComparison.OrdinalIgnoreCase))
					{
						MessageBox.Show(Lang.UI.MainWindow.MsgBox.InvalidArg.Text);
						return;
					}
					//AnalysisFiles.Add(args[i]);
					AnalysisFiles.Add(path);
				}
			}
			#endregion
		}

		ApplicationConfiguration.Initialize();

		using var win = new Forms.MainWindow(args);
		{
			#region EM_私家版_Emuera多言語化改造
			win.TranslateUI();
			#endregion
			#region EM_私家版_Icon指定機能
			if (icon != null)
				win.SetupIcon(icon);
			#endregion

			Application.Run(win);
			/* VVII版マージ前の起動処理
			MainWindow win = null;
			StartTime = WinmmTimer.TickCount;
			using (win = new MainWindow())
			{
				#region EM_私家版_Emuera多言語化改造
				win.TranslateUI();
				#endregion
				#region EM_私家版_Icon指定機能
				if (icon != null)
					win.SetupIcon(icon);
				#endregion
				Application.Run(win);
				Content.AppContents.UnloadContents();
				if (!Reboot)
					break;
				RebootWinState = win.WindowState;
				if (win.WindowState == FormWindowState.Normal)
				{
					RebootClientY = win.ClientSize.Height;
					RebootLocation = win.Location;
				}
				else
				{
					RebootClientY = 0;
					RebootLocation = new Point();
				}
			}
			//条件次第ではParserMediatorが空でない状態で再起動になる場合がある
			ParserMediator.ClearWarningList();
			ParserMediator.Initialize(null);
			GlobalStatic.Reset();
			//GC.Collect();
			#region EE_メモリリークの解決
			ConfigData.Instance.ReLoadConfig();

			break;
			*/
		}
		/*
		if (rebootFlag)
			Application.Restart();
		#endregion
		*/
	}

	#region 統合_Dataフォルダ配置の自動判別
	/// <summary>
	/// .netEmuera 系のバリアントは ERB/CSV を exe 直下ではなく Data\ 以下に置く。
	/// 直下に ERB が無く Data\ERB がある場合だけ Data\ を実行ディレクトリとみなす。
	/// --ExeDir が明示された場合はこの判別を通さない。
	/// </summary>
	private static string ResolveGameDir(string workingDir)
	{
		if (Directory.Exists(Path.Combine(workingDir, "erb")))
			return workingDir;

		string dataDir = Path.Combine(workingDir, "Data");
		if (Directory.Exists(Path.Combine(dataDir, "erb")))
			return dataDir;

		return workingDir;
	}
	#endregion

	[MemberNotNull(nameof(ExeDir))]
	[MemberNotNull(nameof(CsvDir))]
	[MemberNotNull(nameof(ErbDir))]
	[MemberNotNull(nameof(DebugDir))]
	[MemberNotNull(nameof(DatDir))]
	[MemberNotNull(nameof(ContentDir))]

	private static void SetDirPaths(string exeDir)
	{

		ExeDir = Path.GetFullPath(new DirectoryInfo(exeDir).FullName + Path.DirectorySeparatorChar);

		CsvDir = Path.Combine(ExeDir, "csv") + Path.DirectorySeparatorChar;
		ErbDir = Path.Combine(ExeDir, "erb") + Path.DirectorySeparatorChar;
		DebugDir = Path.Combine(ExeDir, "debug") + Path.DirectorySeparatorChar;
		DatDir = Path.Combine(ExeDir, "dat") + Path.DirectorySeparatorChar;
		ContentDir = Path.Combine(ExeDir, "resources") + Path.DirectorySeparatorChar;
		#region EE_PLAYSOUND系
		SoundDir = Path.Combine(ExeDir, "sound") + Path.DirectorySeparatorChar;
		#endregion
		#region EE_フォントファイル対応
		FontDir = Path.Combine(ExeDir, "font") + Path.DirectorySeparatorChar;
		#endregion

		/*
		CsvDir = WorkingDir + "csv\\";
		ErbDir = WorkingDir + "erb\\";
		DebugDir = WorkingDir + "debug\\";
		DatDir = WorkingDir + "dat\\";
		ContentDir = WorkingDir + "resources\\";
		#region EE_フォントファイル対応
		FontDir = WorkingDir + "font\\";
		#endregion
		*/
	}

	#region eee_カレントディレクトリー
	/// <summary>
	/// 実行ファイルのディレクトリ。最後にPath.DirectorySeparatorCharを付けたstring
	/// </summary>
	public static string ExeDir { get; private set; }
	#endregion
	public static string CsvDir { get; private set; }
	public static string ErbDir { get; private set; }
	public static string DebugDir { get; private set; }
	public static string DatDir { get; private set; }
	public static string ContentDir { get; private set; }
	public static string ExeName { get; private set; }
	#region EE_PLAYSOUND系
	public static string SoundDir { get; private set; }
	#endregion
	#region EE_フォントファイル対応
	public static string FontDir { get; private set; }
	#endregion


	public static bool rebootFlag;
	//public static int RebootClientX = 0;
	//public static int RebootClientY = 0;
	public static FormWindowState RebootWinState = FormWindowState.Normal;
	//public static Point RebootLocation;

	public static bool AnalysisMode;
	public static List<string> AnalysisFiles;

	//public static bool debugMode = false;
	//public static bool DebugMode { get { return debugMode; } }
	public static bool DebugMode { get; private set; }

	static Program()
	{
		var baseDirectory = AppContext.BaseDirectory;
		if (Directory.Exists(Path.Combine(baseDirectory, "Data", "erb")))
		{
			baseDirectory = Path.Combine(baseDirectory, "Data");
		}
		SetDirPaths(baseDirectory);
	}
}
