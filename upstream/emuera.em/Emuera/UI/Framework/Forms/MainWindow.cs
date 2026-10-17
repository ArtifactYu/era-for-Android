using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using MinorShift.Emuera.UI;
using MinorShift.Emuera.UI.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using trmb = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.MessageBox;
using System.ComponentModel;

namespace MinorShift.Emuera.Forms
{
	internal sealed partial class MainWindow : Form
	{
		public MainWindow(string[] args)
		{
			InitializeComponent();
			_args = args;

			if (Program.DebugMode)
			{
				デバッグモードで再起動ToolStripMenuItem.Visible = false;
				デバッグToolStripMenuItem.Visible = true;
			}
			#region EM_私家版_Emuera多言語化改造
			SetLanguageOptions();
			#endregion

			mainPicBox.SetStyle();
			initControlSizeAndLocation();
			richTextBox1.ForeColor = Config.ForeColor;
			richTextBox1.BackColor = Config.BackColor;
			mainPicBox.BackColor = Config.BackColor;//これは実際には使用されないはず

			mainPicBox.BackColor = Config.BackColor;//これは実際には使用されないはず

			BackColor = Config.BackColor;

			richTextBox1.Font = Config.DefaultFont;
			richTextBox1.LanguageOption = RichTextBoxLanguageOptions.UIFonts;
			folderSelectDialog.SelectedPath = Program.ErbDir;
			folderSelectDialog.ShowNewFolderButton = false;

			openFileDialog.InitialDirectory = Program.ErbDir;
			openFileDialog.FileName = "";
			openFileDialog.Multiselect = true;
			openFileDialog.RestoreDirectory = true;
			string Emuera_verInfo = AssemblyData.EmueraVersionText;
			EmuVerToolStripTextBox.Text = Emuera_verInfo;

			console = new EmueraConsole(this);
			macroMenuItems[0] = マクロ01ToolStripMenuItem;
			macroMenuItems[1] = マクロ02ToolStripMenuItem;
			macroMenuItems[2] = マクロ03ToolStripMenuItem;
			macroMenuItems[3] = マクロ04ToolStripMenuItem;
			macroMenuItems[4] = マクロ05ToolStripMenuItem;
			macroMenuItems[5] = マクロ06ToolStripMenuItem;
			macroMenuItems[6] = マクロ07ToolStripMenuItem;
			macroMenuItems[7] = マクロ08ToolStripMenuItem;
			macroMenuItems[8] = マクロ09ToolStripMenuItem;
			macroMenuItems[9] = マクロ10ToolStripMenuItem;
			macroMenuItems[10] = マクロ11ToolStripMenuItem;
			macroMenuItems[11] = マクロ12ToolStripMenuItem;
			foreach (ToolStripMenuItem item in macroMenuItems)
				item.Click += new EventHandler(マクロToolStripMenuItem_Click);

			richTextBox1.MouseWheel += new MouseEventHandler(richTextBox1_MouseWheel);
			mainPicBox.MouseWheel += new MouseEventHandler(richTextBox1_MouseWheel);
			vScrollBar.MouseWheel += new MouseEventHandler(richTextBox1_MouseWheel);


			richTextBox1.KeyDown += new KeyEventHandler(richTextBox1_KeyDown);

			#region EM_私家版_INPUT系機能拡張
			richTextBox1.KeyUp += new KeyEventHandler(richTextBox1_ModifierRecorder_KeyUp);
			richTextBox1.KeyDown += new KeyEventHandler(richTextBox1_ModifierRecorder_KeyDown);
			#endregion

			#region EM_私家版_Emuera多言語化改造
			labelMacroGroupChanged.Font = new Font(Lang.MFont, 24F, FontStyle.Regular, GraphicsUnit.Point, 128);
			richTextBox1.Font = new Font(Config.DefaultFont.FontFamily, Config.FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
			#endregion

			#region EM_textbox位置指定拡張
			textBoxInfo.Left = richTextBox1.Left;
			textBoxInfo.Top = richTextBox1.Top;
			textBoxInfo.Size = richTextBox1.Size;
			textBoxState = TextBoxState.Unchanged;
			vScrollBar.ValueChanged += new EventHandler(textBoxHandleScrollValueChanged);
			#endregion

			#region 統合_ゲームパッド対応
			SetupGamePad();
			#endregion
		}
		private ToolStripMenuItem[] macroMenuItems = new ToolStripMenuItem[KeyMacro.MaxFkey];
		//private System.Diagnostics.FileVersionInfo emueraVer = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
		public PictureBox MainPicBox { get { return mainPicBox; } }
		public VScrollBar ScrollBar { get { return vScrollBar; } }
		public RichTextBox TextBox { get { return richTextBox1; } }
		public ToolTip ToolTip { get { return toolTipButton; } }
		private EmueraConsole console;

		#region EM_私家版_Icon指定機能
		public void SetupIcon(Icon icon)
		{
			Icon = icon;
		}
		#endregion

		#region EM_私家版_Emuera多言語化改造
		public void TranslateUI()
		{
			fileToolStripMenuItem.Text = Lang.UI.MainWindow.File.Text;
			rebootToolStripMenuItem.Text = Lang.UI.MainWindow.File.Restart.Text;
			デバッグモードで再起動ToolStripMenuItem.Text = Lang.UI.MainWindow.File.RestartDebug.Text;
			ログをクリップボードにコピーToolStripMenuItem.Text = Lang.UI.MainWindow.File.CopyLogToClipboard.Text;
			ログを保存するSToolStripMenuItem.Text = Lang.UI.MainWindow.File.SaveLog.Text;
			タイトルへ戻るTToolStripMenuItem.Text = Lang.UI.MainWindow.File.BackToTitle.Text;
			コードを読み直すcToolStripMenuItem.Text = Lang.UI.MainWindow.File.ReloadAllScripts.Text;
			フォルダを読み直すFToolStripMenuItem.Text = Lang.UI.MainWindow.File.ReloadFolder.Text;
			ファイルを読み直すFToolStripMenuItem.Text = Lang.UI.MainWindow.File.ReloadScriptFile.Text;
			リソースフォルダを読み直すToolStripMenuItem.Text = Lang.UI.MainWindow.File.ReloadResource.Text;
			exitToolStripMenuItem.Text = Lang.UI.MainWindow.File.Exit.Text;
			openFileDialog.Filter = Lang.UI.MainWindow.FileFilter.Text + " (*.erb)|*.erb";

			デバッグToolStripMenuItem.Text = Lang.UI.MainWindow.Debug.Text;
			デバッグウインドウを開くToolStripMenuItem.Text = Lang.UI.MainWindow.Debug.OpenDebugWindow.Text;
			デバッグ情報の更新ToolStripMenuItem.Text = Lang.UI.MainWindow.Debug.UpdateDebugInfo.Text;

			ツールToolStripMenuItem.Text = Lang.UI.MainWindow.Tools.Text;
			ウィンドウ幅のロックToolStripMenuItem.Text = Lang.UI.MainWindow.Tools.LockWindowWidth.Text;
			クリップボードにコピーToolStripMenuItem.Text = Lang.UI.MainWindow.Tools.CopyToClipboard.Text;

			ヘルプHToolStripMenuItem.Text = Lang.UI.MainWindow.Help.Text;
			コンフィグCToolStripMenuItem.Text = Lang.UI.MainWindow.Help.Config.Text;

			LanguageToolStripMenuItem.Text = Lang.UI.MainWindow.Language.Text;

			マクロToolStripMenuItem.Text = Lang.UI.MainWindow.ContextMenu.KeyMacro.Text;
			for (int i = 0; i < マクロToolStripMenuItem.DropDownItems.Count; i++)
				マクロToolStripMenuItem.DropDownItems[i].Text = Lang.UI.MainWindow.ContextMenu.KeyMacro.Text + i.ToString("D2");
			マクログループToolStripMenuItem.Text = Lang.UI.MainWindow.ContextMenu.KeyMacroGroup.Text;
			for (int i = 0; i < マクログループToolStripMenuItem.DropDownItems.Count; i++)
				マクログループToolStripMenuItem.DropDownItems[i].Text = Lang.UI.MainWindow.ContextMenu.KeyMacroGroup.Group.Text + i;

			切り取り.Text = Lang.UI.MainWindow.ContextMenu.Cut.Text;
			コピー.Text = Lang.UI.MainWindow.ContextMenu.Copy.Text;
			貼り付け.Text = Lang.UI.MainWindow.ContextMenu.Paste.Text;
			削除.Text = Lang.UI.MainWindow.ContextMenu.Delete.Text;
			実行.Text = Lang.UI.MainWindow.ContextMenu.Execute.Text;
		}
		#endregion

		#region EM_textbox位置指定拡張
		void textBoxHandleScrollValueChanged(object sender, EventArgs e)
		{
			if (TextBoxIgnoreScrollBarChanges) return;
			if (vScrollBar.Value < vScrollBar.Maximum && TextBoxPosChanged)
				ScrollBackTextBoxPos();
			else if (vScrollBar.Value == vScrollBar.Maximum && TextBoxPosScrolledBack)
				ApplyTextBoxChanges();
		}
		struct TextBoxInfo
		{
			public int Top, Left;
			public Size Size;
		}
		TextBoxInfo textBoxInfo, nextTextBoxInfo;
		enum TextBoxState { Unchanged, WatingToChange, Changed, ScrollBack };
		TextBoxState textBoxState;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool TextBoxIgnoreScrollBarChanges { get; set; }
		public bool TextBoxPosChanged { get { return textBoxState == TextBoxState.Changed; } }
		public bool TextBoxPosScrolledBack { get { return textBoxState == TextBoxState.ScrollBack; } }
		public bool TextBoxPosWatingToChange { get { return textBoxState == TextBoxState.WatingToChange; } }
		public void SetTextBoxPos(int xOffset, int yOffset, int width)
		{
			nextTextBoxInfo.Left = Math.Max(0, Math.Min(xOffset, ClientSize.Width - 50));
			nextTextBoxInfo.Top = Math.Min(Math.Max(ClientSize.Height - yOffset - richTextBox1.Height, 0), ClientSize.Height - richTextBox1.Height);
			nextTextBoxInfo.Size = new Size(Math.Max(50, Math.Min(width, ClientSize.Width - richTextBox1.Left)), richTextBox1.Size.Height);
			textBoxState = TextBoxState.WatingToChange;
		}
		public void ResetTextBoxPos()
		{
			SetTextBoxPos(textBoxInfo);
			textBoxState = TextBoxState.Unchanged;
		}
		public void ScrollBackTextBoxPos()
		{
			SetTextBoxPos(textBoxInfo);
			textBoxState = TextBoxState.ScrollBack;
		}
		public void ApplyTextBoxChanges()
		{
			if (TextBoxPosWatingToChange || TextBoxPosScrolledBack)
			{
				SetTextBoxPos(nextTextBoxInfo);
				textBoxState = TextBoxState.Changed;
			}
		}
		void SetTextBoxPos(TextBoxInfo info)
		{
			richTextBox1.Left = info.Left;
			richTextBox1.Top = info.Top;
			richTextBox1.Size = info.Size;
		}
		#endregion

		#region EE_textbox拡張
		public void ChangeTextBox(string str)
		{
			richTextBox1.Text = str;
		}
		#endregion

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			//1823 INPUTMOUSEKEY Key入力全てを捕まえてERB側で処理する
			//if (console != null && console.IsWaitingPrimitiveKey)
			if (console != null && console.IsWaitingPrimitive)
			{
				return false;
			}
			#region EE_ホットキー拡張
			DialogResult result;
			switch (keyData & Keys.KeyCode)
			{
				case Keys.B when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					if (WindowState != FormWindowState.Minimized)
					{
						WindowState = FormWindowState.Minimized;
						return true;
					}
					break;
				case Keys.C when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
				case Keys.Insert when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					if (string.IsNullOrEmpty(TextBox.SelectedText))
					{
						var dialog = new ClipBoardDialog { StartPosition = FormStartPosition.CenterParent };
						dialog.Setup(console);
						dialog.ShowDialog();
						return true;
					}
					break;
				case Keys.O when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
				case Keys.Insert when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					{
						var doit = console != null;
						if (console != null && console.IsInProcess)
						{
							MessageBox.Show(trmb.NotAvailableDuringScript.Text);
							doit = false;
						}
						if (doit)
						{
							result = openFileDialog.ShowDialog();
							var filepath = new List<string>();
							if (result == DialogResult.OK)
							{
								foreach (var fname in openFileDialog.FileNames)
								{
									if (!File.Exists(fname))
									{
										MessageBox.Show(trmb.FileNotFound.Text);
										doit = false;
									}
									else if (Path.GetExtension(fname).ToUpper() != ".ERB")
									{
										MessageBox.Show(trmb.IsNotErb.Text, trmb.FileFormatError.Text); //
										doit = false;
									}
									if (fname.StartsWith(Program.ErbDir, StringComparison.OrdinalIgnoreCase))
										filepath.Add(Program.ErbDir + fname.Substring(Program.ErbDir.Length));
									else
										filepath.Add(fname);
								}
								if (doit)
								{
									console.ReloadPartialErb(filepath);
									return true;
								}
							}
						}
						break;
					}
				case Keys.T when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
				case Keys.Insert when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					{
						var doit = true;
						{
							if (console == null)
								doit = false;
							if (console != null && console.IsInProcess)
							{
								MessageBox.Show(trmb.NotAvailableDuringScript.Text);
								doit = false;
							}
							if (console.notToTitle)
							{
								if (console.byError)
									MessageBox.Show(trmb.ErrorInAnalysisMode.Text);
								else
									MessageBox.Show(trmb.CanNotReturnToTitle.Text);
								doit = false;
							}
							if (doit)
							{
								result = MessageBox.Show(trmb.ReturnToTitleAsk.Text, trmb.ReturnToTitle.Text,
									MessageBoxButtons.OKCancel);
								if (result != DialogResult.OK)
									doit = false;
								if (doit)
								{
									GotoTitle();
									return true;
								}
							}
						}
						break;
					}
				case Keys.R when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
				case Keys.Insert when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					result = MessageBox.Show(trmb.RestartAsk.Text, trmb.Restart.Text, MessageBoxButtons.OKCancel);
					if (result == DialogResult.OK)
					{
						Reboot();
						return true;
					}
					break;
				case Keys.V when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
				case Keys.Insert when (keyData & Keys.Modifiers & Keys.Shift) == Keys.Shift:
					if (Clipboard.GetDataObject() == null || !Clipboard.ContainsText())
					{
						return true;
					}
					else
					{
						if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text))
							TextBox.Paste(DataFormats.GetFormat(DataFormats.UnicodeText));
						return true;
					}
				//else if (((int)keyData == (int)Keys.Control + (int)Keys.D) && Program.DebugMode)
				//{
				//    console.OpenDebugDialog();
				//    return true;
				//}
				//else if (((int)keyData == (int)Keys.Control + (int)Keys.R) && Program.DebugMode)
				//{
				//    if ((console.DebugDialog != null) && (console.DebugDialog.Created))
				//        console.DebugDialog.UpdateData();
				//}
				#region EE_AnchorのCB機能移植
				case Keys.Up when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					if (Config.CBUseClipboard && console.CBProc.ScrollUp(1)) return true;
					break;
				case Keys.Down when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					if (Config.CBUseClipboard && console.CBProc.ScrollDown(1)) return true;
					break;
				#endregion
				//HOTKEY STATE
				case Keys.D when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					hotkeyState.Toggle();
					break;
				default:
					//if ((keyData & Keys.Modifiers & Keys.Alt) == Keys.Alt) return true;
					if (Config.UseKeyMacro)
					{
						int keyCode = (int)(keyData & Keys.KeyCode);
						bool shiftPressed = (keyData & Keys.Modifiers) == Keys.Shift;
						bool ctrlPressed = (keyData & Keys.Modifiers) == Keys.Control;
						bool unPressed = (keyData & Keys.Modifiers) == 0;
						if (keyCode >= (int)Keys.F1 && keyCode <= (int)Keys.F12)
						{
							int macroNum = keyCode - (int)Keys.F1;
							if (shiftPressed)
							{
								if (!string.IsNullOrEmpty(richTextBox1.Text))
									KeyMacro.SetMacro(macroNum, macroGroup, richTextBox1.Text);
								return true;
							}
							else if (unPressed)
							{
								richTextBox1.Text = KeyMacro.GetMacro(macroNum, macroGroup);
								richTextBox1.SelectionStart = richTextBox1.Text.Length;
								return true;
							}
						}
						else if (ctrlPressed)
						{
							int newGroupNum = -1;
							if (keyCode >= (int)Keys.D0 && keyCode <= (int)Keys.D9)
								newGroupNum = keyCode - (int)Keys.D0;
							else if (keyCode >= (int)Keys.NumPad0 && keyCode <= (int)Keys.NumPad9)
								newGroupNum = keyCode - (int)Keys.NumPad0;
							if (newGroupNum >= 0)
							{
								setNewMacroGroup(newGroupNum);
							}
						}
					}
					break;
				case Keys.Z when (keyData & Keys.Modifiers & Keys.Control) == Keys.Control:
					console?.GotoTitleAndLoadAndRepeatInput();
					break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}
		#endregion


