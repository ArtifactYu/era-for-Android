using System.Text.Json.Serialization;

namespace MinorShift.Emuera.Runtime.Config.JSON;

#region 統合_netEmuera
//.netEmuera が setting.json に書く列挙値。値の並び順が数値と対応するので変更不可
enum Resampler
{
	NearnestNeighber,
	Linear,
	Cubic,
}

enum FontAntialias
{
	None,
	Normal,
	Full,
}
#endregion

//JSONの定義
sealed class JSONConfigData
{
	//ボタンにカーソルを合わせたときに背景色を変更するか
	[JsonPropertyName("UseButtonFocusBackgroundColor")]
	public bool UseButtonFocusBackgroundColor { get; set; }

	[JsonPropertyName("UseNewRandom")]
	public bool UseNewRandom { get; set; }

	public bool UseScopedVariableInstruction { get; set; }

	#region 統合_netEmuera
	//.netEmuera 側の設定を素通しするためのキー。
	//これが無いと JSONConfig.Save() で相手側の設定を消してしまう。
	//ImageSamplingOption / FontAntialias は SkiaSharp 前提の描画品質指定なので
	//EM+EE の描画では解釈しないが、値だけは保持する。
	public Resampler ImageSamplingOption { get; set; } = Resampler.Linear;

	public bool CheckUTF8withBOM { get; set; }

	public FontAntialias FontAntialias { get; set; } = FontAntialias.Normal;

	//キャラクタCSVの読み込みに _Rename.csv を適用するか
	public bool UseRenameInCharaCSV { get; set; }
	#endregion

	#region 統合_ゲームパッド対応
	public GamePadConfig GamePad { get; set; } = new();
	#endregion
}

#region 統合_ゲームパッド対応
/// <summary>
/// ゲームパッドの割り当て。値は XInput のボタンビットで、複数割り当ては OR で足す。
/// 0x0001 上 / 0x0002 下 / 0x0004 左 / 0x0008 右 / 0x0010 START / 0x0020 BACK
/// 0x0040 左スティック押込 / 0x0080 右スティック押込 / 0x0100 LB / 0x0200 RB
/// 0x1000 A / 0x2000 B / 0x4000 X / 0x8000 Y
/// 左スティックの傾きは十字キーと同じビットに合成される。
/// </summary>
sealed class GamePadConfig
{
	public bool Enabled { get; set; } = true;

	//選択カーソルの移動
	public int Up { get; set; } = 0x0001;
	public int Down { get; set; } = 0x0002;
	public int Left { get; set; } = 0x0004;
	public int Right { get; set; } = 0x0008;

	//決定（左クリック相当）と、入力欄を消すキャンセル
	public int Decide { get; set; } = 0x1000;
	public int Cancel { get; set; } = 0x2000;

	//右クリック相当。era系では「本文を飛ばして進める」に割り当てられていることが多い
	public int RightClick { get; set; } = 0x4000;

	//履歴スクロール
	public int ScrollUp { get; set; } = 0x0100;
	public int ScrollDown { get; set; } = 0x0200;
}
#endregion
