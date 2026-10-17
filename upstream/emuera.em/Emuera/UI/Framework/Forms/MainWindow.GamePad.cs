//統合_ゲームパッド対応: パッドを一定間隔で読んで、選択カーソルの移動と決定に割り当てる。
//決定はマウスクリックと同じ経路（PressEnterKey の inputsByMouse）に流すので、
//ERB 側から見た挙動はボタンをクリックしたときと同じになる。
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Config.JSON;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MinorShift.Emuera.Forms
{
	internal sealed partial class MainWindow
	{
		private Timer gamePadTimer;
		private readonly Stopwatch gamePadClock = new();

		private void SetupGamePad()
		{
			if (!JSONConfig.Data.GamePad.Enabled)
				return;
			gamePadClock.Start();
			gamePadTimer = new Timer { Interval = 16 };
			gamePadTimer.Tick += GamePadTimer_Tick;
			gamePadTimer.Start();
		}

		private void GamePadTimer_Tick(object sender, EventArgs e)
		{
			if (console == null || console.IsInProcess)
				return;
			//他のウィンドウを操作している間は反応させない
			if (!ContainsFocus)
			{
				GamePad.Reset();
				return;
			}

			int pressed = GamePad.PollPressedButtons(gamePadClock.ElapsedMilliseconds);
			if (pressed == 0)
				return;

			GamePadConfig cfg = JSONConfig.Data.GamePad;

			if ((pressed & cfg.ScrollUp) != 0) { ScrollLogByGamePad(-10); return; }
			if ((pressed & cfg.ScrollDown) != 0) { ScrollLogByGamePad(10); return; }

			//INPUTMOUSEKEY などで生の入力を待っている画面では、選択カーソルではなく
			//キーボード・マウスと同じ経路に流す。
			//決定/キャンセルはクリックとして送る（キーだけでは進まないゲームがあるため。
			//ゲーム側はクリック位置やボタン値を見ることがあるので、カーソル位置をそのまま使う）。
			if (console.IsWaitingPrimitive)
			{
				if ((pressed & cfg.Decide) != 0) { ClickByGamePad(MouseButtons.Left); return; }
				if ((pressed & cfg.RightClick) != 0) { ClickByGamePad(MouseButtons.Right); return; }
				if ((pressed & cfg.Cancel) != 0) { console.PressPrimitiveKey(Keys.Escape, Keys.Escape, Keys.None); return; }
				Keys key = PrimitiveKeyFromGamePad(pressed, cfg);
				if (key != Keys.None)
					console.PressPrimitiveKey(key, key, Keys.None);
				return;
			}

			if ((pressed & cfg.Cancel) != 0)
			{
				console.ClearSelectingButton();
				richTextBox1.Clear();
				return;
			}

			if ((pressed & cfg.Decide) != 0)
			{
				DecideByGamePad(false);
				return;
			}

			if ((pressed & cfg.RightClick) != 0)
			{
				DecideByGamePad(true);
				return;
			}

			//左右は行内の隣へ、上下は上/下の行の近い位置へ
			if ((pressed & cfg.Left) != 0) { console.MoveSelectingButton(ButtonMoveDirection.Left); return; }
			if ((pressed & cfg.Right) != 0) { console.MoveSelectingButton(ButtonMoveDirection.Right); return; }
			if ((pressed & cfg.Up) != 0) { console.MoveSelectingButton(ButtonMoveDirection.Up); return; }
			if ((pressed & cfg.Down) != 0) { console.MoveSelectingButton(ButtonMoveDirection.Down); return; }
		}

		/// <summary>
		/// 生入力待ちの画面で、マウスをクリックしたのと同じ入力を送る。
		/// 位置はカーソルの現在地。画面外にいるときは画面中央にしておく
		/// （その場でクリックしたのと同じ挙動にするため）。
		/// </summary>
		private void ClickByGamePad(MouseButtons button)
		{
			Point point = mainPicBox.PointToClient(Control.MousePosition);
			if (!mainPicBox.ClientRectangle.Contains(point))
				point = new Point(mainPicBox.Width / 2, mainPicBox.Height / 2);
			console.MouseDown(point, button);
		}

		/// <summary>生入力待ちの画面向けに、十字キーをキーコードへ読み替える</summary>
		private static Keys PrimitiveKeyFromGamePad(int pressed, GamePadConfig cfg)
		{
			if ((pressed & cfg.Left) != 0) return Keys.Left;
			if ((pressed & cfg.Right) != 0) return Keys.Right;
			if ((pressed & cfg.Up) != 0) return Keys.Up;
			if ((pressed & cfg.Down) != 0) return Keys.Down;
			return Keys.None;
		}

		/// <summary>
		/// 決定。マウスクリックと同じ経路に流す。
		/// 選択中ボタンがあればその入力値を、無ければ単なる改行待ちの解除として扱う。
		/// rightClick なら右クリック相当（era系では本文を飛ばして進める挙動になる）。
		/// </summary>
		private void DecideByGamePad(bool rightClick)
		{
			//履歴を見ている最中は最新に戻すだけ
			if (vScrollBar.Value != vScrollBar.Maximum)
			{
				vScrollBar.Value = vScrollBar.Maximum;
				console.RefreshStrings(true);
				return;
			}

			string str = console.SelectedString;
			if (console.IsWaitingEnterKey && str == null)
			{
				PressEnterKey(rightClick, true);
				return;
			}
			if (console.IsWaintingInputWithMouse && !console.IsError && str != null)
			{
				//RESULT:1 に押したボタン(左=1/右=2)が入る。マウスと同じ約束
				GlobalStatic.Process.InputInteger(1, rightClick ? 2 : 1);
				GlobalStatic.Process.InputString(1, str);
				console.PressEnterKey(rightClick, str, true);
				return;
			}
			if (str != null)
			{
				console.PressEnterKey(rightClick, str, true);
				return;
			}
			PressEnterKey(rightClick, false);
		}

		private void ScrollLogByGamePad(int move)
		{
			if ((vScrollBar.Value == vScrollBar.Maximum && move > 0) || (vScrollBar.Value == vScrollBar.Minimum && move < 0))
				return;
			int value = vScrollBar.Value + move;
			if (value >= vScrollBar.Maximum) vScrollBar.Value = vScrollBar.Maximum;
			else if (value <= vScrollBar.Minimum) vScrollBar.Value = vScrollBar.Minimum;
			else vScrollBar.Value = value;
			console.RefreshStrings((vScrollBar.Value == vScrollBar.Maximum) || (vScrollBar.Value == vScrollBar.Minimum));
		}
	}
}