		protected override void WndProc(ref Message m)
		{
			const int WM_SYSCOMMAND = 0x112;
			//const int WM_MOUSEWHEEL = 0x020A;
			const int SC_MOVE = 0xf010;
			const int SC_MAXIMIZE = 0xf030;

			// WM_SYSCOMMAND (SC_MOVE) を無視することでフォームを移動できないようにする
			switch (m.Msg)
			{
				case WM_SYSCOMMAND:
					{
						int wparam = m.WParam.ToInt32() & 0xfff0;
						switch (wparam)
						{
							case SC_MOVE:
								//if (WindowState == FormWindowState.Maximized)
								//	return;
								break;
							case SC_MAXIMIZE:
								if (Screen.AllScreens.Length == 1)
								{
									MaximizedBounds = new Rectangle(Left, 0, Config.WindowX, Screen.PrimaryScreen!.WorkingArea.Height);
								}
								else
								{
									for (int i = 0; i < Screen.AllScreens.Length; i++)
									{
										if (Left >= Screen.AllScreens[i].Bounds.Left && Left < Screen.AllScreens[i].Bounds.Right)
										{
											MaximizedBounds = new Rectangle(Left - Screen.AllScreens[i].Bounds.Left, Screen.AllScreens[i].Bounds.Top, Config.WindowX, Screen.AllScreens[i].WorkingArea.Height);
											break;
										}
									}
								}
								break;
						}
						break;
					}

					//MouseWheelイベントをここで処理しようと思ったけどなんかここまで来ない (Windows 7)
					//case WM_MOUSEWHEEL:
					//	{
					//		if (!vScrollBar.Enabled)
					//			break;
					//		if (console == null)
					//			break;
					//		//int wparam_hiword = m.WParam.ToInt32() >> 16;
					//		int move = (m.WParam.ToInt32() >> 16) / 120 * -1;
					//		if ((vScrollBar.Value == vScrollBar.Maximum && move > 0) || (vScrollBar.Value == vScrollBar.Minimum && move < 0))
					//			break;
					//		int value = vScrollBar.Value + move;
					//		if (value >= vScrollBar.Maximum)
					//			vScrollBar.Value = vScrollBar.Maximum;
					//		else if (value <= vScrollBar.Minimum)
					//			vScrollBar.Value = vScrollBar.Minimum;
					//		else
					//			vScrollBar.Value = value;
					//		bool force_refresh = (vScrollBar.Value == vScrollBar.Maximum) || (vScrollBar.Value == vScrollBar.Minimum);

					//		//ボタンとの関係をチェック
					//		if (Config.UseMouse)
					//			force_refresh = console.MoveMouse(mainPicBox.PointToClient(Control.MousePosition)) || force_refresh;
					//		//上端でも下端でもなくボタン選択状態のアップデートも必要ないなら描画を控えめに。
					//		console.RefreshStrings(force_refresh);

					//		break;
					//	}
			}
			base.WndProc(ref m);
		}

