using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.PluginSystem;
using MinorShift.Emuera.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using trmb = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.MessageBox;

namespace MinorShift.Emuera.Runtime.Config;

internal static class Config
{

	#region config
	public static Encoding Encode = EncodingHandler.UTF8BOMEncoding;
	public static Encoding SaveEncode = EncodingHandler.UTF8BOMEncoding;
	private static Dictionary<ConfigCode, string> nameDic;
	public static string GetConfigName(ConfigCode code)
	{
		return nameDic[code];
	}

	public static void SetConfig(ConfigData instance)
	{
		nameDic = instance.GetConfigNameDic();
		IgnoreCase = instance.GetConfigValue<bool>(ConfigCode.IgnoreCase);
		if (IgnoreCase)
		{
			StringComparison = StringComparison.OrdinalIgnoreCase;
			StrComper = StringComparer.OrdinalIgnoreCase;
		}
		else
		{
			StringComparison = StringComparison.Ordinal;
			StrComper = StringComparer.Ordinal;
		}
		UseRenameFile = instance.GetConfigValue<bool>(ConfigCode.UseRenameFile);
		UseReplaceFile = instance.GetConfigValue<bool>(ConfigCode.UseReplaceFile);
		UseMouse = instance.GetConfigValue<bool>(ConfigCode.UseMouse);
		UseMenu = instance.GetConfigValue<bool>(ConfigCode.UseMenu);
		UseDebugCommand = instance.GetConfigValue<bool>(ConfigCode.UseDebugCommand);
		AllowMultipleInstances = instance.GetConfigValue<bool>(ConfigCode.AllowMultipleInstances);
		AutoSave = instance.GetConfigValue<bool>(ConfigCode.AutoSave);
		UseKeyMacro = instance.GetConfigValue<bool>(ConfigCode.UseKeyMacro);
		SizableWindow = instance.GetConfigValue<bool>(ConfigCode.SizableWindow);
		//UseImageBuffer = instance.GetConfigValue<bool>(ConfigCode.UseImageBuffer);
		TextDrawingMode = instance.GetConfigValue<TextDrawingMode>(ConfigCode.TextDrawingMode);
		WindowX = instance.GetConfigValue<int>(ConfigCode.WindowX);
		WindowY = instance.GetConfigValue<int>(ConfigCode.WindowY);
		WindowPosX = instance.GetConfigValue<int>(ConfigCode.WindowPosX);
		WindowPosY = instance.GetConfigValue<int>(ConfigCode.WindowPosY);
		SetWindowPos = instance.GetConfigValue<bool>(ConfigCode.SetWindowPos);
		MaxLog = instance.GetConfigValue<int>(ConfigCode.MaxLog);
		PrintCPerLine = instance.GetConfigValue<int>(ConfigCode.PrintCPerLine);
		PrintCLength = instance.GetConfigValue<int>(ConfigCode.PrintCLength);
		ForeColor = instance.GetConfigValue<Color>(ConfigCode.ForeColor);
		BackColor = instance.GetConfigValue<Color>(ConfigCode.BackColor);
		FocusColor = instance.GetConfigValue<Color>(ConfigCode.FocusColor);
		LogColor = instance.GetConfigValue<Color>(ConfigCode.LogColor);
		FontSize = instance.GetConfigValue<int>(ConfigCode.FontSize);
		FontName = instance.GetConfigValue<string>(ConfigCode.FontName);
		LineHeight = instance.GetConfigValue<int>(ConfigCode.LineHeight);
		FPS = instance.GetConfigValue<int>(ConfigCode.FPS);
		//SkipFrame = instance.GetConfigValue<int>(ConfigCode.SkipFrame);
		ScrollHeight = instance.GetConfigValue<int>(ConfigCode.ScrollHeight);
		InfiniteLoopAlertTime = instance.GetConfigValue<int>(ConfigCode.InfiniteLoopAlertTime);
		SaveDataNos = instance.GetConfigValue<int>(ConfigCode.SaveDataNos);
		WarnBackCompatibility = instance.GetConfigValue<bool>(ConfigCode.WarnBackCompatibility);
		WindowMaximixed = instance.GetConfigValue<bool>(ConfigCode.WindowMaximixed);
		WarnNormalFunctionOverloading = instance.GetConfigValue<bool>(ConfigCode.WarnNormalFunctionOverloading);
		SearchSubdirectory = instance.GetConfigValue<bool>(ConfigCode.SearchSubdirectory);
		SortWithFilename = instance.GetConfigValue<bool>(ConfigCode.SortWithFilename);

		AllowFunctionOverloading = instance.GetConfigValue<bool>(ConfigCode.AllowFunctionOverloading);
		if (!AllowFunctionOverloading)
			WarnFunctionOverloading = true;
		else
			WarnFunctionOverloading = instance.GetConfigValue<bool>(ConfigCode.WarnFunctionOverloading);

		DisplayWarningLevel = instance.GetConfigValue<int>(ConfigCode.DisplayWarningLevel);
		DisplayReport = instance.GetConfigValue<bool>(ConfigCode.DisplayReport);
		ReduceArgumentOnLoad = instance.GetConfigValue<ReduceArgumentOnLoadFlag>(ConfigCode.ReduceArgumentOnLoad);
		IgnoreUncalledFunction = instance.GetConfigValue<bool>(ConfigCode.IgnoreUncalledFunction);
		FunctionNotFoundWarning = instance.GetConfigValue<DisplayWarningFlag>(ConfigCode.FunctionNotFoundWarning);
		FunctionNotCalledWarning = instance.GetConfigValue<DisplayWarningFlag>(ConfigCode.FunctionNotCalledWarning);


		ChangeMasterNameIfDebug = instance.GetConfigValue<bool>(ConfigCode.ChangeMasterNameIfDebug);
		LastKey = instance.GetConfigValue<long>(ConfigCode.LastKey);
		ButtonWrap = instance.GetConfigValue<bool>(ConfigCode.ButtonWrap);

		TextEditor = instance.GetConfigValue<string>(ConfigCode.TextEditor);
		EditorType = instance.GetConfigValue<TextEditorType>(ConfigCode.EditorType);
		EditorArg = instance.GetConfigValue<string>(ConfigCode.EditorArgument);

		CompatiErrorLine = instance.GetConfigValue<bool>(ConfigCode.CompatiErrorLine);
		CompatiCALLNAME = instance.GetConfigValue<bool>(ConfigCode.CompatiCALLNAME);
		UseSaveFolder = instance.GetConfigValue<bool>(ConfigCode.UseSaveFolder);
		CompatiRAND = instance.GetConfigValue<bool>(ConfigCode.CompatiRAND);
		//CompatiDRAWLINE = instance.GetConfigValue<bool>(ConfigCode.CompatiDRAWLINE);
		CompatiLinefeedAs1739 = instance.GetConfigValue<bool>(ConfigCode.CompatiLinefeedAs1739);
		SystemAllowFullSpace = instance.GetConfigValue<bool>(ConfigCode.SystemAllowFullSpace);
		SystemSaveInBinary = instance.GetConfigValue<bool>(ConfigCode.SystemSaveInBinary);
		SystemIgnoreTripleSymbol = instance.GetConfigValue<bool>(ConfigCode.SystemIgnoreTripleSymbol);
		SystemIgnoreStringSet = instance.GetConfigValue<bool>(ConfigCode.SystemIgnoreStringSet);

		CompatiFuncArgAutoConvert = instance.GetConfigValue<bool>(ConfigCode.CompatiFuncArgAutoConvert);
		CompatiFuncArgOptional = instance.GetConfigValue<bool>(ConfigCode.CompatiFuncArgOptional);
		CompatiCallEvent = instance.GetConfigValue<bool>(ConfigCode.CompatiCallEvent);
		CompatiSPChara = instance.GetConfigValue<bool>(ConfigCode.CompatiSPChara);

		AllowLongInputByMouse = instance.GetConfigValue<bool>(ConfigCode.AllowLongInputByMouse);

		TimesNotRigorousCalculation = instance.GetConfigValue<bool>(ConfigCode.TimesNotRigorousCalculation);
		//一文字変数の禁止オプションを考えた名残
		//ForbidOneCodeVariable = instance.GetConfigValue<bool>(ConfigCode.ForbidOneCodeVariable);
		SystemNoTarget = instance.GetConfigValue<bool>(ConfigCode.SystemNoTarget);

		#region EE版_UPDATECHECK
		ForbidUpdateCheck = instance.GetConfigValue<bool>(ConfigCode.ForbidUpdateCheck);
		#endregion
		#region EE版_ERDConfig
		UseERD = instance.GetConfigValue<bool>(ConfigCode.UseERD);
		#endregion
		#region EE_ERDNAME
		VarsizeDimConfig = instance.GetConfigValue<bool>(ConfigCode.VarsizeDimConfig);
		#endregion
		#region EE_重複定義の確認
		CheckDuplicateIdentifier = instance.GetConfigValue<bool>(ConfigCode.CheckDuplicateIdentifier);
		#endregion
		#region EE_行連結の改行コード置換
		ReplaceContinuationBR = instance.GetConfigValue<string>(ConfigCode.ReplaceContinuationBR);
		#endregion
		#region EE_CALLSHARP注意
		PluginAvailableWarn = instance.GetConfigValue<bool>(ConfigCode.PluginAvailableWarn);
		#endregion

		#region EM_私家版_LoadText＆SaveText機能拡張
		ValidExtension = instance.GetConfigValue<List<string>>(ConfigCode.ValidExtension);
		#endregion
		#region EM_私家版_セーブ圧縮
		ZipSaveData = instance.GetConfigValue<bool>(ConfigCode.ZipSaveData);
		#endregion
		#region EM_私家版_Emuera多言語化改造
		EnglishConfigOutput = instance.GetConfigValue<bool>(ConfigCode.EnglishConfigOutput);
		EmueraLang = instance.GetConfigValue<string>(ConfigCode.EmueraLang);
		#endregion
		#region EM_私家版_Icon指定機能
		EmueraIcon = instance.GetConfigValue<string>(ConfigCode.EmueraIcon);
		#endregion
		#region EE_AnchorのCB機能移植
		CBUseClipboard = instance.GetConfigValue<bool>(ConfigCode.CBUseClipboard);
		CBIgnoreTags = instance.GetConfigValue<bool>(ConfigCode.CBIgnoreTags);
		CBReplaceTags = instance.GetConfigValue<string>(ConfigCode.CBReplaceTags);
		CBNewLinesOnly = instance.GetConfigValue<bool>(ConfigCode.CBNewLinesOnly);
		CBClearBuffer = instance.GetConfigValue<bool>(ConfigCode.CBClearBuffer);
		CBTriggerLeftClick = instance.GetConfigValue<bool>(ConfigCode.CBTriggerLeftClick);
		CBTriggerMiddleClick = instance.GetConfigValue<bool>(ConfigCode.CBTriggerMiddleClick);
		CBTriggerDoubleLeftClick = instance.GetConfigValue<bool>(ConfigCode.CBTriggerDoubleLeftClick);
		CBTriggerAnyKeyWait = instance.GetConfigValue<bool>(ConfigCode.CBTriggerAnyKeyWait);
		CBTriggerInputWait = instance.GetConfigValue<bool>(ConfigCode.CBTriggerInputWait);
		CBMaxCB = instance.GetConfigValue<int>(ConfigCode.CBMaxCB);
		CBBufferSize = instance.GetConfigValue<int>(ConfigCode.CBBufferSize);
		CBScrollCount = instance.GetConfigValue<int>(ConfigCode.CBScrollCount);
		CBMinTimer = instance.GetConfigValue<int>(ConfigCode.CBMinTimer);
		#endregion
		#region EmuEra-Rikaichan related settings
		RikaiEnabled = instance.GetConfigValue<bool>(ConfigCode.RikaiEnabled);
		RikaiFilename = instance.GetConfigValue<string>(ConfigCode.RikaiFilename);
		RikaiColorBack = instance.GetConfigValue<Color>(ConfigCode.RikaiColorBack);
		RikaiColorText = instance.GetConfigValue<Color>(ConfigCode.RikaiColorText);
		RikaiUseSeparateBoxes = instance.GetConfigValue<bool>(ConfigCode.RikaiUseSeparateBoxes);
		#endregion

		Ctrl_Z_Enabled = instance.GetConfigValue<bool>(ConfigCode.Ctrl_Z_Enabled);


		UseLanguage lang = instance.GetConfigValue<UseLanguage>(ConfigCode.useLanguage);
		switch (lang)
		{
			case UseLanguage.JAPANESE:
				Language = 0x0411; LangManager.setEncode(932); break;
			case UseLanguage.KOREAN:
				Language = 0x0412; LangManager.setEncode(949); break;
			case UseLanguage.CHINESE_HANS:
				Language = 0x0804; LangManager.setEncode(936); break;
			case UseLanguage.CHINESE_HANT:
				Language = 0x0404; LangManager.setEncode(950); break;
		}

		if (FontSize < 8)
		{
			Dialog.Show(trmb.ConfigError.Text, trmb.TooSmallFontSize.Text);
			FontSize = 8;
		}
		if (LineHeight < FontSize)
		{
			Dialog.Show(trmb.ConfigError.Text, trmb.LineHeightLessThanFontSize.Text);
			LineHeight = FontSize;
		}
		if (SaveDataNos < 20)
		{
			Dialog.Show(trmb.ConfigError.Text, trmb.TooSmallDisplaySaveData.Text);
			SaveDataNos = 20;
		}
		if (SaveDataNos > 80)
		{
			Dialog.Show(trmb.ConfigError.Text, trmb.TooLargeDisplaySaveData.Text);
			SaveDataNos = 80;
		}
		if (MaxLog < 500)
		{
			Dialog.Show(trmb.ConfigError.Text, trmb.TooSmallLogSize.Text);
			MaxLog = 500;
		}
		if (TextDrawingMode == TextDrawingMode.WINAPI)
		{
			MessageBox.Show(trmb.DoNotSupportWINAPI.Text);
			TextDrawingMode = TextDrawingMode.TEXTRENDERER;
		}

		DrawingParam_ShapePositionShift = 0;
		if (TextDrawingMode != TextDrawingMode.WINAPI)
			DrawingParam_ShapePositionShift = Math.Max(2, FontSize / 6);
		DrawableWidth = WindowX - DrawingParam_ShapePositionShift;
		#region eee_カレントディレクトリー
		// ForceSavDir = Program.ExeDir + "sav\\";
		ForceSavDir = Program.ExeDir + "sav" + Path.DirectorySeparatorChar;
		if (UseSaveFolder)
			// SavDir = Program.ExeDir + "sav\\";
			SavDir = Program.ExeDir + "sav" + Path.DirectorySeparatorChar;
		else
			// SavDir = Program.ExeDir;
			SavDir = Program.ExeDir;
		#endregion
		if (UseSaveFolder && !Directory.Exists(SavDir))
			createSavDirAndMoveFiles();
	}
	#region EM_私家版_Emuera多言語化改造
	public static void UpdateLangSetting(ConfigData instance)
	{
		EnglishConfigOutput = instance.GetConfigValue<bool>(ConfigCode.EnglishConfigOutput);
		EmueraLang = instance.GetConfigValue<string>(ConfigCode.EmueraLang);
	}
	public static void SetLanguageSetting(ConfigData instance, string lang)
	{
		instance.GetConfigItem(ConfigCode.EmueraLang).SetValue(lang);
		UpdateLangSetting(instance);
		instance.SaveConfig();
	}
	#endregion

