//統合_ゲームパッド対応: XInput を直接叩いてパッドの状態を読む。
//XInput1_4.dll は Windows 8 以降に標準で入っているので追加依存は無い。
using System;
using System.Runtime.InteropServices;

namespace MinorShift.Emuera.Runtime.Utils;

/// <summary>押されたと判定された操作</summary>
internal enum GamePadAction
{
	None,
	Up,
	Down,
	Left,
	Right,
	Decide,
	Cancel,
	ScrollUp,
	ScrollDown,
}

internal static class GamePad
{
	//XInput のボタンビット
	public const int DPAD_UP = 0x0001;
	public const int DPAD_DOWN = 0x0002;
	public const int DPAD_LEFT = 0x0004;
	public const int DPAD_RIGHT = 0x0008;
	public const int START = 0x0010;
	public const int BACK = 0x0020;
	public const int LEFT_THUMB = 0x0040;
	public const int RIGHT_THUMB = 0x0080;
	public const int LEFT_SHOULDER = 0x0100;
	public const int RIGHT_SHOULDER = 0x0200;
	public const int A = 0x1000;
	public const int B = 0x2000;
	public const int X = 0x4000;
	public const int Y = 0x8000;

	//左スティックを方向キー扱いするしきい値。XInput の推奨デッドゾーンより大きめにして誤爆を防ぐ
	private const short ThumbThreshold = 16000;

	[StructLayout(LayoutKind.Sequential)]
	private struct XInputGamepad
	{
		public ushort wButtons;
		public byte bLeftTrigger;
		public byte bRightTrigger;
		public short sThumbLX;
		public short sThumbLY;
		public short sThumbRX;
		public short sThumbRY;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct XInputState
	{
		public uint dwPacketNumber;
		public XInputGamepad Gamepad;
	}

	[DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
	private static extern uint XInputGetState14(uint dwUserIndex, ref XInputState pState);

	[DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
	private static extern uint XInputGetState91(uint dwUserIndex, ref XInputState pState);

	private const uint ERROR_SUCCESS = 0;

	//xinput1_4 が無い環境では 9_1_0 に落とす。どちらも無ければ以後あきらめる
	private static int dllChoice = -1;

	private static uint GetState(uint index, ref XInputState state)
	{
		if (dllChoice == 0)
			return XInputGetState14(index, ref state);
		if (dllChoice == 1)
			return XInputGetState91(index, ref state);
		if (dllChoice == 2)
			return uint.MaxValue;

		try
		{
			uint ret = XInputGetState14(index, ref state);
			dllChoice = 0;
			return ret;
		}
		catch (DllNotFoundException) { }
		catch (EntryPointNotFoundException) { }
		try
		{
			uint ret = XInputGetState91(index, ref state);
			dllChoice = 1;
			return ret;
		}
		catch (DllNotFoundException) { }
		catch (EntryPointNotFoundException) { }

		dllChoice = 2;
		return uint.MaxValue;
	}

	//前回の押下状態。押しっぱなしを毎フレーム拾わないための立ち上がり検出用
	private static int prevButtons;
	//押しっぱなしのときのリピート開始/間隔（ミリ秒）
	private const int RepeatDelay = 400;
	private const int RepeatInterval = 80;
	private static int repeatButton;
	private static long repeatNextTick;

	/// <summary>
	/// 現在の入力を「押された瞬間」のビットとして返す。押しっぱなしの方向キーはリピートする。
	/// 接続されていなければ 0。
	/// </summary>
	public static int PollPressedButtons(long nowMs)
	{
		XInputState state = default;
		int buttons = 0;
		//4台まで見て、最初に見つかったものを使う
		for (uint i = 0; i < 4; i++)
		{
			if (GetState(i, ref state) != ERROR_SUCCESS)
				continue;
			buttons = state.Gamepad.wButtons;
			//左スティックを方向キーに合成する
			if (state.Gamepad.sThumbLY > ThumbThreshold) buttons |= DPAD_UP;
			if (state.Gamepad.sThumbLY < -ThumbThreshold) buttons |= DPAD_DOWN;
			if (state.Gamepad.sThumbLX < -ThumbThreshold) buttons |= DPAD_LEFT;
			if (state.Gamepad.sThumbLX > ThumbThreshold) buttons |= DPAD_RIGHT;
			break;
		}

		int pressed = buttons & ~prevButtons;
		prevButtons = buttons;

		//方向キーだけリピートさせる
		const int repeatable = DPAD_UP | DPAD_DOWN | DPAD_LEFT | DPAD_RIGHT;
		int held = buttons & repeatable;
		if (pressed != 0 && (pressed & repeatable) != 0)
		{
			repeatButton = pressed & repeatable;
			repeatNextTick = nowMs + RepeatDelay;
		}
		else if (held != 0 && held == repeatButton && nowMs >= repeatNextTick)
		{
			pressed |= repeatButton;
			repeatNextTick = nowMs + RepeatInterval;
		}
		else if (held == 0)
		{
			repeatButton = 0;
		}

		return pressed;
	}

	public static void Reset()
	{
		prevButtons = 0;
		repeatButton = 0;
		repeatNextTick = 0;
	}
}