		private async void Init(object sender, EventArgs e)
		{
			await console.Initialize();
		}

		/// <summary>
		/// 1819 リサイズ時の処理を全廃しAnchor&Dock処理にマルナゲ
		/// 初期設定のみここで行う。ついでに再起動時の位置・サイズ処理も追加
		/// </summary>
		private void initControlSizeAndLocation()
		{
			//Windowのサイズ設定
			int winWidth = Config.WindowX + vScrollBar.Width;
			int winHeight = Config.WindowY;
			bool winMaximize = false;
			if (Config.SizableWindow)
			{
				FormBorderStyle = FormBorderStyle.Sizable;
				MaximizeBox = true;
				winMaximize = Config.WindowMaximixed;
			}
			else
			{
				FormBorderStyle = FormBorderStyle.Fixed3D;
				MaximizeBox = false;
			}

			int menuHeight;
			if (Config.UseMenu)
			{
				menuStrip.Enabled = true;
				menuStrip.Visible = true;
				winHeight += menuStrip.Height;
				menuHeight = menuStrip.Height;
			}
			else
			{
				menuStrip.Enabled = false;
				menuStrip.Visible = false;
				menuHeight = 0;
			}
			//Windowの位置設定
			if (Config.SetWindowPos)
			{
				StartPosition = FormStartPosition.Manual;
				Location = new Point(Config.WindowPosX, Config.WindowPosY);
			}
			else if (!winMaximize)
			{
				StartPosition = FormStartPosition.Manual;
			}
			ClientSize = new Size(winWidth, winHeight);

			//EmuVerToolStripTextBox.Location = new Point(Config.WindowX - vScrollBar.Width - EmuVerToolStripTextBox.Width, 3);

			mainPicBox.Location = new Point(0, menuHeight);
			mainPicBox.Size = new Size(Config.WindowX, winHeight - menuHeight - Config.LineHeight);

			richTextBox1.Location = new Point(0, winHeight - Config.LineHeight);
			richTextBox1.Size = new Size(Config.WindowX, Config.LineHeight);
			vScrollBar.Location = new Point(winWidth - vScrollBar.Size.Width, menuHeight);
			vScrollBar.Size = new Size(vScrollBar.Size.Width, winHeight - menuHeight);

			int minimamY = 100;
			if (minimamY < menuHeight + Config.LineHeight * 2)
				minimamY = menuHeight + Config.LineHeight * 2;
			if (minimamY > Height)
				minimamY = Height;
			int maximamY = 2560;
			if (maximamY < Height)
				maximamY = Height;
			MinimumSize = new Size(Width, minimamY);
			MaximumSize = new Size(Width, maximamY);
			if (winMaximize)
				WindowState = FormWindowState.Maximized;
		}