	public static Font DefaultFont { get { return FontFactory.GetFont("", FontStyle.Regular); } }


	/// <summary>
	/// ディレクトリ作成失敗のExceptionは呼び出し元で処理すること
	/// </summary>
	public static void ForceCreateSavDir()
	{
		if (!Directory.Exists(ForceSavDir))
		{
			Directory.CreateDirectory(ForceSavDir);
		}
	}

	/// <summary>
	/// ディレクトリ作成失敗のExceptionは呼び出し元で処理すること
	/// </summary>
	public static void CreateSavDir()
	{
		if (UseSaveFolder && !Directory.Exists(SavDir))
		{
			Directory.CreateDirectory(SavDir);
		}
	}

	private static void createSavDirAndMoveFiles()
	{
		try
		{
			Directory.CreateDirectory(SavDir);
		}
		catch
		{
			Dialog.Show(trmb.FolderCreationFailure.Text, trmb.FailedCreateSavFolder.Text);
			return;
		}
		#region eee_カレントディレクトリー
		// bool existGlobal = File.Exists(Program.ExeDir + "global.sav");
		// string[] savFiles = Directory.GetFiles(Program.ExeDir, "save*.sav", SearchOption.TopDirectoryOnly);
		bool existGlobal = File.Exists(Program.ExeDir + "global.sav");
		string[] savFiles = Directory.GetFiles(Program.ExeDir, "save*.sav", SearchOption.TopDirectoryOnly);
		#endregion
		if (!existGlobal && savFiles.Length == 0)
			return;
		var result = Dialog.ShowPrompt(trmb.SavFolderCreated.Text, trmb.DataTransfer.Text);
		if (result == false)
			return;
		//ダイアログが開いている間にフォルダを消してしまうような邪悪なユーザーがいるかもしれない
		if (!Directory.Exists(SavDir))
		{
			Dialog.Show(trmb.DataTransferFailure.Text, trmb.MissingSavFolder.Text);
			return;
		}
		//ダイアログが開いている間にファイルを変更するような邪悪なユーザーがいるかもしれない
		try
		{
			#region eee_カレントディレクトリー
			//if (File.Exists(Program.ExeDir + "global.sav"))
			//	File.Move(Program.ExeDir + "global.sav", SavDir + "global.sav");
			//savFiles = Directory.GetFiles(Program.ExeDir, "save*.sav", SearchOption.TopDirectoryOnly);
			if (File.Exists(Program.ExeDir + "global.sav"))
				File.Move(Program.ExeDir + "global.sav", SavDir + "global.sav");
			savFiles = Directory.GetFiles(Program.ExeDir, "save*.sav", SearchOption.TopDirectoryOnly);
			#endregion
			foreach (string oldpath in savFiles)
				File.Move(oldpath, SavDir + Path.GetFileName(oldpath));
		}
		catch
		{
			Dialog.Show(trmb.DataTransferFailure.Text, trmb.FailedMoveSavFiles.Text);
		}
	}
	//先にSetConfigを呼ぶこと
	//戻り値はセーブが必要かどうか
	public static bool CheckUpdate()
	{
		if (ReduceArgumentOnLoad != ReduceArgumentOnLoadFlag.ONCE)
		{
			if (ReduceArgumentOnLoad == ReduceArgumentOnLoadFlag.YES)
				NeedReduceArgumentOnLoad = true;
			else if (ReduceArgumentOnLoad == ReduceArgumentOnLoadFlag.NO)
				NeedReduceArgumentOnLoad = false;
			return false;
		}

		long key = getUpdateKey();
		bool updated = LastKey != key;
		LastKey = key;
		return updated;
	}

