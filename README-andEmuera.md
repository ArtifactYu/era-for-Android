# andEmuera

era 系ゲームのエンジンを Android で動かすための移植です。

Windows 専用だった **Emuera.EM+EE**（.NET / WinForms）から Windows 依存を切り離し、
スマートフォン単体で era ゲームを遊べるようにすることを目指しています。

さらに [EmueraEX](https://github.com/BluishCat/EmueraEX) の統合パッチを取り込んでいるため、
**Emuera.NET（.netEmuera）系のバリアント**（ShinEraTenseiP など）も同じアプリで動きます。
`MATCHALL` / `GETCSVNOBY*` / `HASH_XXH*` / `DICT_*` / `G_POLYGON_*` / `SQL_*` / `VARI`,`VARS` と
`HTML_PRINT` の `<div>` 方言・`<font size>` に対応しています。

## これは改変版です

本ソフトウェアは [Emuera](https://ja.osdn.net/projects/emuera/)（MinorShift 氏）および
その派生である [Emuera.EM+EE](https://gitlab.com/EvilMask/emuera.em)（EvilMask 氏 / Enter 氏）を**改変**したものです。
さらに [Emuera.NET](https://gitlab.com/alnatiyan/EmueraDotNet)（VVII 氏 / alnatiyan 氏）から
機能を移植しています（[EmueraEX](https://github.com/BluishCat/EmueraEX) 経由）。
オリジナルの作者ではありません。

Emuera / Emuera.NET のライセンス（どちらも zlib ライセンス相当。全文は [licenses/](licenses/)）に従い、
改変した旨をここに明示します。上流ソースへの変更は `patches/` に差分として保管しています。

```
Copyright (C) 2008- MinorShift, 妊）|дﾟ)の中の人, VVII
```

## 制作について

**このリポジトリの内容は [Claude Code](https://claude.com/claude-code)（Anthropic）を使って作りました。**
Windows 依存の切り離し、SkiaSharp 裏打ちの互換シム、実機での動作確認まで含みます。

## 仕組み

```
andEmuera.apk (net10.0-android)
├─ MainActivity        全画面 WebView + フォントの登録 + ゲームフォルダの検出
├─ Emuera.WebHost      127.0.0.1 のローカル HTTP/WebSocket サーバー
│    GET /screen.png     現在の画面 (上流と同じ描画結果)
│    WS  /ws             タップ・スクロール・入力の受け取り
└─ Emuera.Core         上流のパーサ・インタプリタ (ソースはリンク参照。改変は最小)
```

描画は上流の `EmueraConsole` にそのまま行わせ、その結果を画像として WebView に出しています。
表示互換が完全で、タップ座標をそのままマウス座標として渡せるためです。
画像は WebSocket のバイナリフレームで push します（`GET /screen.png` はデバッグ用に残してあります）。
タッチ操作（拡大・パン・慣性スクロール・長押し）は `Emuera.WebHost/wwwroot/index.html` が担当します。
`System.Drawing` / `System.Windows.Forms` は SkiaSharp 裏打ちの互換シム
(`src/Emuera.Core/Compat/`) で置き換えています。

## ビルド

必要なもの:

- .NET 10 SDK
- Android ワークロード (`Microsoft.NET.Runtime.MonoTargets.Sdk` を含む最新版)
- Android SDK

```bash
git clone https://gitlab.com/EvilMask/emuera.em.git upstream/emuera.em
```

上流を取得したら、`patches/` の差分を**番号順に**当ててからビルドします。

| 番号 | 内容 |
|---|---|
| `00`〜`09`・`10-gamepad-2d-nav` 以降 | Emuera.NET（.netEmuera）との統合。配布元は [EmueraEX](https://github.com/BluishCat/EmueraEX)（`tools/sync-android.ps1` でこちらへ配られる） |
| `10-android-portability` | Android 移植のための修正 |
| `11-performance` | 高速化 |

統合パッチが先です。順番を入れ替えると当たりません
（ファイル名順に当てれば正しい順序になります）。

```bash
git -C upstream/emuera.em apply ../../patches/*.patch
dotnet build src/andEmuera.Android/andEmuera.Android.csproj -t:Install
```

統合パッチにより、.netEmuera 系のバリアント（ShinEraTenseiP など）も動きます。
`MATCHALL` / `GETCSVNOBY*` / `HASH_XXH*` / `DICT_*` / `G_POLYGON_*` / `SQL_*` / `VARI`,`VARS` と
`HTML_PRINT` の `<div>` 方言・`<font size>` に対応しています。

- `SQL_*` は `SQLitePCLRaw.bundle_e_sqlite3` が Android のネイティブ SQLite を持ってきます
- `G_POLYGON_*` は `Compat/Drawing/Graphics.cs` の `DrawPolygon`/`FillPolygon`（SkiaSharp 裏打ち）で動きます
- ゲームパッド対応は WinForms 前提のため Android では無効です
  （`Runtime/Utils/GamePad.cs` は XInput の P/Invoke なので `Emuera.Core.csproj` で除外）。
  ボタン選択の移動処理そのもの（`EmueraConsole.MoveSelectingButton`）はコア側にあるので、
  Android の入力層から呼べば同じ操作を実装できます

## 配布物を作る

`tools/pack.ps1` が `dist/` に 2 つの zip を出します。

- `andEmuera-<版>.zip` — APK・導入手順 (`README.txt`)・出典表示 (`NOTICE.txt`)・`licenses/`
- `andEmuera-<版>-src.zip` — ソース一式（`upstream/` は含めない）。
  上流のライセンスが求める「改変した旨の明示」のために一緒に出します。改変の中身は `patches/`

最初の 1 回だけ、リリース署名鍵を作ります。

```powershell
.\tools\make-keystore.ps1
```

鍵は `%USERPROFILE%\.andemuera\andemuera-release.keystore` に作られ、リポジトリには入りません。
**この鍵を失うと、配布済みのアプリに更新版を届けられなくなります**
（利用者がアンインストールするしかなくなり、セーブも消えます）。必ずバックアップしてください。

```powershell
$env:ANDEMUERA_KEYSTORE_PASS = Read-Host '鍵のパスワード'
.\tools\pack.ps1
```

パスワードは MSBuild へ変数名のまま (`env:ANDEMUERA_KEYSTORE_PASS`) 渡すので、
プロセス一覧やビルドログには出ません。`Read-Host` で受けるのは、コマンド履歴に
平文で残さないためです。

毎回入れるのが面倒なら、暗号化して置いておけます。最初の 1 回だけ:

```powershell
.\tools\save-keystore-pass.ps1
```

`%USERPROFILE%\.andemuera\pass.dpapi` に DPAPI で暗号化して保存します。以降は

```powershell
.\tools\pack-signed.ps1
```

で済みます。復号した平文はこのプロセスの環境変数にだけ入り、終わったら消えるので、
他のプロセスからは見えません。DPAPI はユーザ＋マシン単位なので、ファイルを持ち出しても
復号できません。逆に OS の再インストールや別ユーザへの移行では読めなくなるので、
**鍵のパスワードそのものの控えは別に残してください**。

一度ビルドしたあとで zip の中身だけ作り直すときは `-SkipBuild` を付けます。
署名済み APK をそのまま使うので、鍵もパスワードも要りません。

```powershell
.\tools\pack.ps1 -SkipBuild
```

`-SkipBuild` は `publish` に残っている APK をそのまま包みます。版を上げたあとや
ビルドが失敗したあとに使うと、**前の版の APK に新しい版の名前が付いた配布物**が
できてしまうので、APK が `csproj` より古い場合はエラーで止まります。

**`pack.ps1`（`dotnet publish`）を実行したあとに `dotnet build` すると、`obj/Release` を
共有しているせいで起動できない APK ができます。** 起動直後に

```
java.lang.UnsatisfiedLinkError: No implementation found for void ...MainActivity.n_onCreate
```

で落ちたらこれです。`src/*/bin` と `src/*/obj` を消してビルドし直してください。

**リリースのたびに `andEmuera.Android.csproj` の `ApplicationVersion`（versionCode）を +1 してください。**
これを上げないと、端末側が更新版と認識しません。表示用の版は `ApplicationDisplayVersion` です。

Release ビルドについて 2 点:

- **トリミングと AOT は入れられません。** Emuera は識別子の解決やプラグイン機構で
  リフレクションを多用するため、トリミングすると実行時に落ちます。
  AOT はトリミング前提 (XA1030) なので同時に無効です
- そのぶん RID 1 つあたり `lib/` が 24 MB 前後になるので、**`android-arm64` だけ**を出します
  （32 bit ARM も入れると APK が 27 MB → 51 MB）

## ゲームデータの入れ方

`csv` と `erb` を含むゲームフォルダを、アプリ固有の外部ストレージに置きます。**複数入れられます。**

```bash
adb push <ゲームフォルダ> /sdcard/Android/data/rip.eragames.andemuera/files/games/
```

`tar` でまとめて送るのは避けてください。Windows の `tar` はファイル名を CP932 で格納するため、
端末上で日本語ファイル名が壊れます。`adb push` は UTF-8 で扱うので問題ありません
（5,900 ファイルで 5 秒程度）。

アプリは `games/` 直下から `csv` と `erb` を両方持つフォルダを探し、

- 1 つだけならそのまま起動
- 複数あれば**一覧から選択**（前回遊んだものが先頭に出ます）

します。フォルダ名は `csv` でも `CSV` でも構いません（Android は大文字小文字を区別するので、
どちらの表記も拾うようにしてあります）。

ただし**その配下のフォルダ名・ファイル名の大文字小文字は区別されます**。
画像 (`resources`) をスクリプトの想定と違う大小で置くと、Windows では表示できても端末では出ません。

### フォント

era のスクリプトは選択肢の桁揃えをエンジンに任せており、エンジンは**半角スペースで桁を作ります**。
そのため **`半角 = 全角の半分` の等幅フォント**でないと、選択肢が横一列に繋がって読めなくなります。
Android には等幅の日本語フォントが入っていないため、
[BIZ UDGothic](https://github.com/googlefonts/morisawa-biz-ud-gothic) Regular (SIL OFL) を
**APK に同梱**しています (`src/andEmuera.Android/Assets/fonts/`)。何も置かなくても桁は揃います。

名前で引けなかったときの受け皿は、次の順に見て**最初に見つかった等幅フォント**になります。

1. **共有 `fonts/`** — 全ゲーム共通の置き場。同梱フォントを差し替えたいときに使う

   ```bash
   adb push <好きな等幅フォント>.ttf /sdcard/Android/data/rip.eragames.andemuera/files/fonts/
   ```

2. **APK 同梱の BIZ UDGothic**
3. 端末のフォント（1・2 が読めなかったときの保険。比例フォントになりがち）

**等幅でないものは飛ばします。** `emuera.config` が `ＭＳ ゴシック` のような Android に無い
フォント名を指しているゲーム（eraTOWN ほか大半）では、**この受け皿がそのまま本文フォントになる**ため、
ここに比例フォントを置くとそれらが軒並み崩れるからです。
どれが選ばれ、何を見送ったかは `adb logcat -s andEmuera` の「受け皿に採用:」に出ます。

**ゲームが `font/` を同梱している場合** (erablue_resort など) は、そのフォルダごと送れば
`emuera.config` の指定どおりのフォントで描かれます（フォルダ丸ごと push していれば自動的に入ります）。

差し替えるときは、半角が全角のちょうど半分になるフォントを選んでください
（BIZ UD**P**Gothic のような "P" 付きは比例フォントなので不可）。
等幅でないフォントで起動した場合は、画面上部に注意書きが出ます。

**そのフォントに無い記号は端末のフォントから自動で補います。**
Windows は GDI+ が勝手に別フォントへ回してくれますが、Skia はしないため、
何もしないと `✕` `❤` や簡体字が豆腐 (□) になります
（BIZ UDGothic は JIS の範囲しか持っていません）。
補ったぶんの送り幅は主フォントの半角／全角セルにスナップするので、**桁揃えは動きません**。
どちらのセルに置くかは **MS ゴシックの分け方**に合わせます。
`▋` `█` `═` のような JIS の外の記号を MS ゴシックは半角で持っており、
era のスクリプトはその桁数で枠を組んでいるためです
（端末のフォントは同じ字を全角で持つので、素直に代替すると罫線の枠が右へずれます）。

**遊ぶ途中で別のバリアントに切り替えるにはアプリを再起動してください。**
Emuera はゲームフォルダのパスを静的に保持するため、1 回の起動で扱えるのは 1 バリアントです。

### セーブデータの持ち込み

`sav` フォルダを一緒に送れば、PC で遊んでいた続きをそのまま再開できます。
セーブはバイナリ形式ですがプラットフォーム非依存で、実機でのロードを確認済みです。

```bash
adb push <ゲームフォルダ>/sav /sdcard/Android/data/rip.eragames.andemuera/files/games/<ゲーム名>/
```

同名ファイルは**上書き**されます。端末側で遊び進めている場合は先に退避してください。
逆に端末から PC へ戻すときは `adb pull` を使います。

## PC で動かす

実機なしで開発・確認できます。ブラウザで表示され、クリックとホイールで操作できます。

```bash
dotnet run --project src/Emuera.TestHarness -- <ゲームフォルダ> --serve --port 8321
```

読み込みの確認や画面のキャプチャだけなら:

```bash
dotnet run --project src/Emuera.TestHarness -- <ゲームフォルダ> --input 0 --shot title.png
```

表示を変えずに速くする改修をしたときは、**画素が一致していること**を確かめてください
（PNG のバイト列は圧縮設定で変わるので、比較はデコード後の画素で行います）。

```bash
dotnet run -c Release --project src/Emuera.TestHarness -- <ゲームフォルダ> --capture out --input 0 --input 1
```

`out/hashes.txt` に各段階の生ピクセルの SHA-256 が出るので、改修の前後で突き合わせます。
`--compare a.png b.png` で 2 枚を直接比べることもできます。
ただし**ゲームによっては実行のたびに変わる表示があります**（乱数を引く画面など）。
ハッシュが違ったら、まず差分の位置が 1 文字ぶんかどうかを見てください
（詳しくは [docs/porting-notes.md](docs/porting-notes.md) の「検証で踏んだ罠」）。

エンコード設定そのものの検証と計測は `--verify-encoders` / `--bench`、
起動時間の内訳は `ANDEMUERA_BOOT_PROFILE=1` を付けると `time.log` に出ます。

タップ 1 回の待ち時間（画面左上の「処理中」が出ている区間）の内訳は、
`--input` を付けて走らせると 1 操作ごとに出ます。**端末は縦長で画素が 2 倍以上あり
描画の比重が変わる**ので、`--size` で端末相当にして測ってください。

```bash
dotnet run -c Release --project src/Emuera.TestHarness -- <ゲームフォルダ> --size 1600x2691 --input 0
```

```
→ 処理 97.1ms = スクリプト 88.2ms + フル描画 2 回 8.9ms (1 回あたり 4.4ms)
```

動いているサーバーの内訳は `GET /stats` で取れます（`lastInput` が直近 1 回の同じ内訳）。

パス周りは Windows と Android で挙動が変わるため、区切り文字を切り替えた自己テストを用意しています。
ゲームフォルダを渡すと、実データに対する `EXISTFILE` / `ENUMFILES` / `GCREATEFROMFILE` 相当も確認します。

```bash
dotnet run --project src/Emuera.TestHarness -- --selftest-path <ゲームフォルダ>
```

フォント周りも同じように確かめられます。`--selftest-font` が桁揃え (等幅かどうか)、
`--selftest-glyph` がグリフ欠け（そのフォントに無い文字を ERB / CSV から洗い出し、
代替フォントが見つかるか・送り幅が MS ゴシックと同じセルに収まるか）を見ます。
`--shot` を付けると実際に描いた見本が出ます。

**どちらも `--font-fallback` を付けてください。** PC には MS ゴシックが入っているため、
付けないと本文フォントがそちらに解決され、実機で代替へ回る字が代替へ回りません
（＝検査が素通りします）。付けると OS のフォントを使わず、APK 同梱フォントを受け皿にした
実機の状態を再現します。

```bash
dotnet run --project src/Emuera.TestHarness -- --selftest-glyph <ゲームフォルダ> --font-fallback --shot glyphs.png
```

## 現状

動くもの:

- ERB / CSV の読み込み（手元の erablue_resort で ERB 2,697 本 / CSV 3,044 本、実機で約 9 秒）
- タイトルからのゲーム進行、選択肢のタップ、文字入力
- フリックでのバックログ送り（慣性つき）と「▼ 最新へ」で復帰
- ピンチ / 「🔍 拡大」での拡大と 1 本指パン。長押しまたは「⏩ スキップ」で右クリック（メッセージスキップ）
- 操作状態の表示（「処理中」「選択肢をタップ」「数値を入力」などを画面左上に常時表示し、
  タップが通ったか空振りしたかを波紋の色で返す）
- 日本語表示、色分け、罫線、PRINTC の桁揃え
- フォントに無い記号 (`✕` `❤` 簡体字ほか) の代替フォントによる補完
- `CALLSHARP LAUNCH_BROWSER`（内蔵実装 → Android の Intent）

未対応・確認中:

- 辞書ポップアップ (Rikaichan) — PC 専用機能のため対象外
- `Plugins/*.dll` の読み込み — 内蔵実装で置き換える方針
- 音声 (PLAYSOUND / PLAYBGM) — バックエンド未実装
- 表示は画面全体の画像転送。行モデルを JSON で送る方式への載せ替えを検討中

## ドキュメント

移植の経緯と、判明した仕様・注意点は [docs/porting-notes.md](docs/porting-notes.md) に記録しています。