		private void mainPicBox_MouseMove(object sender, MouseEventArgs e)
		{
			if (!Config.UseMouse)
				return;
			if (console == null)
				return;
			if (console.MoveMouse(e.Location))
				console.RefreshStrings(true);
		}
		#region EE_AnchorのCB機能移植
		private void mainPicBox_MouseClickCBCheck(object sender, MouseEventArgs e)
		{
			if (Config.CBUseClipboard)
			{
				if (e.Button == MouseButtons.Left) console.CBProc.Check(ClipboardProcessor.CBTriggers.LeftClick);
				else if (e.Button == MouseButtons.Middle) console.CBProc.Check(ClipboardProcessor.CBTriggers.MiddleClick);
			}
		}

		private void mainPicBox_MouseDoubleClickCBCheck(object sender, MouseEventArgs e)
		{
			if (Config.CBUseClipboard && e.Button == MouseButtons.Left) console.CBProc.Check(ClipboardProcessor.CBTriggers.DoubleLeftClick);
		}
		#endregion
		bool changeTextbyMouse;
		private void mainPicBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (!Config.UseMouse)
				return;
			if (console == null || console.IsInProcess)
				return;
			if (console.IsWaitingPrimitive)
			//			if (console.IsWaitingPrimitiveMouse)
			{
				console.MouseDown(e.Location, e.Button);
				#region EM_私家版_INPUT系機能拡張
				if (vScrollBar.Value == vScrollBar.Maximum && console.SelectingButton != null)
					GlobalStatic.Process.InputInteger(6, console.SelectingButton.GetMappedColor(e.X, e.Y));
				#endregion
				return;
			}
			bool isBacklog = vScrollBar.Value != vScrollBar.Maximum;
			string str = console.SelectedString;