	private static long getUpdateKey()
	{
		//andEmuera: Android では検索パターンの大文字小文字が区別されるため非依存で列挙する
		var option = SearchSubdirectory ? CaseInsensitiveAllDirectories : CaseInsensitiveTopDirectory;
		string[] erbFiles = Directory.GetFiles(Program.ErbDir, "*.ERB", option);
		string[] csvFiles = Directory.GetFiles(Program.CsvDir, "*.CSV", option);
		long[] writetimes = new long[erbFiles.Length + csvFiles.Length];
		for (int i = 0; i < erbFiles.Length; i++)
			if (Path.GetExtension(erbFiles[i]).Equals(".ERB", StringComparison.OrdinalIgnoreCase))
				writetimes[i] = File.GetLastWriteTime(erbFiles[i]).ToBinary();
		for (int i = 0; i < csvFiles.Length; i++)
			if (Path.GetExtension(csvFiles[i]).Equals(".CSV", StringComparison.OrdinalIgnoreCase))
				writetimes[i + erbFiles.Length] = File.GetLastWriteTime(csvFiles[i]).ToBinary();
		long key = 0;
		for (int i = 0; i < writetimes.Length; i++)
		{
			unchecked
			{
				key ^= writetimes[i] * 1103515245 + 12345;
			}
		}
		return key;
	}


