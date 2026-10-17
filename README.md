# era for Android 1.1.1

安卓 Era 游戏播放器。应用与界面为本项目新建，引擎基于 Emuera.EM+EE 与 andEmuera 修改。

请先阅读 BUILD-ERAFLOW.md。源码已包含完整修补引擎，无需重复应用历史 patches。原说明保留为 README-andEmuera.md，作者与许可证见 licenses。qa 包含测试工具、原始游戏校验清单（仅名称与哈希，不含下载地址）、输入序列和自主编写的中性游戏。第三方游戏、工具链和私有签名密钥不在包内。

## 许可

本仓库是混合许可，按代码来源区分：

- **原创部分**（`src/EraFlow.Android` 应用层、`src/EraFlow.Storage`、`src/EraFlow.Probe`、`qa` 中的自研脚本与测试游戏等）：[PolyForm Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0)，**禁止商业使用**。全文见根目录 [`LICENSE`](LICENSE)；如译文与英文原文有出入，以英文原文为准。
- **引擎与移植部分**（`src/Emuera.Core`、`src/Emuera.WebHost`、`src/andEmuera.Android`、`upstream/emuera.em` 及其修改/补丁）：沿用 Emuera 的 zlib 式许可（允许包括商业使用在内的任何用途），版权归原作者所有。全文见 [`licenses/Emuera.LICENSE.txt`](licenses/Emuera.LICENSE.txt) 与 [`licenses/EmueraDotNet.LICENSE.md`](licenses/EmueraDotNet.LICENSE.md)。
- 本项目是 Emuera / Emuera.EM+EE / andEmuera 的**修改版**，修改内容见 `patches/` 与 [`licenses/era-for-Android.NOTICES.md`](licenses/era-for-Android.NOTICES.md)。
- 本项目**不包含任何游戏本体，也不提供游戏下载地址**；请使用你合法拥有的游戏。

### 免责声明

本软件仅为通用文本脚本运行环境，开发者不提供、不制作、不背书任何第三方游戏脚本。严禁将本软件用于违反当地法律法规的用途（包括但不限于非法牟利、传播违法内容）。一旦用于任何商业倒卖或违法用途，授权即刻自动终止，一切法律责任由违规使用者及倒卖者自行承担。

## 致谢

- 特别感谢 [@RikaMundus](https://x.com/RikaMundus)（X）对本项目的支持。
- 引擎与移植作者见上方"许可"章节及 [`licenses/`](licenses/)。