			if (isBacklog)
				if ((e.Button == MouseButtons.Left) || (e.Button == MouseButtons.Right))
				{
					vScrollBar.Value = vScrollBar.Maximum;
					console.RefreshStrings(true);
				}
			if (console.IsWaitingEnterKey && str == null)
			{
				if (isBacklog)
					return;
				if (console.IsError)
				{
					if (e.Button == MouseButtons.Left)
					{
						PressEnterKey(false, true);
						return;
					}
				}

				if (e.Button == MouseButtons.Right)
					PressEnterKey(true, true);
				else if (e.Button == MouseButtons.Left)
					PressEnterKey(false, true);
				return;
			}
			#region EM_私家版_INPUT系機能拡張
			else if (console.IsWaintingInputWithMouse && !console.IsError && str != null)
			{
				if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
				{
					if (!isBacklog)
						GlobalStatic.Process.InputInteger(3, console.SelectingButton.GetMappedColor(e.X, e.Y));
					if (modifiersWhileWaintingInputWithMouse != null)
					{
						GlobalStatic.Process.InputInteger(2, (long)modifiersWhileWaintingInputWithMouse);
					}
					GlobalStatic.Process.InputString(1, str);
					if (e.Button == MouseButtons.Middle)
					{
						GlobalStatic.Process.InputInteger(1, 3);
						console.PressEnterKey(false, str, true);
					}
					else if (e.Button == MouseButtons.Right)
					{
						GlobalStatic.Process.InputInteger(1, 2);
						console.PressEnterKey(true, str, true);
					}
					else
					{
						GlobalStatic.Process.InputInteger(1, 1);
						console.PressEnterKey(false, str, true);
					}
					return;
				}
			}
			#endregion
			#region EE_INPUT第二引数修正
			if (console.IsWaintingInputWithMouse && !console.IsError && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle))
			{
				//念のため
				if (console.IsWaintingOnePhrase)
					last_inputed = "";
				richTextBox1.Text = string.Empty;

				if (str != null)
					GlobalStatic.VEvaluator.RESULTS_ARRAY[1] = str;
				if (e.Button == MouseButtons.Left)
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = 1;
				if (e.Button == MouseButtons.Right)
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = 2;
				if (e.Button == MouseButtons.Middle)
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = 3;
				long result2 = 0;
				if ((ModifierKeys & Keys.Shift) == Keys.Shift)
					result2 += (long)Math.Pow(2, 16);
				if ((ModifierKeys & Keys.Control) == Keys.Control)
					result2 += (long)Math.Pow(2, 17);
				if ((ModifierKeys & Keys.Alt) == Keys.Alt)
					result2 += (long)Math.Pow(2, 18);
				GlobalStatic.VEvaluator.RESULT_ARRAY[2] = result2;
				console.inputReq.Timelimit = 0;

				PressEnterKey(false, true);
				return;
			}
			#endregion
			//左が押されたなら選択。
			else if (str != null && ((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & MouseButtons.Middle) == MouseButtons.Middle))
			{
				changeTextbyMouse = console.IsWaintingOnePhrase;
				richTextBox1.Text = str;
				//念のため
				if (console.IsWaintingOnePhrase)
					last_inputed = "";
				//ミドルクリックならRESULT:1を1にする
				if ((e.Button & MouseButtons.Middle) == MouseButtons.Middle)
					GlobalStatic.VEvaluator.RESULT_ARRAY[1] = 3;
				//右が押しっぱなしならスキップ追加。
				if ((MouseButtons & MouseButtons.Right) == MouseButtons.Right)
					PressEnterKey(true, true);
				else
					PressEnterKey(false, true);
				return;
			}
		}

		private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			//上端でも下端でもないなら描画を控えめに。
			if (console == null)
				return;
			console.RefreshStrings((vScrollBar.Value == vScrollBar.Maximum) || (vScrollBar.Value == vScrollBar.Minimum));
		}

		public void PressEnterKey(bool mesSkip, bool inputsByMouse)
		{
			if (console == null || console.IsInProcess)
				return;
			//if (console.inProcess)
			//{
			//	richTextBox1.Text = "";
			//	return;
			//}
			string str = richTextBox1.Text;
			if (console.IsWaintingOnePhrase && last_inputed.Length > 0)
			{
				str = str.Remove(0, last_inputed.Length);
				last_inputed = "";
			}
			changeTextbyMouse = false;
			updateInputs(str);
			console.PressEnterKey(mesSkip, str, inputsByMouse);
		}

		readonly string[] prevInputs = new string[100];
		int selectedInputs = 100;
		int lastSelected = 100;
		void updateInputs(string cur)
		{
			if (string.IsNullOrEmpty(cur))
			{
				richTextBox1.Text = "";
				return;
			}
			if (selectedInputs == prevInputs.Length || cur != prevInputs[prevInputs.Length - 1])
			{
				for (int i = 0; i < prevInputs.Length - 1; i++)
				{
					prevInputs[i] = prevInputs[i + 1];
				}
				prevInputs[prevInputs.Length - 1] = cur;
				//1729a eramakerと同じ処理系に変更 1730a 再修正
				if (selectedInputs > 0 && selectedInputs != prevInputs.Length && cur == prevInputs[selectedInputs - 1])
					lastSelected = --selectedInputs;
				else
					lastSelected = 100;
			}
			else
			{
				lastSelected = selectedInputs;
			}
			richTextBox1.Text = "";
			selectedInputs = prevInputs.Length;
		}

		void movePrev(int move)
		{
			if (move == 0)
				return;
			//if((selectedInputs != prevInputs.Length) &&(prevInputs[selectedInputs] != richTextBox1.Text))
			//	selectedInputs =  prevInputs.Length;
			int next;
			if (lastSelected != prevInputs.Length && selectedInputs == prevInputs.Length)
			{
				if (move == -1)
					move = 0;
				next = lastSelected + move;
				lastSelected = prevInputs.Length;
			}
			else
				next = selectedInputs + move;
			if ((next < 0) || (next > prevInputs.Length))
				return;
			if (next == prevInputs.Length)
			{
				selectedInputs = next;
				richTextBox1.Text = "";
				return;
			}
			if (string.IsNullOrEmpty(prevInputs[next]))
				if (++next == prevInputs.Length)
					return;

			selectedInputs = next;
			richTextBox1.Text = prevInputs[next];
			richTextBox1.SelectionStart = 0;
			richTextBox1.SelectionLength = richTextBox1.Text.Length;
			return;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult result = MessageBox.Show(trmb.ExitAsk.Text, trmb.Exit.Text, MessageBoxButtons.OKCancel);
			if (result != DialogResult.OK)
				return;
			Close();

		}

		private void rebootToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult result = MessageBox.Show(trmb.RestartAsk.Text, trmb.Restart.Text, MessageBoxButtons.OKCancel);
			if (result != DialogResult.OK)
				return;
			Reboot();
		}

		//private void loadToolStripMenuItem_Click(object sender, EventArgs e)
		//{
		//    openFileDialog.InitialDirectory = StaticConfig.SavDir;
		//    DialogResult result = openFileDialog.ShowDialog();
		//    string filepath = openFileDialog.FileName;
		//    if (!File.Exists(filepath))
		//    {
		//        MessageBox.Show("ファイルがありません", "File Not Found");
		//        return;
		//    }
		//}

		readonly string[] _args = [];
		public void Reboot()
		{
			//新たにアプリケーションを起動する
			Process.Start(Application.ExecutablePath, _args);

			//現在のアプリケーションを終了する
			Application.ExitThread();
		}

		public void GotoTitle()
		{
			if (console == null)
				return;
			console.GotoTitle();
		}

		public async Task ReloadErb()
		{
			if (console == null)
				return;
			await console.ReloadErb();
		}

		private void mainPicBox_MouseLeave(object sender, EventArgs e)
		{
			if (Config.UseMouse)
				console.LeaveMouse();
		}

		private void コンフィグCToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowConfigDialog();
		}

		public void ShowConfigDialog()
		{
			string lang = Config.EmueraLang;
			ConfigDialog dialog = new();
			dialog.TranslateUI();
			dialog.SetupLang(Lang.GetLangList());
			dialog.StartPosition = FormStartPosition.CenterParent;
			dialog.SetConfig(this);
			dialog.ShowDialog();
			if (dialog.Result == ConfigDialogResult.SaveReboot)
			{
				Reboot();
			}
			if (Config.EmueraLang != lang)
			{
				Lang.ReloadLang();
				KeyMacro.ResetNames();
				TranslateUI();
				ResetCheckedLanguage();
			}
		}

		private void タイトルへ戻るTToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (console == null)
				return;
			if (console.IsInProcess)
			{
				MessageBox.Show(trmb.NotAvailableDuringScript.Text);
				return;
			}
			if (console.notToTitle)
			{
				if (console.byError)
					MessageBox.Show(trmb.ErrorInAnalysisMode.Text);
				else
					MessageBox.Show(trmb.CanNotReturnToTitle.Text);
				return;
			}
			DialogResult result = MessageBox.Show(trmb.ReturnToTitleAsk.Text, trmb.ReturnToTitle.Text, MessageBoxButtons.OKCancel);
			if (result != DialogResult.OK)
				return;
			GotoTitle();
		}

		private async void コードを読み直すcToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (console == null)
				return;
			if (console.IsInProcess)
			{
				MessageBox.Show(trmb.NotAvailableDuringScript.Text);
				return;
			}
			DialogResult result = MessageBox.Show(trmb.ReloadErbAsk.Text, trmb.ReloadErb.Text, MessageBoxButtons.OKCancel);
			if (result != DialogResult.OK)
				return;
			await ReloadErb();

		}

		private void mainPicBox_Paint(object sender, PaintEventArgs e)
		{
			if (console == null)
				return;
			console.OnPaint(e.Graphics);
		}

		private void ログを保存するSToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (console == null)
				return;
			#region eee_カレントディレクトリー
			//saveFileDialog.InitialDirectory = Program.ExeDir;
			saveFileDialog.InitialDirectory = Program.ExeDir;
			#endregion
			DateTime time = DateTime.Now;
			string fname = time.ToString("yyyyMMdd-HHmmss");
			fname += ".log";
			saveFileDialog.FileName = fname;
			DialogResult result = saveFileDialog.ShowDialog();
			if (result == DialogResult.OK)
			{
				#region EE_OUTPUTLOG
				// console.OutputLog(Path.GetFullPath(saveFileDialog.FileName));
				console.OutputSystemLog(Path.GetFullPath(saveFileDialog.FileName));
				#endregion

			}
		}

		private void ログをクリップボードにコピーToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				ClipBoardDialog dialog = new()
				{
					Text = Lang.UI.ClipBoardDialog.Text
				};
				dialog.Setup(console);
				dialog.ShowDialog();
			}
			catch (Exception)
			{
				MessageBox.Show(trmb.CanNotOpenClipboard.Text);
				return;
			}
		}

		private async void ファイルを読み直すFToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (console == null)
				return;
			if (console.IsInProcess)
			{
				MessageBox.Show(trmb.NotAvailableDuringScript.Text);
				return;
			}
			DialogResult result = openFileDialog.ShowDialog();
			List<string> filepath = [];
			if (result == DialogResult.OK)
			{
				foreach (string fname in openFileDialog.FileNames)
				{
					if (!File.Exists(fname))
					{
						MessageBox.Show(trmb.FileNotFound.Text, trmb.FileNotFound.Text);
						return;
					}
					if (!Path.GetExtension(fname).Equals(".ERB", StringComparison.OrdinalIgnoreCase))
					{
						MessageBox.Show(trmb.IsNotErb.Text, trmb.FileFormatError.Text);
						return;
					}
					if (fname.StartsWith(Program.ErbDir, StringComparison.OrdinalIgnoreCase))
						filepath.Add(Program.ErbDir + fname[Program.ErbDir.Length..]);
					else
						filepath.Add(fname);
				}
				await console.ReloadPartialErb(filepath);
			}
		}

		private void リソースフォルダを読み直すToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (console == null)
				return;
			if (console.IsInProcess)
			{
				MessageBox.Show(trmb.NotAvailableDuringScript.Text);
				return;
			}
			DialogResult result = MessageBox.Show(trmb.ReloadResourceAsk.Text, trmb.ReloadResource.Text, MessageBoxButtons.OKCancel);
			if (result != DialogResult.OK)
				return;
			console.ReloadResource();
		}

		private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (Config.UseKeyMacro)
				KeyMacro.SaveMacro();
			if (console != null)
			{
				//ほっとしても勝手に閉じるが、その場合はDebugDialogのClosingイベントが発生しない
				if (Program.DebugMode && (console.DebugDialog != null) && console.DebugDialog.Created)
					console.DebugDialog.Close();
				console.Dispose();
				console = null;
			}
		}

		private async void フォルダを読み直すFToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (console == null)
				return;
			if (console.IsInProcess)
			{
				MessageBox.Show(trmb.NotAvailableDuringScript.Text);
				return;
			}
			//List<KeyValuePair<string, string>> filepath = new List<KeyValuePair<string, string>>();
			if (folderSelectDialog.ShowDialog() == DialogResult.OK)
			{
				await console.ReloadFolder(folderSelectDialog.SelectedPath);
			}
		}

		void richTextBox1_MouseWheel(object? sender, MouseEventArgs e)
		{
			//if (!Config.UseMouse)
			//	return;
			if (!vScrollBar.Enabled)
				return;
			if (console == null)
				return;

			if (console.IsWaitingPrimitive)
			//			if (console.IsWaitingPrimitiveMouse)
			{
				console.MouseWheel(mainPicBox.PointToClient(MousePosition), e.Delta);
				return;
			}
			//e.Deltaには大きな値が入っているので符号のみ採用する
			int move = -Math.Sign(e.Delta) * vScrollBar.SmallChange * Config.ScrollHeight;
			#region EE_AnchorのCB機能移植
			//Clipboard scroll only when using ctrl
			if (Config.CBUseClipboard && ModifierKeys == Keys.Control)
			{
				if (move > 0) console.CBProc.ScrollDown(move);
				else if (move < 0) console.CBProc.ScrollUp(-move);
				return;
			}
			#endregion
			//スクロールが必要ないならリターンする
			if ((vScrollBar.Value == vScrollBar.Maximum && move > 0) || (vScrollBar.Value == vScrollBar.Minimum && move < 0))
				return;
			int value = vScrollBar.Value + move;
			if (value >= vScrollBar.Maximum)
				vScrollBar.Value = vScrollBar.Maximum;
			else if (value <= vScrollBar.Minimum)
				vScrollBar.Value = vScrollBar.Minimum;
			else
				vScrollBar.Value = value;
			bool force_refresh = (vScrollBar.Value == vScrollBar.Maximum) || (vScrollBar.Value == vScrollBar.Minimum);

			//ボタンとの関係をチェック
			if (Config.UseMouse)
				force_refresh = console.MoveMouse(mainPicBox.PointToClient(MousePosition)) || force_refresh;
			//上端でも下端でもなくボタン選択状態のアップデートも必要ないなら描画を控えめに。
			console.RefreshStrings(force_refresh);
		}

		private bool textBox_flag = true;
		private string last_inputed = "";

		public void update_lastinput()
		{
			richTextBox1.TextChanged -= new EventHandler(richTextBox1_TextChanged);
			richTextBox1.KeyDown -= new KeyEventHandler(richTextBox1_KeyDown);
			Application.DoEvents();
			richTextBox1.TextChanged += new EventHandler(richTextBox1_TextChanged);
			richTextBox1.KeyDown += new KeyEventHandler(richTextBox1_KeyDown);
			last_inputed = richTextBox1.Text;
		}

		public void clear_richText()
		{
			richTextBox1.Clear();
		}

		private void richTextBox1_TextChanged(object? sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;
			if (!textBox_flag)
				return;
			if (!console.IsWaintingOnePhrase && !console.IsWaitAnyKey)
				return;
			if (string.IsNullOrEmpty(richTextBox1.Text))
				return;
			if (changeTextbyMouse)
				return;
			//テキストの削除orテキストに変化がない場合は入力されたとみなさない
			if (richTextBox1.Text.Length <= last_inputed.Length)
			{
				last_inputed = richTextBox1.Text;
				return;
			}
			textBox_flag = false;
			if (console.IsWaitAnyKey)
			{
				richTextBox1.Clear();
				last_inputed = "";
			}
			//if (richTextBox1.Text.Length > 1)
			//    richTextBox1.Text = richTextBox1.Text.Remove(1);
			PressEnterKey(false, false);
			textBox_flag = true;
		}
		#region EM_私家版_INPUT系機能拡張
		Keys? modifiersWhileWaintingInputWithMouse;
		private void richTextBox1_ModifierRecorder_KeyUp(object sender, KeyEventArgs e)
		{
			if (console == null || !console.IsWaintingInputWithMouse)
				return;
			modifiersWhileWaintingInputWithMouse = null;
		}
		private void richTextBox1_ModifierRecorder_KeyDown(object sender, KeyEventArgs e)
		{
			if (console == null || !console.IsWaintingInputWithMouse)
				return;
			modifiersWhileWaintingInputWithMouse = e.Modifiers;
		}
		#endregion


		//HOTKEY STATE
		public HotkeyState hotkeyState = new();

		private void richTextBox1_KeyDown(object? sender, KeyEventArgs e)
		{
			if (console == null)
				return;
			//1823 INPUTMOUSEKEY Key入力全てを捕まえてERB側で処理する
			//if (console.IsWaitingPrimitiveKey)
			if (console.IsWaitingPrimitive)
			{
				e.SuppressKeyPress = true;
				console.PressPrimitiveKey(e.KeyCode, e.KeyData, e.Modifiers);
				return;
			}
			//HOTKEY STATE
			{
				//int res = hotkeyState.keyToNumberHardcoded(e);
				int res = hotkeyState.keyToNumberRunInterpreter(e);
				if (res != -1)
				{
					richTextBox1.Clear();
					richTextBox1.AppendText(res.ToString());
					e.SuppressKeyPress = true;
					if (!console.IsInProcess)
						PressEnterKey(false, false);
					richTextBox1.SelectAll();
					return;
				}
			}
			if ((int)e.KeyData == (int)Keys.PageUp || (int)e.KeyData == (int)Keys.PageDown)
			{
				e.SuppressKeyPress = true;
				int move = 10;
				if ((int)e.KeyData == (int)Keys.PageUp)
					move *= -1;
				//スクロールが必要ないならリターンする
				if ((vScrollBar.Value == vScrollBar.Maximum && move > 0) || (vScrollBar.Value == vScrollBar.Minimum && move < 0))
					return;
				int value = vScrollBar.Value + move;
				if (value >= vScrollBar.Maximum)
					vScrollBar.Value = vScrollBar.Maximum;
				else if (value <= vScrollBar.Minimum)
					vScrollBar.Value = vScrollBar.Minimum;
				else
					vScrollBar.Value = value;
				//上端でも下端でもないなら描画を控えめに。
				console.RefreshStrings((vScrollBar.Value == vScrollBar.Maximum) || (vScrollBar.Value == vScrollBar.Minimum));
				return;
			}
			else if (vScrollBar.Value != vScrollBar.Maximum)
			{
				vScrollBar.Value = vScrollBar.Maximum;
				console.RefreshStrings(true);
			}
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				if (!console.IsInProcess)
					PressEnterKey(false, false);
				return;
			}
			if (e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				console.KillMacro = true;
				if (!console.IsInProcess)
					PressEnterKey(true, false);
				return;
			}
			if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Home || e.KeyCode == Keys.Back)
			{
				if ((richTextBox1.SelectionStart == 0 && richTextBox1.SelectedText.Length == 0) || richTextBox1.Text.Length == 0)
				{
					e.SuppressKeyPress = true;
					return;
				}
			}
			if (e.KeyCode == Keys.Right || e.KeyCode == Keys.End)
			{
				if (richTextBox1.SelectionStart == richTextBox1.Text.Length || richTextBox1.Text.Length == 0)
				{
					e.SuppressKeyPress = true;
					return;
				}
			}
			if (e.KeyCode == Keys.Up)
			{
				e.SuppressKeyPress = true;
				if (console.IsInProcess)
					return;
				movePrev(-1);
				return;
			}
			if (e.KeyCode == Keys.Down)
			{
				e.SuppressKeyPress = true;
				if (console.IsInProcess)
					return;
				movePrev(1);
				return;
			}
			if (e.KeyCode == Keys.Insert)
			{
				e.SuppressKeyPress = true;
				return;
			}
		}

		private void デバッグウインドウを開くToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!Program.DebugMode)
				return;
			console.OpenDebugDialog();
		}

		private void デバッグ情報の更新ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!Program.DebugMode)
				return;
			if ((console.DebugDialog != null) && console.DebugDialog.Created)
				console.DebugDialog.UpdateData();
		}

		private void AutoVerbMenu_Opened(object sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess)
			{
				切り取り.Enabled = false;
				コピー.Enabled = false;
				貼り付け.Enabled = false;
				実行.Enabled = false;
				削除.Enabled = false;
				マクロToolStripMenuItem.Enabled = false;
				for (int i = 0; i < macroMenuItems.Length; i++)
					macroMenuItems[i].Enabled = false;
				return;
			}
			実行.Enabled = true;
			if (Config.UseKeyMacro)
			{
				マクロToolStripMenuItem.Enabled = true;

				for (int i = 0; i < macroMenuItems.Length; i++)
					macroMenuItems[i].Enabled = KeyMacro.GetMacro(i, macroGroup).Length > 0;
			}
			else
			{
				マクロToolStripMenuItem.Enabled = false;
				for (int i = 0; i < macroMenuItems.Length; i++)
					macroMenuItems[i].Enabled = false;
			}
			if (richTextBox1.SelectedText.Length > 0)
			{
				切り取り.Enabled = true;
				コピー.Enabled = true;
				削除.Enabled = true;
			}
			else
			{
				切り取り.Enabled = false;
				コピー.Enabled = false;
				削除.Enabled = false;
			}
			if (Clipboard.ContainsText())
				貼り付け.Enabled = true;
			else
				貼り付け.Enabled = false;

		}

		private void 切り取り_Click(object sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess || !切り取り.Enabled)
				return;
			if (richTextBox1.SelectedText.Length > 0)
				richTextBox1.Cut();
		}

		private void コピー_Click(object sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess || !コピー.Enabled)
				return;
			else if (richTextBox1.SelectedText.Length > 0)
				richTextBox1.Copy();
		}

		private void 貼り付け_Click(object sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess || !貼り付け.Enabled)
				return;
			if (Clipboard.GetDataObject() != null && Clipboard.ContainsText())
			{
				if (Clipboard.GetDataObject()!.GetDataPresent(DataFormats.Text))
					//Clipboard.SetText(Clipboard.GetText(TextDataFormat.UnicodeText));
					richTextBox1.Paste(DataFormats.GetFormat(DataFormats.UnicodeText));
				//richTextBox1.Paste();
				//if (richTextBox1.SelectedText.Length > 0)
				//    richTextBox1.SelectedText = "";
				//richTextBox1.AppendText(Clipboard.GetText());
			}
		}

		private void 削除_Click(object sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess || !削除.Enabled)
				return;
			if (richTextBox1.SelectedText.Length > 0)
				richTextBox1.SelectedText = "";
		}

		private void 実行_Click(object sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess || !実行.Enabled)
				return;
			PressEnterKey(false, false);
		}

		int macroGroup;
		private void マクロToolStripMenuItem_Click(object? sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess)
				return;
			if (!Config.UseKeyMacro)
				return;
			if (sender is ToolStripMenuItem item)
			{
				int fkeynum = (int)item.ShortcutKeys - (int)Keys.F1;
				string macro = KeyMacro.GetMacro(fkeynum, macroGroup);
				if (macro.Length > 0)
				{
					richTextBox1.Text = macro;
					richTextBox1.SelectionStart = richTextBox1.Text.Length;
				}
			}
		}

		private void グループToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if ((console == null) || console.IsInProcess)
				return;
			if (!Config.UseKeyMacro)
				return;
			if (sender is ToolStripMenuItem item)
			{
				if (item.Tag is string tag)
				{
					setNewMacroGroup(int.Parse(tag));//とても無駄なキャスト&Parse
				}
				else
				{
					throw new Exception();
				}
			}
		}

		private void timerKeyMacroChanged_Tick(object sender, EventArgs e)
		{
			labelTimerCount++;
			if (labelTimerCount > 10)
			{
				timerKeyMacroChanged.Stop();
				timerKeyMacroChanged.Enabled = false;
				labelMacroGroupChanged.Visible = false;
			}
		}

		int labelTimerCount;
		private void setNewMacroGroup(int group)
		{
			labelTimerCount = 0;
			macroGroup = group;
			labelMacroGroupChanged.Text = KeyMacro.GetGroupName(group);
			timerKeyMacroChanged.Interval = 200;
			timerKeyMacroChanged.Enabled = true;
			timerKeyMacroChanged.Start();
			labelMacroGroupChanged.Location = new Point(4, richTextBox1.Location.Y - labelMacroGroupChanged.Height - 4);
			labelMacroGroupChanged.Visible = true;
		}

		/* EMEEではツールチップ拡張が実装されてるからいらないと思う……
		Font? _tooltipFont;

		private void toolTipButton_Draw(object sender, DrawToolTipEventArgs e)
		{
			e.DrawBackground();
			e.DrawBorder();

			TextRenderer.DrawText(e.Graphics, e.ToolTipText, _tooltipFont, new Point(0, 0), Color.Black);
		}

		private void toolTipButton_Popup(object sender, PopupEventArgs e)
		{
			_tooltipFont ??= new Font(Config.DefaultFont.FontFamily, Config.DefaultFont.Size * 0.6f);

			var toolTip = (ToolTip)sender;
			e.ToolTipSize = TextRenderer.MeasureText(toolTip.GetToolTip(e.AssociatedControl), _tooltipFont);
		}
		*/

		bool _isWidthLocked = true;
		private void ウィンドウ幅のロック変更ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_isWidthLocked)
			{
				_isWidthLocked = false;
				MinimumSize = new Size(0, 0);
				MaximumSize = new Size(int.MaxValue,
										int.MaxValue);
			}
			else
			{
				_isWidthLocked = true;

				if (Config.SizableWindow)
				{
					MinimumSize = Size with
					{
						Height = 0
					};
					MaximumSize = Size with
					{
						Height = int.MaxValue
					};
				}
				else
				{
					MinimumSize = Size;
					MaximumSize = Size;
				}

				ConfigData.Instance.GetConfigItem(ConfigCode.WindowX).SetValue(mainPicBox.Width);
				ConfigData.Instance.GetConfigItem(ConfigCode.WindowY).SetValue(mainPicBox.Height + Config.LineHeight);
				ConfigData.Instance.SaveConfig();
			}
		}
		private void デバッグモードで再起動ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//新たにアプリケーションを起動する
			Process.Start(Application.ExecutablePath, [.. _args, "-Debug"]);

			//現在のアプリケーションを終了する
			Application.ExitThread();

		}
		
		private void クリップボードにコピーToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			if (クリップボードにコピーToolStripMenuItem.Checked)
				GlobalStatic.Console.CBProc.Init();
			else
				GlobalStatic.Console.CBProc.Reset();
			ConfigData.Instance.GetConfigItem(ConfigCode.CBUseClipboard).SetValue(クリップボードにコピーToolStripMenuItem.Checked);
			Config.SetConfig(ConfigData.Instance);
			ConfigData.Instance.SaveConfig();
		}
		#region EM_私家版_多言語化改造
		private void SetLanguageOptions()
		{
			if(Config.EmueraLang == Lang.DefaultLanguage)
				JapaneseToolStripMenuItem.Checked = true;
			foreach (var lang in Lang.GetLangList())
			{
				var language = new ToolStripMenuItem();
				language.Text = lang;
				language.Checked = Config.EmueraLang == lang;
				language.Click += (_, _) =>
				{
					if (Config.EmueraLang == lang)
						return;
					Config.SetLanguageSetting(ConfigData.Instance, lang);
					Lang.ReloadLang();
					ResetCheckedLanguage();
					TranslateUI();
				};
				LanguageToolStripMenuItem.DropDownItems.Add(language);
			}
		}

		public void ResetCheckedLanguage()
		{
			foreach (ToolStripMenuItem item in LanguageToolStripMenuItem.DropDownItems)
			{
				item.Checked = Config.EmueraLang == item.Text;
			}
		}
		private void JapaneseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Config.EmueraLang == Lang.DefaultLanguage)
				return;
			Config.SetLanguageSetting(ConfigData.Instance, Lang.DefaultLanguage);
			Lang.ReloadLang();
			ResetCheckedLanguage();
			TranslateUI();
		}
		#endregion
	}
}