	//andEmuera: 大文字小文字を区別しないファイル列挙オプション (Android 対応)
	//MatchType / AttributesToSkip は EnumerationOptions の既定 (Simple / Hidden|System) ではなく
	//SearchOption 版と同じ値を明示する。ENUMFILES は ERB が任意のパターン ("*.*" など) を渡すため、
	//既定のままだと Windows 版と結果が食い違う。
	internal static readonly EnumerationOptions CaseInsensitiveTopDirectory = new()
	{
		MatchCasing = MatchCasing.CaseInsensitive,
		MatchType = MatchType.Win32,
		AttributesToSkip = 0,
		RecurseSubdirectories = false,
		IgnoreInaccessible = true,
	};

	internal static readonly EnumerationOptions CaseInsensitiveAllDirectories = new()
	{
		MatchCasing = MatchCasing.CaseInsensitive,
		MatchType = MatchType.Win32,
		AttributesToSkip = 0,
		RecurseSubdirectories = true,
		IgnoreInaccessible = true,
	};

	public static List<KeyValuePair<string, string>> GetFiles(string rootdir, string pattern)
	{
		return getFiles(rootdir, rootdir, pattern, !SearchSubdirectory, SortWithFilename);
	}

	public static List<KeyValuePair<string, string>> GetFiles(string dir, string rootdir, string pattern)
	{
		return getFiles(dir, rootdir, pattern, !SearchSubdirectory, SortWithFilename);
	}

