# 移植メモ — Windows 依存の実態と切り分け

対象上流: `upstream/emuera.em` (GitLab EvilMask/emuera.em)
ベース: HEAD `2175f8a` (2026-07-25) / 手元 exe は `cde11d6` (2025-11-14, EEv55)

調査・移植・実機での検証は [Claude Code](https://claude.com/claude-code)（Anthropic）を使って行った。

## ライセンス

`Readme/License/Emuera.LICENSE.txt` は zlib ライセンス相当。
**商用含め任意目的での使用・改変・再頒布が許可**されている。遵守事項は 3 点:

1. 出自を偽らない（オリジナル作者を騙らない）
2. 改変した場合はその旨を明示する
3. ソース頒布物からライセンス表示を削除・変更しない

→ Android 版の公開・配布は可能。README に「Emuera / Emuera.EM+EE の改変版である」旨と原著作権表示を残すこと。

## 規模

| 区分 | ファイル | 行数 |
|---|---|---|
| `Emuera/Runtime/` (パーサ・インタプリタ) | 93 | 49,165 |
| `Emuera/UI/` (WinForms) | 41 | 20,994 |
| 合計 | 136 | 70,581 |

UI のうち移植不要（Android では捨てる）:
- `UI/Framework/Forms/**` … WinForms のウィンドウ・ダイアログ一式 約 4,500 行
- `UI/Game/Rikaichan*.cs` … 辞書ポップアップ 約 4,800 行

→ **実際に移植が要る UI は約 8,000 行**（EmueraConsole / HtmlManager / Image / 表示パーツ群 / 文字計測）。

## ビルド確認

- 上流の既定構成は WMP の COM 参照を使うため `dotnet build` では通らない（MSB4803）。
  **`-c Release-NAudio -p:Platform=x64` なら dotnet CLI で 0 エラー**。これをリファレンスビルドとする。
- 出力は `upstream/emuera.em/Emuera/artifacts/` 配下（`Directory.Build.props` の `ArtifactsPath`）。

## 移植方式: ソースコピーではなく「リンク参照 + 差し替え」

`src/Emuera.Core/Emuera.Core.csproj` は上流の `.cs` を `<Compile Include>` でリンクし、
Android で成立しないファイルだけ `<Compile Remove>` する。上流追従が `git pull` だけで済む。

現時点の除外:

| 除外 | 代替 |
|---|---|
| `UI/Framework/**` | Android UI + MainWindow 互換アダプタ |
| `UI/Game/Rikaichan*.cs` | 当面非対応 |
| `Runtime/Utils/Sound.{WMP,NAudio}.cs`, `NAudio_LoopStream.cs` | Android MediaPlayer |
| `Runtime/Utils/WebPWrapper.cs` | SkiaSharp のデコーダ |
| `Runtime/Utils/WinmmTimer.cs`, `WinInput.cs` | .NET 標準 / Android 入力 |

## 未解決シンボル（この状態でのビルドエラー = 222 件のみ）

エラーは 3 種類しか出ず、**すべて「型が無い」だけ**。ロジック側の非互換は現れていない。

### System.Drawing.Common の型（CS1069, 156 件）

| 型 | 件数 | 対応 |
|---|---|---|
| `Graphics` | 56 | SkiaSharp `SKCanvas` 裏打ちのシム |
| `Bitmap` | 32 | `SKBitmap` 裏打ちのシム |
| `Font` / `FontStyle` | 36 | `SKFont`/`SKTypeface` 裏打ち |
| `Brush` / `Pen` | 14 | `SKPaint` 裏打ち |
| `ImageAttributes` / `ColorMatrix` | 10 | SkiaSharp のカラーフィルタ |
| `StringFormat` / `CharacterRange` | 4 | 文字計測用 |
| `Icon` / `PrivateFontCollection` | 4 | スタブ / `SKTypeface.FromFile` |

`Color` / `Point` / `Size` / `Rectangle` は **System.Drawing.Primitives にあり Android でもそのまま使える**ので対応不要。

### System.Windows.Forms（CS0234 28 件 + CS0246 一部）

`using System.Windows.Forms;` が 28 箇所。必要な型は
`Keys` / `MouseButtons` / `KeyEventArgs` / `TextFormatFlags` / `Timer` / `TextRenderer` /
`PopupEventArgs` / `DrawToolTipEventArgs` のみ。ほぼ enum と薄いスタブで足りる。

### 除外ファイル由来（CS0246）

`MainWindow` / `DebugDialog` / `Rikaichan` / `Sound`。

## MainWindow 互換アダプタが要る理由

`EmueraConsole`(2,695 行) は表示行の管理・スクロール・入力待ちを持っており、
Android 版でも**そのまま使い回したい**。EmueraConsole が MainWindow に要求するメンバは
以下に限られるので、WinForms を持たない互換クラスを用意すれば EmueraConsole を無改変で通せる。

| メンバ | 件数 | Android での扱い |
|---|---|---|
| `MainPicBox` | 37 | 描画先。WebView が描くのでサイズ情報だけ持つスタブ |
| `ToolTip` | 27 | WebView 側のツールチップ（長押し）に委譲 |
| `ScrollBar` | 20 | WebView のスクロールに委譲。値だけ保持 |
| `TextBox` 系 | 13 | WebView の入力欄に委譲 |
| `Invoke` / `Refresh` / `Close` / `Reboot` ほか | 20 | UI スレッドマーシャリング等 |

## Emuera.Core のビルド成功 (2026-07-31)

`src/Emuera.Core` が **net10.0（プラットフォーム非依存）で 0 エラー**。
上流 70,581 行のうち WinForms のウィンドウ実装だけを差し替え、本体ロジックは無改変で通っている。
出力: `src/Emuera.Core/bin/Debug/net10.0/Emuera.Core.dll` (約 1.3MB)。警告 134 件（すべて上流由来）。

実装したシム (`src/Emuera.Core/Compat/`):

| ファイル | 内容 |
|---|---|
| `Drawing/Primitives.cs` | FontStyle / GraphicsUnit / StringFormat / CharacterRange / ColorMatrix / ImageAttributes / GraphicsPath / PrivateFontCollection ほか |
| `Drawing/Font.cs` | Font / FontFamily と、フォント名を SKTypeface に解決する `FontResolver` |
| `Drawing/Bitmap.cs` | Image / Bitmap / ImageFormat / Icon。`LockBits` は SKBitmap のピクセルバッファを直接返す |
| `Drawing/Graphics.cs` | Graphics / Brush / Pen（SKCanvas 裏打ち） |
| `Drawing/Extras.cs` | Region（SKPath の論理演算）/ BitmapData / CombineMode / InstalledFontCollection |
| `WinForms/Keys.cs` | Keys / MouseButtons（Win32 仮想キーコード互換） |
| `WinForms/TextRenderer.cs` | 文字列の幅計測と描画 |
| `WinForms/Controls.cs` | Control / PictureBox / ScrollBar / TextBox / ToolTip / Timer / Clipboard / MessageBox ほか |
| `Platform/MainWindow.cs` | MainWindow 互換アダプタ + `IWindowHost`（UI 層への通知口）/ DebugDialog |
| `Platform/Program.cs` | ゲームフォルダのパス群と実行モード。`Program.Initialize(gameDir)` で初期化 |
| `Platform/Sound.cs` | Sound → `ISoundBackend` へ委譲 / Rikaichan 無効化スタブ |
| `Platform/NativeStubs.cs` | WinInput（キー状態）/ WebP（SkiaSharp デコード） |

上流と名前空間がずれていた点（実装時に判明）:
- `MainWindow` / `DebugDialog` は `MinorShift.Emuera.Forms`（ディレクトリは `UI/Framework/Forms/`）
- `EmueraConsole` は `MinorShift.Emuera.GameView` で **internal**

## 実データでの動作確認 (2026-07-31)

`src/Emuera.TestHarness` で erablue_resort の csv / erb をスクラッチパッドに複製して実行。
**ユーザーのゲームフォルダは読み取りのみで、書き換えていない**（Emuera は emuera.log / sav を書くため）。

```
ERB: 2697 本 / CSV: 3044 本
ロード完了: 3517ms  IsError=False  再描画要求=6回
[入力] 0  → キャラメイク画面へ遷移
emuera.log は出力されていません (警告なし)
```

- ERB / CSV のロード、タイトル画面の実行、入力の受け取りと次画面への遷移まで成功
- オフスクリーン描画した PNG で、日本語・色分け・罫線・PRINTC の桁揃えがすべて正常に出ることを確認
- 残る差分は 2 つとも想定内: `<img src='タイトルロゴ'>` がタグのまま出る（resources を複製していないため）、
  `CALLSHARP LAUNCH_BROWSER` の警告（プラグイン非対応）

### 重要な発見: 表示状態は「描画時」に確定する

`EmueraConsole.escapedParts`（表示中の HTML パーツ一覧）は **`OnPaint` の中でしか初期化されない**。
`BINPUT` などボタン数を数える命令がこれを参照するため、初回の実行は
`NullReferenceException at BINPUT_Instruction.DoInstruction` で停止した。

つまり **WebView 版でも「描画パス自体は走らせる」必要がある**。
対応として `MainWindow.RenderOffscreen()` を追加し、`Refresh()` / `Invalidate()` のたびに
オフスクリーン Bitmap へ `OnPaint` を実行して表示状態を確定させている。これでエラーは解消した。

この副産物として、上流と同じ描画結果が `MainWindow.BackBuffer` に残る。
Phase 2 では「行モデルを JSON で送る」方式を本命としつつ、
**この画像をそのまま WebView に出す暫定モードも取れる**（実装が早く、表示互換は完全）。

## WebHost — ブラウザから操作できる状態まで (2026-07-31)

`src/Emuera.WebHost` に、依存ゼロの HTTP + WebSocket サーバーと `IWindowHost` 実装を用意した。
ASP.NET Core を持ち込まないのは Android 上で確実に動く構成にするため。
WebSocket はハンドシェイクのみ自前で、フレーム処理は `WebSocket.CreateFromStream` に任せている。

| エンドポイント | 内容 |
|---|---|
| `GET /` | UI (埋め込みリソースの index.html) |
| `GET /screen.png` | 現在の画面。世代番号でキャッシュし、変化時のみ再エンコード |
| `GET /status` | 読み込み状態・画面サイズ・世代 |
| `WS /ws` | server→client: `redraw` / `input` / `title` / `tip`、client→server: `click` / `move` / `scroll` / `submit` / `enter` / `resize` |

PC でそのまま動くので、実機なしで開発サイクルを回せる:

```
dotnet run --project src/Emuera.TestHarness -- <ゲームフォルダ> --serve --port 8321
```

タイトル画面の表示、選択肢のタップ、次画面への遷移までブラウザで確認済み。

### 選択肢のクリックは 2 段構え

`EmueraConsole.MouseDown` は `INPUTMOUSEKEY` 待ちのときしか反応しない。
通常の選択肢は **`MoveMouse` でポインタ下のボタンを選択 → `PressEnterKey(mesSkip, 選択文字列, true)` で確定**する。
上流 MainWindow のマウス処理（バックログ・メッセージ待ち・INPUT 待ち・通常選択肢の 4 分岐）を
`MainWindow.HandleClick` として移植した。

### 表示は当面「画面 PNG + タップ座標」

上流の描画パスをそのまま走らせるため表示互換は完全で、タップ座標もそのままマウス座標として渡せる。
行モデルを JSON で送る本命モードは、この土台の上に載せ替える。

## 環境: Android ワークロードのバージョン不整合

この PC のワークロードは Visual Studio 管理下で、`Microsoft.NET.Runtime.MonoTargets.Sdk` は 10.0.3 まで。
一方 SDK が読むマニフェスト `microsoft.net.workload.mono.toolchain.current/10.0.110` は 10.0.10 を要求するため、
Android ビルドが `NETSDK1147: wasm-tools が必要` という誤解を招くエラーで止まる。

- SDK を 10.0.103 に固定してもマニフェストは最新が選ばれるため解消しない
- `global.json` の `sdk.workloadVersion` はワークロードセット用で、この環境には該当セットが無い
- 対応: **Visual Studio Installer でワークロードを更新する**（CLI から入れると VS 管理と二重になる）

## Android 実機で動作 (2026-07-31)

端末 SC-55E (arm64) で erablue_resort が起動し、タップ操作でゲームが進行することを確認。

```
andEmuera: フォント: 97##fallback
andEmuera: サーバー起動: http://127.0.0.1:42773/
andEmuera: ゲーム読み込み完了 IsError=False   (約 9 秒)
```

タイトル → 「[0] 最初から始める」→ スタイル選択 → 確認プロンプトまで到達。警告ゼロ。

### ゲームデータの転送は adb push が正解

`tar` 経由は 2 つの理由で失敗した:

- Windows の `tar` が既定で作る pax 形式を端末の toybox が読めない (`tar: bad header`) → `--format=gnutar` が必要
- **`tar` が日本語ファイル名を CP932 で格納する**。`chcp 65001` でも変わらず、端末上でファイル名が壊れて
  `FileNotFoundException` になる

`adb push` はファイル名を UTF-8 で扱うため問題なく、**5,915 ファイルが 4.4 秒**で転送できた。tar は不要。

```
adb push <ゲームフォルダ>/csv /sdcard/Android/data/rip.eragames.andemuera/files/games/<名前>/
adb push <ゲームフォルダ>/erb /sdcard/Android/data/rip.eragames.andemuera/files/games/<名前>/
adb push <ゲームフォルダ>/font /sdcard/Android/data/rip.eragames.andemuera/files/games/<名前>/
adb push <ゲームフォルダ>/emuera.config <ゲームフォルダ>/setting.json .../games/<名前>/
```

**`font` を忘れないこと。** 端末に等幅の日本語フォントは入っておらず、これが無いと
PRINTC の桁揃えが全滅する (後述の「PRINTC の桁揃えは等幅フォント前提」)。

### 上流へのパッチ: 大文字小文字を区別しないファイル列挙

Emuera は `Directory.GetFiles(dir, "*.CSV")` のようにパターンで列挙する。
**Windows は検索パターンの大文字小文字を区別しないが、Android(ext4) は区別する。**
erablue_resort には `.CSV` (大文字) のキャラ定義が 2 件あり、これが読み込まれず
「定義していないキャラクタを作成しようとしました」で停止した。加えて警告が 496 行出ていた。

`EnumerationOptions { MatchCasing = CaseInsensitive }` に置き換えて解消。
変更点は `patches/01-android-portability.patch` に記録してある (5 ファイル / 30 行)。

| ファイル | 箇所 |
|---|---|
| `Runtime/Config/Config.cs` | `getFiles` (ERB/CSV の主要列挙) と `getUpdateKey`。オプション定数もここに定義 |
| `Runtime/Script/Loader/ErhLoader.cs` | `*.erd` |
| `Runtime/Script/Data/ConstantData.cs` | `VarExt*.csv` |
| `UI/Game/Image/AppContents.cs` | resources の `*.csv` |
| `UI/Game/EmueraConsole.cs` | リロード時の `*.ERB` |

### 環境: ワークロード更新で解決

Visual Studio Installer で更新したところ android ワークロードが 36.1.43 になり、
`Microsoft.NET.Runtime.MonoTargets.Sdk` 10.0.10 が入って `NETSDK1147` は解消した。

## 画面レイアウトの調整

- `Theme.Material.NoActionBar` で全画面化
- Android 15 以降は画面がシステムバーの裏まで広がるため、`OnApplyWindowInsets` で
  ステータスバー・ナビゲーションバー・キーボードの分だけルートビューにパディングを入れる
- **描画幅は端末の実ピクセルではなく `emuera.config` のウィンドウ幅 (1600) を使う**。
  era のバリアントはその幅を前提にレイアウトを組んでおり、スマホの幅で描くと 1 行が収まらない。
  高さは端末の縦横比から計算し (1600x1374)、WebView 側で縮小表示する。
  `EmueraEngine.StartAsync(..., useConfigWidth: true)` / `Resize` が幅を固定したまま比率だけ追従する
- 文字が小さくなるのでピンチズームを有効化 (`BuiltInZoomControls` + viewport の `user-scalable=yes`)
- 入力欄は普段畳んでおき、「⌨ 入力」で開く。常時表示だと描画領域を圧迫するため

## 画像 (resources) — パス区切りの罠

- **画像はユーザーが自分で用意して入れる方式**。`resources/<キャラ番号><名前>/顔_水着.webp` のように
  命名規則でリネームして置く (`___キャラ画像の入れ方.txt`)
- 手元のデータは 155,267 ファイル / 1.82GB。`adb push` で 16 分、端末上で約 1.9GB

### 上流へのパッチ: パス区切りを `\` 固定にしない

resources を入れて起動したところ、`警告Lv1:list.csv:186行目:指定されたファイルの読み込みに失敗しました`
が大量に出た。ファイルは端末に正しく存在し、PC では同じデータで警告が出ない。

原因は `AppContents.cs` の

```csharp
string directory = Path.GetDirectoryName(path) + "\\";   // ← Android では区切りが '/'
```

Android では `/…/resources/_ANIME\ファイル名.webp` という不正なパスになる。
`Path.DirectorySeparatorChar` / `Path.Combine` に置き換えて解消し、**タイトルロゴが表示された**。
WebP のデコードは SkiaSharp がそのまま扱えている。

同じ書き方が他にもあったので、実害のある 3 箇所を直した:

| ファイル | 用途 |
|---|---|
| `UI/Game/Image/AppContents.cs` | resources の画像パス |
| `Runtime/Script/Data/ConstantData.cs` | `.als` (別名定義) のパス |
| `Runtime/Utils/Sys.cs` | `WorkingDir` / `ExeDir` |

`Config.getFiles` の `RelativePath += "\\"` は ERB の表示名に使われるだけで
ファイルアクセスには使わないため、Windows 版と表示を揃える意味でそのままにしている。

### 上流へのパッチ: EXISTFILE / ENUMFILES が Android で必ず失敗していた

タイトルロゴは出るのに、**キャラの顔グラと背景が一切出ない**。原因は上流の 1 行:

```csharp
// Runtime/Utils/EvilMask/Utils.cs:216  GetValidPath
path = path.Replace('/', '\\').Replace("..\\", "");
```

Android では `\` は区切りではなく**ファイル名に使える普通の文字**なので、
`resources/1001ペコリーヌ/顔_デフォルト.webp` が丸ごと 1 個のファイル名になり、
`File.Exists` / `Directory.Exists` が必ず false になる。

erablue_resort の `@GCREATE_拡張子F` (`ERB/汎用関数/グラ表示汎用関数.ERB`) は

```
IF EXISTFILE(@"resources/%ファイルパス%.webp")
    GCREATEFROMFILE 作成レイヤー, ファイルパス + ".webp"
ELSE
    GCREATEFROMFILE 作成レイヤー, ファイルパス + ".png"
```

なので **常に `.png` 分岐へ落ちていた**。実データは `.webp` が 155,245 個、`.png` は 10 個しかない。
**タイトルロゴだけ出ていたのは、あれが resources の csv 経由 (`AppContents`) で、
ERB からのファイル実在確認を通らないから。**

同じ `GetValidPath` を通る `ENUMFILES` (-1 を返す) も死んでおり、
ランダム画像選択・差分選択・「所持画像」判定がまとめて壊れていた。

正規化は `src/Emuera.Core/Compat/Platform/PortablePath.cs` に切り出した。
`Path.GetFullPath` を使わず**区切り文字を引数に取る純粋関数**にしてあるのは、
Windows 上から Android の挙動を検証するため (`--selftest-path`)。

上流の `Replace("..\\","")` は `String.Replace` が置換後を再走査しないため
`....//etc` が `..\etc` に化けてゲームフォルダの外へ出られた。
セグメント単位のスタック処理に変えて構造的に塞いである。

| ファイル | 箇所 |
|---|---|
| `Runtime/Utils/EvilMask/Utils.cs` | `GetValidPath` を `PortablePath` へ委譲 |
| `Runtime/Script/Statements/Function/Creator.Method.cs` | ENUMFILES の列挙オプションとソート / GCREATEFROMFILE / SAVETEXT / EXISTSOUND |
| `Runtime/Config/Config.cs` | 列挙オプションに `MatchType` / `AttributesToSkip` を追加 |
| `Runtime/Script/Statements/Instraction.Child.cs` | PLAYSOUND / PLAYBGM のパス |

#### ENUMFILES の返り値は `/` 区切りのままでよい

`Path.GetRelativePath` が返すので Android では `resources/1001…/顔_水着.webp` になるが、
erablue_resort の消費側は `SUBSTRING(RESULTS, 10, -1)` のような長さ基準か
`REPLACE(…, ESCAPE("\\"), "/")` による自前の正規化なので影響しない。
逆に `\` へ揃えると、`GetValidPath` を通らない `GCREATEFROMFILE` 側が壊れる。

#### ext4 の readdir 順は不定

`Directory.EnumerateFiles` はソートを保証しない。NTFS は名前順に返すが ext4 は返さないので、
添字がそのまま差分番号になる ERB (`おさわりアーカイブ.ERB`) の並びが起動ごとに変わる。
`Array.Sort` で揃えた。

#### 周回引き継ぎも同じ原因で無言で壊れていた

`SAVETEXT` の親フォルダ作成が `filepath.LastIndexOf('\\')` 決め打ちで、Android では
`dat\人物DT_XML.txt` という**名前のファイル**がゲームフォルダ直下にできていた。
書き込みは成功するので警告もエラーも出ないが、次周の `LOADTEXT "dat/人物DT_XML.txt"` が
空を返し、`DT_FROMXML` が空のデータベースを作る。`Path.GetDirectoryName` に置き換えて解消。

**端末に `dat\*.txt` という名前の残骸が残っていたら削除してよい。**

#### 残る制約: 画像ファイル名の大文字小文字

トップレベルの `resources` / `csv` / `erb` は `Program.ResolveDir` が吸収するが、
**その配下のフォルダ名・ファイル名の大小は解決しない**。
都度フォールバック検索は 2,650 フォルダの走査になり `EXISTFILE` の呼び出し頻度に耐えないため入れていない。

## プラグイン: 内蔵実装に置き換え

上流は `Plugins/*.dll` を `Assembly.LoadFrom` で読むが、Android では WinForms 依存の DLL を持ち込めない。
`PluginManager.LoadPlugins` にパッチを当て、内蔵実装を先に登録するようにした
(同名 DLL があればそちらが優先される。`methods.Add` は重複で例外になるため添字代入に変更)。

- `LAUNCH_BROWSER` … `src/Emuera.Core/Compat/Platform/BuiltinPlugins.cs` に実装。
  http/https 以外は無視し、実際に開く処理は `BrowserLauncher` 経由で Android の Intent に委譲。
  スクリプトが指定した任意の URL なので、開く前に確認ダイアログを出す

### 読めない DLL で起動ごと止まらないようにした (2026-08-01)

内蔵実装を足しただけでは足りなかった。`Plugins/*.dll` が **実在する**ゲーム
(erablue_resort は `LAUNCH_BROWSER.dll` と `StringBuilderPlugin.dll` を同梱している) では、
上流の DLL ループが例外処理なしで `Assembly.LoadFrom` → `GetTypes()` を呼ぶため、
`ReflectionTypeLoadException`（DLL が参照している本家 `Emuera, Version=1.824.0.0` が
この移植版には無い）が `Process.Initialize` まで伝播し、
「ERHの読み込み中にエラーが発生したため処理を終了しました」で**起動そのものが失敗していた**。

プラグイン DLL は必ず本体アセンブリを参照するので、この移植版では
**読み込みが成功することはない**。DLL ごとに try/catch し、
`ParserMediator.Warn` で警告を出して次の DLL へ進むようにした。

```
警告Lv2:プラグイン「LAUNCH_BROWSER.dll」を読み込めませんでした。このDLLが提供する命令は使用できません
Could not load file or assembly 'Emuera, Version=1.824.0.0, ...'. 指定されたファイルが見つかりません。
```

- 詳細メッセージは `Warn` の `stack` 引数に回して 2 行に分けた。
  1 行にまとめると端末幅で切れて肝心の DLL 名が読めない
- `ReflectionTypeLoadException.Message` は「型を読み込めない」としか言わないので、
  解決できなかったアセンブリ名が入っている `LoaderExceptions` 側を表示する
- 警告レベルは 2（既定の `DisplayWarningLevel` は 1 なので既定設定で見える）

検証: `dotnet run --project src/Emuera.TestHarness -- <erablue_resort> --input 0` が
`IsError=False` になり、タイトル画面と `[0] 最初から始める` が出る。
`Plugins` を持たないゲーム (eratohoTW) は変更前後で `--shot` の PNG が 1 バイト違わない
（触ったのは DLL ループの中だけで、`Plugins` が無ければ手前で return する）。

実機 (SC-55E) でも確認した。`adb push` で `Plugins` を送ってから起動すると、
PC と同じ警告 2 行が出たうえでタイトル画面まで進む。
確認後に `Plugins` は端末から消してある — Android では DLL が読めることは無いので、
送る意味は無く、起動のたびに警告 2 行が出るだけになる。

## 入力モード — スマホ特有の落とし穴

キャラメイクで数値入力 (`[999] これで決定する`) を試したところ、
IME が日本語モードのため **全角の「９９９」が入力され、Emuera が受け取れなかった**。
PC では起こらない、スマホ固有の問題。

対応として、実行側が待っている入力の種類を画面へ伝えるようにした:

- `EmueraEngine.InputMode` … `InputRequest.InputType` を `None / EnterKey / Integer / String / Any` に写す
- 変化時に WebSocket で `{t:"mode", v:"integer"}` を送る
- 画面側は `inputMode="numeric"` でテンキーを出し、送信時に全角数字を半角へ正規化する

これでテンキーが出て半角で入力できるようになった。

## 実機で確認できたこと (2026-07-31)

| 項目 | 結果 |
|---|---|
| 起動 | 約 10 秒 (ERB 2,697 / CSV 3,044 / resources 155,267 ファイル)、警告ゼロ |
| 画像 | WebP のタイトルロゴが表示。SkiaSharp がそのままデコードできる |
| 選択肢のタップ | タイトル → スタイル選択 → 確認 → キャラメイクまで進行 |
| 文字入力 | 入力欄から NAME を入力して反映 |
| 数値入力 | テンキーで半角入力 (上記の対応後) |
| キーボード | 表示時に描画領域が縮む (インセット処理が有効) |
| プラグイン | `LAUNCH_BROWSER` の警告が消滅 |

**未確認: セーブ／ロードの往復。** ゲームを進めてセーブし、アプリ再起動後にロードできるかは
実際に遊んで確認するのが早い。バイナリ I/O 自体はプラットフォーム非依存なのでリスクは低いと見ている。

## 複数バリアントの選択

`games/` 直下に複数のゲームフォルダを置けるようにした。

- 候補が 1 つならそのまま起動、複数なら `ListView` で選択させる
- 各項目に ERB 本数を出して見分けられるようにした
- 前回選んだものを `SharedPreferences` に記録し、次回は先頭に「前回遊んだもの」として表示

**制約: 1 回の起動で扱えるのは 1 バリアント。** `Program` がゲームフォルダのパスを静的に保持しており、
`GlobalStatic` にも実行状態が残るため、プロセス内での切り替えは危険。切り替えるにはアプリを再起動する。

実機で erablue_resort (ERB 2,697 本) と eraJK (ERB 411 本) を並べ、両方が起動することを確認済み。

## 最下行が欠ける問題

「選択肢が入力バーとかぶって押しづらい」という指摘から調べたところ、
**画面の最下行そのものが欠けていた**。切り分けは `adb forward` で端末のサーバーに
PC から直接つないで `/screen.png` を取得し、画像自体が欠けていることを確認した。

```
adb forward tcp:9911 tcp:<ログに出るポート>
curl http://127.0.0.1:9911/screen.png
```

原因は 2 つ:

1. **Emuera は描画領域の下端を基準に行を並べる** (`pointY = MainPicBox.Height - LineHeight`)。
   そのため描画領域を高くしても最下行は常に下端に来る。最初「1 行分足す」対応をしたが、
   増えたのは上に表示できる行数だけで効果がなかった
2. **emuera.config のフォントサイズが行の高さを上回ることがある** (erablue_resort はフォント 16px / 行高 18px、
   eraJK は行高 17px)。文字の実高さが行高を超え、最下行の下側が領域の外へはみ出して欠ける

対処:

- `MainWindow.RenderOffscreen` で **ビットマップだけ `LineHeight/2` ぶん高く確保**し、はみ出しを受け止める
- `EmueraEngine.FitToLines` で描画高さを行の高さの倍数に揃え、半端な行が出ないようにする
- 画面側は画像を下端合わせ (`bottom: 12px`) にして操作バーとの間に隙間を作る
- 操作バーの高さも詰めた

## PRINTC の桁揃えは等幅フォント前提 (2026-08-01)

キャラ詳細画面の最下部が横一列に繋がって出ていた。

```
        [101]次のキャラへ[130]画像フォルダ選択(2)[140]汎用喘ぎ個別設定[141]妊娠個別設定[142]同室画面表示設定
```

すぐ上の `[90]通常能力  [91]性的能力 …` の行は無事だった。**この差が原因の切り分けになる**:

- 生き残った行は erablue_resort が `PRINTBUTTON "[90]通常能力　　"` と**全角スペースを直書き**している
- 壊れた行は `PRINTBUTTONLC "[101]次のキャラへ", 101` ([ERB/PRINT_STATE.ERB:185]) で、
  **桁揃えをエンジンに任せている**

### エンジンは半角スペースで桁を作る

[EmueraConsole.Print.cs:547](../upstream/emuera.em/Emuera/UI/Game/EmueraConsole.Print.cs) の `CreateTypeCString`:

```csharp
length = Encoding.GetEncoding("Shift-JIS").GetByteCount(str);   // 半角=1, 全角=2
str += new string(' ', printcLength + 1 - length);              // 半角スペースで詰める
width = stringMeasure.GetDisplayLength(str, font);
while (width > printCWidthL) {                                  // 半角スペース25個ぶんの実測幅
    if (str[^1] != ' ') break;                                  // ← スペースを使い切ると降参
    str = str.Remove(str.Length - 1, 1);
}
```

**「半角スペース N 個の幅 = 半角文字 N 個の幅」が成り立つ等幅フォントを前提にしている。**
比例フォントではラベル自身が既にパディング枠より広いため、`while` が詰めたスペースを
1 つ残らず剥がして `break` に到達する ＝ パディングがゼロになって隣とくっつく。

`GetDisplayLength` は PRINTC だけでなく**ボタン幅・行の折り返し・HTML_PRINT の div レイアウト・
クリック当たり判定**すべての基準なので、ここが崩れると画面全体が崩れる。

### 端末に等幅の CJK フォントは無い

erablue_resort は `emuera.config` で `フォント名:BIZ UDGothic` を指定し、
**ゲームフォルダの `font/` に BIZUDGothic-Regular.ttf / -Bold.ttf を同梱している**。
上流 Emuera はこれを `Program.Main` で `GlobalStatic.Pfc` へ読み込む。

移植版が拾えていなかった理由は 2 つ:

1. そもそも端末に `font/` を push していなかった (README の手順に無かった)
2. `PrivateFontCollection.AddFontFile` が `SKTypeface` を自分のリストに溜めるだけで
   **`FontResolver` に登録していなかった**。上流の `FontFactory` は Pfc 経由で引くが、
   `CreateTypeCString` は `new Font(Style.Fontname, …)` と**名前で直接**引く。
   つまり枠幅の計算と実測で**別のフォントを使う**状態になっていた

`/system/fonts` には `NotoSansCJK-Regular.ttc` (比例) と `DroidSansMono.ttf` (CJK 無し) しか無く、
Samsung 機では `SKFontManager.MatchCharacter('あ')` が `97##fallback` という名前の比例フォントを返す。
半角スペースの送り幅は **0.21em** しかない (BIZ UDGothic は 0.5em)。

### 対処

| 対象 | 内容 |
|---|---|
| `Compat/Drawing/Font.cs` | `FontResolver` を **(名前, スタイル)** キーのレジストリに変更。`RegisterDirectory` でフォルダごと登録 |
| `Compat/Drawing/Font.cs` | 太字・斜体のフェイスが無いときだけ `SKFont.Embolden` / `SkewX` で合成。**どちらも送り幅を変えない** |
| `Compat/Platform/Program.cs` | `Initialize` から `LoadGameFonts()` — `font/` を Pfc と FontResolver の両方へ登録 |
| `Compat/Drawing/Primitives.cs` | `AddFontFile` が `FontResolver.Register` にも流すようにした |
| `MainActivity.SetupFonts` | `files/fonts/` (games/ の隣) も登録。font/ を同梱しないゲーム用の共有置き場 |
| `Api/EmueraEngine.cs` | Config 確定後に等幅かを検査し、崩れる構成なら警告を出す (`FontWarning` → WebView の tip) |
| `UI/FontFactory.cs` (上流) | `fontStyleDic` がメソッドローカルで毎回 `new Font` していたのを static な `fontDic` に一本化 |

`FontFactory` のキャッシュ不備は本件と別のバグだが同じコードパス上にあり、
`GetDisplayLength` が文字列パートごとに `Config.DefaultFont` を引くため
**計測 1 回ごとに `SKFont`/`SKTypeface` の生成が走っていた**。

### 検証

`Emuera.TestHarness --selftest-font <ゲームフォルダ>` を追加した。
Windows にはシステム版 BIZ UDGothic が入っていて素直に走らせると差が出ないため、
`FontResolver.UseSystemFonts` と「ゲームの font/ を使うか」を切り替えて実機の状態を PC 上で再現する。

```
--- ゲームの font/ あり (実機の正しい状態) ---
指定: BIZ UDGothic / 実際: BIZ UDGothic / 16px
半角スペース×32=256px  半角M×32=256px  全角×16=256px  太字M×32=256px
OK  PRINTC "[130]画像フォルダ選択(2)" が 25 桁の枠に収まる   (詰めた結果 200px / 枠 200px)

--- ゲームの font/ なし (実機の壊れた状態) ---
指定: BIZ UDGothic / 実際: Segoe UI / 16px
半角スペース×32=141px  半角M×32=460px  全角×16=141px  太字M×32=490px
    PRINTC "[130]画像フォルダ選択(2)" -> 137px (枠 110px)  ← はみ出して隣とくっつく
```

実機では `font/` を push して再インストールし、同じキャラ詳細画面で
`[101]` `[130]` `[140]` `[141]` `[142]` が 25 桁ごとに並ぶこと、
三サイズ・招待時同行キャラの `：` が縦に揃うことを確認した。

（`全角×16` は後述のグリフフォールバックを入れてから 166px → 141px になった。
Segoe UI は全角スペースを持たないので、いまはセルに丸められる。
`半角M` との差は残るので、等幅でないという判定自体は変わらない）

### 続報: `font/` を同梱しないゲームは受け皿がそのまま本文フォントになる (2026-08-02)

eraTOWN で「コマンド欄の選択肢が完全に横一列に繋がる」。見え方は上と同じだが**入り口が違う**。

- eraTOWN の `emuera.config` は `フォント名:ＭＳ ゴシック` で、**`font/` を同梱していない**。
  手元のバリアントを見るとこちらが普通で、`font/` を同梱する erablue_resort のほうが例外
- Android に ＭＳ ゴシックは無いので `FontResolver.ResolveCore` は名前で引けず、必ず `Fallback` に落ちる。
  **つまりこの受け皿がそのまま本文フォントになる**
- その受け皿は `MainActivity.SetupFonts` の `shared ?? bundled ?? FindDeviceFont()` で決まっており、
  **共有 `files/fonts/` に置いたものが等幅かどうかを問わず APK 同梱の BIZ UDGothic より優先**されていた

`--selftest-font <eraTOWN>` の数値 (17px):

```
--- 受け皿なし (同梱フォントも読めない最悪の端末) ---
指定: ＭＳ ゴシック / 実際: Segoe UI / 17px
半角スペース×32=150px  半角M×32=489px  全角×16=150px  太字M×32=521px
    PRINTC "[130]画像フォルダ選択(2)" -> 132px (枠 117px)  ← はみ出して隣とくっつく

--- 受け皿 = BIZUDGothic-Regular.ttf (実機の正しい状態) ---
指定: ＭＳ ゴシック / 実際: BIZ UDGothic / 17px
半角スペース×32=272px  半角M×32=272px  全角×16=272px  太字M×32=272px
OK  PRINTC "[130]画像フォルダ選択(2)" が 25 桁の枠に収まる   (詰めた結果 213px / 枠 213px)
```

**セル幅が 8.5px のまま扱われる限り、フォントサイズが奇数でも桁は揃う。**
`CreateTypeCString` の詰め方は「全角 a 個 + 半角 b 個 + 詰めスペース」の総幅が常に一定になる構造
(`17a + 8.5b + 8.5(26-2a-b) = 221` で a, b が消える) なので、セル幅の端数そのものは効かない。
— ただし**実機はその 8.5px を 8px に丸めていた**。次々節。

対処:

| 対象 | 内容 |
|---|---|
| `Compat/Drawing/Font.cs` | 等幅判定を `FontMetrics` に 1 本化 (`RatioOk` / `IsMonospaced`)。**フォントを選ぶ側と検査する側が同じ式を通る**ようにするのが目的 |
| `MainActivity.PickFallback` | 受け皿を「共有 fonts/ → APK 同梱 → 端末」の順に見て**等幅の最初のもの**を採る。見送った候補は理由つきで logcat に出す |
| `Api/EmueraEngine.cs` | `CheckMonospaced` / `FontDiagnostics.IsMonospaced` を `FontMetrics` へ寄せた |

「共有 `fonts/` に置いたものが勝つ」という差し替えの仕様は保ったまま、
そこへ比例フォントを置いても桁揃えだけは壊れない。

### ＭＳ ゴシックは Skia では引けない — PC でだけ再現しない (2026-08-02)

上を PC で再現しようとして見つかった別口。`SKTypeface.FromFamilyName("ＭＳ ゴシック")` は
**Windows 上でも失敗する** (既定フォントが返り、`ResolveCore` の名前一致チェックで弾かれる)。
Skia は英語のファミリ名でしか引かないため。GDI+ は全角名でも引けるので、ここだけ本家と挙動が違う。

`FontResolver.localizedNames` に `ＭＳ ゴシック → MS Gothic` などの表を足した。
Android には元から無いので**実機の挙動は変わらない** (従来どおり受け皿に落ちる)。
これが無いと **PC で `--serve` しても実機と違う崩れ方しか見られず**、切り分けが進まない。

### 検証 (追加分)

`--selftest-font` はゲームが `font/` を同梱している前提でしか組まれておらず、
`InspectFonts` が `FontResolver.Fallback` を設定しないため、
**eraTOWN のような構成で「実機の正常系」を再現できていなかった** (PC の既定フォントに落ちる)。

- `InspectFonts(..., fallbackFontPath)` で受け皿を指定できるようにした
- ケースはゲームによって組み替える。`font/` が無いゲームでは
  「受け皿なし (壊れた状態)」「受け皿 = APK 同梱フォント (実機の正常系)」を並べる
- `--font-fallback <ttf>` で任意のフォントを受け皿にできる (利用者が共有 fonts/ に置いたものの再現)
- OS のフォントを使うケースでは太字の送り幅を assert しない。
  MS Gothic は太字フェイスを持たず OS 側が太らせたものを返すので送り幅が動く (本家 Windows と同じ挙動)

erablue_resort / eratohoTW / eraTOWN の 3 本で「すべて成功」。

PC ではロードからメイン画面まで通り、コマンド欄が 4 列・25 桁で並ぶことを画像でも確認した。

```bash
dotnet run --project src/Emuera.TestHarness -- <eraTOWN> --size 1790x1200 \
  --input 1 --input 99 --input 100 --input "" --input "" --input "" --input "" --input "" --shot main.png
```

- `--size` は **emuera.config のウィンドウ幅に合わせる** (eraTOWN なら 1790)。
  `--serve` 以外の経路は `useConfigWidth` を渡しておらず既定 1600 幅で描くため、
  合わせないと実機と別の崩れ方 (折り返しと中央寄せのずれ) を見ることになる
- **`--input ""` は PowerShell からだと空文字が渡らない**（次の引数がそのまま入力になる）。bash から実行すること

### 実機だけ崩れた本当の理由 — Android の Skia は送り幅を整数へ丸める (2026-08-03)

受け皿を直して BIZ UDGothic が使われるようになっても、**実機だけ**まだ警告が出た。

```
andEmuera: 受け皿に採用: BIZ UDGothic (APK 同梱・等幅)
andEmuera: ゲーム読み込み完了 IsError=False 描画サイズ=1790x1530
andEmuera: 等幅フォントが見つかりません (指定: ＭＳ ゴシック / 実際: BIZ UDGothic)
```

PC では同じ 17px の BIZ UDGothic が 272/272/272 と完全に一致するのに、実機では等幅と見なされない。
差は **Skia の既定設定がプラットフォームで違う**ことにあった。
Android 側はヒンティングを効かせて**送り幅を整数へ丸める**ため、半角セル 8.5px が 8px になり、
半角 2 個 (16px) ≠ 全角 1 個 (17px) になる。PRINTC の枠は半角スペース基準・ラベルは全角混じりなので、
この 1px が桁ごとに効いて崩れる。

`Font.CreateSkFont` で `SKFont.LinearMetrics = true` / `Subpixel = true` を立てて解決した。

- **偶数サイズでは表に出ない**。erablue_resort は 16px ＝ 8.0px なので丸めても値が変わらない。
  17px の eraTOWN で初めて出た
- **PC の描画は 1 画素も変わらない**。PC 側は元から丸めていないため。
  eraTOWN のメイン画面を変更前後で `--compare` して「画素が完全一致 (1790x1208)」を確認済み

**教訓**: フォント周りは PC で数値が合っても実機で合うとは限らない。Skia の既定値を疑うこと。
`CheckMonospaced` の警告に実測値 (半角スペース×32 / 半角M×32 / 全角×16) を入れたので、
次からは logcat の 1 行でどの比が崩れているか分かる。

### 実機での確認 (2026-08-03)

端末 SC-55E (Z Fold6)。**そもそも端末に入っていたアプリが古く、同梱フォントを読むコードを持っていなかった**
（`同梱フォント: …` のログが Info も Warn も出ていなかった）。共有 `fonts/` は空だったので、
受け皿は端末の比例フォントに落ちていた。

```
（修正前）andEmuera: フォールバックフォント: 97##fallback
（修正後）andEmuera: 同梱フォント: BIZ UDGothic
          andEmuera: 受け皿に採用: BIZ UDGothic (APK 同梱・等幅)
          andEmuera: フォールバックフォント: BIZ UDGothic
```

入れ直して eraTOWN を起動し、セーブ一覧の日時・名前・日数が縦に揃うこと、
ロード後の体力／気力バーと数値が PC と同じ位置に出ることを確認した。等幅警告の tip も出ない。

## フォントに無い記号が豆腐になる — Skia は font linking をしない (2026-08-01)

キャラ詳細画面の `（成長✕）` の `✕` が □ で出ていた。
文字コードの問題ではなく**グリフ欠け**で、原因は描画層の性質の違いにある。

| | 文字を持っていないとき |
|---|---|
| Windows の GDI+ / `TextRenderer` | font linking で勝手に別フォントへ回す |
| Skia の `SKCanvas.DrawText` | **何もしない**。`.notdef` (豆腐) をそのまま描く |

つまり **PC では出るのに端末では出ない**という形で現れる。
`Compat/Drawing/Graphics.cs` の `DrawString` が `canvas.DrawText` 1 発だったため、
上流の描画は全部ここを通って豆腐になっていた。

### どれだけあるか

同梱フォント (`font/BIZUDGothic-Regular.ttf`) の cmap と erablue_resort の
ERB / CSV を突き合わせた結果 (`--selftest-glyph`):

```
走査 5864 ファイル — フォントに無い文字 166 種 / 延べ 3893 文字
  U+2715 ✕    400 回  → Meiryo UI
  U+7C7B 类    372 回  → Yu Gothic UI
  U+8BBE 设    248 回  → Microsoft JhengHei UI
  ...
  U+2764 ❤     84 回  → Segoe UI Emoji
```

BIZ UDGothic のカバレッジは 11,741 グリフで JIS の範囲はほぼ埋まっているが、
`✔✕✖⚠⚡` `█░`・絵文字・簡体字は持っていない。
一部の CSV (`不徳のギルド` の EX キャラなど) が簡体字を含んでいるのが効いている。

### 送り幅はセルにスナップする

素直に代替フォントで描くと、その字の送り幅がそのまま入って**桁が動く**。
前節のとおり era は PRINTC の桁を実測幅で作るので、ここは譲れない。

`Compat/Drawing/GlyphFallback.cs` を足し、欠けた文字は

- **主フォントの半角セル 1 個ぶん、または 2 個ぶん**の幅に固定する
- どちらにするかは**代替グリフ本来の送り幅**で決める
  (半角セルの 1.15 倍までなら半角、超えたら全角)
- セルからはみ出す字だけキャンバス変換で横に詰める。
  `SKFont.ScaleX` を書き換えないのは、フォントをキャッシュして共有しているため

EAW (East Asian Width) の表で決め打つ手もあるが、そちらは
`✕` `❤` が N 判定で半角に潰れる一方、実フォントの字幅を見る方式なら
「漢字は全角・記号は自然な幅」に落ちる。**計測と描画が同じ判定を通る**限り
どちらでも桁は守れるので、見た目が素直なほうを採った。

### 計測と描画は同じ 1 本を通す

ここがズレると `GetDisplayLength` を基準にしているボタン幅・行の折り返し・
クリック当たり判定がまとめて狂う。入口が少ないのが幸いした:

| 場所 | 変更 |
|---|---|
| `Graphics.DrawString` | 主フォントで全部描けるときだけ従来の 1 発。欠けがあれば `GlyphFallback.Draw` |
| `Graphics.MeasureString` / `MeasureCharacterRanges` | `GlyphFallback.Measure` |
| `WinForms/TextRenderer.MeasureText` | 同上 |
| `Drawing/Font.cs` | 半角セル幅 `HalfCell` (スペースの送り幅) を遅延キャッシュ |
| `MainActivity.SetupFonts` | 代替の解決結果を logcat に流す (1 コードポイント 1 回) |

`Measure` と `Draw` は `GlyphFallback.Walk` という**同じ走査**を通り、
`canvas` が null かどうかだけが違う。幅の計算式が 1 箇所しかないので原理的にズレない。

グリフの有無はタイプフェイスごとに BMP 分の `byte[0x10000]` にメモしている。
2 回目以降は配列添字なので、**全部描ける文字列は従来と同じ経路に落ちる**。

### 検証

- `--selftest-glyph <ゲームフォルダ> [--shot <png>]` を追加した。
  欠け文字の洗い出しに加えて、
  「送り幅が半角セルか全角セルちょうど」「その字を挟んでも前後がずれない」
  「欠けの無い文字列は等幅のまま」を assert する。166 種すべてに代替が見つかった
- **欠け文字を含まない画面は変更前後で画素完全一致**。`--capture` のハッシュで確認した
  （キャラ詳細画面は画像をランダムに選ぶので、連続実行しても一致しない。
  比較に使うならタイトルとキャラメイクの決定的な画面を選ぶこと）
- フル描画の実測は中央値 3.1ms → 3.2ms で、走査ぶんは誤差に埋もれる
  (`--bench`、1600x1129)。`ANDEMUERA_NO_GLYPH_FALLBACK=1` で切って A/B できる
- 実機 (SC-55E) でセーブをロードし、`[124] 干渉力強化` の説明ボックス
  (`干渉操作関連_干渉種類.ERB`、`［干渉力のランク✕２０］%`) で ✕ が出ることを確認した。
  logcat には `代替フォント: ✕ U+2715 → 96##fallback` が 1 回だけ出る。
  端末側の代替は Samsung の内部フォント (CJK 用の `97##fallback` とは別の枝) だった

## セーブデータは PC からそのまま持ち込める

`sav` フォルダを `adb push` するだけで、PC で遊んでいた続きを実機で再開できることを確認した。

- セーブ一覧に日時・キャラ名・職業・日数が正しく表示される
- ロードするとゲーム本編に入り、所持金・体力・気力が復元される
- 体力／気力のバー (GCREATE / SPRITECREATE による画像合成) も正しく描画される

バイナリ形式のセーブだが、`BinaryWriter`/`BinaryReader` ベースで ARM も little endian のため
プラットフォーム間で互換性がある。文字列は CodePagesEncodingProvider を登録済みなので Shift_JIS も読める。

注意: 同名ファイルは上書きになる。端末側で遊び進めている場合は先に退避すること。

## Windows 専用 API: StrConv と Process.Start

erablue_resort のセーブをロードすると、キャラクター整合性チェックの直後に落ちた。

```
通常衣装関連/服データ\/CLOTHES_汎用服データ.ERB の 88 行目で予期しないエラー
System.PlatformNotSupportedException: Operation is not supported on this platform.
   at Microsoft.VisualBasic.Strings.StrConv(String str, VbStrConv Conversion, Int32 LocaleID)
```

**`Microsoft.VisualBasic.Strings.StrConv` は Windows の NLS API に依存**しており Android では例外になる。
era のスクリプトはひらがな⇔カタカナ・半角⇔全角の変換に使う (2 箇所)。

`src/Emuera.Core/Compat/Platform/JapaneseText.cs` に自前実装を書いて置き換えた:

| 関数 | 相当 | 実装 |
|---|---|---|
| `ToKatakana` | `VbStrConv.Katakana` | ぁ〜ゖ を +0x60 |
| `ToHiragana` | `VbStrConv.Hiragana` | ァ〜ヶ を -0x60 |
| `ToWide` | `VbStrConv.Wide` | ASCII → 全角、半角カナ → 全角カナ (濁点・半濁点は 1 文字に合成) |
| `ToNarrow` | `VbStrConv.Narrow` | その逆 (濁音は清音 + 濁点に分解) |

同じ理由で `Process.Start(UseShellExecute = true)` も 1 箇所残っていた (UPDATECHECK の URL を開く処理)。
`BuiltinPlugins.OpenUrl` に委譲し、`LAUNCH_BROWSER` と同じ経路 (Android の Intent) を通すようにした。

**教訓**: WinForms / System.Drawing は型が無いのでコンパイル時に気づけるが、
`Microsoft.VisualBasic` や `Process.Start` は **型もメソッドも存在するのに実行時に落ちる**。
ビルドが通っても安心できない類の依存として、実データで通しておく必要がある。

## フォルダ名の大文字小文字

検索パターン (`*.CSV`) だけでなく **フォルダ名そのもの** も Android では区別される。
`eraTOWN` は `CSV` / `ERB` が大文字で、そのままでは候補にも挙がらず、
`Program.CsvDir` も存在しないパスになっていた。2 箇所を非依存に修正:

- `Program.SetDirPaths` … 実在するほうの表記を採用する `ResolveDir` を追加
- `MainActivity.FindGames` … 候補判定を `FindSubDir` 経由に

## バックログのスクロールが未移植だった (2026-07-31)

「操作性をもう少し」という指摘から調べたところ、**フリックでのログ送りが最初から効いていなかった**。

`EmueraEngine.Scroll` は `EmueraConsole.MouseWheel` を呼ぶだけだが、これは冒頭で

```csharp
if (!IsWaitingPrimitive)   // INPUTMOUSEKEY 待ちのときだけ反応する
    return;
```

で抜ける。**通常のログ送りは EmueraConsole ではなく MainWindow 側の処理**
(`richTextBox1_MouseWheel`) で、`vScrollBar.Value` を動かして `console.RefreshStrings(force)` を呼ぶ。
ここが移植から漏れていた。

`EmueraEngine` に移植し直した:

| API | 内容 |
|---|---|
| `ScrollLines(lines, x, y)` | 正で過去へ。`INPUTMOUSEKEY` 待ちならホイールとして実行側へ渡す |
| `ScrollToLatest()` | 最新行へ戻す |
| `ScrollState` | `(Value, Max)`。画面側が「バックログ中」を判定する |
| `LineHeight` | 画面側がフリック量を行数へ換算するのに使う |

- スクロールの単位は**表示行**。`ScrollBar.Maximum = displayLineList.Count` で、
  `Value` が最下表示行 + 1（`EmueraConsole.verticalScrollBarUpdate` が更新する）
- `RefreshStrings` は `msPerFrame` 未満の再描画を握り潰すので、スクロールは常に `force: true` で呼ぶ
- 動かなかったとき (端に到達) は redraw を出さず `{t:"scrollstate"}` だけ返す。
  1790x3629 の PNG を無駄に再エンコードしないため

## タッチ操作の割り当て

| 操作 | 動作 |
|---|---|
| タップ | クリック |
| 長押し 500ms | 右クリック。メッセージ待ちでは `mesSkip` が立ってスキップになる |
| ⏩ ボタン | 長押しと同じ右クリック (`{t:"skip"}` → `EmueraEngine.MessageSkip` → `Click(Cursor.Position, right)`)。ボタン自身は座標を持たないが、上流の右クリックが「カーソルのある場所で右ボタンを押す」ものなので、`SetPointer` が控えている**最後に触った位置**で押したことにする。選択肢の上にポインタが残っていればその選択肢への右クリックになり、`INPUTMOUSEKEY` 待ちでもポインタ位置がそのまま入力値になる |
| 1 本指 縦ドラッグ | 拡大中は余白ぶんパン → 端に達したらログ送りへ引き継ぐ |
| 1 本指 横ドラッグ | パン (拡大中のみ) |
| ピンチ | 拡大 (1〜6 倍、2 指の中点を固定) |
| 🔍 ボタン | フィット ⇄ 等倍。**下端中央**を固定するので、拡大直後に選択肢が目の前に来る |
| フリック離し | 慣性 (rAF で減衰、端で停止) |

### 拡大は transform: scale ではなく img の CSS 幅を変える

`scale` だと元画像を引き伸ばした絵になり文字がぼやける。レイアウト幅を変えると
ブラウザが元データから描き直すので鮮明になる。ピンチ中だけは追従を優先して `scale` で先に見せ、
指を離した時点で幅に畳む（原点が左下なので畳んでも見た目は動かない）。

### 指への追従は「先行表示」で作る

1 行送るたびにサーバー往復を待つとフリックがカクつく。ドラッグ量を行数に換算し、
**整数行ぶんだけ送って、行に満たない端数は `translate` で先に見せる**。
スクロールは既存の内容が行高ぶん平行移動するだけなので、先行表示と実際の描画はほぼ一致する。
要求は 1 つずつ (in-flight 1 本) に絞り、返事が来るまでの行数は溜めてまとめて送る。

### ダブルタップ拡大は入れていない

ダブルタップを判定するには 1 回目のタップを 300ms 待たせる必要があり、
メッセージ送りのたびに遅延が乗る。拡大は 🔍 ボタンとピンチに寄せた。

### 誤タップ

- `touchend` でクリックを送ったあと、ブラウザが互換の `click` を投げてくることがある。
  `pointerType` の判定だけでは WebView の版によって漏れるので、**時刻でも弾く** (700ms)
- `#screen` は `pointer-events: none` + `-webkit-touch-callout: none`、`contextmenu` は握り潰す
  （長押しで「画像を保存」が出ないように）
- WebView 標準のページズームは無効化した (`BuiltInZoomControls = false` / `SetSupportZoom(false)`)。
  操作バーごと拡大されるうえ、`touch-action: pinch-zoom` の下では 1 本指パンが効かなかった

### 描画サイズの通知はタイマーで粘る

`resize` を `requestAnimationFrame` で送っていたが、**WebView が裏に回っていると rAF が来ない**
（`document.visibilityState === 'hidden'` の間は発火しない）。この場合ゲームは起動時のサイズのまま
固定され、二度と直らない。レイアウトが測れるまで `setTimeout` で送り直すようにした
(`ResizeObserver` も併用)。

## 操作状態の可視化 (2026-08-01)

「操作ができていないのか処理待ちなのか分からない」という指摘への対応。

画面は 1 枚の PNG が貼り替わるだけなので、**タップしても絵が変わらないときに原因が見えない**。
実際には少なくとも 5 通りある。

| 実際に起きていること | 判定に使うもの |
|---|---|
| スクリプト実行中でタップが捨てられた | `EmueraConsole.IsInProcess` |
| 選択肢の外をタップした | `EmueraConsole.SelectedString == null` |
| バックログを遡っていて最新行に戻っただけ | `ScrollBar.Value != Maximum` |
| 値入力待ちで、画面タップではなく入力欄が要る | 最新世代のボタンが 0 個 |
| 強制待ち | `InputType.Void` |

### サーバーが送るもの

| メッセージ | 中身 |
|---|---|
| `{t:"state", v, n}` | いまの待ち状態と、選べる選択肢の数。変化したときだけ送る |
| `{t:"tap", id, v}` | タップ 1 回ごとの結果。`id` はクライアントが付けた連番 |

`v` (state) は `busy` / `enter` / `integer` / `string` / `any` / `mouse` / `void` /
`error` / `nomouse` / `none` / `loading`。従来の `{t:"mode"}` を置き換えたもので、
テンキーを出す判定にもそのまま使っている。

`v` (tap) は `accepted` / `notarget` / `backlog` / `busy` / `disabled`。
判定は上流の `mainPicBox_MouseClick` を写した `MainWindow.HandleClick` の分岐がそのまま持っており、
**どの分岐にも入らずに末尾へ落ちたら `NoTarget`** ＝「入力待ちなのに選択肢の外を押している」。

### 「処理中」は gate に入る前に送る

スクリプトは **WebSocket の受信スレッド上で `lock (gate)` を握ったまま同期実行される**
(`HandleMessage` → `MainWindow.PressEnterKey` → `EmueraConsole.PressEnterKey`)。
処理に入ってしまうと画面へ何も言えなくなるので、`click` / `submit` / `enter` / `skip` は
**ロックを取る前に** `{t:"state", v:"busy"}` を投げてから処理へ入る。
送信自体は `WsClient` のポンプが別スレッドで捌くのでブロックしない。

状態通知は `generation` を増やさないので、PNG の再エンコードも転送も誘発しない。

### 選択肢の数え方

「選択肢をタップ」と「入力欄に数値を」を出し分けるために、いま選べるボタンの数が要る。
上流が入力値からボタンを探すときと同じ手順 (`EmueraConsole` の `InputInteger` 経路) を写した
`EmueraEngine.SelectableButtonCount()` を使う。表示行を後ろから見て
`button.Generation == console.LastButtonGeneration` を数え、**世代の違うボタンに当たったら打ち切る**。
`DisplayLineList` も `LastButtonGeneration` も上流で `public` なので新しいパッチは要らない。

### 処理中のタップは破棄する

上の構造から、**処理中に送ったタップはソケットに溜まり、処理が終わった後の新しい画面に対して
適用される**。選択肢画面に切り替わった瞬間に意図しない項目を確定してしまう。

対策として、画面側は入力を送った瞬間に `localBusy` を立て、その間の
`click` / `submit` / `enter` / `skip` を**送らずに橙の波紋だけ返す**。
サーバーの `busy` を待たないのは、往復の隙間で連打が抜けるため。
解除はサーバーから `busy` 以外の `state` が来たとき。
保険として、サーバーからのメッセージが 20 秒途切れたら待ちを打ち切る
(スクリプト実行中も `RefreshStrings` 経由で `redraw` が流れてくるので、実際には無音のときしか発火しない)。

代償として、**連打でメッセージ送りを先送りすることはできなくなった**。
誤爆を防ぐほうを取っている。

### 見せ方

`#screenWrap` の中に `position: absolute` で重ねる。どれも `pointer-events: none` なので
タップ判定 (`#screenWrap` のリスナ) には触らない。レイアウトを変えないので
`sendResize` の計測にも影響しない。

| 要素 | 役割 |
|---|---|
| `#pill` (左上・常時) | ドット + 文言。`処理中…` / `選択肢をタップ` / `数値を入力 — ⌨ から` / `待機中 — 操作できません` / `バックログ表示中` / `切断 — 再接続中…` |
| `#busy` | 処理が **200ms** を超えたら薄暗くしてスピナー。1 秒を超えたら経過秒 |
| `#ripples` | タップ位置の波紋。結果が返ったら色を差し替える (緑=通った / 赤✕=空振り / 青▼=最新へ戻した / 橙=処理中で破棄) |

`#status` (トースト) はピルと重ならないよう `top: 34px` へ下げた。

細かいが効いた点:

- 状態が `busy` のときは `applyInputMode` を素通りさせる。処理中は一瞬の通過点でしかないので、
  ここで `textBox.inputMode` を組み替えると**タップのたびにソフトキーボードが作り直される**
- 波紋は `animationend` だけで消すと、**裏に回っている間はアニメーションが進まず溜まり続ける**。
  時間 (1.5 秒) でも消す
- 選択肢が 0 個の値入力待ちで入力欄が閉じているときは `#toggle` (⌨) を光らせる。
  「画面をタップし続けても何も起きない」で詰まるのを防ぐ

## 高速化 (2026-08-01)

「動作が重い」という指摘から、描画・転送パイプラインと起動処理に手を入れた。
**画面のピクセルは変えない**ことを条件にしたので、PNG のままで可逆な範囲に限っている。

### 1 タップで 5.5MPix を 4 回描いていた

手を入れる前、選択肢を 1 回タップすると起きていたこと:

| 段 | 回数 | 理由 |
|---|---|---|
| フル `OnPaint` | 4 | 画面側が `move` と `click` を別々に送り 2 世代進む × 各世代で描画 2 回 |
| 22MB のピクセルコピー | 4 | `SKImage.FromBitmap` は mutable な `SKBitmap` を丸ごと複製する |
| PNG エンコード | 2 | しかも Skia の既定 = 毎行 5 種のフィルタを試す + zlib 6 |
| TCP 新規接続 | 2 | `Connection: close` 固定 |
| 画面側のデコード | 2 | |

#### `move` は最初から要らなかった

`EmueraEngine.Click` は中で `console.MoveMouse(point)` を呼んでいる
（選択中のボタンはポインタ位置で決まるため）。つまり画面側が別途送っていた `move` は完全に冗長で、
その 1 通が「フル描画 + フルエンコード + 転送 + デコード」を丸ごと 1 回焼いていた。

サーバー側も `MoveMouse` の戻り値を捨てて `changed = true` のままにしていた。
上流の戻り値は `この後でRefreshStringsが必要かどうか` なので、そのまま `changed` に使う
（PC のマウス移動でボタン外を通っても再エンコードしない）。

#### 二重描画 — ただし「描画時に表示状態が確定する」ので注意が要る

`RenderPng()` が無条件に `RenderOffscreen()` を呼んでいた。直前の `RefreshStrings` で
同じ内容を描き終わっているのに、もう一度 5.5MPix を描いていた。

素直に消せないのは、`EmueraConsole.escapedParts` が **`OnPaint` の中でしか更新されない**ため
（前述「表示状態は描画時に確定する」）。`BINPUT` がこれを読むので、
「描画を減らす」は「参照される時点では必ず描かれている」保証とセットにする必要がある。

`MainWindow` に dirty フラグを持たせた:

| 場所 | 扱い |
|---|---|
| `MarkDirty()` | 状態を変えうる公開 API (`Click` / `SubmitInput` / `Scroll` / `Resize` ほか) の先頭 |
| `EnsureRendered()` | dirty のときだけ描く。`RenderPng` / `SaveScreenshot` / `HandleMessage` の入口 |
| `RenderOffscreen()` | 描き切ったときだけ dirty を落とす。再入で早期 return した回は落とさない |

**上流 `RefreshStrings` の 3 つの早期 return にも `window.MarkDirty()` を足した。**
「描かずに戻る」＝バックバッファが表示状態より古くなるので、印を残さないと
60fps スロットルに当たった最後の更新が画面に出ないことがある。
これで「上流が描いた直後なら 0 回、描いていなければ 1 回」になり、**元の挙動より少なくならない**。

検証は `EnsureRendered` を「常に描く」に一時改造した版とキャプチャを突き合わせ、
5 画面すべてで生ピクセルの SHA-256 が一致することを確認した。

#### PNG エンコードは既定が一番遅い設定だった

`Image.Save(Stream, ImageFormat)` は `SKImage.FromBitmap` → `Encode(format, 100)` →
`MemoryStream` → `ToArray()`。PNG では quality は無視され、
**`AllFilters`（毎行 5 種のフィルタを試す）+ `ZLibLevel 6`** という最遅設定が使われる。

`Bitmap.EncodePng(filter, zlibLevel)` を足した。`PeekPixels()` はピクセルバッファを
指すだけなのでコピーが消え、フィルタと圧縮レベルを選べる。
`Save` 自体は上流の SAVEIMAGE 系が通るので触っていない。

`Emuera.TestHarness --verify-encoders <ゲームフォルダ>` で総当たりできる。
**全設定の出力をデコードし直して画素完全一致を assert する**ので、
「バイト列は変わるが表示は変わらない」ことを機械的に確かめられる。

erablue_resort / 1600x1129 の実測 (キャラメイク 2 画面目):

```
設定            ms      KB   対 既定
All/6         38.0     141     1.00   ← Skia の既定 = 従来
All/2         26.9     154     1.09
Up/6          19.4     150     1.06
Up/2           7.8     159     1.13
Sub/2          8.1     149     1.05
None/6        18.3      77     0.55
None/3         7.5     115     0.82   ← 採用
NoFilters/1   26.8     158     1.12   ← 0 は「フィルタ指定なし」で既定に落ちる
```

**フィルタを `None`（無変換のみ）にすると速いうえに小さい。**
era の画面は平坦な背景 + 文字なので、行ごとに差分を取るより zlib にそのまま任せた方がよい。
`None/3` を既定にして **エンコード 5 倍速 + 18% 小型化**。

#### 内容が同じフレームを作らない

世代番号は「操作があった」だけで進むので、中身が同じでも再エンコードしていた。
`SETANIMETIMER` は erablue_resort が実際に使っており (`erb/` 配下 10 ファイル、値 25 / 50)、
アニメが実質止まっていても 20〜40fps で回り続ける。

エンコード直前にピクセルのハッシュ (`Api/FrameHash.cs`、4 レーンの xor-multiply) を取り、
前回と一致したら再エンコードも送信も省く。**実測でハッシュ 0.6ms 対エンコード 17ms** なので割に合う。
`RenderOffscreen` の中には入れていない（上流の force refresh 30 箇所ぶん払うことになる）。

#### アニメ用タイマーが排他なしで描いていた

`Compat/WinForms/Controls.cs` の `Timer` シムは `System.Timers.Timer` ＝ スレッドプール発火で、
`tickRedrawTimer` はロックを取らずに `window.Refresh()` を呼んでいた。
スクリプト実行中の `displayLineList` / `escapedParts` を並行に読み書きし、
`painting` フラグは非 volatile かつ「競合したら描かずに return」なので、
**描きかけのバッファをエンコードして返す**経路があった。

`IWindowHost` に default interface method で `TryRunExclusive` を足し、
`MainWindow.Refresh()` / `Invalidate()` をこれ経由にした。WebHost 側は
`Monitor.TryEnter(gate)`（**タイムアウト 0 で待たない**）で実装している。

- 待たないのでロック順序の逆転もデッドロックも起きない
  （`endTimer` が別スレッドから `RunEmueraProgram` を呼ぶ既存の危険を踏まずに済む）
- 通常経路は同一スレッドの再入なので `Monitor` が再帰的に通し、挙動は元のまま
- 取れなかった回は `MarkDirty()` だけ残す

### 画面は HTTP で取りに行かず WebSocket で押し出す

`redraw` を WS で通知 → 画面側が `GET /screen.png` → **1 フレームごとに TCP 新規接続**
という往復だった。しかもサーバーは `?v=` を無視して現在の世代を返すので、
連続描画の末尾で同一バイト列をもう 1 回取りに行っていた。

`Connection: keep-alive` ではなく **WS のバイナリフレームで push** に変えた。
往復が消えること自体より、**サーバー側でフレームを合流できる**ほうがフリックに効くため。

| 仕掛け | 効果 |
|---|---|
| 1 接続 = 1 送信ポンプ (`WsClient`) | 同一 `WebSocket` への並行 `SendAsync` を構造的に排除。**元の fire-and-forget は `InvalidOperationException` を投げうるが catch されておらず、たまたま落ちていなかっただけ** |
| 画像は `pendingImage` 1 枚だけ保持 (latest-wins) | 送信待ちの古いフレームは捨てる |
| `ack` によるバックプレッシャ | 画面が表示し終えるまで次を作らない ＝ 誰も見ないフレームを焼かない。2 秒のウォッチドッグつき |
| 16 バイトの自己記述ヘッダ | `'E','M'` + 世代 + スクロール位置。JSON と順序を対応付けない |

画面側は `<img>` のまま（canvas 化しない）。拡大時に元データから描き直させて文字を鮮明に保つため。
`Blob` → `URL.createObjectURL` → 裏の `new Image()` でデコード → `onload` で差し替え → **1 つ前を revoke**。
先に revoke すると今表示中の `src` が無効になる。`onerror` でも必ず `ack` を返すこと
（返さないとサーバーが次を作らなくなる）。

`?legacy=1` で従来の `/screen.png` 取得に戻せる。画面側は接続直後に `{t:"hello", binary:…}` を送り、
サーバーはそれを見て push するかどうかを決める。`GET /screen.png` は `adb forward` での
切り分け用に残してある。

### 計測できるようにした

| 手段 | 内容 |
|---|---|
| `GET /stats` | `paintsPerGen` / `encoded` / `skipped` / `renderMs` / `hashMs` / `encodeMs` / `inputWaitMs` ほか |
| `--verify-encoders` | 全エンコード設定を画素一致で検証しつつ速度と大きさを出す |
| `--bench` | `EnsureRendered` 単体・`HashBackBuffer`・各エンコード設定 |
| `--capture <出力先>` | 入力を進めながら各段階の PNG と**生ピクセルの SHA-256** を `hashes.txt` へ |
| `--compare a.png b.png` | デコードして画素比較。最初の差分座標を出す |

**表示互換はバイト列ではなく画素で見る。** zlib レベルやフィルタを変えればバイト列は当然変わる。

### 起動時間

上流には `time.log` の仕組みがあるが、**2 つのバグで常に 0 バイトだった**:

```csharp
if (Config.DisplayReport)
{
    using var fs = new FileStream(Program.ExeDir + "time.log", FileMode.OpenOrCreate);
    logWriter = new StreamWriter(fs);   // ← 直後に fs が閉じる
}                                       // ← 以後 Flush も Dispose もされない
```

`StreamWriter` の既定バッファは 1024 文字で出力は 700 文字程度。**一度も溢れないので例外も出ない。**

`using` を外して `AutoFlush`、最後に `Dispose` するようにし、**条件も `Program.BootProfile` に分離した**。
`Config.DisplayReport` は ERB 1 本ごとの画面出力も有効にし、`PrintSystemLine` →
`RefreshStrings` → フル描画を誘発する。**計測したいものより重い処理が足されてしまう。**

```bash
ANDEMUERA_BOOT_PROFILE=1 dotnet run --project src/Emuera.TestHarness -- <ゲームフォルダ>
```

実機では `adb pull .../games/<名前>/time.log`。

#### resources の再帰走査が起動時に 2 回走っていた

`AppContents.LoadContents` は `Directory.EnumerateFiles` の結果を
`foreach` してから `AsParallel()` に流していた。`EnumerateFiles` は遅延列挙なので**再列挙される**。
しかも `foreach` の中身はループ変数を使わず、`reload == false`（起動時）には完全な空ループ。

**実データは 155,268 ファイル / 2,740 フォルダ**で、`GetExternalFilesDir` 配下は
Android の FUSE 越し。`if (reload)` をループの外に出すだけで走査が半分になる。

#### CHARA*.csv を 1 本読むたびに全件ソートしていた

`loadCharacterDataFile` の末尾に `CharacterTmplList.Sort(...)` があり、
**ファイル 1 本読むたびに、それまでの全テンプレートを並べ替えていた**。
CHARA が 2,999 本ある構成では Σ n·log n ≒ **5,200 万回の比較**。

`loadCharacterData` 側へ移して 1 回だけにした。番号が重複したときの前後関係を
実行ごとにぶれさせないよう、`List.Sort`（不安定）ではなく安定ソートで並べる。
`GetCharacterTemplate` の `BinarySearch` がソート済みを前提にしているので、
読み終えた後に 1 回並べれば足りる。

PC 実測で CSV 段が **351ms → 222ms**。

#### ERB 2,697 本ぶんの continuation が UI スレッドへ post されていた

`ErbLoader` は `await Task.Run(() => loadErb(...))` を **1 本ずつ await** する（並列度 1）。
`ConfigureAwait(false)` はリポジトリのどこにも無く、`MainActivity.OnCreate`（UI スレッド）から
`await` が繋がっているので、**継続が 2,697 回 Android のメインルーパへ post される**。

`EmueraWebHost.StartAsync` で `Task.Run` を一段挟んで `SynchronizationContext` を切った。
描画はオフスクリーンの SkiaSharp なので UI スレッド固有の要求は無い。
**PC のコンソールアプリでは `SynchronizationContext` が無いので再現しない Android 固有のコスト。**

#### そのほか

- `FindGames` の ERB 本数カウントは `RecurseSubdirectories = true` で `erb/` 配下を
  丸ごと再帰列挙していた（実データで 2,867 ファイル / 655 フォルダ）。用途は選択画面の表示だけで、
  しかも `OnCreate` の同期実行なので**その間ずっと画面が真っ黒**だった。
  一覧を出してから裏で数えるようにした
- ロード完了後に `Preload.Clear()`。**65MB / 約 140 万本の `string`** が常駐したままだった。
  `OpenOnCache` を使うのは ERB / ERH / CHARA のローダだけで、
  `ReloadErb` は自分で `Preload.Load` をやり直すので影響しない
- `_Rename.csv` の正規表現 `\[\[.*?\]\]` を **ERB の全行 (845,461 行)** に当てていた。
  `"[["` を含まない行は絶対にマッチしないので事前に弾く
  （上流が理由不明でコメントアウトしていた最適化。判定は `Ordinal` 固定）

### 効き方

1 タップあたりのサーバー仕事量:

| | 従来 | 現在 |
|---|---|---|
| フル `OnPaint` | 4 回 | 1 回（上流が描いていれば 0 回） |
| 22MB のピクセルコピー | 4 回 | **0 回** |
| PNG エンコード | 2 回 (38ms/枚) | 1 回 (7ms/枚)、無変化なら 0 回 |
| TCP 接続 | 2 回 | **0 回** |
| 画面側のデコード | 2 回 | 1 回 |

`/stats` の実測 (1600x2691 ≒ 4.3MPix、スマホ相当の縦長):
`paintsPerGen 0.71` / `renderMs 0` / `hashMs 0.61` / `encodeMs 16.8` / 画面側の復号 2ms。

## 「処理中」が長い — 捨てフレームと背景の全画素コピー (2026-08-01)

「特定の重い処理で処理中が長い」という指摘。前回の高速化は**転送パイプライン**を潰したもので、
**gate の中**（＝「処理中」ピルが出ている区間そのもの）には手が入っていなかった。

### まず 1 入力の内訳を測れるようにした

「処理中」が消えるのは**スクリプトが終わった瞬間**（`HandleMessage` が `lock (gate)` を抜けて
`SendState()` を送る時点）で、エンコードも転送もその外側にある。
つまり体感の待ち時間は全部 gate の中なのに、そこの内訳を出す手段が無かった。

| 追加した窓 | 中身 |
|---|---|
| `GET /stats` の `lastInput` | `totalMs` / `scriptMs` / `paintMs` / `paints`。直近 32 回の最大・中央値も |
| `MainWindow.PaintMs` | フル描画の累計時間。前後で差を取れば「その入力で何 ms 描いたか」 |
| TestHarness の `--input` | 1 操作ごとに `処理 Xms = スクリプト Yms + フル描画 N 回 Zms` |
| TestHarness の `--size 1600x2691` | 端末相当の縦長で測る。**画素が 2.4 倍あり描画の比重が変わる**ので、PC 既定のままでは判断を誤る |
| `ANDEMUERA_MEASURE_PROFILE=1` | 文字幅計測 (`GlyphFallback.Measure`) の回数・文字数・時間 |

### 1 コマンドで 13 回描き、12 回は誰も見ないフレームだった

erablue_resort の「能力表示」1 回（1600x2691）:

```
処理 169.7ms = スクリプト 84.0ms + フル描画 13 回 85.6ms
```

**待ち時間の半分が描画**で、しかもそのうち 12 回は画面に出ない。
スクリプトは WS 受信スレッドが `gate` を握ったまま同期実行されるので、
**実行中は転送側 (`ProduceFramesAsync`) が `lock (gate)` に入れず 1 枚もエンコードできない**。
上流は `PRINTC` のたびに `RefreshStrings` を呼ぶので、60fps スロットルを通った回だけ
`window.Refresh()` → フル `OnPaint` が走り、その結果は次の描画で上書きされて消える。

`MainWindow.RepaintNow()` で**実行中は描かず `MarkDirty()` だけ**にした。
実際の描画は既存の `EnsureRendered()`（転送直前・次の入力の入口）に任せる。
通知 (`RequestRedraw`) は従来どおり出すので、転送側は gate が空いた瞬間に最新状態を 1 回だけ焼く。

唯一の条件が**描画時に確定する `escapedParts`**。参照元は BINPUT 系 4 箇所だけなので、
`EmueraConsole.EscapedParts` の getter で `window.EnsureRendered()` を呼ぶ 1 行を上流に足した
（`RefreshStrings` の早期 return に `MarkDirty()` を足したのと同じ考え方）。
`ANDEMUERA_EAGER_PAINT=1` で従来動作に戻せる。

### 背景を毎描画まるごとコピーしていた

`OnPaint` は毎回 `graph.DrawImage(bakedBackground, 0, 0)` を通る。
その先の `Graphics.DrawImage` は `SKImage.FromBitmap` で、**mutable な SKBitmap を丸ごと複製する**。
前回 `RenderPng` から追い出したはずの「22MB のピクセルコピー」が、**描画側に残っていた**
（背景は画面と同じ大きさ。1600x2691 で約 17MB／描画）。CBG・スプライトも同じ経路。

`Image` に `SKImage` を持たせ、`SKImage.FromPixels(PeekPixels())` で**参照だけ**作るようにした
(`EncodePng` が `PeekPixels` を使っているのと同じ理屈)。画素を書き換える入口
(`Graphics.FromImage` / `LockBits` / `UnlockBits` / `SetPixel` / `MakeTransparent` / `Dispose`) で捨てる。
**描画先と同じビットマップを描画元にするときだけ**従来どおり複製する
（raster は転送元と転送先が重なる場合を保証しない）。`ANDEMUERA_NO_IMAGE_CACHE=1` で従来動作。

フル描画 (`--bench` の `EnsureRendered` 中央値、1600x2691): **7.1ms → 4.5ms**。

### 測って**やめた**もの

- **文字幅計測のキャッシュ**。`StringMeasure` はノーキャッシュで、`PRINTC` は
  「スペースを 1 個ずつ剥がしては測り直す」ので効きそうに見えるが、実測は
  **コマンド画面 800ms 中 5.7ms (88 回)** で 1% に届かない。計測用のカウンタだけ残した
- **SKPaint の色ごとの共有**。1 画面で数百個作っているが、フル描画 4.5ms に対して差が出なかった。
  共有した可変オブジェクトを持ち回るぶん危ないだけなので戻した

### 効き方 (erablue_resort / 1600x2691 / PC)

| 操作 | 従来 | 現在 | 内訳 |
|---|---|---|---|
| 能力表示コマンド | 169.7ms | **97.1ms** | 描画 13 回 85.6ms → 2 回 8.9ms |
| 選択肢のタップ | 24.0ms | **14.1ms** | 描画 2 回 12.3ms → 1 回 3.6ms |
| メッセージ送り | 14.0ms | **5.5ms** | 描画 2 回 13.3ms → 1 回 4.8ms |
| セーブのロード (14MB) | 3522ms | 3157ms | 描画 47ms → 5.3ms。**残りは全部スクリプト** |
| コマンド一覧の表示 | 886ms | 906ms | 同上。スクリプト律速で誤差の範囲 |

### 実機での実測 (SC-55E / 描画 1600x1116)

`debug.mono.env` に退避用の環境変数を入れれば、実機でも同じ A/B が取れる。

```bash
adb shell setprop debug.mono.env "'ANDEMUERA_EAGER_PAINT=1|ANDEMUERA_NO_IMAGE_CACHE=1'"
adb shell am force-stop rip.eragames.andemuera   # 反映は再起動から
adb forward tcp:8399 tcp:<Logcat に出るポート>   # あとは PC から GET /stats
```

| 操作 | 従来 | 現在 |
|---|---|---|
| 能力表示コマンド | 1109ms（描画 13 回 **35.3ms**） | 1122ms（描画 2 回 **7.6ms**） |
| コマンド一覧の表示 | 3682ms（描画 3 回 16.5ms） | 3298ms（描画 2 回 9.7ms） |
| メッセージ送り | 25.1ms | **13.0ms**（描画 1 回 8.1ms） |
| セーブのロード (14MB) | 21058ms（描画 7 回 20.8ms） | 21669ms（描画 1 回 2.9ms） |

**描画は狙いどおり 1/4〜1/5 になったが、この端末では総時間がほとんど動かない。**
重い場面は**スクリプトが 97%** で、描画は誤差に埋もれる。

理由は画面の大きさ。この端末は内側画面がほぼ正方形で描画領域が 1600x1116 (1.8MPix) しかなく、
**フル描画 1 回が 2〜4ms** で済む。PC で縦長 (1600x2691 / 4.3MPix) を再現したときは
1 回 6.6ms・13 回で 85.6ms あり、待ち時間の半分を占めていた。
**縦長の端末ほど効く**改修で、この端末は一番効かない側にいる。

### スクリプト律速の場面 (実機ではこちらが本体)

実機で「処理中」が長いのは**全部ここ**:

| 場面 | 実機 | うちスクリプト |
|---|---|---|
| セーブのロード (14MB) | **21.7 秒** | 21.67 秒 (99.9%) |
| コマンド一覧の表示 | **3.3 秒** | 3.29 秒 (99.7%) |
| 能力表示コマンド | **1.1 秒** | 1.11 秒 (99.3%) |

中身は上流のセーブ復元と ERB の実行そのもの。PC 比で 7 倍（ロード 3.1 秒 → 21.7 秒）。

## スクリプト時間をプロファイラで割る (2026-08-01)

`dotnet-trace` でロード (PC 3.1 秒) をサンプリングした。手順:

```bash
dotnet tool install --global dotnet-trace
dotnet-trace collect --format speedscope -o load.nettrace -- \
  src/Emuera.TestHarness/bin/Release/net10.0/Emuera.TestHarness.exe <ゲームフォルダ> --input 1 --input 0
```

`dotnet run` に付けても**中で起動される子プロセスは追えない**ので、ビルド済み exe を直接渡すこと。
出力の speedscope JSON は素の JSON なので、`SubmitInput` がスタックに載っている区間だけを
自己時間で集計すれば「1 入力の内訳」がそのまま出る
（`CPU_TIME` / `UNMANAGED_CODE_TIME` は擬似フレームなので読み飛ばす）。

### 見つかったもの

| 犯人 | PC ロード 3,090ms 中 | 直したか |
|---|---|---|
| `EnumNameMethod` の `item.ToUpper()` | 250ms | **直した** |
| `ExistFunctionMethod` の `ToUpper()` | 209ms | **直した** |
| `VariableToken.CheckElement` の `[true,true,true]` | 530ms (見かけ) | 直したが壁時計は動かず |
| `CharacterData..ctor` の配列確保 | 350ms | 手つかず (キャラ数ぶん要る) |
| `EraBinaryDataReader.ReadIntArray2D` | 104ms | 手つかず (セーブの実体) |
| `EnumFilesMethod` の `Directory.Exists` + パス正規化 | 190ms | 手つかず (**端末では FUSE 越し**) |
| `VariableEvaluator.GetMatch` / `FindChara` | 360ms | 手つかず (ゲームの実行内容) |

**大文字化が効いていた理由**が本命だった。どちらも「大文字にしてから比較する」書き方で、

- `EXISTFUNCTION` は `GetNonEventLabel(functionname.ToUpper())`。
  **辞書は `Config.StrComper` = `OrdinalIgnoreCase` で引く**ので、事前の `ToUpper` は結果を変えない。
  ロード時対応の機能検出で何千回も呼ばれ、そのぶんカルチャ依存の大文字化 (ICU) が走っていた
- `ENUMFUNCBEGINSWITH` 系は候補 1 本ごとに `item.ToUpper()`。候補は**登録済みの全関数名**なので、
  1 回の呼び出しで数千本ぶんの文字列確保と ICU 変換になる

どちらも「大文字化してから序数比較」＝大小無視の比較そのものなので、
`StartsWith` / `EndsWith` / `Contains` / `string.Equals` の `OrdinalIgnoreCase` に置き換えた。
確保が消え、カルチャにも依存しなくなる（元のコードはトルコ語ロケールの端末で `i` の扱いが変わり、
**辞書が `OrdinalIgnoreCase` なのに引けなくなる**ほうがむしろ危なかった）。

### 効き方

PC (1600x2691、5 回の中央値):

| 操作 | 描画改修のみ | + 文字列改修 |
|---|---|---|
| コマンド一覧の表示 | 876ms | **722ms** |
| セーブのロード | 3,195ms | 3,068ms |
| 能力表示コマンド | 97ms | 97ms |

実機 (1 回ずつ):

| 操作 | 最初 | 現在 |
|---|---|---|
| コマンド一覧の表示 | 3,682ms | **3,096ms** |
| セーブのロード | 21,058ms | 20,168ms |

**コマンド一覧は PC・実機とも 2 割弱減った。** ロードは 4% で、残りは上の表の「手つかず」側にある。

### 実機でプロファイルを取る (PC の profile は当てにならなかった)

**PC の順位は実機の順位ではない。** 実機で取り直したら犯人が入れ替わった。

`dotnet-dsrouter` 経由で `dotnet-trace` を実機の .NET へ繋ぐ:

```bash
dotnet tool install --global dotnet-dsrouter
dotnet-dsrouter android                    # 127.0.0.1:9001 で待つ。adb reverse も張ってくれる
adb reverse tcp:9000 tcp:9001
adb shell setprop debug.mono.env "'DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend'"
adb shell am force-stop rip.eragames.andemuera   # 反映は再起動から
dotnet-trace collect -p <dsrouter の pid> --format speedscope --duration 00:00:50
```

**踏んだ罠が 2 つある:**

1. **`-p:AndroidEnableProfiler=true` でビルドした APK でないと、診断ポートを指定した瞬間にアプリが即死する。**
   通常ビルドには診断コンポーネントが入っていないため
2. **`debug.mono.env` の 1 項目は 55 文字まで。** 超えると
   `monodroid: Attempt to store too much data in a buffer (capacity: 55; exceeded by: 1)` で
   `SIGABRT`。`DOTNET_DiagnosticPorts=127.0.0.1:9000,nosuspend,connect` はちょうど 1 文字溢れる
   （`,connect` は既定なので落として通した）

`-t:Install` はファストデプロイなので**実機のアセンブリが更新されないことがある**。
計測用に入れ直すときは `-p:EmbedAssembliesIntoApk=true` で APK を作って `adb install -r` する。

### 実機の内訳 (ロード 22.9 秒ぶん)

| 犯人 | 実機 | PC では |
|---|---|---|
| `MATCH` (`VariableEvaluator.GetMatch`) | **3,802ms (16.6%)** | 目立たず |
| `GetCharacterTemplate_UseSp` の `BinarySearch` | **2,600ms (11%)** | 目立たず |
| `StaticInt1DVariableToken.IfNullInitArray` | 1,387ms (6.1%) | — |
| ファイル列挙 (`FindNextEntry` + `Stat` + `MatchPattern`) | 1,571ms (6.9%) | 190ms |
| `Process.runScriptProc` + `EmueraConsole.IsRunning` | 1,944ms (8.5%) | — |

**ENUMFILES の重さは当たっていたが、本命ではなかった** (7%)。上位 2 つを直した:

- **`GetCharacterTemplate_UseSp`**: `CharacterTmplList.BinarySearch(null, Comparer<CharacterTemplate>.Create(...))`
  と書かれており、**呼び出しのたびに比較子とクロージャを作り**、比較のたびに
  インタフェース → デリゲート → クロージャ と 2 段跳んでいた。CSV 由来のキャラ変数を
  1 個読むたびに通る。中身は `No` の二分探索なので手で書いた
  （刻み方を `List.BinarySearch` と揃えたので、`No` が重複していても同じ要素を返す）
- **`GetMatch` / `GetMatchChara`**: 要素 1 個ごとに添字の `long[]` を作り直していた。
  添字はループ変数しか変わらないので 1 本を使い回す。
  文字列版は空文字を探すとき同じ値を 2 回読んでいたのも 1 回にした

### ENUMFILES はキャッシュできない — 実測で確かめた

実機の 7% を占めるファイル列挙は「同じフォルダを何度も見ているならキャッシュ」で消せそうに見えるが、
**呼び出しを全部記録して数えたら成り立たなかった**（ロード 1 回ぶん）:

| | 回数 |
|---|---|
| `ENUMFILES` の呼び出し | 4,177 回 |
| 異なる (フォルダ, パターン, 再帰) | 3,866 通り (**重複は 7%**) |
| 異なるフォルダ | 2,618 個 (フォルダ単位でまとめても 37% しか減らない) |

キャラごと・ポーズごとに別のフォルダを見に行くので、**そもそも重複がない**。
結果キャッシュは割に合わないうえ、`SAVETEXT` などで増えたファイルを見落とす危険を背負うだけなので入れない。

代わりに 1 回あたりの固定費だけ削った:

- **`Directory.Exists` の事前確認をやめた。** フォルダが無ければ `EnumerateFiles` が投げ、
  既存の `catch` が同じ `-1` を返すので結果は変わらない。**4,177 回ぶんの `stat` が消える**
- **`Path.GetRelativePath` をやめた。** 列挙結果は必ずゲームフォルダ配下なので切り落とせば済む
  （前提が崩れたときだけ従来の経路へ）

`IfNullInitArray` (6%) は中身が null 判定だけなので、6 か所に `AggressiveInlining` を付けた。

### 効き方 (実機 SC-55E)

| 操作 | 最初 | 描画 | + 文字列 | + 変数アクセス | + 列挙/埋め込み |
|---|---|---|---|---|---|
| セーブのロード (14MB) | 21,058ms | 20,168ms | 20,168ms | 16,465ms | **15,259ms** |
| コマンド一覧の表示 | 3,682ms | 3,298ms | 3,096ms | 2,719ms | **2,578ms** |
| 能力表示コマンド | 1,109ms | 1,122ms | 1,195ms | 1,099ms | **1,052ms** |

**ロード -28%、コマンド一覧 -30%、能力表示 -5%。**
PC では同じ改修が -7% 程度にしか見えない（ARM64 と端末の GC ではデリゲートと確保の値段が違う）。

### 次に削るなら

- `MATCH` は改修後もまだ上位のはず。線形走査そのものはゲーム側の書き方なので、
  次は `GetIntValue(exm, long[])` を通らない専用経路が要る
- `CharacterData` の確保。キャラ 1 人ごとに変数配列を作るので**キャラ数に比例**する
- ファイル列挙の残り。**1 回あたりの固定費は削ったので、あとは FUSE の readdir そのもの**
- 計測の作法として: **サンプラの数字をそのまま信じないこと**。`[true,true,true]` の除去は
  プロファイラ上 530ms あったのに壁時計は動かなかった。
  必ず前後の壁時計を複数回取って中央値で見て、**PC と実機の両方で確かめる**

### パッチの作り直し方

`01` と `02` は同じファイル (`EmueraConsole.cs` / `Creator.Method.cs`) を触るので、
`git diff` の結果をそのまま `02` に足すと **`01` のハンクを巻き込む**。
`02` の定義は「`01` を当てた状態」との差分なので、そう作る:

```bash
git -C upstream/emuera.em archive HEAD | tar -x -C /tmp/base
(cd /tmp/base && git apply .../patches/01-android-portability.patch)
for f in <02 が触るファイル>; do
  diff -u --label "a/$f" --label "b/$f" /tmp/base/$f upstream/emuera.em/$f
done > patches/02-performance.patch
```

**当てた結果が作業ツリーと一致することを毎回確かめる** (`diff -r --brief`)。
Windows 側で上流ファイルを書き換えるときは**改行を CRLF のまま**にすること
（LF で書き戻すとファイル全体が差分になる）。

### 検証で踏んだ罠: 画面には実行ごとに変わる部分がある

`--capture` の画素ハッシュは、**同じビルドを 2 回走らせても一致しない画面がある**。
erablue_resort のメイン画面は「今日のモブ観光客人数」を毎回引き直すので、
その数字のセル (15x13px) だけが変わる。**改修の影響と取り違えないこと。**

このときの見分け方:

1. `ANDEMUERA_EAGER_PAINT=1` / `ANDEMUERA_NO_IMAGE_CACHE=1` で従来動作に戻した実行と突き合わせる
2. 差分の**外接矩形**を見る（1 文字ぶんの矩形なら乱数、レイアウトのずれなら本物）
3. 乱数を含まない画面（タイトル〜ロード直後）は完全一致することを確認する
4. 描画時に確定するもの (`--dump` の「直近の描画パーツ」) が一致することを確認する

### 退避用の環境変数

高速化はどれも「従来動作へ 1 個ずつ戻せる」ようにしてある。A/B 計測にも使う。

| 変数 | 立てたときの動作 |
|---|---|
| `ANDEMUERA_EAGER_PAINT=1` | スクリプト実行中もその場でフル描画する（畳み込みなし） |
| `ANDEMUERA_NO_IMAGE_CACHE=1` | 画像描画のたびに `SKImage.FromBitmap` で全画素を複製する |
| `ANDEMUERA_NO_GLYPH_FALLBACK=1` | 主フォントに無い字を代替フォントで補わない（豆腐に戻す） |
| `ANDEMUERA_BOOT_PROFILE=1` | 起動時間の内訳を `time.log` に書く |
| `ANDEMUERA_MEASURE_PROFILE=1` | 文字幅計測の回数・文字数・時間を数える |

## コマンド一覧が出ない — ResetClip の空実装 (2026-08-01)

「ロード後、コマンド表示がなくなってしまっている」という報告の**本命の原因**。
画面写真では内容が中央に寄り、下 2〜3 割が背景色のまま空いていた。

`--dump` で表示行を吐かせると、**コマンド一覧は表示行にちゃんと存在していた**
（`-----[日常]---` `会話[300]` … `＜コマンド履歴:＞`）。スクロール位置も `284/284` で正常。
つまり「文字はあるのに描かれていない」。

犯人は `src/Emuera.Core/Compat/Drawing/Graphics.cs` の

```csharp
public void ResetClip() { }   // ← 空実装だった
```

上流の `ConsoleDivPart.DrawTo` は **「div の矩形にクリップ → 中身を描く → `ResetClip()` で戻す」**
という手順で描く。戻らないと、**それ以降に描くものすべてが直前の div の矩形に閉じ込められる**。

`EmueraConsole.OnPaint` の描画順は

1. 奥行きの深いパーツ（`depth` の大きい順）
2. **通常の行テキスト**（`depth == 0` のところで一括描画）
3. 手前のパーツ

なので、**奥に div が 1 つでもある画面では行テキストが丸ごと消える**。
erablue_resort の同室画面は `<div ypos='-441' … >`（部屋の背景）が `depth=999` にいるため、
その矩形＝ベッド画像のあたりだけが描かれ、コマンド一覧は範囲外で全滅していた。
TIPS 欄しか div が無いショップ画面が無事だったのは、`depth` が 0 だけで
「テキスト → パーツ」の順になり、クリップが残らないため。

Skia のクリップは差分適用しかできないので、`ClipCore` が Replace で使っている
「保存レベルを 1 段戻して掛け直す」を共通化し、`ResetClip` からも呼ぶようにした。

回帰チェックは `--selftest-draw`（クリップを掛ける→解除→外側に描けるか）。
**旧実装に戻すと落ちることも確認済み。**

この手の「画像だけ見ても分からない」不具合のために、ハーネスへ調査用の口を足した:

| フラグ | 内容 |
|---|---|
| `--dump N` | 表示中のログの末尾 N 行をテキストで出す。「文字が無い」のか「描かれていない」のかを分ける |
| `--scroll N` | バックログ送り（正で過去へ）。タッチ操作を伴う不具合を PC で再現する |

`--dump` は併せて「直近の描画パーツの奥行きと位置」も出す。
奥行き 0 が無い画面では上流が行テキストを描かない仕様なので、そこも警告する。

**教訓**: シムの「何もしない実装」は、呼び出し側が*戻す*ことを期待している API では
無害ではない。`ResetClip` / `ResetTransform` / `Restore` の類は特に危ない。

## 履歴を遡ったまま入力すると画面が壊れる (2026-08-01)

上の調査中に見つかった別口の不具合。**履歴を遡ったまま入力を確定すると、
以後まったく入力を受け付けなくなる**（`BINPUT` が「ボタンが一つも無い」で停止する）。

原因は 2 つあり、どちらも**上流が WinForms の副作用に頼っている箇所**を写しそこねていた。

### 1. 入力の前に最新行へ戻していなかった

上流はマウスでもキーでも、**入力を処理する前に必ず最新行へ戻す**。

| 上流 | 場所 |
|---|---|
| `mainPicBox_MouseDown` | 履歴表示中の左右クリックで `vScrollBar.Value = vScrollBar.Maximum` |
| `richTextBox1_KeyDown` | PageUp/PageDown **以外のキーすべて**で同じ処理 |

移植版はクリック (`MainWindow.HandleClick`) にしか入れておらず、
`EmueraEngine.SubmitInput` / `PressEnter`（画面下の「決定」「送る」、文字入力）が素通りしていた。

遡ったまま実行すると、`EmueraConsole.OnPaint` は**見えている行のボタンしか登録しない**ため
（`escapedParts` は描画時に作られる）、次の画面の `BINPUT` が

```
エラー内容：デフォルト値が無く、ボタンが一つも無い状態でBINPUTが行われました。全ての入力を受け付けなくなります
```

で止まる。**以後どこをタップしても反応しない**のはこれ。
`MainWindow.ReturnToLatestLine()` を追加し、`PressEnterKey` の先頭で呼ぶようにした。

### 2. ScrollBar 互換シムに範囲クランプが無かった

`EmueraConsole.verticalScrollBarUpdate` は表示行が減ったとき `ScrollBar.Maximum = 行数;` と
書くだけで、`Value` の締め直しを WinForms に任せている
（本物は Maximum を Value より小さくすると Value も引き下げる）。

`Compat/WinForms/Controls.cs` の `ScrollBar` は素の自動プロパティだったので、
`CLEARLINE` や画面クリアで行が減ると **`Value > Maximum` のまま残る**。描画は
`bottomLineNo = Value - 1` を最下行として使うため、超過ぶんだけ画面全体が上へずれ、
下側が空く。さらに `Value != Maximum` は「履歴表示中」と見なされるので、
一度ずれると戻る道が無い。WinForms と同じクランプ（`Value` / `Minimum` / `Maximum` の相互調整）を実装した。

ついでに `LargeChange` の既定を上流 Designer と同じ **1** にし、`ValueChanged` を実際に発火させて
上流の `textBoxHandleScrollValueChanged`（行内入力欄の位置）を移植した。

### 再現と検証

`--scroll` をハーネスに足した（正で過去へ）。erablue_resort で:

```bash
dotnet run --project src/Emuera.TestHarness -- <ゲームフォルダ> \
  --input 1 --input 0 --input "" --input "" --input 809 --input 121 --input 9999 \
  --scroll 220 --input 113
```

修正前は `IsError=True` で以後入力を受け付けない。修正後は履歴から復帰して正常に描画される
（スクロールした場合としない場合で画面が一致することを `--compare` で確認。
ただし TIPS 欄はゲーム側が毎回ランダムに選ぶので、そこだけは実行ごとに変わる）。

`--input` のたびに `Value <= Max` を確かめるようにし、破れたら終了コード 5 を返す。
シム単体の回帰は `--selftest-scroll`。

**教訓**: 互換シムは「メンバがあること」ではなく**代入したときの副作用**まで写す必要がある。
`StrConv` のときと同じで、ビルドが通っても実行時にしか出ない。

### PC で erablue_resort が起動しない件（別件）

→ 「[プラグイン: 内蔵実装に置き換え](#プラグイン-内蔵実装に置き換え)」で対処済み。

## 等幅なだけでは足りない — MS ゴシックの半角/全角 (2026-08-13)

ShinEraTenseiP の素質選択画面で、CLASS1 の右端罫線が CLASS2 の枠の中へ入り込み、
CLASS2 の右端罫線は枠の外へ飛び出していた。崩れるのはステータスの 3 行だけで、
他の行の罫線は揃っている。

原因はステータスバーに使う `▋` U+258B の送り幅。

[SET_MASTER_MAJORING.ERB:662-675] はバーを `▋` × 本数で描き、直後に
`" " * (25 - TEMP - 10)` で 25 桁へ詰める。この引き算は **`▋` を半角として数えている**。

```
TEMPS   = 　%GET_BASESTATUS(L_ST), 4%　{TEMP}          ; 全角+4桁+全角+数値 = 9 桁
TEMPS:1 = <font …>%" " + TEMP * UNICODE(0x258B)%</font> ; 1 + ▋ × TEMP
TEMPS:1 += " " * (25 - TEMP - 10)                      ; 残りを半角スペースで埋める
```

合計が 25 桁に収まるのは `▋` が半角 (1 桁) のときだけで、全角なら本数ぶん行が伸びる。

### MS ゴシックは JIS の外を半角で持つ

| | `▋` U+258B | `█` U+2588 | `─` U+2500 | `│` U+2502 | `■` U+25A0 |
|---|---|---|---|---|---|
| MS ゴシック | **0.50em** | **0.50em** | 1.00em | 1.00em | 1.00em |
| BIZ UDGothic | グリフ無し | グリフ無し | 1.00em | 1.00em | 1.00em |

MS ゴシックは **JIS X 0208 にある字を全角、Unicode 拡張として足した記号を半角**で持つ。
罫線でも `─ │ ┌ ┐` (JIS にある) は全角、`┄ ╌ ═ ║` (JIS に無い) は半角。
EAW の表とは一致しないので、Unicode の分類では代用できない
(U+2550–2573 は EAW=Ambiguous だが MS ゴシックは半角)。

era のスクリプトは `emuera.config` の既定フォント＝ MS ゴシックの幅で桁を組んでいるため、
**端末側もこの分け方まで再現しないと枠が崩れる**。等幅の検査 (`FontMetrics.IsMonospaced`) は
「半角スペース : 半角文字 : 全角 = 1 : 1 : 2」しか見ないので、ここは素通りする。

### 崩れ方

`GlyphFallback.EmitSubstitute` は代替グリフ本来の送り幅でセルを選んでいた。
端末のフォント (実機は Noto、PC で再現すると Yu Gothic UI / Meiryo UI) は `▋` を全角で持つので
全角セルへ置かれ、バー 1 本につき半角 1 桁ぶん行が伸びる。

素質選択の 1 行は「左の一覧 + CLASS1 + CLASS2」を連結したものなので、伸びたぶんが後ろへ波及する:

- CLASS1 の中身で 5 桁 (力 2 + 知恵 3) → CLASS1 の右端と CLASS2 の左端の `│` がまとめて右へ
- CLASS2 の中身でさらに 5 桁 → CLASS2 の右端 `│` は計 10 桁ぶん枠の外へ

ステータス行だけが崩れて見えたのは、`▋` を使う行がそこだけだから。

### 対処

`Compat/Drawing/MsGothicWidths.cs` に「MS ゴシックが半角で持つコードポイント」の表
(1414 文字 / 140 範囲) を置き、`GlyphFallback.EmitSubstitute` のセル選択で先に引く。
表に無い字は従来どおり代替グリフ本来の送り幅で決めるので、簡体字や絵文字は全角のまま。

セルからはみ出すぶんを横へ詰める既存の仕組みがそのまま効き、`▋` は細い縦棒として描かれる
— MS ゴシックの半角 `▋` と同じ見た目になる。

表は msgothic.ttc の cmap / hmtx から採った実測値で、フォントの複製ではなく
「どのコードポイントが半角か」という数値だけを持つ。作り直しは:

```
Emuera.TestHarness --dump-halfwidth "MS Gothic"
```

### 検証

`--selftest-glyph` に `--font-fallback` を足し、`--selftest-font` と同じく
「OS のフォントを使わず、APK 同梱フォントを受け皿にする」＝実機の状態を PC で再現できるようにした。
PC には MS ゴシックが入っているため、これが無いと実機で代替へ回る字が代替へ回らず、検査が素通りする。

送り幅の検査も「半角セルか全角セルちょうど」から
「**MS ゴシックと同じ側の**セルちょうど」へ強めた (`MsGothicWidths.IsHalfWidth` で期待値を出す)。

```
$ Emuera.TestHarness --selftest-glyph <ShinEraTenseiP\Data> --font-fallback
      修正前                                            修正後
NG  ▋ U+258B が半角セル (18px)              →   OK  ▋ U+258B が半角セル (9px)
NG  U+2588 █ …MS ゴシックと同じ半角セル (18px)  →   OK  … (9px)
NG  U+2551 ║ / U+2592 ▒ / U+2550 ═ / U+2595 ▕ / U+2591 ░ / U+2593 ▓   同上
OK  U+1F332 🌲 …MS ゴシックと同じ全角セル (18px)  →   OK  … (18px)
```

ShinEraTenseiP は `█` を 128 回、`║` を 88 回、`▒` を 70 回…と Block Elements と罫線を
広く使っており、崩れていたのは素質選択画面だけではなかった。
erablue_resort / eraTOWN でも `--selftest-glyph` が通り、簡体字 (`态`) と `❤` は全角のままであることを確認した。

### 残る制約: 主フォントが持っている字は直せない

この経路は **主フォントに無い字** にしか効かない。主フォントが持っていれば
`AllCovered` の高速パスに乗り、そのフォント自身の送り幅がそのまま出る。
つまり「MS ゴシックは半角だが同梱フォントは全角で持っている」字があると同じズレが残る。

BIZ UDGothic で突き合わせたところ該当は 49 文字だけで、内訳はギリシャ文字とキリル文字の
アクセント付き (`Ά` `ή` `ђ` `ґ` など) と `∏` U+220F。era のスクリプトが枠組みに使う字ではないため、
今回は手を入れていない。直すとなると全文字の幅を 1 字ずつ検査することになり、
`AllCovered` の一発計測 (PRINTC の桁揃えで何千回も通る) を捨てることになる。

## 次の作業

1. （完了）セーブ／ロードの往復確認
2. 起動時間の短縮 — **安いところは一通り入れた**。実機で `time.log` を取り直し、
   まだ長いなら ERB のパース結果キャッシュ（無効化キーの設計を誤ると
   「古いスクリプトで動く」という最悪のバグになるので慎重に）
3. 文字幅計測（`TextRenderer.MeasureText`）を実描画と突き合わせて調整する
4. 音声 (PLAYSOUND / PLAYBGM) のバックエンド実装
5. 表示を行モデル JSON 方式へ載せ替え
6. 不透明 PNG (RGB 3byte 化)。`--capture` は全画面で α が全面 255 だと報告しているので、
   `SKAlphaType.Opaque` に読み替えれば生データが 22MB → 16.5MB になる。
   α が 255 でない画素が出る画面が無いか、もう少し広く確認してから
