using MinorShift.Emuera.Runtime.Config.JSON;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Sub;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Config;

/// <summary>
/// プログラム全体で使用される値でWindow作成前に設定して以後変更されないもの
/// (という予定だったが今は違う)
/// 1756 Config → ConfigDataへ改名
/// </summary>
internal sealed class ConfigData
{
	#region eee_カレントディレクトリー
	readonly static string configPath = Program.ExeDir + "emuera.config";
	#endregion
	readonly static string configdebugPath = Program.DebugDir + "debug.config";

	static ConfigData() { }
	private static ConfigData instance = new();
	public static ConfigData Instance { get { return instance; } }

	private ConfigData() { setDefault(); }
	#region EM_私家版_Emuera多言語化改造
	//適当に大き目の配列を作っておく。
	#region EE_configArrayの拡張
	// private AConfigItem[] configArray = new AConfigItem[80];
	private List<AConfigItem> configArray = [];
	#endregion
	//private AConfigItem[] replaceArray = new AConfigItem[50];
	//private AConfigItem[] debugArray = new AConfigItem[20];
	private List<AConfigItem> replaceArray = [];
	private List<AConfigItem> debugArray = [];
	#endregion

	#region EM_私家版_Emuera多言語化改造
	private void setDefault()
	{
		configArray.Add(new ConfigItem<bool>(ConfigCode.IgnoreCase, "大文字小文字の違いを無視する", "Ignore case", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseRenameFile, "_Rename.csvを利用する", "Use _Rename.csv file", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseReplaceFile, "_Replace.csvを利用する", "Use _Replace.csv file", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseMouse, "マウスを使用する", "Use mouse", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseMenu, "メニューを使用する", "Show menu", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseDebugCommand, "デバッグコマンドを使用する", "Allow debug commands", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.AllowMultipleInstances, "多重起動を許可する", "Allow multiple instances", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.AutoSave, "オートセーブを行なう", "Make autosaves", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseKeyMacro, "キーボードマクロを使用する", "Use keyboard macros", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.SizableWindow, "ウィンドウの高さを可変にする", "Changeable window height", true));
		configArray.Add(new ConfigItem<TextDrawingMode>(ConfigCode.TextDrawingMode, "描画インターフェース", "Drawing interface", TextDrawingMode.TEXTRENDERER));

		configArray.Add(new ConfigItem<int>(ConfigCode.WindowX, "ウィンドウ幅", "Window width", 760));
		configArray.Add(new ConfigItem<int>(ConfigCode.WindowY, "ウィンドウ高さ", "Window height", 480));
		configArray.Add(new ConfigItem<int>(ConfigCode.WindowPosX, "ウィンドウ位置X", "Window X position", 0));
		configArray.Add(new ConfigItem<int>(ConfigCode.WindowPosY, "ウィンドウ位置Y", "Window Y position", 0));
		configArray.Add(new ConfigItem<bool>(ConfigCode.SetWindowPos, "起動時のウィンドウ位置を指定する", "Fixed window starting position", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.WindowMaximixed, "起動時にウィンドウを最大化する", "Maximize window on startup", false));
		configArray.Add(new ConfigItem<int>(ConfigCode.MaxLog, "履歴ログの行数", "Max history log lines", 5000));
		configArray.Add(new ConfigItem<int>(ConfigCode.PrintCPerLine, "PRINTCを並べる数", "Items per line for PRINTC", 3));
		configArray.Add(new ConfigItem<int>(ConfigCode.PrintCLength, "PRINTCの文字数", "Number of Item characters for PRINTC", 25));
		configArray.Add(new ConfigItem<string>(ConfigCode.FontName, "フォント名", "Font name", "ＭＳ ゴシック"));
		configArray.Add(new ConfigItem<int>(ConfigCode.FontSize, "フォントサイズ", "Font size", 18));
		configArray.Add(new ConfigItem<int>(ConfigCode.LineHeight, "一行の高さ", "Line height", 19));
		configArray.Add(new ConfigItem<Color>(ConfigCode.ForeColor, "文字色", "Text color", Color.FromArgb(192, 192, 192)));//LIGHTGRAY
		configArray.Add(new ConfigItem<Color>(ConfigCode.BackColor, "背景色", "Background color", Color.FromArgb(0, 0, 0)));//BLACK
		configArray.Add(new ConfigItem<Color>(ConfigCode.FocusColor, "選択中文字色", "Highlight color", Color.FromArgb(255, 255, 0)));//YELLOW
		configArray.Add(new ConfigItem<Color>(ConfigCode.LogColor, "履歴文字色", "History log color", Color.FromArgb(192, 192, 192)));//LIGHTGRAY//Color.FromArgb(128, 128, 128));//GRAY
		configArray.Add(new ConfigItem<int>(ConfigCode.FPS, "フレーム毎秒", "FPS", 5));
		configArray.Add(new ConfigItem<int>(ConfigCode.SkipFrame, "最大スキップフレーム数", "Skip frames", 3));
		configArray.Add(new ConfigItem<int>(ConfigCode.ScrollHeight, "スクロール行数", "Lines per scroll", 1));
		configArray.Add(new ConfigItem<int>(ConfigCode.InfiniteLoopAlertTime, "無限ループ警告までのミリ秒数", "Milliseconds for infinite loop warning", 5000));
		configArray.Add(new ConfigItem<int>(ConfigCode.DisplayWarningLevel, "表示する最低警告レベル", "Minimum warning level", 1));
		configArray.Add(new ConfigItem<bool>(ConfigCode.DisplayReport, "ロード時にレポートを表示する", "Display loading report", false));
		configArray.Add(new ConfigItem<ReduceArgumentOnLoadFlag>(ConfigCode.ReduceArgumentOnLoad, "ロード時に引数を解析する", "Reduce argument on load", ReduceArgumentOnLoadFlag.NO));

		configArray.Add(new ConfigItem<bool>(ConfigCode.IgnoreUncalledFunction, "呼び出されなかった関数を無視する", "Ignore uncalled functions", true));
		configArray.Add(new ConfigItem<DisplayWarningFlag>(ConfigCode.FunctionNotFoundWarning, "関数が見つからない警告の扱い", "Function is not found warning", DisplayWarningFlag.IGNORE));
		configArray.Add(new ConfigItem<DisplayWarningFlag>(ConfigCode.FunctionNotCalledWarning, "関数が呼び出されなかった警告の扱い", "Function not called warning", DisplayWarningFlag.IGNORE));


		configArray.Add(new ConfigItem<bool>(ConfigCode.ChangeMasterNameIfDebug, "デバッグコマンドを使用した時にMASTERの名前を変更する", "Change MASTER mame in debug", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.ButtonWrap, "ボタンの途中で行を折りかえさない", "Button wrapping", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.SearchSubdirectory, "サブディレクトリを検索する", "Search subfolders", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.SortWithFilename, "読み込み順をファイル名順にソートする", "Sort filenames", false));
		configArray.Add(new ConfigItem<long>(ConfigCode.LastKey, "最終更新コード", "Latest identify code", 0));
		configArray.Add(new ConfigItem<int>(ConfigCode.SaveDataNos, "表示するセーブデータ数", "Save data count per page", 20));
		configArray.Add(new ConfigItem<bool>(ConfigCode.WarnBackCompatibility, "eramaker互換性に関する警告を表示する", "Eramaker compatibility warning", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.AllowFunctionOverloading, "システム関数の上書きを許可する", "Allow overriding system functions", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.WarnFunctionOverloading, "システム関数が上書きされたとき警告を表示する", "System function override warning", true));
		configArray.Add(new ConfigItem<string>(ConfigCode.TextEditor, "関連づけるテキストエディタ", "Text editor", "notepad"));
		configArray.Add(new ConfigItem<TextEditorType>(ConfigCode.EditorType, "テキストエディタコマンドライン指定", "Text editor command line setting", TextEditorType.USER_SETTING));
		configArray.Add(new ConfigItem<string>(ConfigCode.EditorArgument, "エディタに渡す行指定引数", "Text editor command line arguments", ""));
		configArray.Add(new ConfigItem<bool>(ConfigCode.WarnNormalFunctionOverloading, "同名の非イベント関数が複数定義されたとき警告する", "Duplicated functions warning", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiErrorLine, "解釈不可能な行があっても実行する", "Execute error lines", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiCALLNAME, "CALLNAMEが空文字列の時にNAMEを代入する", "Use NAME if CALLNAME is empty", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseSaveFolder, "セーブデータをsavフォルダ内に作成する", "Use sav folder", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiRAND, "擬似変数RANDの仕様をeramakerに合わせる", "Imitate behavior for RAND", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiDRAWLINE, "DRAWLINEを常に新しい行で行う", "Always start DRAWLINE in a new line", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiFunctionNoignoreCase, "関数・属性については大文字小文字を無視しない", "Do not ignore case for functions and attributes", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.SystemAllowFullSpace, "全角スペースをホワイトスペースに含める", "Whitespace includes full-width space", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiLinefeedAs1739, "ver1739以前の非ボタン折り返しを再現する", "Reproduce wrapping behavior like in pre ver1739", false));
		configArray.Add(new ConfigItem<UseLanguage>(ConfigCode.useLanguage, "内部で使用する東アジア言語", "Default ANSI encoding", UseLanguage.JAPANESE));
		configArray.Add(new ConfigItem<bool>(ConfigCode.AllowLongInputByMouse, "ONEINPUT系命令でマウスによる2文字以上の入力を許可する", "Allow long input by mouse for ONEINPUT", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiCallEvent, "イベント関数のCALLを許可する", "Allow CALL on event functions", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiSPChara, "SPキャラを使用する", "Allow SP characters", false));

		configArray.Add(new ConfigItem<bool>(ConfigCode.SystemSaveInBinary, "セーブデータをバイナリ形式で保存する", "Use the binary format for saving data", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiFuncArgOptional, "ユーザー関数の全ての引数の省略を許可する", "Allow arguments omission for user functions", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CompatiFuncArgAutoConvert, "ユーザー関数の引数に自動的にTOSTRを補完する", "Auto TOSTR conversion for user function arguments", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.SystemIgnoreTripleSymbol, "FORM中の三連記号を展開しない", "Do not process triple symbols inside FORM", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.TimesNotRigorousCalculation, "TIMESの計算をeramakerにあわせる", "Imitate behavior for TIMES", false));

		configArray.Add(new ConfigItem<bool>(ConfigCode.SystemNoTarget, "キャラクタ変数の引数を補完しない", "Do not auto-complete arguments for character variables", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.SystemIgnoreStringSet, "文字列変数の代入に文字列式を強制する", "String variable assignment on valid with string expression", false));

		#region EE_UPDATECHECK
		configArray.Add(new ConfigItem<bool>(ConfigCode.ForbidUpdateCheck, "UPDATECHECKを許可しない", "Disallow UPDATECHECK", false));
		#endregion
		#region EE_ERDConfig
		configArray.Add(new ConfigItem<bool>(ConfigCode.UseERD, "ERD機能を利用する", "Use ERD", true));
		#endregion
		#region EE_ERDNAME
		configArray.Add(new ConfigItem<bool>(ConfigCode.VarsizeDimConfig, "VARSIZEの次元指定をERD機能に合わせる", "Imitate ERD to VARSIZE dimension specification", false));
		#endregion
		#region EE_重複定義の確認
		configArray.Add(new ConfigItem<bool>(ConfigCode.CheckDuplicateIdentifier, "ERDで定義した識別子とローカル変数の重複を確認する", "Check duplicate ERD identifier and private variablea", false));
		#endregion
		#region EE_行連結の改行コード置換
		configArray.Add(new ConfigItem<string>(ConfigCode.ReplaceContinuationBR, "行連結の改行コードの置換文字列", "String of replacing new line code inside continuation", "\" \""));
		#endregion
		#region EE_CALLSHARP注意
		configArray.Add(new ConfigItem<bool>(ConfigCode.PluginAvailableWarn, "外部プラグインが有効時に警告を表示する", "If available pllugins, Show warning", true));
		#endregion
		#region EM_私家版_LoadText＆SaveText機能拡張
		configArray.Add(new ConfigItem<List<string>>(ConfigCode.ValidExtension, "LOADTEXTとSAVETEXTで使える拡張子", "Valid extensions for LOADTEXT and SAVETEXT", ["txt"]));
		#endregion
		#region EM_私家版_セーブ圧縮
		configArray.Add(new ConfigItem<bool>(ConfigCode.ZipSaveData, "セーブデータを圧縮して保存する", "Compress save data", false));
		#endregion
		#region EM_私家版_Emuera多言語化改造
		configArray.Add(new ConfigItem<bool>(ConfigCode.EnglishConfigOutput, "CONFIGファイルの内容を英語で保存する", "Output English items in the config file", false));
		configArray.Add(new ConfigItem<string>(ConfigCode.EmueraLang, "Emueraの表示言語", "Emuera interface language", string.Empty));
		#endregion
		#region EM_私家版_Icon指定機能
		configArray.Add(new ConfigItem<string>(ConfigCode.EmueraIcon, "Emueraのアイコンのパス", "Path to a custom window icon", string.Empty));
		#endregion
		#region EE_AnchorのCB機能移植
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBUseClipboard, "表示したテキストをクリップボードにコピーする", "Clipboard- Copy text to Clipboard during Game", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBIgnoreTags, "テキスト中の<>タグを無視する", "Clipboard- ignore <> tags in text", false));
		configArray.Add(new ConfigItem<string>(ConfigCode.CBReplaceTags, "<>を次の文で置き換える", "Clipboard- Replace <> with this", "."));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBNewLinesOnly, "新しい行のみコピーする", "Clipboard- Show new lines only", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBClearBuffer, "画面のリフレッシュ時にクリップボードとバッファを消去する", "Clipboard- Clear Buffer when game clears screen", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBTriggerLeftClick, "左クリックをトリガーにする", "Clipboard- LeftClick Trigger", true));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBTriggerMiddleClick, "ホイールクリックをトリガーにする", "Clipboard- MiddleClick Trigger", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBTriggerDoubleLeftClick, "ダブルクリックをトリガーにする", "Clipboard- Double Left Click Trigger", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBTriggerAnyKeyWait, "WAITをトリガーにする", "Clipboard- AnyKey Wait Trigger ", false));
		configArray.Add(new ConfigItem<bool>(ConfigCode.CBTriggerInputWait, "INPUTをトリガーにする", "Clipboard- Wait for Input Trigger", true));
		configArray.Add(new ConfigItem<int>(ConfigCode.CBMaxCB, "クリップボードに貼り付ける行数", "Clipboard- Length of Clipboard", 25));
		configArray.Add(new ConfigItem<int>(ConfigCode.CBBufferSize, "総バッファサイズ", "Clipboard- Buffer Size", 300));
		configArray.Add(new ConfigItem<int>(ConfigCode.CBScrollCount, "スクロールの行数", "Clipboard- Scrolled Lines per Key", 5));
		configArray.Add(new ConfigItem<int>(ConfigCode.CBMinTimer, "クリップボードの更新間隔(ミリ秒)", "Clipboard- min time between pastes", 800));
		#endregion
		#region EmuEra-Rikaichan related settings
		configArray.Add(new ConfigItem<bool>(ConfigCode.RikaiEnabled, "Rikaichanを使用する", "Rikai- Enabled", false));
		configArray.Add(new ConfigItem<string>(ConfigCode.RikaiFilename, "Rikaichanのファイルパス", "Rikai- Dictionary Filename", "Emuera-Rikai-edict.txt-eucjp"));
		configArray.Add(new ConfigItem<Color>(ConfigCode.RikaiColorBack, "ポップアップの背景色", "Rikai- Back Color", Color.FromArgb(0, 0, 0x8B))); ////Color.DarkBlue
		configArray.Add(new ConfigItem<Color>(ConfigCode.RikaiColorText, "ポップアップの文字色", "Rikai- Text Color", Color.FromArgb(0xFF, 0xFF, 0xFF))); ////Color.White
		configArray.Add(new ConfigItem<bool>(ConfigCode.RikaiUseSeparateBoxes, "翻訳中の語句を強調表示する", "Rikai- Use Separate Boxes", true));
		#endregion

		configArray.Add(new ConfigItem<bool>(ConfigCode.Ctrl_Z_Enabled, "Ctrl-Zで元に戻す機能を有効にする", "Enable undo with ctrl-z", false));

		debugArray.Add(new ConfigItem<bool>(ConfigCode.DebugShowWindow, "起動時にデバッグウインドウを表示する", "Show debug window on startup", true));
		debugArray.Add(new ConfigItem<bool>(ConfigCode.DebugWindowTopMost, "デバッグウインドウを最前面に表示する", "Debug window always on top", true));
		debugArray.Add(new ConfigItem<int>(ConfigCode.DebugWindowWidth, "デバッグウィンドウ幅", "Debug window width", 400));
		debugArray.Add(new ConfigItem<int>(ConfigCode.DebugWindowHeight, "デバッグウィンドウ高さ", "Debug window height", 300));
		debugArray.Add(new ConfigItem<bool>(ConfigCode.DebugSetWindowPos, "デバッグウィンドウ位置を指定する", "Fixed debug window starting position", false));
		debugArray.Add(new ConfigItem<int>(ConfigCode.DebugWindowPosX, "デバッグウィンドウ位置X", "Debug window X position", 0));
		debugArray.Add(new ConfigItem<int>(ConfigCode.DebugWindowPosY, "デバッグウィンドウ位置Y", "Debug window Y position", 0));

		replaceArray.Add(new ConfigItem<string>(ConfigCode.MoneyLabel, "お金の単位", "Currency symbol", "$"));
		replaceArray.Add(new ConfigItem<bool>(ConfigCode.MoneyFirst, "単位の位置", "Currency symbol position", true));
		replaceArray.Add(new ConfigItem<string>(ConfigCode.LoadLabel, "起動時簡略表示", "Loading message", "Now Loading..."));
		replaceArray.Add(new ConfigItem<int>(ConfigCode.MaxShopItem, "販売アイテム数", "Max shop item storage", 100));
		replaceArray.Add(new ConfigItem<string>(ConfigCode.DrawLineString, "DRAWLINE文字", "DRAWLINE character", "-"));
		replaceArray.Add(new ConfigItem<char>(ConfigCode.BarChar1, "BAR文字1", "BAR character 1", '*'));
		replaceArray.Add(new ConfigItem<char>(ConfigCode.BarChar2, "BAR文字2", "BAR character 2", '.'));
		replaceArray.Add(new ConfigItem<string>(ConfigCode.TitleMenuString0, "システムメニュー0", "System menu 0", "最初からはじめる"));
		replaceArray.Add(new ConfigItem<string>(ConfigCode.TitleMenuString1, "システムメニュー1", "System menu 1", "ロードしてはじめる"));
		replaceArray.Add(new ConfigItem<int>(ConfigCode.ComAbleDefault, "COM_ABLE初期値", "Default COM_ABLE", 1));
		replaceArray.Add(new ConfigItem<List<long>>(ConfigCode.StainDefault, "汚れの初期値", "Default Stain", new List<long>(new long[] { 0, 0, 2, 1, 8 })));
		replaceArray.Add(new ConfigItem<string>(ConfigCode.TimeupLabel, "時間切れ表示", "Time up message", "時間切れ"));
		replaceArray.Add(new ConfigItem<List<long>>(ConfigCode.ExpLvDef, "EXPLVの初期値", "Default EXPLV", new List<long>(new long[] { 0, 1, 4, 20, 50, 200 })));
		replaceArray.Add(new ConfigItem<List<long>>(ConfigCode.PalamLvDef, "PALAMLVの初期値", "Default PALAMLV", new List<long>(new long[] { 0, 100, 500, 3000, 10000, 30000, 60000, 100000, 150000, 250000 })));
		replaceArray.Add(new ConfigItem<long>(ConfigCode.pbandDef, "PBANDの初期値", "Default PBAND", 4));
		replaceArray.Add(new ConfigItem<long>(ConfigCode.RelationDef, "RELATIONの初期値", "Default RELATION", 0));
	}
	#endregion

	/*
	private void setDefault()
	{
		int i = 0;
		configArray[i++] = new ConfigItem<bool>(ConfigCode.IgnoreCase, "大文字小文字の違いを無視する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseRenameFile, "_Rename.csvを利用する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseReplaceFile, "_Replace.csvを利用する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseMouse, "マウスを使用する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseMenu, "メニューを使用する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseDebugCommand, "デバッグコマンドを使用する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.AllowMultipleInstances, "多重起動を許可する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.AutoSave, "オートセーブを行なう", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseKeyMacro, "キーボードマクロを使用する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SizableWindow, "ウィンドウの高さを可変にする", true);
		configArray[i++] = new ConfigItem<TextDrawingMode>(ConfigCode.TextDrawingMode, "描画インターフェース", TextDrawingMode.TEXTRENDERER);
		//configArray[i++] = new ConfigItem<bool>(ConfigCode.UseImageBuffer, "イメージバッファを使用する", true);
		configArray[i++] = new ConfigItem<int>(ConfigCode.WindowX, "ウィンドウ幅", 760);
		configArray[i++] = new ConfigItem<int>(ConfigCode.WindowY, "ウィンドウ高さ", 480);
		configArray[i++] = new ConfigItem<int>(ConfigCode.WindowPosX, "ウィンドウ位置X", 0);
		configArray[i++] = new ConfigItem<int>(ConfigCode.WindowPosY, "ウィンドウ位置Y", 0);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SetWindowPos, "起動時のウィンドウ位置を指定する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.WindowMaximixed, "起動時にウィンドウを最大化する", false);
		configArray[i++] = new ConfigItem<int>(ConfigCode.MaxLog, "履歴ログの行数", 5000);
		configArray[i++] = new ConfigItem<int>(ConfigCode.PrintCPerLine, "PRINTCを並べる数", 3);
		configArray[i++] = new ConfigItem<int>(ConfigCode.PrintCLength, "PRINTCの文字数", 25);
		configArray[i++] = new ConfigItem<string>(ConfigCode.FontName, "フォント名", "ＭＳ ゴシック");
		configArray[i++] = new ConfigItem<int>(ConfigCode.FontSize, "フォントサイズ", 18);
		configArray[i++] = new ConfigItem<int>(ConfigCode.LineHeight, "一行の高さ", 19);
		configArray[i++] = new ConfigItem<Color>(ConfigCode.ForeColor, "文字色", Color.FromArgb(192, 192, 192));//LIGHTGRAY
		configArray[i++] = new ConfigItem<Color>(ConfigCode.BackColor, "背景色", Color.FromArgb(0, 0, 0));//BLACK
		configArray[i++] = new ConfigItem<Color>(ConfigCode.FocusColor, "選択中文字色", Color.FromArgb(255, 255, 0));//YELLOW
		configArray[i++] = new ConfigItem<Color>(ConfigCode.LogColor, "履歴文字色", Color.FromArgb(192, 192, 192));//LIGHTGRAY//Color.FromArgb(128, 128, 128);//GRAY
		configArray[i++] = new ConfigItem<int>(ConfigCode.FPS, "フレーム毎秒", 5);
		configArray[i++] = new ConfigItem<int>(ConfigCode.SkipFrame, "最大スキップフレーム数", 3);
		configArray[i++] = new ConfigItem<int>(ConfigCode.ScrollHeight, "スクロール行数", 1);
		configArray[i++] = new ConfigItem<int>(ConfigCode.InfiniteLoopAlertTime, "無限ループ警告までのミリ秒数", 5000);
		configArray[i++] = new ConfigItem<int>(ConfigCode.DisplayWarningLevel, "表示する最低警告レベル", 1);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.DisplayReport, "ロード時にレポートを表示する", false);
		configArray[i++] = new ConfigItem<ReduceArgumentOnLoadFlag>(ConfigCode.ReduceArgumentOnLoad, "ロード時に引数を解析する", ReduceArgumentOnLoadFlag.NO);
		//configArray[i++] = new ConfigItem<bool>(ConfigCode.ReduceFormattedStringOnLoad, "ロード時にFORM文字列を解析する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.IgnoreUncalledFunction, "呼び出されなかった関数を無視する", true);
		configArray[i++] = new ConfigItem<DisplayWarningFlag>(ConfigCode.FunctionNotFoundWarning, "関数が見つからない警告の扱い", DisplayWarningFlag.IGNORE);
		configArray[i++] = new ConfigItem<DisplayWarningFlag>(ConfigCode.FunctionNotCalledWarning, "関数が呼び出されなかった警告の扱い", DisplayWarningFlag.IGNORE);
		//configArray[i++] = new ConfigItem<List<string>>(ConfigCode.IgnoreWarningFiles, "指定したファイル中の警告を無視する", new List<string>());
		configArray[i++] = new ConfigItem<bool>(ConfigCode.ChangeMasterNameIfDebug, "デバッグコマンドを使用した時にMASTERの名前を変更する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.ButtonWrap, "ボタンの途中で行を折りかえさない", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SearchSubdirectory, "サブディレクトリを検索する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SortWithFilename, "読み込み順をファイル名順にソートする", false);
		configArray[i++] = new ConfigItem<long>(ConfigCode.LastKey, "最終更新コード", 0);
		configArray[i++] = new ConfigItem<int>(ConfigCode.SaveDataNos, "表示するセーブデータ数", 20);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.WarnBackCompatibility, "eramaker互換性に関する警告を表示する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.AllowFunctionOverloading, "システム関数の上書きを許可する", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.WarnFunctionOverloading, "システム関数が上書きされたとき警告を表示する", true);
		configArray[i++] = new ConfigItem<string>(ConfigCode.TextEditor, "関連づけるテキストエディタ", "notepad");
		configArray[i++] = new ConfigItem<TextEditorType>(ConfigCode.EditorType, "テキストエディタコマンドライン指定", TextEditorType.USER_SETTING);
		configArray[i++] = new ConfigItem<string>(ConfigCode.EditorArgument, "エディタに渡す行指定引数", "");
		configArray[i++] = new ConfigItem<bool>(ConfigCode.WarnNormalFunctionOverloading, "同名の非イベント関数が複数定義されたとき警告する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiErrorLine, "解釈不可能な行があっても実行する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiCALLNAME, "CALLNAMEが空文字列の時にNAMEを代入する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseSaveFolder, "セーブデータをsavフォルダ内に作成する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiRAND, "擬似変数RANDの仕様をeramakerに合わせる", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiDRAWLINE, "DRAWLINEを常に新しい行で行う", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiFunctionNoignoreCase, "関数・属性については大文字小文字を無視しない", false); ;
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemAllowFullSpace, "全角スペースをホワイトスペースに含める", true);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemSaveInUTF8, "セーブデータをUTF-8で保存する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiLinefeedAs1739, "ver1739以前の非ボタン折り返しを再現する", false);
		configArray[i++] = new ConfigItem<UseLanguage>(ConfigCode.useLanguage, "内部で使用する東アジア言語", UseLanguage.JAPANESE);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.AllowLongInputByMouse, "ONEINPUT系命令でマウスによる2文字以上の入力を許可する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiCallEvent, "イベント関数のCALLを許可する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiSPChara, "SPキャラを使用する", false);

		configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemSaveInBinary, "セーブデータをバイナリ形式で保存する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiFuncArgOptional, "ユーザー関数の全ての引数の省略を許可する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiFuncArgAutoConvert, "ユーザー関数の引数に自動的にTOSTRを補完する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemIgnoreTripleSymbol, "FORM中の三連記号を展開しない", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.TimesNotRigorousCalculation, "TIMESの計算をeramakerにあわせる", false);
		//一文字変数の禁止オプションを考えた名残
		//configArray[i++] = new ConfigItem<bool>(ConfigCode.ForbidOneCodeVariable, "一文字変数の使用を禁止する", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemNoTarget, "キャラクタ変数の引数を補完しない", false);
		configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemIgnoreStringSet, "文字列変数の代入に文字列式を強制する", false);

		#region EE_UPDATECHECK
		configArray[i++] = new ConfigItem<bool>(ConfigCode.ForbidUpdateCheck, "UPDATECHECKを許可しない", false);
		#endregion
		#region EE_ERDConfig
		configArray[i++] = new ConfigItem<bool>(ConfigCode.UseERD, "ERD機能を利用する", true);
		#endregion

		#region EM_私家版_LoadText＆SaveText機能拡張
		configArray[i++] = new ConfigItem<List<string>>(ConfigCode.ValidExtension, "LOADTEXTとSAVETEXTで使える拡張子", new List<string> { "txt" });
		#endregion
		#region EM_私家版_セーブ圧縮
		configArray[i++] = new ConfigItem<bool>(ConfigCode.ZipSaveData, "セーブデータを圧縮して保存する", false);
		#endregion

		i = 0;
		debugArray[i++] = new ConfigItem<bool>(ConfigCode.DebugShowWindow, "起動時にデバッグウインドウを表示する", true);
		debugArray[i++] = new ConfigItem<bool>(ConfigCode.DebugWindowTopMost, "デバッグウインドウを最前面に表示する", true);
		debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowWidth, "デバッグウィンドウ幅", 400);
		debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowHeight, "デバッグウィンドウ高さ", 300);
		debugArray[i++] = new ConfigItem<bool>(ConfigCode.DebugSetWindowPos, "デバッグウィンドウ位置を指定する", false);
		debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowPosX, "デバッグウィンドウ位置X", 0);
		debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowPosY, "デバッグウィンドウ位置Y", 0);

		i = 0;
		replaceArray[i++] = new ConfigItem<string>(ConfigCode.MoneyLabel, "お金の単位", "$");
		replaceArray[i++] = new ConfigItem<bool>(ConfigCode.MoneyFirst, "単位の位置", true);
		replaceArray[i++] = new ConfigItem<string>(ConfigCode.LoadLabel, "起動時簡略表示", "Now Loading...");
		replaceArray[i++] = new ConfigItem<int>(ConfigCode.MaxShopItem, "販売アイテム数", 100);
		replaceArray[i++] = new ConfigItem<string>(ConfigCode.DrawLineString, "DRAWLINE文字", "-");
		replaceArray[i++] = new ConfigItem<char>(ConfigCode.BarChar1, "BAR文字1", '*');
		replaceArray[i++] = new ConfigItem<char>(ConfigCode.BarChar2, "BAR文字2", '.');
		replaceArray[i++] = new ConfigItem<string>(ConfigCode.TitleMenuString0, "システムメニュー0", "最初からはじめる");
		replaceArray[i++] = new ConfigItem<string>(ConfigCode.TitleMenuString1, "システムメニュー1", "ロードしてはじめる");
		replaceArray[i++] = new ConfigItem<int>(ConfigCode.ComAbleDefault, "COM_ABLE初期値", 1);
		replaceArray[i++] = new ConfigItem<List<Int64>>(ConfigCode.StainDefault, "汚れの初期値", new List<Int64>(new Int64[] { 0, 0, 2, 1, 8 }));
		replaceArray[i++] = new ConfigItem<string>(ConfigCode.TimeupLabel, "時間切れ表示", "時間切れ");
		replaceArray[i++] = new ConfigItem<List<Int64>>(ConfigCode.ExpLvDef, "EXPLVの初期値", new List<long>(new Int64[] { 0, 1, 4, 20, 50, 200 }));
		replaceArray[i++] = new ConfigItem<List<Int64>>(ConfigCode.PalamLvDef, "PALAMLVの初期値", new List<long>(new Int64[] { 0, 100, 500, 3000, 10000, 30000, 60000, 100000, 150000, 250000 }));
		replaceArray[i++] = new ConfigItem<Int64>(ConfigCode.pbandDef, "PBANDの初期値", 4);
		replaceArray[i++] = new ConfigItem<Int64>(ConfigCode.RelationDef, "RELATIONの初期値", 0);
	}
	*/

	public ConfigData Copy()
	{
		#region EM_私家版_Emuera多言語化改造
		ConfigData config = new();
		// for (int i = 0; i < configArray.Length; i++)
		for (int i = 0; i < configArray.Count; i++)
			if ((configArray[i] != null) && (config.configArray[i] != null))
				configArray[i].CopyTo(config.configArray[i]);
		//for (int i = 0; i < configArray.Length; i++)
		for (int i = 0; i < debugArray.Count; i++)
			if ((debugArray[i] != null) && (config.debugArray[i] != null))
				debugArray[i].CopyTo(config.debugArray[i]);
		//for (int i = 0; i < replaceArray.Length; i++)
		for (int i = 0; i < replaceArray.Count; i++)
			if ((replaceArray[i] != null) && (config.replaceArray[i] != null))
				replaceArray[i].CopyTo(config.replaceArray[i]);
		return config;
		#endregion
	}

	public Dictionary<ConfigCode, string> GetConfigNameDic()
	{
		Dictionary<ConfigCode, string> ret = [];
		#region EM_私家版_Emuera多言語化改造
		foreach (AConfigItem item in configArray)
		{
			if (item != null)
				// ret.Add(item.Code, item.Text);
				ret.Add(item.Code, $"{item.Text}/{item.EngText}");
		}
		#endregion
		return ret;
	}

	public T GetConfigValue<T>(ConfigCode code)
	{
		AConfigItem item = GetItem(code);
		//if ((item != null) && (item is ConfigItem<T>))
		return ((ConfigItem<T>)item).Value;
		//throw new ExeEE("GetConfigValueのCodeまたは型が不適切");
	}

	#region getitem
	public AConfigItem GetItem(ConfigCode code)
	{
		AConfigItem item = GetConfigItem(code);
		if (item == null)
		{
			item = GetReplaceItem(code);
			if (item == null)
			{
				item = GetDebugItem(code);
			}
		}
		return item;
	}
	public AConfigItem GetItem(string key)
	{
		AConfigItem item = GetConfigItem(key);
		if (item == null)
		{
			item = GetReplaceItem(key);
			if (item == null)
			{
				item = GetDebugItem(key);
			}
		}
		return item;
	}

	public AConfigItem GetConfigItem(ConfigCode code)
	{
		foreach (AConfigItem item in configArray)
		{
			if (item == null)
				continue;
			if (item.Code == code)
				return item;
		}
		return null;
	}
	public AConfigItem GetConfigItem(string key)
	{
		if (LegacyConfigNames.Items.TryGetValue(key, out var legacyCode))
			return GetConfigItem(legacyCode);
		#region EM_私家版_Emuera多言語化改造
		key = key.ToUpper();
		foreach (AConfigItem item in configArray)
		{
			if (item == null)
				continue;
			if (string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase))
				return item;
			if (item.Text == key)
				return item;
			if (item.EngText == key)
				return item;
		}
		return null;
		#endregion
	}

	public AConfigItem GetReplaceItem(ConfigCode code)
	{
		foreach (AConfigItem item in replaceArray)
		{
			if (item == null)
				continue;
			if (item.Code == code)
				return item;
		}
		return null;
	}
	public AConfigItem GetReplaceItem(string key)
	{
		#region EM_私家版_Emuera多言語化改造
		key = key.ToUpper();
		foreach (AConfigItem item in replaceArray)
		{
			if (item == null)
				continue;
			if (item.Name == key)
				return item;
			if (item.Text == key)
				return item;
		}
		return null;
		#endregion
	}

	public AConfigItem GetDebugItem(ConfigCode code)
	{
		foreach (AConfigItem item in debugArray)
		{
			if (item == null)
				continue;
			if (item.Code == code)
				return item;
		}
		return null;
	}
	public AConfigItem GetDebugItem(string key)
	{
		#region EM_私家版_Emuera多言語化改造
		key = key.ToUpper();
		foreach (AConfigItem item in debugArray)
		{
			if (item == null)
				continue;
			if (item.Name == key)
				return item;
			if (item.Text == key)
				return item;
		}
		return null;
		#endregion
	}

	public static SingleTerm GetConfigValueInERB(string text, ref string errMes)
	{
		AConfigItem item = Instance.GetItem(text);
		if (item == null)
		{
			errMes = string.Format(trerror.InvalidConfigName.Text, text);
			return null;
		}
		SingleTerm term;
		switch (item.Code)
		{
			//<bool>
			case ConfigCode.AutoSave://"オートセーブを行なう"
			case ConfigCode.MoneyFirst://"単位の位置"
				if (item.GetValue<bool>())
					term = new SingleLongTerm(1);
				else
					term = new SingleLongTerm(0);
				break;
			//<int>
			case ConfigCode.WindowX:// "ウィンドウ幅"
			case ConfigCode.PrintCPerLine:// "PRINTCを並べる数"
			case ConfigCode.PrintCLength:// "PRINTCの文字数"
			case ConfigCode.FontSize:// "フォントサイズ"
			case ConfigCode.LineHeight:// "一行の高さ"
			case ConfigCode.SaveDataNos:// "表示するセーブデータ数"
			case ConfigCode.MaxShopItem:// "販売アイテム数"
			case ConfigCode.ComAbleDefault:// "COM_ABLE初期値"
				term = new SingleLongTerm(item.GetValue<int>());
				break;
			//<Color>
			case ConfigCode.ForeColor://"文字色"
			case ConfigCode.BackColor://"背景色"
			case ConfigCode.FocusColor://"選択中文字色"
			case ConfigCode.LogColor://"履歴文字色"
				{
					Color color = item.GetValue<Color>();
					term = new SingleLongTerm(((color.R * 256) + color.G) * 256 + color.B);
				}
				break;

			//<Int64>
			case ConfigCode.pbandDef:// "PBANDの初期値"
			case ConfigCode.RelationDef:// "RELATIONの初期値"
				term = new SingleLongTerm(item.GetValue<long>());
				break;

			//<string>
			case ConfigCode.FontName:// "フォント名"
			case ConfigCode.MoneyLabel:// "お金の単位"
			case ConfigCode.LoadLabel:// "起動時簡略表示"
			case ConfigCode.DrawLineString:// "DRAWLINE文字"
			case ConfigCode.TitleMenuString0:// "システムメニュー0"
			case ConfigCode.TitleMenuString1:// "システムメニュー1"
			case ConfigCode.TimeupLabel:// "時間切れ表示"
				term = new SingleStrTerm(item.GetValue<string>());
				break;

			//<char>
			case ConfigCode.BarChar1:// "BAR文字1"
			case ConfigCode.BarChar2:// "BAR文字2"
				term = new SingleStrTerm(item.GetValue<char>().ToString());
				break;
			//<TextDrawingMode>
			case ConfigCode.TextDrawingMode:// "描画インターフェース"
				term = new SingleStrTerm(item.GetValue<TextDrawingMode>().ToString());
				break;
			default:
				{
					if (Enum.IsDefined(typeof(ConfigCode), item.Code))
					{
						switch (item.ValueToString())
						{
							case "YES":
								term = new SingleLongTerm(1);
								break;
							case "NO":
								term = new SingleLongTerm(0);
								break;
							default:
								string val = item.ValueToString();
								if (long.TryParse(val, out long i))
									term = new SingleLongTerm(i);
								else
									term = new SingleStrTerm(val);
								break;
						}
					}
					else
					{
						errMes = string.Format(trerror.NotAllowGetConfigValue.Text, text);
						return null;
					}
					break;
					/** try-catchで解決しようとした名残
					try
					{
						term = item.GetValue<bool>() == true ? new SingleTerm(1) : new SingleTerm(0) ;
					}
					catch
					{
						try
						{
							term = new SingleTerm(item.GetValue<int>());
						}
						catch
						{
							try
							{
								term = new SingleTerm(item.GetValue<string>());
							}
							catch
							{
								errMes = string.Format(trerror.NotAllowGetConfigValue.Text, text);
								return null;
							}
						}
					}
					break;
					**/
				}
		}
		return term;
	}
	#endregion


	public bool SaveConfig()
	{
		StreamWriter writer = null;

		try
		{
			#region EM_私家版_Emuera多言語化改造
			// writer = new StreamWriter(configPath, false, Config.Encode);
			writer = new StreamWriter(configPath, false, Config.Encode);

			// for (int i = 0; i < configArray.Length; i++)
			for (int i = 0; i < configArray.Count; i++)
			#endregion
			{
				AConfigItem item = configArray[i];
				if (item == null)
					continue;

				//1806beta001 CompatiDRAWLINEの廃止、CompatiLinefeedAs1739へ移行
				if (item.Code == ConfigCode.CompatiDRAWLINE)
					continue;
				if ((item.Code == ConfigCode.ChangeMasterNameIfDebug) && item.GetValue<bool>())
					continue;
				if ((item.Code == ConfigCode.LastKey) && (item.GetValue<long>() == 0))
					continue;
				#region EM_私家版_LoadText＆SaveText機能拡張
				if (item.Code == ConfigCode.ValidExtension)
				{
					var ex = (ConfigItem<List<string>>)item;
					var sb = new System.Text.StringBuilder();
					#region EM_私家版_Emuera多言語化改造
					// sb.Append(ex.Text).Append(":");
					sb.Append(Config.EnglishConfigOutput ? ex.EngText : ex.Text).Append(":");
					#endregion
					foreach (var str in ex.Value)
					{
						sb.Append(str).Append(",");
					}
					sb.Remove(sb.Length - 1, 1);
					writer.WriteLine(sb.ToString());
					continue;
				}
				#endregion
				//if (item.Code == ConfigCode.IgnoreWarningFiles)
				//{
				//    List<string> files = item.GetValue<List<string>>();
				//    foreach (string filename in files)
				//        writer.WriteLine(item.Text + ":" + filename.ToString());
				//    continue;
				//}
				writer.WriteLine(item.ToString());
			}
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			if (writer != null)
				writer.Close();
		}
		return true;
	}

	public bool ReLoadConfig()
	{
		//_fixed.configの中身が変わった場合、非固定になったものが保持されてしまうので、ここで一旦すべて解除
		foreach (AConfigItem item in configArray)
		{
			if (item == null)
				continue;
			if (item.Fixed)
				item.Fixed = false;
		}
		LoadConfig();
		return true;
	}

	public bool LoadConfig()
	{
		string defaultConfigPath = Program.CsvDir + "_default.config";
		string fixedConfigPath = Program.CsvDir + "_fixed.config";
		if (!File.Exists(defaultConfigPath))
			defaultConfigPath = Program.CsvDir + "default.config";
		if (!File.Exists(fixedConfigPath))
			fixedConfigPath = Program.CsvDir + "fixed.config";

		loadConfig(defaultConfigPath, false);
		loadConfig(configPath, false);
		loadConfig(fixedConfigPath, true);

		Config.SetConfig(this);
		bool needSave = false;
		if (!File.Exists(configPath))
			needSave = true;
		if (Config.CheckUpdate())
		{
			GetItem(ConfigCode.LastKey).SetValue(Config.LastKey);
			needSave = true;
		}
		if (needSave)
			SaveConfig();
		return true;
	}

	private bool loadConfig(string confPath, bool fix)
	{
		if (!File.Exists(confPath))
			return false;
		using var eReader = new EraStreamReader(false);
		if (!eReader.Open(confPath))
			return false;
		ScriptPosition? pos = null;
		try
		{
			string line = null;
			//bool defineIgnoreWarningFiles = false;
			while ((line = eReader.ReadLine()) != null)
			{
				if ((line.Length == 0) || (line[0] == ';'))
					continue;
				pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
				string[] tokens = line.Split([':']);
				if (tokens.Length < 2)
					continue;
				#region EM_私家版_Emuera多言語化改造
				AConfigItem item = GetConfigItem(tokens[0].Trim());
				#endregion
				if (item != null)
				{
					//1806beta001 CompatiDRAWLINEの廃止、CompatiLinefeedAs1739へ移行
					if (item.Code == ConfigCode.CompatiDRAWLINE)
					{
						item = GetConfigItem(ConfigCode.CompatiLinefeedAs1739);
					}
					//if ((item.Code == ConfigCode.IgnoreWarningFiles))
					//{ 
					//    if (!defineIgnoreWarningFiles)
					//        (item.GetValue<List<string>>()).Clear();
					//    defineIgnoreWarningFiles = true;
					//    if ((item.Fixed) && (fix))
					//        item.Fixed = false;
					//}

					if (item.Code == ConfigCode.TextEditor)
					{
						//パスの関係上tokens[2]は使わないといけない
						if (tokens.Length > 2)
						{
							if (tokens[2].StartsWith('\\'))
								tokens[1] += ":" + tokens[2];
							if (tokens.Length > 3)
							{
								for (int i = 3; i < tokens.Length; i++)
								{
									tokens[1] += ":" + tokens[i];
								}
							}
						}
					}
					if (item.Code == ConfigCode.EditorArgument)
					{
						//半角スペースを要求する引数が必要なエディタがあるので別処理で
						((ConfigItem<string>)item).Value = tokens[1];
						continue;
					}
					if (item.Code == ConfigCode.MaxLog && Program.AnalysisMode)
					{
						//解析モード時はここを上書きして十分な長さを確保する
						tokens[1] = "10000";
					}
					if (item.TryParse(tokens[1]) && fix)
						item.Fixed = true;
				}
			}
		}
		catch (EmueraException ee)
		{
			ParserMediator.ConfigWarn(ee.Message, pos, 1, null);
		}
		catch (Exception exc)
		{
			ParserMediator.ConfigWarn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
		}
		finally { eReader.Dispose(); }
		return true;
	}

	#region replace
	// 1.52a改変部分　（単位の差し替えおよび前置、後置のためのコンフィグ処理）
	public void LoadReplaceFile(string filename)
	{
		using var eReader = new EraStreamReader(false);
		if (!eReader.Open(filename))
			return;
		ScriptPosition? pos = null;
		try
		{
			string line = null;
			while ((line = eReader.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.Length == 0)
					continue;
				if (line[0] == ';')
					continue;
				pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
				string[] tokens = line.Split(',', ':');
				if (tokens.Length < 2)
					continue;
				string itemName = tokens[0].Trim();
				tokens[1] = line[(tokens[0].Length + 1)..];
				if (string.IsNullOrEmpty(tokens[1].Trim()))
					continue;
				AConfigItem item = GetReplaceItem(itemName);
				if (item != null)
					item.TryParse(tokens[1]);
			}
		}
		catch (EmueraException ee)
		{
			ParserMediator.Warn(ee.Message, pos, 1);
		}
		catch (Exception exc)
		{
			ParserMediator.Warn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
		}
		finally { eReader.Dispose(); }
	}

	#endregion

	#region debug


	public bool SaveDebugConfig()
	{
		StreamWriter writer = null;
		try
		{
			#region EM_私家版_Emuera多言語化改造
			// writer = new StreamWriter(configdebugPath, false, Config.Encode);
			writer = new StreamWriter(configdebugPath, false, Config.Encode);

			// for (int i = 0; i < debugArray.Length; i++)
			for (int i = 0; i < debugArray.Count; i++)
			#endregion
			{
				AConfigItem item = debugArray[i];
				if (item == null)
					continue;
				writer.WriteLine(item.ToString());
			}
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			if (writer != null)
				writer.Close();
		}
		return true;
	}

	public bool LoadDebugConfig()
	{
		using var eReader = new EraStreamReader(false);
		if (!File.Exists(configdebugPath))
			goto err;
		if (!eReader.Open(configdebugPath))
			goto err;
		ScriptPosition? pos = null;
		try
		{
			string line = null;
			while ((line = eReader.ReadLine()) != null)
			{
				if ((line.Length == 0) || (line[0] == ';'))
					continue;
				pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
				string[] tokens = line.Split([':']);
				if (tokens.Length < 2)
					continue;
				AConfigItem item = GetDebugItem(tokens[0].Trim());
				if (item != null)
				{
					item.TryParse(tokens[1]);
				}
#if DEBUG
				//else
				//	throw new Exception("コンフィグファイルが変");
#endif
			}
		}
		catch (EmueraException ee)
		{
			ParserMediator.ConfigWarn(ee.Message, pos, 1, null);
			goto err;
		}
		catch (Exception exc)
		{
			ParserMediator.ConfigWarn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
			goto err;
		}
		finally { eReader.Dispose(); }
		Config.SetDebugConfig(this);
		return true;
	err:
		Config.SetDebugConfig(this);
		return false;
	}

	#endregion
}