	//KeyValuePair<相対パス, 完全パス>のリストを返す。
	private static List<KeyValuePair<string, string>> getFiles(string dir, string rootdir, string pattern, bool toponly, bool sort)
	{
		List<KeyValuePair<string, string>> retList = [];

		string RelativePath;//相対ディレクトリ名
		if (string.Equals(dir, rootdir, StringComparison.OrdinalIgnoreCase))//現在のパスが検索ルートパスに等しい
			RelativePath = "";
		else
		{
			if (!dir.StartsWith(rootdir, StringComparison.OrdinalIgnoreCase))
				RelativePath = dir;
			else
				RelativePath = dir[rootdir.Length..];//前方が検索ルートパスと一致するならその部分を切り取る
			if (!RelativePath.EndsWith('\\') && !RelativePath.EndsWith('/'))
				RelativePath += "\\";//末尾が\又は/で終わるように。後でFile名を直接加算できるようにしておく
		}
		//filepathsは完全パスである
		//andEmuera: Windows は検索パターンの大文字小文字を区別しないが、Android(ext4)は区別する。
		//"*.CSV" で ".csv" のファイルを取りこぼさないよう、明示的に大文字小文字非依存で列挙する。
		string[] filepaths = Directory.GetFiles(dir, pattern, CaseInsensitiveTopDirectory);
		if (sort)
			Array.Sort(filepaths);
		for (int i = 0; i < filepaths.Length; i++)
			if (Path.GetExtension(filepaths[i]).Length <= 4)//".erb"や".csv"であること。放置すると".erb*"等を拾う。
				retList.Add(new KeyValuePair<string, string>(Path.Combine(RelativePath, Path.GetFileName(filepaths[i])), filepaths[i]));

		if (!toponly)
		{//サブフォルダ内の検索
			string[] dirList = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
			if (dirList.Length > 0)
			{
				if (sort)
					Array.Sort(dirList);
				for (int i = 0; i < dirList.Length; i++)
					retList.AddRange(getFiles(dirList[i], rootdir, pattern, toponly, sort));
			}
		}

		return retList;
	}

