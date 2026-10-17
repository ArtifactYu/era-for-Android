//統合_ゲームパッド対応: パッドで画面上のボタンを選ぶための処理。
//マウスのホバー選択と同じ selectingButton を動かすので、決定はクリックと同じ経路に乗る。
using MinorShift.Emuera.Runtime;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using MinorShift.Emuera.UI.Game;
using System.Collections.Generic;

namespace MinorShift.Emuera.GameView;

#region 統合_ゲームパッド対応
internal enum ButtonMoveDirection
{
	Left,
	Right,
	Up,
	Down,
}
#endregion

internal sealed partial class EmueraConsole
{
	/// <summary>選べるボタンと、その画面上のおおよその位置</summary>
	private readonly struct SelectableButton(ConsoleButtonString button, int x, int y)
	{
		public readonly ConsoleButtonString Button = button;
		/// <summary>ボタンの中心X。左右移動と、上下移動で「近い列」を選ぶのに使う</summary>
		public readonly int X = x;
		/// <summary>行のY。同じ値なら同じ行とみなす</summary>
		public readonly int Y = y;
	}

	/// <summary>
	/// 今選べるボタンを画面の並び順（上の行から、行内は左から）で集める。
	/// 選べる条件はマウスの当たり判定と同じ。
	/// div の中のボタンも対象にする（マウスは TestChildHitbox で拾えるため）。
	/// </summary>
	private List<SelectableButton> CollectSelectableButtons()
	{
		List<SelectableButton> ret = [];
		if (state != ConsoleState.WaitInput || !inputReq.NeedValue)
			return ret;

		bool needInteger = inputReq.InputType == InputType.IntValue || inputReq.InputType == InputType.IntButton;
		int y = 0;
		foreach (ConsoleDisplayLine line in displayLineList)
		{
			collectFromLine(line, y, needInteger, ret);
			y += Config.LineHeight;
		}
		//行をまたいだ div があるので、最後に画面の並び順へ整える
		ret.Sort((a, b) => a.Y != b.Y ? a.Y - b.Y : a.X - b.X);
		return ret;

		void collectFromLine(ConsoleDisplayLine line, int lineY, bool needInt, List<SelectableButton> list)
		{
			foreach (ConsoleButtonString button in line.Buttons)
			{
				if (button == null)
					continue;
				if (button.IsButton && button.Generation == lastButtonGeneration && (!needInt || button.IsInteger))
					list.Add(new SelectableButton(button, button.PointX + button.Width / 2, lineY));
				if (button.StrArray == null)
					continue;
				//div の中身は別の行の並びを持っている
				foreach (AConsoleDisplayNode part in button.StrArray)
				{
					if (part is not ConsoleDivPart div)
						continue;
					int childY = lineY + div.Top;
					foreach (ConsoleDisplayLine child in div.Children)
					{
						collectFromLine(child, childY, needInt, list);
						childY += Config.LineHeight;
					}
				}
			}
		}
	}

	/// <summary>
	/// 選択カーソルを動かす。動かせたら true。
	/// 左右は画面の並び順で1つ隣へ（行末まで行けば次の行へ）。
	/// 上下は「一番近い上/下の行」の中から、今の位置に横が一番近いボタンへ。
	/// </summary>
	public bool MoveSelectingButton(ButtonMoveDirection dir)
	{
		List<SelectableButton> buttons = CollectSelectableButtons();
		if (buttons.Count == 0)
			return false;

		int index = -1;
		if (selectingButton != null)
			index = buttons.FindIndex(b => b.Button == selectingButton);

		int next;
		if (index < 0)
			//未選択なら端から入る
			next = dir is ButtonMoveDirection.Right or ButtonMoveDirection.Down ? 0 : buttons.Count - 1;
		else if (dir is ButtonMoveDirection.Left or ButtonMoveDirection.Right)
		{
			next = index + (dir == ButtonMoveDirection.Right ? 1 : -1);
			//端で止めず反対側へ回す
			if (next < 0) next = buttons.Count - 1;
			else if (next >= buttons.Count) next = 0;
		}
		else
			next = findVertical(buttons, index, dir == ButtonMoveDirection.Down);

		selectingButton = buttons[next].Button;
		//選んだボタンが履歴に隠れていたら最新まで戻す
		if (window.ScrollBar.Value != window.ScrollBar.Maximum)
			window.ScrollBar.Value = window.ScrollBar.Maximum;
		RefreshStrings(true);
		return true;

		static int findVertical(List<SelectableButton> list, int from, bool down)
		{
			SelectableButton cur = list[from];

			//まず移動先の行（今の行から一番近い上/下の行）を決める
			int targetY = 0;
			bool found = false;
			foreach (SelectableButton b in list)
			{
				if (down ? b.Y <= cur.Y : b.Y >= cur.Y)
					continue;
				if (!found || (down ? b.Y < targetY : b.Y > targetY))
				{
					targetY = b.Y;
					found = true;
				}
			}
			//上下の端まで来たら反対側の端の行へ回す
			if (!found)
			{
				targetY = cur.Y;
				foreach (SelectableButton b in list)
					if (down ? b.Y < targetY : b.Y > targetY)
						targetY = b.Y;
				if (targetY == cur.Y)
					return from;
			}

			//その行の中で横位置が一番近いものを選ぶ
			int best = from;
			int bestDistance = int.MaxValue;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Y != targetY)
					continue;
				int distance = System.Math.Abs(list[i].X - cur.X);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i;
				}
			}
			return best;
		}
	}

	/// <summary>選択カーソルを外す</summary>
	public void ClearSelectingButton()
	{
		if (selectingButton == null)
			return;
		selectingButton = null;
		RefreshStrings(true);
	}

	/// <summary>パッドで操作できる状態か</summary>
	public bool CanUseGamePadSelection => state == ConsoleState.WaitInput && inputReq != null && inputReq.NeedValue;
}