	/// <summary>
	/// 関数名・属性名的な名前のIgnoreCaseフラグ
	/// 関数・属性・BEGINのキーワード 
	/// どうせeramaker用の互換処理なのでEmuera専用構文については適当に。
	/// </summary>
	public static bool IgnoreCase { get; private set; }

	/// <summary>
	/// 関数名・属性名的な名前の比較フラグ
	/// </summary>
	public static StringComparison StringComparison { get; private set; }

	/// <summary>
	/// ファイル名的な名前の比較フラグ
	/// </summary>
	public const StringComparison SCIgnoreCase = StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// 式中での文字列比較フラグ
	/// </summary>
	public const StringComparison SCExpression = StringComparison.Ordinal;

	/// <summary>
	/// GDI+利用時に発生する文字列と図形・画像間の位置ずれ補正
	/// </summary>
	public static int DrawingParam_ShapePositionShift { get; private set; }


	public static bool UseRenameFile { get; private set; }
	public static bool UseReplaceFile { get; private set; }
	public static bool UseMouse { get; private set; }
	public static bool UseMenu { get; private set; }
	public static bool UseDebugCommand { get; private set; }
	public static bool AllowMultipleInstances { get; private set; }
	public static bool AutoSave { get; private set; }
	public static bool UseKeyMacro { get; private set; }
	public static bool SizableWindow { get; private set; }
	//public static bool UseImageBuffer { get; private set; }
	public static TextDrawingMode TextDrawingMode { get; private set; }
	public static int WindowX { get; private set; }
	/// <summary>
	/// 実際に描画可能な横幅
	/// </summary>
	public static int DrawableWidth { get; private set; }
	public static int WindowY { get; private set; }
	public static int WindowPosX { get; private set; }
	public static int WindowPosY { get; private set; }
	public static bool SetWindowPos { get; private set; }
	public static int MaxLog { get; private set; }
	public static int PrintCPerLine { get; private set; }
	public static int PrintCLength { get; private set; }
	public static Color ForeColor { get; private set; }
	public static Color BackColor { get; private set; }
	public static Color FocusColor { get; private set; }
	public static Color LogColor { get; private set; }
	public static int FontSize { get; private set; }
	public static string FontName { get; private set; }
	public static int LineHeight { get; private set; }
	public static int FPS { get; private set; }
	//public static int SkipFrame { get; private set; }
	public static int ScrollHeight { get; private set; }
	public static int InfiniteLoopAlertTime { get; private set; }
	public static int SaveDataNos { get; private set; }
	public static bool WarnBackCompatibility { get; private set; }
	public static bool WindowMaximixed { get; private set; }
	public static bool WarnNormalFunctionOverloading { get; private set; }
	public static bool SearchSubdirectory { get; private set; }
	public static bool SortWithFilename { get; private set; }

	public static bool AllowFunctionOverloading { get; private set; }
	public static bool WarnFunctionOverloading { get; private set; }

	public static int DisplayWarningLevel { get; private set; }
	public static bool DisplayReport { get; private set; }
	public static ReduceArgumentOnLoadFlag ReduceArgumentOnLoad { get; private set; }
	public static bool IgnoreUncalledFunction { get; private set; }
	public static DisplayWarningFlag FunctionNotFoundWarning { get; private set; }
	public static DisplayWarningFlag FunctionNotCalledWarning { get; private set; }

	public static bool ChangeMasterNameIfDebug { get; private set; }
	public static long LastKey { get; private set; }
	public static bool ButtonWrap { get; private set; }

	public static string TextEditor { get; private set; }
	public static TextEditorType EditorType { get; private set; }
	public static string EditorArg { get; private set; }

	public static bool CompatiErrorLine { get; private set; }
	public static bool CompatiCALLNAME { get; private set; }
	public static bool UseSaveFolder { get; private set; }
	public static bool CompatiRAND { get; private set; }
	//public static bool CompatiDRAWLINE { get; private set; }
	public static bool CompatiLinefeedAs1739 { get; private set; }
	public static bool SystemAllowFullSpace { get; private set; }
	public static bool SystemSaveInBinary { get; private set; }
	public static bool CompatiFuncArgAutoConvert { get; private set; }
	public static bool CompatiFuncArgOptional { get; private set; }
	public static bool CompatiCallEvent { get; private set; }
	public static bool CompatiSPChara { get; private set; }
	public static bool SystemIgnoreTripleSymbol { get; private set; }
	public static bool SystemNoTarget { get; private set; }
	public static bool SystemIgnoreStringSet { get; private set; }

	public static int Language { get; private set; }

	public static string SavDir { get; private set; }
	public static string ForceSavDir { get; private set; }

	public static bool NeedReduceArgumentOnLoad { get; private set; }

	public static bool AllowLongInputByMouse { get; private set; }

	public static bool TimesNotRigorousCalculation { get; private set; }
	//一文字変数の禁止オプションを考えた名残
	//public static bool ForbidOneCodeVariable { get; private set; }
	#endregion

	#region debug
	public static void SetDebugConfig(ConfigData instance)
	{
		DebugShowWindow = instance.GetConfigValue<bool>(ConfigCode.DebugShowWindow);
		DebugWindowTopMost = instance.GetConfigValue<bool>(ConfigCode.DebugWindowTopMost);
		DebugWindowWidth = instance.GetConfigValue<int>(ConfigCode.DebugWindowWidth);
		DebugWindowHeight = instance.GetConfigValue<int>(ConfigCode.DebugWindowHeight);
		DebugSetWindowPos = instance.GetConfigValue<bool>(ConfigCode.DebugSetWindowPos);
		DebugWindowPosX = instance.GetConfigValue<int>(ConfigCode.DebugWindowPosX);
		DebugWindowPosY = instance.GetConfigValue<int>(ConfigCode.DebugWindowPosY);
	}
	public static bool DebugShowWindow { get; private set; }
	public static bool DebugWindowTopMost { get; private set; }
	public static int DebugWindowWidth { get; private set; }
	public static int DebugWindowHeight { get; private set; }
	public static bool DebugSetWindowPos { get; private set; }
	public static int DebugWindowPosX { get; private set; }
	public static int DebugWindowPosY { get; private set; }


	#endregion

	#region replace
	public static void SetReplace(ConfigData instance)
	{
		MoneyLabel = instance.GetConfigValue<string>(ConfigCode.MoneyLabel);
		MoneyFirst = instance.GetConfigValue<bool>(ConfigCode.MoneyFirst);
		LoadLabel = instance.GetConfigValue<string>(ConfigCode.LoadLabel);
		MaxShopItem = instance.GetConfigValue<int>(ConfigCode.MaxShopItem);
		DrawLineString = instance.GetConfigValue<string>(ConfigCode.DrawLineString);
		if (string.IsNullOrEmpty(DrawLineString))
			DrawLineString = "-";
		BarChar1 = instance.GetConfigValue<char>(ConfigCode.BarChar1);
		BarChar2 = instance.GetConfigValue<char>(ConfigCode.BarChar2);
		TitleMenuString0 = instance.GetConfigValue<string>(ConfigCode.TitleMenuString0);
		TitleMenuString1 = instance.GetConfigValue<string>(ConfigCode.TitleMenuString1);
		ComAbleDefault = instance.GetConfigValue<int>(ConfigCode.ComAbleDefault);
		StainDefault = instance.GetConfigValue<List<long>>(ConfigCode.StainDefault);
		TimeupLabel = instance.GetConfigValue<string>(ConfigCode.TimeupLabel);
		ExpLvDef = instance.GetConfigValue<List<long>>(ConfigCode.ExpLvDef);
		PalamLvDef = instance.GetConfigValue<List<long>>(ConfigCode.PalamLvDef);
		PbandDef = instance.GetConfigValue<long>(ConfigCode.pbandDef);
		RelationDef = instance.GetConfigValue<long>(ConfigCode.RelationDef);
	}

	public static string MoneyLabel { get; private set; }
	public static bool MoneyFirst { get; private set; }
	public static string LoadLabel { get; private set; }
	public static int MaxShopItem { get; private set; }
	public static string DrawLineString { get; private set; }
	public static char BarChar1 { get; private set; }
	public static char BarChar2 { get; private set; }
	public static string TitleMenuString0 { get; private set; }
	public static string TitleMenuString1 { get; private set; }
	public static int ComAbleDefault { get; private set; }
	public static List<long> StainDefault { get; private set; }
	public static string TimeupLabel { get; private set; }
	public static List<long> ExpLvDef { get; private set; }
	public static List<long> PalamLvDef { get; private set; }
	public static long PbandDef { get; private set; }
	public static long RelationDef { get; private set; }
	#endregion

	public static StringComparer StrComper = StringComparer.OrdinalIgnoreCase;

	#region EE版_UPDATECHECK
	public static bool ForbidUpdateCheck { get; private set; }
	#endregion
	#region EE版_ERDConfig
	public static bool UseERD { get; private set; }
	#endregion
	#region EE_ERDNAME
	public static bool VarsizeDimConfig { get; private set; }
	#endregion
	#region EE_重複定義の確認
	public static bool CheckDuplicateIdentifier { get; private set; }
	#endregion
	#region EE_行連結の改行コード置換
	public static string ReplaceContinuationBR { get; private set; }
	#endregion
	#region EE_CALLSHARP注意
	public static bool PluginAvailableWarn { get; private set; }
	#endregion
	#region EM_私家版_LoadText＆SaveText機能拡張
	public static List<string> ValidExtension { get; private set; }
	#endregion
	#region EM_私家版_セーブ圧縮
	public static bool ZipSaveData { get; private set; }
	#endregion
	#region EM_私家版_Emuera多言語化改造
	public static bool EnglishConfigOutput { get; private set; }
	public static string EmueraLang { get; private set; }
	#endregion
	#region EM_私家版_Icon指定機能
	public static string EmueraIcon { get; private set; }
	#endregion
	#region EE_AnchorのCB機能移植
	public static bool CBUseClipboard { get; private set; }
	public static bool CBIgnoreTags { get; private set; }
	public static string CBReplaceTags { get; private set; }
	public static bool CBNewLinesOnly { get; private set; }
	public static bool CBClearBuffer { get; private set; }
	public static bool CBTriggerLeftClick { get; private set; }
	public static bool CBTriggerMiddleClick { get; private set; }
	public static bool CBTriggerDoubleLeftClick { get; private set; }
	public static bool CBTriggerAnyKeyWait { get; private set; }
	public static bool CBTriggerInputWait { get; private set; }
	public static int CBMaxCB { get; private set; }
	public static int CBBufferSize { get; private set; }
	public static int CBScrollCount { get; private set; }
	public static int CBMinTimer { get; private set; }
	#endregion
	#region EmuEra-Rikaichan related settings
	public static bool RikaiEnabled { get; private set; }
	public static string RikaiFilename { get; private set; }
	public static Color RikaiColorBack { get; private set; }
	public static Color RikaiColorText { get; private set; }
	public static bool RikaiUseSeparateBoxes { get; private set; }
	#endregion

	public static bool Ctrl_Z_Enabled { get; private set; }
}
